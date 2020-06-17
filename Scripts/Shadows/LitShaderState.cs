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
		const string KEYWORD_MAIN_LIGHT_SHADOW = "P4LWRP_MAIN_LIGHT_SHADOWS";
		const string KEYWORD_ADDITIONAL_LIGHT_SHADOW = "P4LWRP_ADDITIONAL_LIGHT_SHADOWS";
		const string KEYWORD_ADDITIONAL_LIGHT_SHADOW_SINGLE_TEX = "P4LWRP_ADDITIONAL_LIGHT_SHADOWS_SINGLE_TEX";
		const string KEYWORD_ADDITIONAL_LIGHT_SHADOWS_DOUBLE_TEX = "P4LWRP_ADDITIONAL_LIGHT_SHADOWS_DOUBLE_TEX";
		static Texture p4lwrp_shadowBufferTex = null;
		static Texture p4lwrp_additionalShadowBufferTex = null;
		static Vector4[] p4lwrp_additionalLightShadowChannelIndex;
		static int p4lwrp_shadowBufferTexId;
		static int p4lwrp_additionalShadowBufferTexId;
		static int p4lwrp_additionalLightShadowChannelIndexId;
		static bool s_mainTextureHasAdditionalShadow;
		static bool s_mainLightShadowEnabled;

		// collect shadows pass
		const string KEYWORD_COLLECT_MAINLIGHT_SHADOWS = "P4LWRP_COLLECT_MAINLIGHT_SHADOWS";
		const string KEYWORD_COLLECT_ADDITIONALLIGHT_SHADOWS = "P4LWRP_COLLECT_ADDITIONALLIGHT_SHADOWS";
		const string KEYWORD_COLLECT_SHADOWMASK_R = "P4LWRP_COLLECT_SHADOWMASK_R";
		const string KEYWORD_COLLECT_SHADOWMASK_G = "P4LWRP_COLLECT_SHADOWMASK_G";
		const string KEYWORD_COLLECT_SHADOWMASK_B = "P4LWRP_COLLECT_SHADOWMASK_B";
		const string KEYWORD_COLLECT_SHADOWMASK_A = "P4LWRP_COLLECT_SHADOWMASK_A";
		static Vector4[] p4lwrp_shadowMaskWriteMasks;
		static Vector4[] p4lwrp_shadowMaskWriteMasksInv;
		static Vector4[] p4lwrp_additionalLightShadowWriteMask;
		static int p4lwrp_shadowMaskWriteMasksId;
		static int p4lwrp_shadowMaskWriteMasksInvId;
		static int p4lwrp_additionalLightShadowWriteMaskId;
		static LayerMask s_shadowReceiverLayers;
		static bool s_collectMainLightShadows;
		static bool s_collectAdditionalLightShadows;
		static byte s_collectShadowmaskChannels;
		static ShaderTagId s_collectShadowsPassName;
		static LitShaderState()
		{
			p4lwrp_additionalLightShadowChannelIndex = new Vector4[MAX_VISIBLE_LIGHTS];
			p4lwrp_shadowBufferTexId = Shader.PropertyToID("p4lwrp_shadowBufferTex");
			p4lwrp_additionalShadowBufferTexId = Shader.PropertyToID("p4lwrp_additionalShadowBufferTex");
			p4lwrp_additionalLightShadowChannelIndexId = Shader.PropertyToID("p4lwrp_additionalLightShadowChannelIndex");

			// collect shadows pass
			p4lwrp_shadowMaskWriteMasks = new Vector4[4];
			p4lwrp_shadowMaskWriteMasksInv = new Vector4[4];
			p4lwrp_additionalLightShadowWriteMask = new Vector4[MAX_VISIBLE_LIGHTS];
			p4lwrp_shadowMaskWriteMasksId = Shader.PropertyToID("p4lwrp_shadowMaskWriteMasks");
			p4lwrp_shadowMaskWriteMasksInvId = Shader.PropertyToID("p4lwrp_shadowMaskWriteMasksInv");
			p4lwrp_additionalLightShadowWriteMaskId = Shader.PropertyToID("p4lwrp_additionalLightShadowWriteMask");
			s_collectShadowsPassName = new ShaderTagId("P4LWRPCollectShadows");
		}
		static void ClearAdditionalLightChannelMask()
		{
			for (int i = 0; i < MAX_VISIBLE_LIGHTS; ++i)
			{
				p4lwrp_additionalLightShadowChannelIndex[i] = Vector4.one;
			}
		}
		public static void ClearStates()
		{
			p4lwrp_shadowBufferTex = null;
			p4lwrp_additionalShadowBufferTex = null;
			ClearAdditionalLightChannelMask();
			s_mainTextureHasAdditionalShadow = false;
			s_mainLightShadowEnabled = false;
		}
		public static void SetupStates(CommandBuffer cmd)
		{
			if (p4lwrp_shadowBufferTex != null)
			{
				cmd.SetGlobalTexture(p4lwrp_shadowBufferTexId, p4lwrp_shadowBufferTex);
			}
			if (s_mainLightShadowEnabled)
			{
				cmd.EnableShaderKeyword(KEYWORD_MAIN_LIGHT_SHADOW);
			}
			else
			{
				cmd.DisableShaderKeyword(KEYWORD_MAIN_LIGHT_SHADOW);
			}
			if (p4lwrp_additionalShadowBufferTex != null)
			{
				if (p4lwrp_shadowBufferTex == p4lwrp_additionalShadowBufferTex)
				{
					cmd.DisableShaderKeyword(KEYWORD_ADDITIONAL_LIGHT_SHADOW);
					cmd.EnableShaderKeyword(KEYWORD_ADDITIONAL_LIGHT_SHADOW_SINGLE_TEX);
					cmd.DisableShaderKeyword(KEYWORD_ADDITIONAL_LIGHT_SHADOWS_DOUBLE_TEX);
				}
				else if (s_mainTextureHasAdditionalShadow && p4lwrp_shadowBufferTex != null)
				{
					cmd.DisableShaderKeyword(KEYWORD_ADDITIONAL_LIGHT_SHADOW);
					cmd.DisableShaderKeyword(KEYWORD_ADDITIONAL_LIGHT_SHADOW_SINGLE_TEX);
					cmd.EnableShaderKeyword(KEYWORD_ADDITIONAL_LIGHT_SHADOWS_DOUBLE_TEX);
				}
				else
				{
					cmd.EnableShaderKeyword(KEYWORD_ADDITIONAL_LIGHT_SHADOW);
					cmd.DisableShaderKeyword(KEYWORD_ADDITIONAL_LIGHT_SHADOW_SINGLE_TEX);
					cmd.DisableShaderKeyword(KEYWORD_ADDITIONAL_LIGHT_SHADOWS_DOUBLE_TEX);
				}
				cmd.SetGlobalTexture(p4lwrp_additionalShadowBufferTexId, p4lwrp_additionalShadowBufferTex);
				cmd.SetGlobalVectorArray(p4lwrp_additionalLightShadowChannelIndexId, p4lwrp_additionalLightShadowChannelIndex);
			}
			else
			{
				cmd.DisableShaderKeyword(KEYWORD_ADDITIONAL_LIGHT_SHADOW);
				cmd.DisableShaderKeyword(KEYWORD_ADDITIONAL_LIGHT_SHADOW_SINGLE_TEX);
			}
		}
		public static void SetMainLightShadow(Texture shadowTexture, LayerMask collectRealtimeShadowLayers, int shadowMaskChannel = -1)
		{
			s_mainLightShadowEnabled = true;
			p4lwrp_shadowBufferTex = shadowTexture;
			// TODO: Multiply channel mask by shadow strength
			if (collectRealtimeShadowLayers != 0)
			{
				s_shadowReceiverLayers |= collectRealtimeShadowLayers;
				s_collectMainLightShadows = true;
				CollectShadowmask(shadowMaskChannel, 3);
			}
		}
		private static void CollectShadowmask(int shadowMaskChannel, int channelIndex)
		{
			if (0 <= shadowMaskChannel)
			{
				s_collectShadowmaskChannels |= (byte)(1 << shadowMaskChannel);
				p4lwrp_shadowMaskWriteMasks[shadowMaskChannel] = Vector4.zero;
				p4lwrp_shadowMaskWriteMasks[shadowMaskChannel][channelIndex] = 1.0f;
				p4lwrp_shadowMaskWriteMasksInv[shadowMaskChannel] = Vector4.one;
				p4lwrp_shadowMaskWriteMasksInv[shadowMaskChannel][channelIndex] = 0.0f;
			}
		}
		public static bool SetAdditionalLightShadow(int lightIndex, Texture shadowTexture, int colorChannel, LayerMask collectRealtimeShadowLayers, int shadowMaskChannel = -1)
		{
			Debug.Assert(lightIndex < MAX_VISIBLE_LIGHTS);
			if (shadowTexture == p4lwrp_shadowBufferTex)
			{
			}
			else if (p4lwrp_additionalShadowBufferTex == null)
			{
				p4lwrp_additionalShadowBufferTex = shadowTexture;
			}
			else if (p4lwrp_additionalShadowBufferTex != shadowTexture)
			{
				return false;
			}
			// TODO: Multiply channel mask by shadow strength 
			int channelIndex = 3 - colorChannel;
			if (shadowTexture == p4lwrp_additionalShadowBufferTex)
			{
				p4lwrp_additionalLightShadowChannelIndex[lightIndex].x = channelIndex;
				p4lwrp_additionalLightShadowChannelIndex[lightIndex].y = 0;
			}
			else
			{
				s_mainTextureHasAdditionalShadow = true;
				p4lwrp_additionalLightShadowChannelIndex[lightIndex].z = channelIndex;
				p4lwrp_additionalLightShadowChannelIndex[lightIndex].w = 0;
			}
			if (collectRealtimeShadowLayers != 0)
			{
				s_shadowReceiverLayers |= collectRealtimeShadowLayers;
				s_collectAdditionalLightShadows = true;
				p4lwrp_additionalLightShadowWriteMask[lightIndex] = Vector4.zero;
				p4lwrp_additionalLightShadowWriteMask[lightIndex][channelIndex] = 1.0f;
				CollectShadowmask(shadowMaskChannel, channelIndex);
			}
			return true;
		}
		public static void BeginCollectShadows()
		{
			s_shadowReceiverLayers = 0;
			s_collectMainLightShadows = false;
			s_collectAdditionalLightShadows = false;
			s_collectShadowmaskChannels = 0;
			for (int i = 0; i < MAX_VISIBLE_LIGHTS; ++i)
			{
				p4lwrp_additionalLightShadowWriteMask[i] = Vector4.zero;
			}
		}
		const string COLLECT_REALTIMESHADOW_PASS = "Collect Realtime Shadows";
		public static void EndCollectShadowsForSingleTexture(ScriptableRenderContext context, ref RenderingData renderingData, Texture shadowBuffer)
		{
			if (s_shadowReceiverLayers != 0)
			{
				CommandBuffer cmd = CommandBufferPool.Get(COLLECT_REALTIMESHADOW_PASS);
				using (new ProfilingSample(cmd, COLLECT_REALTIMESHADOW_PASS))
				{
					if (s_collectMainLightShadows)
					{
						cmd.EnableShaderKeyword(KEYWORD_COLLECT_MAINLIGHT_SHADOWS);
					}
					else
					{
						cmd.DisableShaderKeyword(KEYWORD_COLLECT_MAINLIGHT_SHADOWS);
					}
					if (s_collectAdditionalLightShadows)
					{
						cmd.EnableShaderKeyword(KEYWORD_COLLECT_ADDITIONALLIGHT_SHADOWS);
						cmd.SetGlobalVectorArray(p4lwrp_additionalLightShadowWriteMaskId, p4lwrp_additionalLightShadowWriteMask);
					}
					else
					{
						cmd.DisableShaderKeyword(KEYWORD_COLLECT_ADDITIONALLIGHT_SHADOWS);
					}
					if ((s_collectShadowmaskChannels & ((1 << 4) - 1)) != 0)
					{
						cmd.SetGlobalVectorArray(p4lwrp_shadowMaskWriteMasksId, p4lwrp_shadowMaskWriteMasks);
						cmd.SetGlobalVectorArray(p4lwrp_shadowMaskWriteMasksInvId, p4lwrp_shadowMaskWriteMasksInv);
						if ((s_collectShadowmaskChannels & (1 << 0)) != 0)
						{
							cmd.EnableShaderKeyword(KEYWORD_COLLECT_SHADOWMASK_R);
						}
						else
						{
							cmd.DisableShaderKeyword(KEYWORD_COLLECT_SHADOWMASK_R);
						}
						if ((s_collectShadowmaskChannels & (1 << 1)) != 0)
						{
							cmd.EnableShaderKeyword(KEYWORD_COLLECT_SHADOWMASK_G);
						}
						else
						{
							cmd.DisableShaderKeyword(KEYWORD_COLLECT_SHADOWMASK_G);
						}
						if ((s_collectShadowmaskChannels & (1 << 2)) != 0)
						{
							cmd.EnableShaderKeyword(KEYWORD_COLLECT_SHADOWMASK_B);
						}
						else
						{
							cmd.DisableShaderKeyword(KEYWORD_COLLECT_SHADOWMASK_B);
						}
						if ((s_collectShadowmaskChannels & (1 << 3)) != 0)
						{
							cmd.EnableShaderKeyword(KEYWORD_COLLECT_SHADOWMASK_A);
						}
						else
						{
							cmd.DisableShaderKeyword(KEYWORD_COLLECT_SHADOWMASK_A);
						}
					}
					context.ExecuteCommandBuffer(cmd);
					cmd.Clear();
					// render collect shadows pass
					PerObjectData perObjectData = PerObjectData.LightData;
					if (s_collectAdditionalLightShadows)
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
