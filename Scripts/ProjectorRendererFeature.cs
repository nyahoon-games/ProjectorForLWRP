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
		private static ProjectorRendererFeature s_instance;
		private Dictionary<Camera, RenderProjectorPass> m_projectorPasses = new Dictionary<Camera, RenderProjectorPass>();
		public static void AddProjector(ProjectorForLWRP projector, Camera camera)
		{
			if (s_instance == null)
			{
#if UNITY_EDITOR
				Debug.LogError("No ProjectorRendererFeature in the ForwardRendererData!");
#endif
				return;
			}
			s_instance.AddProjectorInternal(projector, camera);
		}
		public static bool checkUnityProjectorComponentEnabled { get { return s_instance != null && s_instance.m_checkUnityProjectorComponentEnabled; } }
		public bool m_checkUnityProjectorComponentEnabled = true;
		public ProjectorRendererFeature()
		{
			m_projectorPasses = new Dictionary<Camera, RenderProjectorPass>();
			s_instance = this;
		}
		public override void Create()
		{
		}
		public override void AddRenderPasses(UnityEngine.Rendering.Universal.ScriptableRenderer renderer, ref UnityEngine.Rendering.Universal.RenderingData renderingData)
		{
			RenderProjectorPass pass;
			if (m_projectorPasses.TryGetValue(renderingData.cameraData.camera, out pass))
			{
				renderer.EnqueuePass(pass);
			}
		}
		private void AddProjectorInternal(ProjectorForLWRP projector, Camera camera)
		{
			RenderProjectorPass pass;
			if (!m_projectorPasses.TryGetValue(camera, out pass))
			{
				pass = new RenderProjectorPass(camera);
				pass.renderPassEvent = projector.renderPassEvent;
				m_projectorPasses.Add(camera, pass);
			}
			pass.AddProjector(projector);
		}
	}
}
