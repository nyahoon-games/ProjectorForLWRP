//
// ApplyShadowBufferPass.cs
//
// Projector For LWRP
//
// Copyright (c) 2020 NYAHOON GAMES PTE. LTD.
//

using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;
using UnityEngine;

namespace ProjectorForLWRP
{
	public class ApplyShadowBufferPass : ScriptableRenderPass
	{
		ShadowBuffer m_shadowBuffer;
		internal ApplyShadowBufferPass(ShadowBuffer shadowBuffer)
		{
			m_shadowBuffer = shadowBuffer;
		}
		public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
		{
		}
		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			m_shadowBuffer.ApplyShadowBuffer(context, ref renderingData);
			m_shadowBuffer.ClearProjectosForCamera(renderingData.cameraData.camera);
		}
		public override void FrameCleanup(CommandBuffer cmd)
		{
			m_shadowBuffer.ReleaseTemporaryShadowTexture();
		}
	}
}
