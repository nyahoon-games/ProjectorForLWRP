//
// ProjectorRendererFeature.cs
//
// Projector For LWRP
//
// Copyright (c) 2019 NYAHOON GAMES PTE. LTD.
//

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;
using System.Collections.Generic;

namespace ProjectorForLWRP
{
	public class ProjectorRendererFeature : ScriptableRendererFeature
	{
		private class ProjectorPassManager
		{
			private Dictionary<Camera, Dictionary<RenderPassEvent, RenderProjectorPass>> m_cameraToProjectorPassDicitionary = new Dictionary<Camera, Dictionary<RenderPassEvent, RenderProjectorPass>>();
			private Dictionary<Camera, List<RenderProjectorPass>> m_cameraToProjectorPassList = new Dictionary<Camera, List<RenderProjectorPass>>();
			private ObjectPool<Dictionary<RenderPassEvent, RenderProjectorPass>> m_projectorPassDictionaryPool = new ObjectPool<Dictionary<RenderPassEvent, RenderProjectorPass>>();
			private ObjectPool<List<RenderProjectorPass>> m_projectorPassListPool = new ObjectPool<List<RenderProjectorPass>>();
			private ObjectPool<RenderProjectorPass> m_renderProjectorPassPool = new ObjectPool<RenderProjectorPass>();
			public void AddProjector(Camera camera, ProjectorForLWRP projector)
			{
				Dictionary<RenderPassEvent, RenderProjectorPass> passDictionary;
				if (!m_cameraToProjectorPassDicitionary.TryGetValue(camera, out passDictionary))
				{
					passDictionary = m_projectorPassDictionaryPool.Get();
					var passList = m_projectorPassListPool.Get();
					m_cameraToProjectorPassDicitionary.Add(camera, passDictionary);
					m_cameraToProjectorPassList.Add(camera, passList);
				}
				RenderProjectorPass pass;
				if (!passDictionary.TryGetValue(projector.renderPassEvent, out pass))
				{
					pass = m_renderProjectorPassPool.Get();
					passDictionary.Add(projector.renderPassEvent, pass);
					m_cameraToProjectorPassList[camera].Add(pass);
				}
				pass.renderPassEvent = projector.renderPassEvent;
				pass.AddProjector(projector);
			}
			public void EnqueProjectorPassesToRenderer(Camera camera, ScriptableRenderer renderer)
			{
				List<RenderProjectorPass> passes;
				if (m_cameraToProjectorPassList.TryGetValue(camera, out passes))
				{
					for (int i = 0, count = passes.Count; i < count; ++i)
					{
						renderer.EnqueuePass(passes[i]);
					}
				}
			}
			public void ClearProjectorPasesForCamera(Camera camera)
			{
				List<RenderProjectorPass> passes;
				if (m_cameraToProjectorPassList.TryGetValue(camera, out passes))
				{
					for (int i = 0, count = passes.Count; i < count; ++i)
					{
						passes[i].ClearProjectors();
					}
					m_cameraToProjectorPassList.Remove(camera);
					m_cameraToProjectorPassDicitionary.Remove(camera);
				}
			}
			public void ClearAll()
			{
				m_cameraToProjectorPassDicitionary.Clear();
				m_cameraToProjectorPassList.Clear();
				m_projectorPassDictionaryPool.Clear();
				m_projectorPassListPool.Clear();
				m_renderProjectorPassPool.Clear();
			}
		}
		private static ProjectorRendererFeature s_currentInstance = null;
		private static int s_instanceCount = 0;
		private static ProjectorPassManager s_projectorPassManager = new ProjectorPassManager();
		private static Dictionary<Camera, List<ShadowBuffer>> s_activeShadowBufferList = new Dictionary<Camera, List<ShadowBuffer>>();
		private static ObjectPool<List<ShadowBuffer>> s_shadowBufferListPool = new ObjectPool<List<ShadowBuffer>>();
#if UNITY_EDITOR
		private static bool IsLightweightRenderPipelineSetupCorrectly()
		{
			// check if the current Forward Renderer has the ProjectorRendererFeature instance.
			LightweightRenderPipelineAsset renderPipelineAsset = LightweightRenderPipeline.asset;
			if (renderPipelineAsset == null)
			{
				return false;
			}
			UnityEditor.SerializedObject serializedObject = new UnityEditor.SerializedObject(renderPipelineAsset);
			UnityEditor.SerializedProperty rendererDataProperty = serializedObject.FindProperty("m_RendererData");
			ScriptableRendererData rendererData = rendererDataProperty.objectReferenceValue as ScriptableRendererData;
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

		public static void AddShadowBuffer(ShadowBuffer shadowBuffer, Camera camera)
		{
#if UNITY_EDITOR
			if (!IsLightweightRenderPipelineSetupCorrectly())
			{
				return;
			}
#endif
			AddShadowBufferInternal(shadowBuffer, camera);
		}

		public int m_stencilMask = 0xFF;
		public ProjectorRendererFeature()
		{
			if (s_instanceCount++ == 0)
			{
				RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
			}
			s_currentInstance = this;
		}
		~ProjectorRendererFeature()
		{
			if (m_stencilMask != -1 && --s_instanceCount == 0)
			{
				s_projectorPassManager.ClearAll();
				s_activeShadowBufferList.Clear();
				s_shadowBufferListPool.Clear();
				RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
			}
			m_stencilMask = -1; // mark as destructed. destructor may be called more than onece. make sure to decrement the counter only once.
			if (s_currentInstance == this)
			{
				s_currentInstance = null;
			}
		}
		public override void Create()
		{
			s_projectorPassManager.ClearAll();
			s_activeShadowBufferList.Clear();
			s_shadowBufferListPool.Clear();
		}
		private CollectShadowBufferPass m_collectShadowBufferPass = null;
		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			s_currentInstance = this;
			StencilMaskAllocator.Init(m_stencilMask);
			s_projectorPassManager.EnqueProjectorPassesToRenderer(renderingData.cameraData.camera, renderer);

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
			s_projectorPassManager.AddProjector(camera, projector);
		}
		private static void AddShadowProjectorInternal(ShadowProjectorForLWRP projector, Camera camera)
		{
			if (projector.shadowBuffer != null)
			{
				projector.shadowBuffer.RegisterProjector(camera, projector);
				AddShadowBufferInternal(projector.shadowBuffer, camera);
			}
			else
			{
				AddProjectorInternal(projector, camera);
			}
		}
		private static void AddShadowBufferInternal(ShadowBuffer shadowBuffer, Camera camera)
		{
			List<ShadowBuffer> shadowBufferList;
			if (!s_activeShadowBufferList.TryGetValue(camera, out shadowBufferList))
			{
				shadowBufferList = s_shadowBufferListPool.Get();
				s_activeShadowBufferList.Add(camera, shadowBufferList);
			}
			if (!shadowBufferList.Contains(shadowBuffer))
			{
				shadowBufferList.Add(shadowBuffer);
			}
		}
		private static void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
		{
			s_projectorPassManager.ClearProjectorPasesForCamera(camera);
			List<ShadowBuffer> shadowBufferList;
			if (s_activeShadowBufferList.TryGetValue(camera, out shadowBufferList))
			{
				shadowBufferList.Clear();
				s_shadowBufferListPool.Release(shadowBufferList);
				s_activeShadowBufferList.Remove(camera);
				CommandBuffer cmd = CommandBufferPool.Get();
				if (LitShaderState.ClearStates(cmd))
				{
					context.ExecuteCommandBuffer(cmd);
					cmd.Clear();
					CommandBufferPool.Release(cmd);
				}
			}
		}
	}
}
