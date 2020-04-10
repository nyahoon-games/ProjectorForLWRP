//
// RenderProjectorPass.cs
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
	public class RenderProjectorPass : UnityEngine.Rendering.Universal.ScriptableRenderPass
	{
		List<ProjectorForLWRP> m_projectors;
		Camera m_camera;
		public RenderProjectorPass(Camera camera)
		{
			m_camera = camera;
			m_projectors = new List<ProjectorForLWRP>();
		}
		public void AddProjector(ProjectorForLWRP projector)
		{
			if (!m_projectors.Contains(projector))
			{
				m_projectors.Add(projector);
			}
		}
		public bool isActive
		{
			get { return 0 < m_projectors.Count; }
		}
		public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
		{
			base.Configure(cmd, cameraTextureDescriptor);
		}
		public override void Execute(ScriptableRenderContext context, ref UnityEngine.Rendering.Universal.RenderingData renderingData)
		{
			for (int i = 0, count = m_projectors.Count; i < count; ++i)
			{
				m_projectors[i].Render(context, m_camera, renderingData.supportsDynamicBatching, true);
			}
		}
		public override void FrameCleanup(CommandBuffer cmd)
		{
			m_projectors.Clear();
			base.FrameCleanup(cmd);
		}
	}
}
