//
// ShadowProjectorController.cs
//
// Projector For LWRP For LWRP
//
// Copyright (c) 2020 NYAHOON GAMES PTE. LTD.
//

using UnityEngine;

namespace ProjectorForLWRP
{
	[ExecuteInEditMode]
	[RequireComponent(typeof(Projector))]
	public class ShadowProjectorController : MonoBehaviour
	{
		[Header("Light source of the shadow")]
		[SerializeField]
		private Light m_lightSource = null;
		[Header("Shadow Caster")]
		[SerializeField]
		private CasterObject m_casterObject = null;
		[SerializeField]
		private Rect m_textureRect = new Rect(0.2f, 0.2f, 0.6f, 0.6f);

		private Projector m_projector;
		private bool initialSetup { get; set; } = false;

		private void OnValidate()
		{
			if (!initialSetup)
			{
				initialSetup = true;
				if (m_lightSource == null)
				{
					ShadowMaterialProperties shadowMaterialProperties = GetComponent<ShadowMaterialProperties>();
					if (shadowMaterialProperties != null)
					{
						m_lightSource = shadowMaterialProperties.lightSource;
					}
				}
				if (m_casterObject == null)
				{
					Transform parent = transform.parent;
					while (parent != null)
					{
						CasterObject casterObject = GetComponent<CasterObject>();
						if (casterObject != null)
						{
							m_casterObject = casterObject;
							break;
						}
						parent = parent.parent;
					}
				}
			}
		}

		private void Awake()
		{
			m_projector = GetComponent<Projector>();
		}

		private void LateUpdate()
		{
			// TODO: calculate some shader propeties, and set them to Projector material.
			UpdateProjector(m_projector);
		}

		protected void UpdateProjector(Projector projector)
		{
			if (m_lightSource == null || m_casterObject == null)
			{
				return;
			}
			bool isOrthographic = m_lightSource.type == LightType.Directional;

			// calculate shadow bounds
			CasterObject.ShadowBounds shadowBounds = new CasterObject.ShadowBounds();
			CasterObject.ProjectorBases projectorBases = new CasterObject.ProjectorBases();
			m_casterObject.CalculateShadowBounds(m_lightSource.transform, isOrthographic, out shadowBounds, out projectorBases);

			Transform projectorTransform = projector.transform;
			// calculate the projector frustom parameters
			if (isOrthographic)
			{
				// calculate the position which is mapped on the center of the shadow texture
				float centerX = ((0.5f - m_textureRect.xMin) * shadowBounds.left + (m_textureRect.xMax - 0.5f) * shadowBounds.right) / m_textureRect.width;
				float centerY = ((0.5f - m_textureRect.yMin) * shadowBounds.top + (m_textureRect.yMax - 0.5f) * shadowBounds.bottom) / m_textureRect.height;
				Vector3 projCenter = centerX * projectorBases.right + centerY * projectorBases.up;
				Vector3 position = m_lightSource.transform.position + projCenter;
				// update projector transform
				projectorTransform.position = position;
				projectorTransform.LookAt(position + projectorBases.forward, projectorBases.up);
				// frustum parameters
				Vector3 projectorLosstScale = transform.lossyScale;
				projectorTransform.position += (shadowBounds.near - projector.nearClipPlane * projectorLosstScale.z) * projectorBases.forward;
				projector.orthographicSize = 0.5f * (shadowBounds.top - shadowBounds.bottom) / (m_textureRect.height * projectorLosstScale.y);
				projector.aspectRatio = 0.5f * (shadowBounds.right - shadowBounds.left) / (projector.orthographicSize * m_textureRect.width * projectorLosstScale.z);
			}
			else
			{
				if (m_lightSource.range <= shadowBounds.near)
				{
					projector.enabled = false;
					return;
				}
				projector.enabled = true;
				// calculate the position which is mapped on the center of the shadow texture
				float centerX = ((0.5f - m_textureRect.xMin) * shadowBounds.left + (m_textureRect.xMax - 0.5f) * shadowBounds.right) / m_textureRect.width;
				float centerY = ((0.5f - m_textureRect.yMin) * shadowBounds.top + (m_textureRect.yMax - 0.5f) * shadowBounds.bottom) / m_textureRect.height;
				// rotate projector coordinates to look at projCenter.
				float cosY = 1.0f/Mathf.Sqrt(1.0f + centerX * centerX);
				float sinY = centerX * cosY;
				float tanX = cosY * centerY;
				float cosX = 1.0f / Mathf.Sqrt(1.0f + tanX * tanX);
				float sinX = tanX * cosX;
				// rotate around projCoords.up by cosY, sinY
				Vector3 projForward = cosY * projectorBases.forward - sinY * projectorBases.right;
				projectorBases.right = cosY * projectorBases.right + sinY * projectorBases.forward;
				// rotate around projCoords.right by cosX, sinX
				projectorBases.forward = cosX * projForward + sinX * projectorBases.up;
				projectorBases.up = cosX * projectorBases.up - sinX * projForward;
				// update projector transform
				Vector3 position = m_lightSource.transform.position;
				projectorTransform.position = position;
				projectorTransform.LookAt(position + projectorBases.forward, projectorBases.up);
				// calculate fov and aspect ratio
				Vector3 projectorLosstScale = transform.lossyScale;
				float rightX = shadowBounds.left + (shadowBounds.right - shadowBounds.left) / m_textureRect.width;
				float topY = shadowBounds.bottom + (shadowBounds.top - shadowBounds.bottom) / m_textureRect.height;
				float tanFovY = topY * projectorLosstScale.z / ((1.0f - sinX * cosY) * projectorLosstScale.y);
				float tanFovX = rightX * projectorLosstScale.z / ((1.0f + sinY) * projectorLosstScale.x);
				projector.fieldOfView = 2.0f * Mathf.Rad2Deg * Mathf.Atan(tanFovY);
				projector.aspectRatio = tanFovX / tanFovY;
				// near clip / far clip
				float invScaleZ = 1.0f / projectorLosstScale.z;
				projector.nearClipPlane = invScaleZ * shadowBounds.near;
				projector.farClipPlane = invScaleZ * m_lightSource.range;
			}
		}
#if UNITY_EDITOR
		private Mesh m_textureGizmoMesh;
		public bool drawTargetShadowTextureGizmo { get; set; } = true;
		public string shadowTextureName { get; set; } = "_ShadowTex";
		public float textureGizmoAlpha { get; set; } = 0.5f;
		private static Material s_drawTextureMaterial = null;
		private void OnDrawGizmosSelected()
		{
			if (drawTargetShadowTextureGizmo && m_projector != null && m_projector.material != null)
			{
				if (m_textureGizmoMesh == null)
				{
					m_textureGizmoMesh = new Mesh();
					m_textureGizmoMesh.vertices = new Vector3[] { new Vector3(-1, 1, 0), new Vector3(-1, -1, 0), new Vector3(1, 1, 0), new Vector3(1, -1, 0) };
					m_textureGizmoMesh.uv = new Vector2[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 0), new Vector2(1, 1) };
					m_textureGizmoMesh.triangles = new int[] { 0, 1, 2, 2, 1, 3 };
				}
				if (s_drawTextureMaterial == null)
				{
					s_drawTextureMaterial = HelperFunctions.FindMaterial("Hidden/ProjectorForLWRP/GizmoTexture");
				}
				Texture shadowTexture = m_projector.material.GetTexture(shadowTextureName);
				if (shadowTexture != null)
				{
					s_drawTextureMaterial.mainTexture = shadowTexture;
					s_drawTextureMaterial.SetFloat("_Alpha", textureGizmoAlpha);
					s_drawTextureMaterial.SetPass(0);
					float w, h;
					if (m_projector.orthographic)
					{
						h = m_projector.orthographicSize;
					}
					else
					{
						h = m_projector.nearClipPlane * Mathf.Tan(0.5f * Mathf.Deg2Rad * m_projector.fieldOfView);
					}
					w = h * m_projector.aspectRatio;
					w *= transform.lossyScale.x;
					h *= transform.lossyScale.y;
					Vector3 pos = transform.position + m_projector.nearClipPlane * transform.forward;
					Matrix4x4 worldMatrix = new Matrix4x4(w * transform.right, h * transform.up, transform.forward, pos);
					worldMatrix.m33 = 1;
					Graphics.DrawMeshNow(m_textureGizmoMesh, worldMatrix);
				}
			}
		}
#endif
	}
}
