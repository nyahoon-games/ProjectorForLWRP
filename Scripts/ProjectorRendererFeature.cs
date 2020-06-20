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

namespace ProjectorForLWRP
{
	public class ProjectorRendererFeature : ScriptableRendererFeature
	{
		private class ProjectorPassManager
		{
			private ObjectPool<ObjectPool<RenderProjectorPass>.AutoClearMap<RenderPassEvent>>.Map<Camera> m_cameraToProjectorPassDicitionary = new ObjectPool<ObjectPool<RenderProjectorPass>.AutoClearMap<RenderPassEvent>>.Map<Camera>();
			public void AddProjector(Camera camera, ProjectorForLWRP projector)
			{
				var passDictionary = m_cameraToProjectorPassDicitionary[camera];
				RenderProjectorPass pass = passDictionary[projector.renderPassEvent];
				pass.renderPassEvent = projector.renderPassEvent;
				pass.AddProjector(projector);
			}
			public void EnqueProjectorPassesToRenderer(Camera camera, ScriptableRenderer renderer)
			{
				var passDictionary = m_cameraToProjectorPassDicitionary[camera];
				foreach (var pass in passDictionary.Values)
				{
					renderer.EnqueuePass(pass);
				}
			}
			public void ClearProjectorPasesForCamera(Camera camera)
			{
				m_cameraToProjectorPassDicitionary.Remove(camera);
			}
			public void ClearAll()
			{
				m_cameraToProjectorPassDicitionary.Clear();
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
