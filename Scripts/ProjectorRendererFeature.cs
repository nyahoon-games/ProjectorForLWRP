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
		private static ProjectorRendererFeature s_currentInstance = null;
#if UNITY_EDITOR
		private static bool IsLightweightRenderPipelineSetupCorrectly()
		{
			if (s_currentInstance != null)
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
				return false;
			}
			else
			{
				foreach (var rendererFeature in rendererData.rendererFeatures)
				{
					if (rendererFeature is ProjectorRendererFeature)
					{
						s_currentInstance = rendererFeature as ProjectorRendererFeature;
						break;
					}
				}
				if (s_currentInstance == null)
				{
					Debug.LogError("ProjectorRendererFeature is not added to the current Forward Renderer Data.", rendererData);
					return false;
				}
			}
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
#if UNITY_EDITOR || DEBUG
		public Material m_replaceProjectorMaterialForDebug = null;
		public static Material replaceProjectorMaterialForDebug
		{
			get
			{
#if UNITY_EDITOR
				if (!IsLightweightRenderPipelineSetupCorrectly())
				{
					return null;
				}
#endif
				return s_currentInstance == null ? null : s_currentInstance.m_replaceProjectorMaterialForDebug;
			}
		}
#endif
		public override void Create()
		{
			RenderPipelineManager.endFrameRendering += OnEndFrameRendering;
			s_renderPassList.Clear();
#if !UNITY_EDITOR
			s_currentInstance = this;
#endif
		}
		private void OnDestroy()
		{
			RenderPipelineManager.endFrameRendering -= OnEndFrameRendering;
			if (s_currentInstance == this)
			{
				s_renderPassList.Clear();
				s_currentInstance = null;
			}
		}
		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			StencilMaskAllocator.Init(m_stencilMask);
			var passes = s_renderPassList[renderingData.cameraData.camera];
			foreach (var pass in passes)
			{
				renderer.EnqueuePass(pass);
			}
		}
		private static void OnEndFrameRendering(ScriptableRenderContext context, Camera[] cameras)
		{
			s_renderPassList.Clear();
		}
	}
}
