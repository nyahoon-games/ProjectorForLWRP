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
		private static ProjectorRendererFeature s_currentInstance = null;
		private static int s_instanceCount = 0;
#if UNITY_EDITOR
		private static bool s_pipelineSetupOk = false;
		private static bool IsLightweightRenderPipelineSetupCorrectly()
		{
			if (s_pipelineSetupOk)
			{
				return true;
			}
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
			s_pipelineSetupOk = true;
			return true;
		}
#endif
		static ObjectPool<Collections.AutoClearList<ScriptableRenderPass>>.Map<Camera> s_renderPassList = new ObjectPool<Collections.AutoClearList<ScriptableRenderPass>>.Map<Camera>();
		public static void AddRenderPass(Camera camera, ScriptableRenderPass pass)
		{
#if UNITY_EDITOR
			if (!IsLightweightRenderPipelineSetupCorrectly())
			{
				return;
			}
#endif
			s_renderPassList[camera].Add(pass);
		}

		public int m_stencilMask = 0xFF;
		public ProjectorRendererFeature()
		{
			++s_instanceCount;
			RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
			s_currentInstance = this;
		}
		~ProjectorRendererFeature()
		{
			if (m_stencilMask != -1)
			{
				--s_instanceCount;
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
		}
		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			var passes = s_renderPassList[renderingData.cameraData.camera];
			foreach (var pass in passes)
			{
				renderer.EnqueuePass(pass);
			}
		}
		private static void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
		{
			s_renderPassList.Remove(camera);
		}
	}
}
