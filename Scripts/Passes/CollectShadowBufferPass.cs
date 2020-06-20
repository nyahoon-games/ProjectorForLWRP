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
        public interface ICameraToShadowBufferListMap
        {
            IList<ShadowBuffer> this[Camera camera] { get; }
        }
        public CollectShadowBufferPass(ICameraToShadowBufferListMap cameraToShadowBufferMap)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingPrePasses;
            m_cameraToShadowBufferMap = cameraToShadowBufferMap;
        }

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

        private ICameraToShadowBufferListMap m_cameraToShadowBufferMap;

        const string PROFILER_TAG_COLLECTSHADOWBUFFER = "P4LWRP Collect Shadow Buffer Pass";
        ShaderTagId m_ShaderTagId = new ShaderTagId("DepthOnly");
        private List<RenderTextureRef> m_renderTextureBuffer = new List<RenderTextureRef>();
        private Queue<ShadowBuffer> m_coloredShadowBufferQueue = new Queue<ShadowBuffer>();
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
		{
            int width = cameraTextureDescriptor.width;
            int height = cameraTextureDescriptor.height;
            RenderTextureRef textureRef;
            if (0 < m_renderTextureBuffer.Count)
            {
                textureRef = m_renderTextureBuffer[0];
            }
            else
            {
                textureRef = new RenderTextureRef();
                m_renderTextureBuffer.Add(textureRef);
            }
            textureRef.CreateTemporaryTexture(width, height);

            RenderTargetIdentifier renderTargetId = new RenderTargetIdentifier(m_renderTextureBuffer[0].renderTexture);
            Color clearColor = new Color(1, 1, 1, 1);
            ConfigureTarget(renderTargetId, BuiltinRenderTextureType.CameraTarget);
            ConfigureClear(ClearFlag.Color, clearColor);
		}

        private class ShadowBufferComparer : IComparer<ShadowBuffer>
        {
			public int Compare(ShadowBuffer lhs, ShadowBuffer rhs)
            {
                return lhs.sortIndex.CompareTo(rhs.sortIndex);
            }
        }
        static ShadowBufferComparer s_shadowBufferComparer = new ShadowBufferComparer();

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
            // Shadow buffers used by Lit shaders must be monochrome shadows (use only a single channel),
            // and they must be combined into up to 2 textures (maximum 8 lights).
            // Also, the main light shadow buffer must be stored in A channel.
            // If there are less than 5 shadow buffers which collect realtime shadows, they must be stored in a single texture.
            // If there are more than 4 shadow buffers for additional lights,
            // and they don't collect realtime shadows or main light shadow buffer doesn't collect realtime shadows,
            // 4 of them must be stored in a single texture.
            //
            // we would like to minimize the number of render textues under the above condition.
            //
            // first, we sort shadow buffers.
            //   main light (if collects realtime shadows) -> additional light with realtime shadows
            //      -> additional light -> main light (if doesn't collect realtime shadows)
            //      -> color shadow -> monochrome shadow
            //
            IList<ShadowBuffer> shadowBufferList = m_cameraToShadowBufferMap[renderingData.cameraData.camera];
            int shadowBufferCount = shadowBufferList.Count;
            int litShaderShadowBufferCount = 0;
            int realtimeShadowBufferCount = 0;
            int colorShadowBufferCount = 0;
            int monochromeShadowBufferCount = 0;
            ShadowBuffer mainLightShadowBuffer = null;
            var lightIndexMap = renderingData.cullResults.GetLightIndexMap(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < shadowBufferCount; ++i)
            {
                ShadowBuffer shadowBuffer = shadowBufferList[i];
                shadowBuffer.SetupLightSource(ref renderingData, ref lightIndexMap);
                if (!shadowBuffer.isVisible) {
                    continue;
                }
                if (shadowBuffer.shadowColor == ShadowBuffer.ShadowColor.Colored)
                {
                    ++colorShadowBufferCount;
                }
                else
                {
                    ++monochromeShadowBufferCount;
                    if (shadowBuffer.applyMethod == ShadowBuffer.ApplyMethod.ByLitShaders)
                    {
                        if (0 <= shadowBuffer.additionalLightIndex)
                        {
                            ++litShaderShadowBufferCount;
                        }
                        else if (shadowBuffer.isMainLight)
                        {
                            mainLightShadowBuffer = shadowBuffer;
                            ++litShaderShadowBufferCount;
                        }
                        if (shadowBuffer.collectRealtimeShadows)
                        {
                            ++realtimeShadowBufferCount;
                        }
                    }
                }
            }
            lightIndexMap.Dispose();
            HelperFunctions.GarbageFreeSort(shadowBufferList, s_shadowBufferComparer);
            if (8 < litShaderShadowBufferCount)
            {
                litShaderShadowBufferCount = 8;
            }
            int texCount = (litShaderShadowBufferCount + 3) / 4;
            monochromeShadowBufferCount -= litShaderShadowBufferCount;
            int mainLightTextureIndex = -1;
            if (mainLightShadowBuffer != null)
            {
                if (mainLightShadowBuffer.collectRealtimeShadows)
                {
                    mainLightTextureIndex = 0;
                }
                else
                {
                    mainLightTextureIndex = texCount - 1;
                }
            }
            if ((litShaderShadowBufferCount & 3) == 1 && 0 < colorShadowBufferCount)
            {
                --colorShadowBufferCount;
            }
            else
            {
                monochromeShadowBufferCount -= (litShaderShadowBufferCount & 3);
            }
            if (0 < colorShadowBufferCount)
            {
                texCount += colorShadowBufferCount;
                monochromeShadowBufferCount -= colorShadowBufferCount;
            }
            if (0 < monochromeShadowBufferCount)
            {
                texCount += (monochromeShadowBufferCount + 3) / 4;
            }

            RenderTextureRef textureRef = m_renderTextureBuffer[0];
            CommandBuffer cmd = CommandBufferPool.Get(PROFILER_TAG_COLLECTSHADOWBUFFER);
            cmd.BeginSample(PROFILER_TAG_COLLECTSHADOWBUFFER);
            // ScriptableRenderer does not set a depth buffer if configured depth buffer is BuiltinRenderTextureType.CameraTarget.
            // this seems an intended behaviour. BuiltinRenderTextureType.CameraTarget is just a default value which means the depth buffer bound to the render texture is used.
            // so, we set render target again here. we have to call ClearRenderTarget twice though.
            // we cannot do ConfigureClear(ClearFlag.None, color) in Configure above because SetReRenderTarget will be called with RenderBufferLoadAction.Load.
            // that might be worse than clear.
            RenderBufferStoreAction depthStoreAction = (1 < texCount) ? RenderBufferStoreAction.Store : RenderBufferStoreAction.DontCare;
            cmd.SetRenderTarget(new RenderTargetIdentifier(textureRef.renderTexture), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, depthAttachment, RenderBufferLoadAction.DontCare, depthStoreAction);
            LitShaderState.ClearStates();
            // depth only pass does not use camera depth texture, but a temporary RT.
            // we cannot reuse it.
            // if (!ForwardRendererRequiresDepthOnlyPass(renderingData))
            {
                cmd.ClearRenderTarget(true, false, clearColor);
                // draw depth only pass
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
                var drawSettings = CreateDrawingSettings(m_ShaderTagId, ref renderingData, sortFlags);
                drawSettings.perObjectData = PerObjectData.None;

                FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filteringSettings);
            }
            //else {
            //    context.ExecuteCommandBuffer(cmd);
            //    cmd.Clear();
            //}

            int width = textureRef.renderTexture.width;
            int height = textureRef.renderTexture.height;
            LitShaderState.BeginCollectShadows();
            for (int i = 0, count = shadowBufferList.Count, colorChannelIndex = 0, texIndex = 0; i < count || 0 < m_coloredShadowBufferQueue.Count; )
            {
                if (colorChannelIndex == 4)
                {
                    LitShaderState.EndCollectShadowsForSingleTexture(context, ref renderingData, textureRef.renderTexture);
                    colorChannelIndex = 0;
                    ++texIndex;
                    if (m_renderTextureBuffer.Count == texIndex)
                    {
                        textureRef = new RenderTextureRef();
                        m_renderTextureBuffer.Add(textureRef);
                    }
                    else
                    {
                        textureRef = m_renderTextureBuffer[texIndex];
                    }
                    textureRef.CreateTemporaryTexture(width, height);
                    depthStoreAction = (texIndex < texCount) ? RenderBufferStoreAction.Store : RenderBufferStoreAction.DontCare;
                    cmd.SetRenderTarget(new RenderTargetIdentifier(textureRef.renderTexture), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, depthAttachment, RenderBufferLoadAction.DontCare, depthStoreAction);
                    cmd.ClearRenderTarget(false, true, clearColor);
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                }
                if (colorChannelIndex == 0 && texIndex == mainLightTextureIndex)
                {
                    mainLightShadowBuffer.CollectShadowBuffer(context, ref renderingData, textureRef, ColorWriteMask.Alpha);
                    ++colorChannelIndex;
                    --litShaderShadowBufferCount;
                }
                if (0 < litShaderShadowBufferCount)
                {
                    ShadowBuffer shadowBuffer = shadowBufferList[i++];
                    if (!shadowBuffer.isVisible)
                    {
                        continue;
                    }
                    Debug.Assert(shadowBuffer.shadowColor == ShadowBuffer.ShadowColor.Monochrome);
                    if (shadowBuffer != mainLightShadowBuffer)
                    {
                        shadowBuffer.CollectShadowBuffer(context, ref renderingData, textureRef, (ColorWriteMask)(1 << colorChannelIndex));
                        ++colorChannelIndex;
                        --litShaderShadowBufferCount;
                    }
                }
                else if ((colorChannelIndex == 1 || i == count) && 0 < m_coloredShadowBufferQueue.Count)
                {
                    if (0 < colorChannelIndex)
                    {
                        m_coloredShadowBufferQueue.Dequeue().CollectShadowBuffer(context, ref renderingData, textureRef, ColorWriteMask.All & ~ColorWriteMask.Alpha);
                        colorChannelIndex = 4;
                    }
                }
                else
                {
                    ShadowBuffer shadowBuffer = shadowBufferList[i++];
                    if (shadowBuffer != mainLightShadowBuffer && shadowBuffer.isVisible)
                    {
                        if (shadowBuffer.shadowColor == ShadowBuffer.ShadowColor.Monochrome)
                        {
                            shadowBuffer.CollectShadowBuffer(context, ref renderingData, textureRef, (ColorWriteMask)(1 << colorChannelIndex));
                            ++colorChannelIndex;
                        }
                        else
                        {
                            m_coloredShadowBufferQueue.Enqueue(shadowBuffer);
                        }
                    }
                }
            }
            LitShaderState.EndCollectShadowsForSingleTexture(context, ref renderingData, textureRef.renderTexture);
            LitShaderState.SetupStates(cmd);
            cmd.EndSample(PROFILER_TAG_COLLECTSHADOWBUFFER);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
        public override void FrameCleanup(CommandBuffer cmd)
		{
        }
        //static bool s_depthCopyAvailable = false;
        //static bool s_isDepthCopyAvailabilityChecked = false;
        //static bool IsDepthTextureCopySupported()
        //{
        //    if (!s_isDepthCopyAvailabilityChecked)
        //    {
        //        s_depthCopyAvailable = (SystemInfo.copyTextureSupport != CopyTextureSupport.None) || SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth);
        //    }
        //    return s_depthCopyAvailable;
        //}
        //static bool ForwardRendererRequiresDepthOnlyPass(RenderingData renderingData)
        //{
        //    // this function must return the same value as 'requiresDepthPrepass' local variable used in ForwardRenderer::Setup function.
        //    if (renderingData.cameraData.isSceneViewCamera)
        //    {
        //        return true;
        //    }
        //    if (renderingData.cameraData.requiresDepthTexture)
        //    {
        //        if (renderingData.cameraData.cameraTargetDescriptor.msaaSamples > 1)
        //        {
        //            return true;
        //        }
        //        if (!IsDepthTextureCopySupported())
        //        {
        //            return true;
        //        }
        //        // TODO: There's an issue in multiview and depth copy pass. Atm forcing a depth prepass on XR until
        //        // we have a proper fix.
        //        // (See Setup function in ForwardRenderer.cs)
        //        if (renderingData.cameraData.isStereoEnabled)
        //        {
        //            return true;
        //        }
        //    }
        //    // we have to check if the scene requires to create screen space shadow texture,
        //    // but it is too complecate to check if the main light shadow is visible or not here,
        //    // we assume that the scene does not use main light shadow.
        //    // (because we are using projectors for shadows)
        //    return false;
        //}
    }
}
