//
// ProjectorForLWRP.cs
//
// Projector For LWRP
//
// Copyright (c) 2019 NYAHOON GAMES PTE. LTD.
//

using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace ProjectorForLWRP
{
	[ExecuteInEditMode]
	[RequireComponent(typeof(Projector))]
	public class ProjectorForLWRP : MonoBehaviour
	{
		// serialize field
		[Header("List of cameras in which the projector is rendered")]
		[SerializeField]
		private Camera[] m_cameras = null;

		[Header("Receiver Object Filter")]
		[SerializeField]
		private string[] m_shaderTagList = new string[] { "LightweightForward", "SRPDefaultUnlit" };
		[SerializeField]
		private int m_renderQueueLowerBound = RenderQueueRange.opaque.lowerBound;
		[SerializeField]
		private int m_renderQueueUpperBound = RenderQueueRange.opaque.upperBound;

		[Header("Projector Rendering")]
		[SerializeField]
		private UnityEngine.Rendering.LWRP.RenderPassEvent m_renderPassEvent = UnityEngine.Rendering.LWRP.RenderPassEvent.AfterRenderingOpaques;
		[SerializeField]
		private PerObjectData m_perObjectData = PerObjectData.None;
		[SerializeField]
		[HideInInspector]
		private Material m_stencilPass = null;
		[SerializeField]
		[HideInInspector]
		private int m_stencilRef = 1;
		[SerializeField]
		[HideInInspector]
		private int m_stencilMask = 1;

		// public properties
		public Camera[] cameras {  get { return m_cameras; } }
		public int renderQueueLowerBound
		{
			get { return m_renderQueueLowerBound; }
			set { m_renderQueueLowerBound = value; }
		}
		public int renderQueueUpperBound
		{
			get { return m_renderQueueUpperBound; }
			set { m_renderQueueUpperBound = value; }
		}
		public UnityEngine.Rendering.LWRP.RenderPassEvent renderPassEvent
		{
			get { return m_renderPassEvent; }
			set { m_renderPassEvent = value; }
		}
		PerObjectData perObjectData
		{
			get { return m_perObjectData; }
			set { m_perObjectData = value; }
		}
		public bool useStencilTest
		{
			get { return m_stencilPass != null; }
		}
		public int stencilRef
		{
			get { return m_stencilRef; }
			set { m_stencilRef = value; }
		}
		public int stencilMask
		{
			get { return m_stencilMask; }
			set { m_stencilMask = value; }
		}
		public Material stencilPassMaterial
		{
			get { return m_stencilPass; }
			set { m_stencilPass = value; }
		}

		private Vector3[] m_frustumVertices;
		private Mesh m_meshFrustum;
		private Projector m_projector;
		private const string PROJECTOR_SHADER_KEYWORD = "FSR_PROJECTOR_FOR_LWRP";
		static readonly int[] s_frustumTriangles = {
			0, 1, 2, 2, 1, 3, // near plane
 			0, 4, 1, 1, 4, 5, // left
 			1, 5, 3, 3, 5, 7, // top
			3, 7, 2, 2, 7, 6, // right
			2, 6, 0, 0, 6, 4, // bottom
			6, 7, 4, 4, 7, 5  // far plane
		};
		// if you want to change projector frustum at runtime (I mean, after Awake), please call this function manually
		public void UpdateFrustum()
		{
			float w, h;
			if (m_frustumVertices == null)
			{
				m_frustumVertices = new Vector3[8];
			}
			if (m_projector.orthographic)
			{
				h = m_projector.orthographicSize;
				w = h * m_projector.aspectRatio;
				m_frustumVertices[0].x = m_frustumVertices[1].x = m_frustumVertices[4].x = m_frustumVertices[5].x = -w;
				m_frustumVertices[2].x = m_frustumVertices[3].x = m_frustumVertices[6].x = m_frustumVertices[7].x = w;
				m_frustumVertices[0].y = m_frustumVertices[2].y = m_frustumVertices[4].y = m_frustumVertices[6].y = -h;
				m_frustumVertices[1].y = m_frustumVertices[3].y = m_frustumVertices[5].y = m_frustumVertices[7].y = h;
			}
			else
			{
				float tan = Mathf.Tan(0.5f * Mathf.Deg2Rad * m_projector.fieldOfView);
				h = tan * m_projector.farClipPlane;
				w = h * m_projector.aspectRatio;
				float nearH = tan * m_projector.nearClipPlane;
				float nearW = nearH * m_projector.aspectRatio;
				m_frustumVertices[0].x = m_frustumVertices[1].x = -nearW;
				m_frustumVertices[4].x = m_frustumVertices[5].x = -w;
				m_frustumVertices[2].x = m_frustumVertices[3].x = nearW;
				m_frustumVertices[6].x = m_frustumVertices[7].x = w;
				m_frustumVertices[0].y = m_frustumVertices[2].y = -nearH;
				m_frustumVertices[4].y = m_frustumVertices[6].y = -h;
				m_frustumVertices[1].y = m_frustumVertices[3].y = nearH;
				m_frustumVertices[5].y = m_frustumVertices[7].y = h;
			}
			m_frustumVertices[0].z = m_frustumVertices[1].z = m_frustumVertices[2].z = m_frustumVertices[3].z = m_projector.nearClipPlane;
			m_frustumVertices[4].z = m_frustumVertices[5].z = m_frustumVertices[6].z = m_frustumVertices [7].z = m_projector.farClipPlane;
			m_meshFrustum.vertices = m_frustumVertices;
			m_meshFrustum.triangles = s_frustumTriangles;
		}

		private ShaderTagId[] m_shaderTagIdList;
		public void UpdateShaderTagIdList()
		{
			if (m_shaderTagList == null || m_shaderTagList.Length == 0)
			{
				if (m_shaderTagIdList == null || m_shaderTagIdList.Length != 1)
				{
					m_shaderTagIdList = new ShaderTagId[1];
				}
				m_shaderTagIdList[0] = ShaderTagId.none;
			}
			else
			{
				if (m_shaderTagIdList == null || m_shaderTagIdList.Length != m_shaderTagList.Length)
				{
					m_shaderTagIdList = new ShaderTagId[m_shaderTagList.Length];
				}
				for (int i = 0; i < m_shaderTagList.Length; ++i)
				{
					m_shaderTagIdList[i] = new ShaderTagId(m_shaderTagList[i]);
				}
			}
		}
		private static int s_shaderPropIdStencilRef = -1;
		private static int s_shaderPropIdStencilMask = -1;
		private static int s_shaderPropIdFsrWorldToProjector = -1;
		private static int s_shaderPropIdFsrWorldProjectDir = -1;
		void OnEnable()
		{
			if (s_shaderPropIdStencilRef == s_shaderPropIdStencilMask)
			{
				s_shaderPropIdStencilRef = Shader.PropertyToID("P4LWRP_StencilRef");
				s_shaderPropIdStencilMask = Shader.PropertyToID("P4LWRP_StencilMask");
				s_shaderPropIdFsrWorldToProjector = Shader.PropertyToID("_FSRWorldToProjector");
				s_shaderPropIdFsrWorldProjectDir = Shader.PropertyToID("_FSRWorldProjectDir");
			}
			m_projector = GetComponent<Projector>();
			if (m_projector == null)
			{
				m_projector = gameObject.AddComponent<Projector>();
			}
			if (m_meshFrustum == null)
			{
				m_meshFrustum = new Mesh();
				m_meshFrustum.hideFlags = HideFlags.HideAndDontSave;
			}
			UpdateFrustum();
			if (m_shaderTagIdList == null)
			{
				UpdateShaderTagIdList();
			}
			RenderPipelineManager.beginFrameRendering += OnBeginFrameRendering;
		}

		private void OnDisable()
		{
			RenderPipelineManager.beginFrameRendering -= OnBeginFrameRendering;
		}

		private Dictionary<Camera, CullingResults> m_cullingResults;
		private void OnBeginFrameRendering(ScriptableRenderContext context, Camera[] cameras)
		{
			if (ProjectorRendererFeature.checkUnityProjectorComponentEnabled && !m_projector.enabled)
			{
				return;
			}
			if (m_projector.material == null)
			{
				return;
			}
			if (m_cullingResults == null)
			{
				m_cullingResults = new Dictionary<Camera, CullingResults>();
			}
			m_cullingResults.Clear();
			for (int i = 0, count = cameras.Length; i < count; ++i)
			{
				bool visible = false;
				Camera cam = cameras[i];
#if UNITY_EDITOR
				if (cam.cameraType == CameraType.SceneView)
				{
					visible = StartCullingIfVisible(context, cam);
				}
#endif
				if (m_cameras == null || m_cameras.Length == 0)
				{
#if DEBUG
					if (Camera.main == null)
					{
						Debug.LogError("Camera.main is null! No projectors will be rendered. Please add a camera to Cameras property of Projector For LWRP component.", this);
					}
#endif
					if (cam == Camera.main)
					{
						visible = StartCullingIfVisible(context, cam);
					}
				}
				else {
					for (int j = 0, count2 = m_cameras.Length; j < count2; ++j)
					{
						if (cam == m_cameras[j])
						{
							visible = StartCullingIfVisible(context, cam);
							break;
						}
					}
				}
				if (visible)
				{
					ProjectorRendererFeature.AddProjector(this, cam);
				}
			}
		}

		private class TemporaryData {
			public TemporaryData()
			{
				m_vertices = new Vector3[8];
				m_clipPlanes = new Plane[12];
			}
			public Vector3[] m_vertices;
			public Plane[] m_clipPlanes;
		};
		private TemporaryData m_temporaryData = null;
		private bool StartCullingIfVisible(ScriptableRenderContext context, Camera cam)
		{
			if (m_frustumVertices == null)
			{
				return false;
			}
			ScriptableCullingParameters cullingParameters = new ScriptableCullingParameters();
			if (!cam.TryGetCullingParameters(out cullingParameters))
			{
				return false;
			}
			if (m_temporaryData == null)
			{
				m_temporaryData = new TemporaryData();
			}
			uint flags = 0;
			System.UInt64 flags64 = 0;
			for (int i = 0; i < 8; ++i)
			{
				Vector3 v = m_temporaryData.m_vertices[i] = transform.TransformPoint(m_frustumVertices[i]);
				uint f = 0;
				for (int j = 0; j < cullingParameters.cullingPlaneCount; ++j)
				{
					Plane plane = cullingParameters.GetCullingPlane(j);
					if (plane.GetDistanceToPoint(v) < 0)
					{
						f |= (1U << j);
					}
				}
				flags &= f;
				flags64 |= (f << (8 * i));
			}
			if (flags != 0)
			{
				// projector is not visible from the camera
				return false;
			}
			uint cameraPlanes = 0;
			int planeCount = 0;
			// -x
			flags = (uint)((flags64 >> 0) & (flags64 >> 8) & (flags64 >> 32) & (flags64 >> 40));
			if (flags == 0)
			{
				m_temporaryData.m_clipPlanes[planeCount++] = new Plane(m_temporaryData.m_vertices[0], m_temporaryData.m_vertices[1], m_temporaryData.m_vertices[4]);
			}
			else
			{
				cameraPlanes |= flags;
			}
			// +x
			flags = (uint)((flags64 >> 16) & (flags64 >> 24) & (flags64 >> 48) & (flags64 >> 56));
			if (flags == 0)
			{
				m_temporaryData.m_clipPlanes[planeCount++] = new Plane(m_temporaryData.m_vertices[3], m_temporaryData.m_vertices[2], m_temporaryData.m_vertices[7]);
			}
			else
			{
				cameraPlanes |= flags;
			}
			// -y
			flags = (uint)((flags64 >> 0) & (flags64 >> 16) & (flags64 >> 32) & (flags64 >> 48));
			if (flags == 0)
			{
				m_temporaryData.m_clipPlanes[planeCount++] = new Plane(m_temporaryData.m_vertices[2], m_temporaryData.m_vertices[0], m_temporaryData.m_vertices[6]);
			}
			else
			{
				cameraPlanes |= flags;
			}
			// +y
			flags = (uint)((flags64 >> 8) & (flags64 >> 24) & (flags64 >> 40) & (flags64 >> 56));
			if (flags == 0)
			{
				m_temporaryData.m_clipPlanes[planeCount++] = new Plane(m_temporaryData.m_vertices[1], m_temporaryData.m_vertices[3], m_temporaryData.m_vertices[5]);
			}
			else
			{
				cameraPlanes |= flags;
			}
			// near
			flags = (uint)((flags64 >> 0) & (flags64 >> 8) & (flags64 >> 16) & (flags64 >> 24));
			if (flags == 0)
			{
				m_temporaryData.m_clipPlanes[planeCount++] = new Plane(m_temporaryData.m_vertices[0], m_temporaryData.m_vertices[2], m_temporaryData.m_vertices[1]);
			}
			else
			{
				cameraPlanes |= flags;
			}
			// far
			flags = (uint)((flags64 >> 32) & (flags64 >> 40) & (flags64 >> 48) & (flags64 >> 56));
			if (flags == 0)
			{
				m_temporaryData.m_clipPlanes[planeCount++] = new Plane(m_temporaryData.m_vertices[4], m_temporaryData.m_vertices[5], m_temporaryData.m_vertices[6]);
			}
			else
			{
				cameraPlanes |= flags;
			}
			int maxPlaneCount = ScriptableCullingParameters.maximumCullingPlaneCount;
			for (int i = 0; i < cullingParameters.cullingPlaneCount && planeCount < maxPlaneCount; ++i)
			{
				if ((cameraPlanes & (1U << i)) != 0) {
					m_temporaryData.m_clipPlanes[planeCount++] = cullingParameters.GetCullingPlane(i);
				}
			}
			cullingParameters.cullingPlaneCount = planeCount;
			for (int i = 0; i < planeCount; ++i)
			{
				cullingParameters.SetCullingPlane(i, m_temporaryData.m_clipPlanes[i]);
			}
#if DEBUG
			// To avoid the error: Assertion failed on expression: 'params.cullingPlaneCount == kPlaneFrustumNum'
			cullingParameters.cullingPlaneCount = 6;
#endif
			CullingResults cullingResults = context.Cull(ref cullingParameters);
			m_cullingResults.Add(cam, cullingResults);
			return true;
		}

		private static new void DestroyObject(Object obj)
		{
			if (obj != null)
			{
#if UNITY_EDITOR
				DestroyImmediate(obj);
#else
				Destroy(obj);
#endif
			}
		}

		void OnDestroy()
		{
			DestroyObject(m_meshFrustum);
			m_meshFrustum = null;
		}

		CommandBuffer m_stencilPassCommands = null;
		private MaterialPropertyBlock m_stencilProperties = null;
		private Material m_copiedProjectorMaterial = null;
		public void Render(ScriptableRenderContext context, Camera cam, bool enableDynamicBatching, bool enableInstancing)
		{
			CullingResults cullingResults;
			if (!m_cullingResults.TryGetValue(cam, out cullingResults))
			{
				return;
			}
			if (useStencilTest)
			{
				if (m_stencilProperties == null)
				{
					m_stencilProperties = new MaterialPropertyBlock();
				}
				m_stencilProperties.SetFloat(s_shaderPropIdStencilRef, stencilRef);
				m_stencilProperties.SetFloat(s_shaderPropIdStencilMask, stencilMask);
				if (m_stencilPassCommands == null)
				{
					m_stencilPassCommands = new CommandBuffer();
				}
				m_stencilPassCommands.Clear();
				m_stencilPassCommands.DrawMesh(m_meshFrustum, transform.localToWorldMatrix, stencilPassMaterial, 0, 0, m_stencilProperties);
				m_stencilPassCommands.DrawMesh(m_meshFrustum, transform.localToWorldMatrix, stencilPassMaterial, 0, 1, m_stencilProperties);
				context.ExecuteCommandBuffer(m_stencilPassCommands);
			}
			if (m_copiedProjectorMaterial == null)
			{
				m_copiedProjectorMaterial = new Material(m_projector.material);
			}
			m_copiedProjectorMaterial.CopyPropertiesFromMaterial(m_projector.material);
			m_copiedProjectorMaterial.EnableKeyword(PROJECTOR_SHADER_KEYWORD);
			m_copiedProjectorMaterial.SetMatrix(s_shaderPropIdFsrWorldToProjector, uvProjectionMatrix);
			m_copiedProjectorMaterial.SetVector(s_shaderPropIdFsrWorldProjectDir, projectorDir);

			DrawingSettings drawingSettings = new DrawingSettings(m_shaderTagIdList[0], new SortingSettings(cam));
			for (int i = 1; i < m_shaderTagIdList.Length; ++i)
			{
				drawingSettings.SetShaderPassName(i, m_shaderTagIdList[i]);
			}
			drawingSettings.overrideMaterial = m_copiedProjectorMaterial;
			drawingSettings.overrideMaterialPassIndex = 0;
			drawingSettings.enableDynamicBatching = enableDynamicBatching;
			drawingSettings.enableInstancing = enableInstancing;
			drawingSettings.perObjectData = m_perObjectData;
			FilteringSettings filteringSettings = new FilteringSettings(new RenderQueueRange(m_renderQueueLowerBound, m_renderQueueUpperBound), cam.cullingMask & ~m_projector.ignoreLayers);
			RenderStateBlock renderStateBlock = new RenderStateBlock();
			if (useStencilTest) {
				renderStateBlock.mask = RenderStateMask.Stencil;
				renderStateBlock.stencilReference = m_stencilRef;
				renderStateBlock.stencilState = new StencilState(true, (byte)m_stencilMask, 0, CompareFunction.Equal, StencilOp.Zero, StencilOp.Keep, StencilOp.Keep);
			}
			context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);
		}

		public Matrix4x4 uvProjectionMatrix
		{
			get
			{
				Matrix4x4 matProjection;
				if (m_projector.orthographic)
				{
					float x = m_projector.aspectRatio * m_projector.orthographicSize;
					float y = m_projector.orthographicSize;
					matProjection = Matrix4x4.Ortho(-x, x, -y, y, m_projector.nearClipPlane, m_projector.farClipPlane);
				}
				else
				{
					matProjection = Matrix4x4.Perspective(m_projector.fieldOfView, m_projector.aspectRatio, m_projector.nearClipPlane, m_projector.farClipPlane);
				}
				matProjection.m00 *= 0.5f;
				matProjection.m02 += 0.5f * matProjection.m32;
				matProjection.m03 += 0.5f * matProjection.m33;
				matProjection.m11 *= 0.5f;
				matProjection.m12 += 0.5f * matProjection.m32;
				matProjection.m13 += 0.5f * matProjection.m33;
				float zScale = 1.0f / (m_projector.farClipPlane - m_projector.nearClipPlane);
				matProjection.m22 = zScale;
				matProjection.m23 = -zScale * m_projector.nearClipPlane;
				matProjection = matProjection * transform.worldToLocalMatrix;
				return matProjection;
			}
		}

		public Vector3 projectorDir
		{
			get
			{
				return transform.forward;
			}
		}
	}
}
