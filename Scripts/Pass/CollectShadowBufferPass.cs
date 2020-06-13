//
// CollectShadowBufferPass.cs
//
// Projector For LWRP
//
// Copyright (c) 2020 NYAHOON GAMES PTE. LTD.
//

using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;
using UnityEngine;

namespace ProjectorForLWRP
{
	public class CollectShadowBufferPass : ScriptableRenderPass
	{
        internal class RenderTextureRef
        {
            private RenderTexture m_renderTexture = null;
            private ColorWriteMask m_colorMask = 0;
            public RenderTexture renderTexture { get { return m_renderTexture; } }
            public void CreateTemporaryTexture(int width, int height)
            {
                Clear();
                m_renderTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
                m_renderTexture.filterMode = FilterMode.Point;
            }
            public void Retain(ColorWriteMask color)
            {
                Debug.Assert((m_colorMask & color) == 0);
                m_colorMask |= color;
            }
            public void Release(ColorWriteMask color)
            {
                m_colorMask &= ~color;
                if (m_colorMask == 0)
                {
                    RenderTexture.ReleaseTemporary(m_renderTexture);
                    m_renderTexture = null;
                }
            }
            public void Clear()
            {
                if (m_renderTexture != null)
                {
                    RenderTexture.ReleaseTemporary(m_renderTexture);
                    m_renderTexture = null;
                    m_colorMask = 0;
                }
            }
        }

        const string m_ProfilerTag = "Collect Shadow Buffer Pass";
        const string m_DepthPassTag = "Depth Only Pass";
        ShaderTagId m_ShaderTagId = new ShaderTagId("DepthOnly");
        private List<ShadowBuffer> m_shadowBufferList = null;
        private int m_applyPassCount = 0;
        internal CollectShadowBufferPass()
		{
			renderPassEvent = RenderPassEvent.AfterRenderingPrePasses;
		}
        internal void SetShadowBuffers(List<ShadowBuffer> shadowBuffers, int applyPassCount)
        {
            Debug.Assert(shadowBuffers != null && 0 < shadowBuffers.Count);
            m_shadowBufferList = shadowBuffers;
            m_applyPassCount = applyPassCount;
        }
        private List<RenderTextureRef> m_renderTextureBuffer = new List<RenderTextureRef>();
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
		{
            Debug.Assert(m_shadowBufferList != null && 0 < m_shadowBufferList.Count);
            int width = cameraTextureDescriptor.width;
            int height = cameraTextureDescriptor.height;
            int texCount = (m_shadowBufferList.Count + 3) / 4;
            for (int i = 0; i < texCount; ++i)
            {
                RenderTextureRef textureRef;
                if (i < m_renderTextureBuffer.Count)
                {
                    textureRef = m_renderTextureBuffer[i];
                }
                else {
                    textureRef = new RenderTextureRef();
                    m_renderTextureBuffer.Add(textureRef);
                }
                textureRef.CreateTemporaryTexture(width, height);
            }
            RenderTargetIdentifier renderTargetId = new RenderTargetIdentifier(m_renderTextureBuffer[0].renderTexture);
            // we have to set render target here, because ScriptableRenderer does not set the depth buffer if it is BuiltinRenderTextureType.CameraTarget.
            RenderBufferStoreAction depthStoreAction = (texCount == 1) ? RenderBufferStoreAction.DontCare : RenderBufferStoreAction.Store;
            cmd.SetRenderTarget(renderTargetId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, depthAttachment, RenderBufferLoadAction.DontCare, depthStoreAction);
            Color clearColor = new Color(1, 1, 1, 1);
            ConfigureTarget(renderTargetId, BuiltinRenderTextureType.CameraTarget);
            ConfigureClear(ClearFlag.Color, clearColor);
		}

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
            if (4 < m_shadowBufferList.Count)
            {
                // if there are more than 4 shadow buffers, sort the list to combine important light shadows into the first texture.
                for (int i = 0, count = m_shadowBufferList.Count; i < count; ++i)
                {
                    m_shadowBufferList[i].CalculateSortIndex(ref renderingData);
                }
                m_shadowBufferList.Sort();
            }
            RenderTextureRef textureRef = m_renderTextureBuffer[0];
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            cmd.BeginSample(m_ProfilerTag);
            // ScriptableRenderer does not set a depth buffer if configured depth buffer is BuiltinRenderTextureType.CameraTarget.
            // this seems an intended behaviour. BuiltinRenderTextureType.CameraTarget is just a default value which means the depth buffer bound to the render texture is used.
            // so, we set render target again here. we have to call ClearRenderTarget twice though.
            // we cannot do ConfigureClear(ClearFlag.None, color) in Configure above because SetReRenderTarget will be called with RenderBufferLoadAction.Load.
            // that might be worse than clear.
            cmd.SetRenderTarget(new RenderTargetIdentifier(textureRef.renderTexture), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, depthAttachment, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            LitShaderState.ClearStates();
            // depth only pass does not use camera depth texture, but a temporary RT.
            // we cannot reuse it.
            // if (!ForwardRendererRequiresDepthOnlyPass(renderingData))
            {
                cmd.ClearRenderTarget(true, false, clearColor);
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
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            LitShaderState.BeginCollectShadows();
            for (int i = 0, colorChannelIndex = 0, count = m_shadowBufferList.Count; i < count; ++i, ++colorChannelIndex)
            {
                if (colorChannelIndex == 4)
                {
                    LitShaderState.EndCollectShadowsForSingleTexture(context, ref renderingData, textureRef.renderTexture);
                    colorChannelIndex = 0;
                    textureRef = m_renderTextureBuffer[i / 4];
                    RenderBufferStoreAction depthStoreAction = (i + 4 < count) ? RenderBufferStoreAction.Store : RenderBufferStoreAction.DontCare;
                    cmd.SetRenderTarget(new RenderTargetIdentifier(textureRef.renderTexture), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, depthAttachment, RenderBufferLoadAction.DontCare, depthStoreAction);
                    cmd.ClearRenderTarget(false, true, clearColor);
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                }
                m_shadowBufferList[i].CollectShadowBuffer(context, ref renderingData, textureRef, colorChannelIndex);
            }
            LitShaderState.EndCollectShadowsForSingleTexture(context, ref renderingData, textureRef.renderTexture);
            LitShaderState.SetupStates(cmd);
            cmd.EndSample(m_ProfilerTag);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
        public override void FrameCleanup(CommandBuffer cmd)
		{
            CleanupAfterRendering();
        }
        internal void ApplyPassFinished()
        {
            if (--m_applyPassCount <= 0)
            {
                CleanupAfterRendering();
                m_applyPassCount = 0;
            }
        }
        public void CleanupAfterRendering()
        {
            m_shadowBufferList.Clear();
            for (int i = 0, count = m_renderTextureBuffer.Count; i < count; ++i)
            {
                m_renderTextureBuffer[i].Clear();
            }
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
