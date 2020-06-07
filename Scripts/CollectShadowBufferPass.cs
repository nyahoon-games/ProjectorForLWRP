//
// CollectShadowBufferPass.cs
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
	public class CollectShadowBufferPass : ScriptableRenderPass
	{
		ShadowBuffer m_shadowBuffer;
        string m_ProfilerTag = "Collect Shadow Buffer Pass";
        string m_DepthPassTag = "Depth Only Pass";
        ShaderTagId m_ShaderTagId = new ShaderTagId("DepthOnly");
        internal CollectShadowBufferPass(ShadowBuffer shadowBuffer)
		{
			m_shadowBuffer = shadowBuffer;
			renderPassEvent = RenderPassEvent.AfterRenderingPrePasses;
		}
        private int m_renderTextureChannelIndex = 0;
		public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
		{
            ShadowBuffer.RenderTextureRef renderTextureRef = m_shadowBuffer.CreateTemporaryShadowTexture(cameraTextureDescriptor.width, cameraTextureDescriptor.height);
            m_renderTextureChannelIndex = renderTextureRef.refCount - 1;
            Color clearColor = new Color(1, 1, 1, 1);
            ConfigureTarget(new RenderTargetIdentifier(renderTextureRef.renderTexture));
            ConfigureClear(ClearFlag.None, clearColor);
		}
		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            cmd.BeginSample(m_ProfilerTag);
            context.ExecuteCommandBuffer(cmd); // just execute BeginSample command
            cmd.Clear();
            if (ShadowBuffer.IsFirstCollectPass() && m_renderTextureChannelIndex == 0 && m_shadowBuffer.insertDepthOnlyPassIfNecessary && !ForwardRendererRequiresDepthOnlyPass(renderingData))
            {
                cmd.ClearRenderTarget(true, true, clearColor);
                // draw depth only pass
                cmd.BeginSample(m_DepthPassTag);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
                var drawSettings = CreateDrawingSettings(m_ShaderTagId, ref renderingData, sortFlags);
                drawSettings.perObjectData = PerObjectData.None;

                ref CameraData cameraData = ref renderingData.cameraData;
                Camera camera = cameraData.camera;
                if (cameraData.isStereoEnabled)
                    context.StartMultiEye(camera);

                FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filteringSettings);

                cmd.EndSample(m_DepthPassTag);
                context.ExecuteCommandBuffer(cmd); // Execute EndSample command
                cmd.Clear();
            }
            else
            {
                cmd.ClearRenderTarget(false, m_renderTextureChannelIndex == 0, clearColor);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            }
            m_shadowBuffer.CollectShadowBuffer(context, ref renderingData);

            cmd.EndSample(m_ProfilerTag);
            context.ExecuteCommandBuffer(cmd); // Execute EndSample command
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
		public override void FrameCleanup(CommandBuffer cmd)
		{
			base.FrameCleanup(cmd);
		}

        static bool s_depthCopyAvailable = false;
        static bool s_isDepthCopyAvailabilityChecked = false;
        static bool IsDepthTextureCopySupported()
        {
            if (!s_isDepthCopyAvailabilityChecked)
            {
                s_depthCopyAvailable = (SystemInfo.copyTextureSupport != CopyTextureSupport.None) || SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth);
            }
            return s_depthCopyAvailable;
        }
        static bool ForwardRendererRequiresDepthOnlyPass(RenderingData renderingData)
        {
            // this function must return the same value as 'requiresDepthPrepass' local variable used in ForwardRenderer::Setup function.
            if (renderingData.cameraData.isSceneViewCamera)
            {
                return true;
            }
            if (renderingData.cameraData.requiresDepthTexture)
            {
                if (renderingData.cameraData.cameraTargetDescriptor.msaaSamples > 1)
                {
                    return true;
                }
                if (!IsDepthTextureCopySupported())
                {
                    return true;
                }
                // TODO: There's an issue in multiview and depth copy pass. Atm forcing a depth prepass on XR until
                // we have a proper fix.
                // (See Setup function in ForwardRenderer.cs)
                if (renderingData.cameraData.isStereoEnabled)
                {
                    return true;
                }
            }
            // we have to check if the scene requires to create screen space shadow texture,
            // but it is too complecate to check if the main light shadow is visible or not here,
            // we assume that the scene does not use main light shadow.
            // (because we are using projectors for shadows)
            return false;
        }
    }
}
