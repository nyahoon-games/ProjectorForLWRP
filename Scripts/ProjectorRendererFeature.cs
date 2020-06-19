//
// ProjectorRendererFeature.cs
//
// Projector For LWRP
//
// Copyright (c) 2019 NYAHOON GAMES PTE. LTD.
//

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
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
						m_renderProjectorPassPool.Release(passes[i]);
					}
					m_projectorPassListPool.Release(passes);
					m_projectorPassDictionaryPool.Release(m_cameraToProjectorPassDicitionary[camera]);
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
#if UNITY_EDITOR
		static bool s_pipelineSetupOk = false;
		private static bool IsLightweightRenderPipelineSetupCorrectly()
		{
			if (s_pipelineSetupOk)
			{
				return true;
			}
			// check if the current Forward Renderer has the ProjectorRendererFeature instance.
			UniversalRenderPipelineAsset renderPipelineAsset = UniversalRenderPipeline.asset;
			if (renderPipelineAsset == null)
			{
				return false;
			}
			UnityEditor.SerializedObject serializedObject = new UnityEditor.SerializedObject(renderPipelineAsset);
			UnityEditor.SerializedProperty rendererDataListProperty = serializedObject.FindProperty("m_RendererDataList");
			UnityEditor.SerializedProperty defaultRendererIndexProperty = serializedObject.FindProperty("m_DefaultRendererIndex");
			ScriptableRendererData rendererData = null;
			if (defaultRendererIndexProperty.intValue < rendererDataListProperty.arraySize)
			{
				rendererData = rendererDataListProperty.GetArrayElementAtIndex(defaultRendererIndexProperty.intValue).objectReferenceValue as ScriptableRendererData;
			}
			if (rendererData == null)
			{
				Debug.LogError("No default renderer found in the current Universal Render Pipeline Asset.", renderPipelineAsset);
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
			s_pipelineSetupOk = true;
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
		}
		public override void AddRenderPasses(UnityEngine.Rendering.Universal.ScriptableRenderer renderer, ref UnityEngine.Rendering.Universal.RenderingData renderingData)
		{
			s_currentInstance = this;
			StencilMaskAllocator.Init(m_stencilMask);
			s_projectorPassManager.EnqueProjectorPassesToRenderer(renderingData.cameraData.camera, renderer);
		}
		private static void AddProjectorInternal(ProjectorForLWRP projector, Camera camera)
		{
			s_projectorPassManager.AddProjector(camera, projector);
		}
		private static void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
		{
			s_projectorPassManager.ClearProjectorPasesForCamera(camera);
		}
	}
}
