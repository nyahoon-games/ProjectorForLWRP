//
// LitShaderState.cs
//
// Projector For LWRP
//
// Copyright (c) 2020 NYAHOON GAMES PTE. LTD.
//

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;

namespace ProjectorForLWRP
{
	internal static class LitShaderState
	{
		const int MAX_VISIBLE_LIGHTS = 16;
		static Texture m_shadowBufferTex = null;
		static Texture m_additionalShadowBufferTex = null;
		static Vector4[] m_additionalLightShadowChannelIndex;
		static bool s_mainTextureHasAdditionalShadow;
		static ShaderKeywords.LitShader.MainLightShadows s_mainLightShadows;

		// collect shadows pass
		static Vector4[] m_shadowMaskWriteMasks;
		static Vector4[] m_shadowMaskWriteMasksInv;
		static Vector4[] m_additionalLightShadowWriteMask;
		static LayerMask s_shadowReceiverLayers;
		static RenderingLayerMask s_shadowReceiverRenderingLayerMask;
		static ShaderKeywords.LitShader.CollectMainLightShadows s_collectMainLightShadows;
		static ShaderKeywords.LitShader.CollectAdditionalLightShadows s_collectAdditionalLightShadows;
		static byte s_collectShadowmaskChannels;
		static ShaderTagId s_collectShadowsPassName;

		static bool s_statesCleared = false;
		static bool s_statesDirty = true;

		static LitShaderState()
		{
			ShaderKeywords.LitShader.Activate();

			m_additionalLightShadowChannelIndex = new Vector4[MAX_VISIBLE_LIGHTS];

			// collect shadows pass
			m_shadowMaskWriteMasks = new Vector4[4];
			m_shadowMaskWriteMasksInv = new Vector4[4];
			m_additionalLightShadowWriteMask = new Vector4[MAX_VISIBLE_LIGHTS];
			s_collectShadowsPassName = new ShaderTagId("P4LWRPCollectShadows");
		}
		static void ClearAdditionalLightChannelMask()
		{
			for (int i = 0; i < MAX_VISIBLE_LIGHTS; ++i)
			{
				m_additionalLightShadowChannelIndex[i] = Vector4.one;
			}
		}
		public static void ClearStates()
		{
			if (!s_statesCleared)
			{
				m_shadowBufferTex = null;
				m_additionalShadowBufferTex = null;
				ClearAdditionalLightChannelMask();
				s_mainTextureHasAdditionalShadow = false;
				s_mainLightShadows = ShaderKeywords.LitShader.MainLightShadows.Off;
				s_statesCleared = true;
				s_statesDirty = true;
			}
		}
		public static bool ClearStates(CommandBuffer cmd)
		{
			ClearStates();
			return SetupStates(cmd);
		}
		public static bool SetupStates(CommandBuffer cmd)
		{
			if (!s_statesDirty)
			{
				return false;
			}
			if (m_shadowBufferTex != null)
			{
				P4LWRPShaderProperties.p4lwrp_shadowBufferTex.SetGlobal(cmd, m_shadowBufferTex);
			}
			ShaderUtils.SetGlobalKeyword(cmd, s_mainLightShadows);
			ShaderKeywords.LitShader.AdditionalLightShadowsTexture additionalShadowTexture = ShaderKeywords.LitShader.AdditionalLightShadowsTexture.None;
			if (m_additionalShadowBufferTex != null || s_mainTextureHasAdditionalShadow)
			{
				if (m_shadowBufferTex == m_additionalShadowBufferTex)
				{
					additionalShadowTexture = ShaderKeywords.LitShader.AdditionalLightShadowsTexture.MainLightTexture;
				}
				else if (s_mainTextureHasAdditionalShadow && m_shadowBufferTex != null)
				{
					additionalShadowTexture = ShaderKeywords.LitShader.AdditionalLightShadowsTexture.Both;
				}
				else
				{
					additionalShadowTexture = ShaderKeywords.LitShader.AdditionalLightShadowsTexture.AdditionalTexture;
				}
				P4LWRPShaderProperties.p4lwrp_additionalShadowBufferTex.SetGlobal(cmd, m_additionalShadowBufferTex);
				P4LWRPShaderProperties.p4lwrp_additionalLightShadowChannelIndex.SetGlobal(cmd, m_additionalLightShadowChannelIndex);
			}
			ShaderUtils.SetGlobalKeyword(cmd, additionalShadowTexture);
			return true;
		}
		public static void SetMainLightShadow(Texture shadowTexture, LayerMask collectRealtimeShadowLayers, RenderingLayerMask collectRealtimeShadowRenderingLayerMask, int shadowMaskChannel = -1)
		{
			s_statesCleared = false;
			s_statesDirty = true;
			s_mainLightShadows = ShaderKeywords.LitShader.MainLightShadows.On;
			m_shadowBufferTex = shadowTexture;
			// TODO: Multiply channel mask by shadow strength
			if (collectRealtimeShadowLayers != 0 && collectRealtimeShadowRenderingLayerMask != 0)
			{
				s_shadowReceiverRenderingLayerMask |= collectRealtimeShadowRenderingLayerMask;
				s_shadowReceiverLayers |= collectRealtimeShadowLayers;
				s_collectMainLightShadows = ShaderKeywords.LitShader.CollectMainLightShadows.On;
				CollectShadowmask(shadowMaskChannel, 3);
			}
		}
		private static void CollectShadowmask(int shadowMaskChannel, int channelIndex)
		{
			if (0 <= shadowMaskChannel)
			{
				s_collectShadowmaskChannels |= (byte)(1 << shadowMaskChannel);
				m_shadowMaskWriteMasks[shadowMaskChannel] = Vector4.zero;
				m_shadowMaskWriteMasks[shadowMaskChannel][channelIndex] = 1.0f;
				m_shadowMaskWriteMasksInv[shadowMaskChannel] = Vector4.one;
				m_shadowMaskWriteMasksInv[shadowMaskChannel][channelIndex] = 0.0f;
			}
		}
		public static bool SetAdditionalLightShadow(int lightIndex, Texture shadowTexture, int colorChannel, LayerMask collectRealtimeShadowLayers, RenderingLayerMask collectRealtimeShadowRenderingLayerMask, int shadowMaskChannel = -1)
		{
			Debug.Assert(lightIndex < MAX_VISIBLE_LIGHTS);
			if (shadowTexture == m_shadowBufferTex)
			{
			}
			else if (m_additionalShadowBufferTex == null)
			{
				m_additionalShadowBufferTex = shadowTexture;
			}
			else if (m_additionalShadowBufferTex != shadowTexture)
			{
				return false;
			}
			s_statesCleared = false;
			s_statesDirty = true;
			// TODO: Multiply channel mask by shadow strength 
			int channelIndex = 3 - colorChannel;
			if (shadowTexture == m_additionalShadowBufferTex)
			{
				m_additionalLightShadowChannelIndex[lightIndex].x = channelIndex;
				m_additionalLightShadowChannelIndex[lightIndex].y = 0;
			}
			else
			{
				s_mainTextureHasAdditionalShadow = true;
				m_additionalLightShadowChannelIndex[lightIndex].z = channelIndex;
				m_additionalLightShadowChannelIndex[lightIndex].w = 0;
			}
			if (collectRealtimeShadowLayers != 0 && collectRealtimeShadowRenderingLayerMask != 0)
			{
				s_shadowReceiverLayers |= collectRealtimeShadowLayers;
				s_shadowReceiverRenderingLayerMask |= collectRealtimeShadowRenderingLayerMask;
				s_collectAdditionalLightShadows = ShaderKeywords.LitShader.CollectAdditionalLightShadows.On;
				m_additionalLightShadowWriteMask[lightIndex] = Vector4.zero;
				m_additionalLightShadowWriteMask[lightIndex][channelIndex] = 1.0f;
				CollectShadowmask(shadowMaskChannel, channelIndex);
			}
			return true;
		}
		public static void BeginCollectShadows()
		{
			s_shadowReceiverLayers = 0;
			s_shadowReceiverRenderingLayerMask = 0;
			s_collectMainLightShadows = ShaderKeywords.LitShader.CollectMainLightShadows.Off;
			s_collectAdditionalLightShadows = ShaderKeywords.LitShader.CollectAdditionalLightShadows.Off;
			s_collectShadowmaskChannels = 0;
			for (int i = 0; i < MAX_VISIBLE_LIGHTS; ++i)
			{
				m_additionalLightShadowWriteMask[i] = Vector4.zero;
			}
		}
		const string COLLECT_REALTIMESHADOW_PASS = "Collect Realtime Shadows";
		public static void EndCollectShadowsForSingleTexture(ScriptableRenderContext context, ref RenderingData renderingData, Texture shadowBuffer)
		{
			if (s_shadowReceiverLayers != 0 && s_shadowReceiverRenderingLayerMask != 0)
			{
				CommandBuffer cmd = CommandBufferPool.Get(COLLECT_REALTIMESHADOW_PASS);
				using (new ProfilingSample(cmd, COLLECT_REALTIMESHADOW_PASS))
				{
					ShaderUtils.SetGlobalKeyword(cmd, s_collectMainLightShadows);
					ShaderUtils.SetGlobalKeyword(cmd, s_collectAdditionalLightShadows);
					if (m_additionalLightShadowWriteMask != null)
					{
						P4LWRPShaderProperties.p4lwrp_additionalLightShadowWriteMask.SetGlobal(cmd, m_additionalLightShadowWriteMask);
					}
					if ((s_collectShadowmaskChannels & ((1 << 4) - 1)) != 0)
					{
						P4LWRPShaderProperties.p4lwrp_shadowMaskWriteMasks.SetGlobal(cmd, m_shadowMaskWriteMasks);
						P4LWRPShaderProperties.p4lwrp_shadowMaskWriteMasksInv.SetGlobal(cmd, m_shadowMaskWriteMasksInv);
						ShaderUtils.SetGlobalKeywordFlag<ShaderKeywords.LitShader.CollectShadowmaskR>(cmd, (s_collectShadowmaskChannels & (1 << 0)) != 0);
						ShaderUtils.SetGlobalKeywordFlag<ShaderKeywords.LitShader.CollectShadowmaskG>(cmd, (s_collectShadowmaskChannels & (1 << 1)) != 0);
						ShaderUtils.SetGlobalKeywordFlag<ShaderKeywords.LitShader.CollectShadowmaskB>(cmd, (s_collectShadowmaskChannels & (1 << 2)) != 0);
						ShaderUtils.SetGlobalKeywordFlag<ShaderKeywords.LitShader.CollectShadowmaskA>(cmd, (s_collectShadowmaskChannels & (1 << 3)) != 0);
					}
					context.ExecuteCommandBuffer(cmd);
					cmd.Clear();
					// render collect shadows pass
					PerObjectData perObjectData = PerObjectData.LightData;
					if (s_collectAdditionalLightShadows == ShaderKeywords.LitShader.CollectAdditionalLightShadows.On)
					{
						perObjectData |= PerObjectData.LightIndices;
					}
					if (s_collectShadowmaskChannels != 0)
					{
						perObjectData |= PerObjectData.ShadowMask;
					}
					DrawingSettings drawingSettings = new DrawingSettings(s_collectShadowsPassName, new SortingSettings(renderingData.cameraData.camera));
					drawingSettings.enableDynamicBatching = renderingData.supportsDynamicBatching;
					drawingSettings.enableInstancing = true;
					drawingSettings.perObjectData = perObjectData;
					FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque, renderingData.cameraData.camera.cullingMask & s_shadowReceiverLayers);
					filteringSettings.renderingLayerMask = (uint)s_shadowReceiverRenderingLayerMask;
					context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
				}
				context.ExecuteCommandBuffer(cmd);
				cmd.Clear();
				CommandBufferPool.Release(cmd);
				// clear states for next texture
				BeginCollectShadows();
			}
		}
	}
}
