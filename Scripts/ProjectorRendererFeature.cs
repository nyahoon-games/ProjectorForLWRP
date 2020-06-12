//
// ProjectorRendererFeature.cs
//
// Projector For LWRP
//
// Copyright (c) 2019 NYAHOON GAMES PTE. LTD.
//

using UnityEngine;
using UnityEngine.Rendering.LWRP;
using System.Collections.Generic;

namespace ProjectorForLWRP
{
	public class ProjectorRendererFeature : ScriptableRendererFeature
	{
		private static ProjectorRendererFeature s_currentInstance = null;
		private static int s_instanceCount = 0;
		private static Dictionary<Camera, RenderProjectorPass> s_projectorPasses = null;
		private static Dictionary<Camera, List<ShadowBuffer>> s_activeShadowBufferList = null;

#if UNITY_EDITOR
		private static bool IsLightweightRenderPipelineSetupCorrectly()
		{
			// check if the current Forward Renderer has the ProjectorRendererFeature instance.
			LightweightRenderPipelineAsset renderPipelineAsset = UnityEngine.Rendering.LWRP.LightweightRenderPipeline.asset;
			if (renderPipelineAsset == null)
			{
				return false;
			}
			UnityEditor.SerializedObject serializedObject = new UnityEditor.SerializedObject(renderPipelineAsset);
			UnityEditor.SerializedProperty rendererDataProperty = serializedObject.FindProperty("m_RendererData");
			UnityEngine.Rendering.LWRP.ScriptableRendererData rendererData = rendererDataProperty.objectReferenceValue as UnityEngine.Rendering.LWRP.ScriptableRendererData;
			if (rendererData == null)
			{
				Debug.LogError("The current Lightweight Render Pipeline Asset does not have Forward Renderer Data! Please set a Forward Renderer Data which contains ProjectorRendererFeature to the current render pipeline asset.", renderPipelineAsset);
			}
			else
			{
				bool found = false;
				foreach (var rendererFeature in rendererData.rendererFeatures)
				{
					if (rendererFeature is ProjectorRendererFeature)
					{
						found = true;
						break;
					}
				}
				if (!found)
				{
					Debug.LogError("ProjectorRendererFeature is not added to the current Forward Renderer Data.", rendererData);
					return false;
				}
			}
			if (s_projectorPasses == null)
			{
				Debug.LogError("No ProjectorRendererFeature instances are created!");
				return false;
			}
			return true;
		}
#endif
		public static void AddProjector(ProjectorForLWRP projector, Camera camera)
		{
#if UNITY_EDITOR
			if (!IsLightweightRenderPipelineSetupCorrectly())
			{
				return;
			}
#endif
			AddProjectorInternal(projector, camera);
		}

		public static void AddShadowProjector(ShadowProjectorForLWRP projector, Camera camera)
		{
#if UNITY_EDITOR
			if (!IsLightweightRenderPipelineSetupCorrectly())
			{
				return;
			}
#endif
			AddShadowProjectorInternal(projector, camera);
		}

		public static bool checkUnityProjectorComponentEnabled { get { return s_currentInstance == null || s_currentInstance.m_checkUnityProjectorComponentEnabled; } }
		public static string[] defaultCameraTags
		{
			get
			{
				if (s_currentInstance == null)
				{
					return null;
				}
				return s_currentInstance.m_defaultCameraTags;
			}
		}
		public int m_stencilMask = 0xFF;
		public bool m_checkUnityProjectorComponentEnabled = true;
		public string[] m_defaultCameraTags = { "MainCamera" };
		public ProjectorRendererFeature()
		{
			if (s_projectorPasses == null)
			{
				s_projectorPasses = new Dictionary<Camera, RenderProjectorPass>();
			}
			if (s_activeShadowBufferList == null)
			{
				s_activeShadowBufferList = new Dictionary<Camera, List<ShadowBuffer>>();
			}
			++s_instanceCount;
			s_currentInstance = this;
		}
		~ProjectorRendererFeature()
		{
			if (m_defaultCameraTags != null && --s_instanceCount == 0)
			{
				s_projectorPasses = null;
				s_activeShadowBufferList = null;
			}
			m_defaultCameraTags = null; // mark as destructed. destructor may be called more than onece. make sure to decrement the counter only once.
			if (s_currentInstance == this)
			{
				s_currentInstance = null;
			}
		}
		public override void Create()
		{
			List<Camera> invalidCameras = null;
			foreach (var pair in s_activeShadowBufferList)
			{
				if (pair.Key == null)
				{
					if (invalidCameras == null)
					{
						invalidCameras = new List<Camera>();
					}
					invalidCameras.Add(pair.Key);
				}
				if (pair.Value != null)
				{
					for (int i = 0; i < pair.Value.Count;)
					{
						if (pair.Value[i] == null)
						{
							pair.Value.RemoveAt(i);
						}
						else
						{
							++i;
						}
					}
				}
			}
			if (invalidCameras != null)
			{
				foreach (Camera key in invalidCameras)
				{
					s_activeShadowBufferList.Remove(key);
				}
			}
		}
		private CollectShadowBufferPass m_collectShadowBufferPass = null;
		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			s_currentInstance = this;
			StencilMaskAllocator.Init(m_stencilMask);
			RenderProjectorPass pass;
			if (s_projectorPasses.TryGetValue(renderingData.cameraData.camera, out pass))
			{
				if (pass.isActive)
				{
					renderer.EnqueuePass(pass);
				}
			}
			List<ShadowBuffer> shadowBufferList;
			if (s_activeShadowBufferList.TryGetValue(renderingData.cameraData.camera, out shadowBufferList))
			{
				if (shadowBufferList != null && 0 < shadowBufferList.Count)
				{
					if (m_collectShadowBufferPass == null)
					{
						m_collectShadowBufferPass = new CollectShadowBufferPass();
					}
					renderer.EnqueuePass(m_collectShadowBufferPass);
					int applyPassCount = 0;
					for (int i = 0; i < shadowBufferList.Count; ++i)
					{
						shadowBufferList[i].AddRenderPasses(renderer, ref renderingData, out applyPassCount);
					}
					m_collectShadowBufferPass.SetShadowBuffers(shadowBufferList, applyPassCount);
				}
			}
		}
		internal static void ApplyShadowBufferPassFinished()
		{
			Debug.Assert(s_currentInstance != null && s_currentInstance.m_collectShadowBufferPass != null);
			s_currentInstance.m_collectShadowBufferPass.ApplyPassFinished();
		}
		private static void AddProjectorInternal(ProjectorForLWRP projector, Camera camera)
		{
			RenderProjectorPass pass;
			if (!s_projectorPasses.TryGetValue(camera, out pass))
			{
				pass = new RenderProjectorPass(camera);
				pass.renderPassEvent = projector.renderPassEvent;
				s_projectorPasses.Add(camera, pass);
			}
			pass.AddProjector(projector);
		}
		private static void AddShadowProjectorInternal(ShadowProjectorForLWRP projector, Camera camera)
		{
			if (projector.shadowBuffer != null)
			{
				projector.shadowBuffer.RegisterProjector(camera, projector);
				List<ShadowBuffer> shadowBufferList;
				if (!s_activeShadowBufferList.TryGetValue(camera, out shadowBufferList))
				{
					shadowBufferList = new List<ShadowBuffer>();
					s_activeShadowBufferList.Add(camera, shadowBufferList);
				}
				if (!shadowBufferList.Contains(projector.shadowBuffer))
				{
					shadowBufferList.Add(projector.shadowBuffer);
				}
			}
			else
			{
				AddProjectorInternal(projector, camera);
			}
		}
	}
}
