//
// LitShaderState.cs
//
// Projector For LWRP
//
// Copyright (c) 2020 NYAHOON GAMES PTE. LTD.
//

using UnityEngine;
using UnityEngine.Rendering;

internal static class LitShaderState
{
	const int MAX_VISIBLE_LIGHTS = 16;
	const string KEYWORD_MAIN_LIGHT_SHADOW = "P4LWRP_MAIN_LIGHT_SHADOWS";
	const string KEYWORD_ADDITIONAL_LIGHT_SHADOW = "P4LWRP_ADDITIONAL_LIGHT_SHADOWS";
	const string KEYWORD_ADDITIONAL_LIGHT_SHADOW_SINGLE_TEX = "P4LWRP_ADDITIONAL_LIGHT_SHADOWS_SINGLE_TEX";
	static Texture p4lwrp_ShadowBufferTex = null;
	static Texture p4lwrp_AdditionalShadowBufferTex = null;
	static Vector4 p4lwrp_MainLightShadowChannelMask = Vector4.zero;
	static Vector4[] p4lwrp_AdditionalLightShadowChannelMask;
	static Vector4[] p4lwrp_AdditionalLightShadowAttenuationBase;
	static int p4lwrp_ShadowBufferTexId;
	static int p4lwrp_AdditionalShadowBufferTexId;
	static int p4lwrp_MainLightShadowChannelMaskId;
	static int p4lwrp_AdditionalLightShadowChannelMaskId;
	static int p4lwrp_AdditionalLightShadowAttenuationBaseId;

	static LitShaderState()
	{
		p4lwrp_AdditionalLightShadowChannelMask = new Vector4[MAX_VISIBLE_LIGHTS];
		p4lwrp_AdditionalLightShadowAttenuationBase = new Vector4[(MAX_VISIBLE_LIGHTS + 3)/4];
		p4lwrp_ShadowBufferTexId = Shader.PropertyToID("p4lwrp_ShadowBufferTex");
		p4lwrp_AdditionalShadowBufferTexId = Shader.PropertyToID("p4lwrp_AdditionalShadowBufferTex");
		p4lwrp_MainLightShadowChannelMaskId = Shader.PropertyToID("p4lwrp_MainLightShadowChannelMask");
		p4lwrp_AdditionalLightShadowChannelMaskId = Shader.PropertyToID("p4lwrp_AdditionalLightShadowChannelMask");
		p4lwrp_AdditionalLightShadowAttenuationBaseId = Shader.PropertyToID("p4lwrp_AdditionalLightShadowAttenuationBase");
		ClearAdditionalLightChannelMask();
	}
	static void ClearAdditionalLightChannelMask()
	{
		for (int i = 0; i < MAX_VISIBLE_LIGHTS; ++i)
		{
			p4lwrp_AdditionalLightShadowChannelMask[i] = Vector4.zero;
		}
		for (int i = 0, count = (MAX_VISIBLE_LIGHTS + 3) / 4; i < count ; ++i)
		{
			p4lwrp_AdditionalLightShadowAttenuationBase[i] = Vector4.one;
		}
	}
	public static void ClearStates()
	{
		p4lwrp_ShadowBufferTex = null;
		p4lwrp_AdditionalShadowBufferTex = null;
		ClearAdditionalLightChannelMask();
	}
	public static void SetupStates(CommandBuffer cmd)
	{
		if (p4lwrp_ShadowBufferTex != null)
		{
			cmd.EnableShaderKeyword(KEYWORD_MAIN_LIGHT_SHADOW);
			cmd.SetGlobalTexture(p4lwrp_ShadowBufferTexId, p4lwrp_ShadowBufferTex);
			cmd.SetGlobalVector(p4lwrp_MainLightShadowChannelMaskId, p4lwrp_MainLightShadowChannelMask);
		}
		else
		{
			cmd.DisableShaderKeyword(KEYWORD_MAIN_LIGHT_SHADOW);
		}
		if (p4lwrp_AdditionalShadowBufferTex != null)
		{
			if (p4lwrp_ShadowBufferTex == p4lwrp_AdditionalShadowBufferTex)
			{
				cmd.DisableShaderKeyword(KEYWORD_ADDITIONAL_LIGHT_SHADOW);
				cmd.EnableShaderKeyword(KEYWORD_ADDITIONAL_LIGHT_SHADOW_SINGLE_TEX);
			}
			else
			{
				cmd.EnableShaderKeyword(KEYWORD_ADDITIONAL_LIGHT_SHADOW);
				cmd.DisableShaderKeyword(KEYWORD_ADDITIONAL_LIGHT_SHADOW_SINGLE_TEX);
			}
			cmd.SetGlobalTexture(p4lwrp_AdditionalShadowBufferTexId, p4lwrp_AdditionalShadowBufferTex);
			cmd.SetGlobalVectorArray(p4lwrp_AdditionalLightShadowChannelMaskId, p4lwrp_AdditionalLightShadowChannelMask);
			cmd.SetGlobalVectorArray(p4lwrp_AdditionalLightShadowAttenuationBaseId, p4lwrp_AdditionalLightShadowAttenuationBase);
		}
		else
		{
			cmd.DisableShaderKeyword(KEYWORD_ADDITIONAL_LIGHT_SHADOW);
			cmd.DisableShaderKeyword(KEYWORD_ADDITIONAL_LIGHT_SHADOW_SINGLE_TEX);
		}
	}
	public static void SetMainLightShadow(Texture shadowTexture, int colorChannel)
	{
		p4lwrp_ShadowBufferTex = shadowTexture;
		p4lwrp_MainLightShadowChannelMask = Vector4.zero;
		p4lwrp_MainLightShadowChannelMask[3 - colorChannel] = 1.0f;
	}
	public static bool SetAdditionalLightShadow(int lightIndex, Texture shadowTexture, int colorChannel)
	{
		Debug.Assert(lightIndex < MAX_VISIBLE_LIGHTS);
		if (p4lwrp_AdditionalShadowBufferTex != null && p4lwrp_AdditionalShadowBufferTex != shadowTexture)
		{
			return false;
		}
		p4lwrp_AdditionalShadowBufferTex = shadowTexture;
		p4lwrp_AdditionalLightShadowChannelMask[lightIndex] = Vector4.zero;
		p4lwrp_AdditionalLightShadowChannelMask[lightIndex][3 - colorChannel] = 1.0f;
		p4lwrp_AdditionalLightShadowAttenuationBase[lightIndex >> 2][lightIndex & 3] = 0.0f;
		return true;
	}
}
