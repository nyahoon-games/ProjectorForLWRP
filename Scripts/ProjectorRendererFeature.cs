//
// ProjectorRendererFeature.cs
//
// Projector For LWRP
//
// Copyright (c) 2019 NYAHOON GAMES PTE. LTD.
//

using UnityEngine;

using System.Collections.Generic;

namespace ProjectorForLWRP
{
	public class ProjectorRendererFeature : UnityEngine.Rendering.Universal.ScriptableRendererFeature
	{
		private static ProjectorRendererFeature s_currentInstance = null;
		private static int s_instanceCount = 0;
		private static Dictionary<Camera, RenderProjectorPass> s_projectorPasses = null;
		public static void AddProjector(ProjectorForLWRP projector, Camera camera)
		{
#if UNITY_EDITOR
			// check if the current Forward Renderer has the ProjectorRendererFeature instance.
			UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset renderPipelineAsset = UnityEngine.Rendering.Universal.UniversalRenderPipeline.asset;
			if (renderPipelineAsset == null)
			{
				return;
			}
			UnityEditor.SerializedObject serializedObject = new UnityEditor.SerializedObject(renderPipelineAsset);
			UnityEditor.SerializedProperty rendererDataListProperty = serializedObject.FindProperty("m_RendererDataList");
			UnityEditor.SerializedProperty defaultRendererIndexProperty = serializedObject.FindProperty("m_DefaultRendererIndex");
			if (rendererDataListProperty.arraySize <= defaultRendererIndexProperty.intValue)
			{
				Debug.LogError("No default renderer found in the current Universal Render Pipeline Asset.", renderPipelineAsset);
			}
			UnityEngine.Rendering.Universal.ScriptableRendererData rendererData = rendererDataListProperty.GetArrayElementAtIndex(defaultRendererIndexProperty.intValue).objectReferenceValue as UnityEngine.Rendering.Universal.ScriptableRendererData;
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
				}
			}
#endif
			if (s_projectorPasses == null)
			{
#if UNITY_EDITOR
				Debug.LogError("No ProjectorRendererFeature instances are created!");
#endif
				return;
			}
			AddProjectorInternal(projector, camera);
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
		public bool m_checkUnityProjectorComponentEnabled = true;
		public string[] m_defaultCameraTags = { "MainCamera" };
		public ProjectorRendererFeature()
		{
			if (s_projectorPasses == null)
			{
				s_projectorPasses = new Dictionary<Camera, RenderProjectorPass>();
			}
			++s_instanceCount;
			s_currentInstance = this;
		}
		~ProjectorRendererFeature()
		{
			if (m_defaultCameraTags != null && --s_instanceCount == 0)
			{
				s_projectorPasses = null;
			}
			m_defaultCameraTags = null; // mark as destructed. destructor may be called more than onece. make sure to decrement the counter only once.
			if (s_currentInstance == this)
			{
				s_currentInstance = null;
			}
		}
		public override void Create()
		{
		}
		public override void AddRenderPasses(UnityEngine.Rendering.Universal.ScriptableRenderer renderer, ref UnityEngine.Rendering.Universal.RenderingData renderingData)
		{
			s_currentInstance = this;
			RenderProjectorPass pass;
			if (s_projectorPasses.TryGetValue(renderingData.cameraData.camera, out pass))
			{
				renderer.EnqueuePass(pass);
			}
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
	}
}
