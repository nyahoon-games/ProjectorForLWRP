//
// OverrideShadowFunctions.hlsl
//
// Projector For LWRP
//
// Copyright (c) 2020 NYAHOON GAMES PTE. LTD.
//

#if !defined(P4LWRP_OVERRIDESHADOWFUNCTIONS_HLSL_INCLUDED)
#define P4LWRP_OVERRIDESHADOWFUNCTIONS_HLSL_INCLUDED

#if defined(P4LWRP_ADDITIONAL_LIGHT_SHADOWS_SINGLE_TEX) && !defined(P4LWRP_ADDITIONAL_LIGHT_SHADOWS)
#define P4LWRP_ADDITIONAL_LIGHT_SHADOWS
#endif

#if (defined(P4LWRP_MAIN_LIGHT_SHADOWS) || defined(P4LWRP_ADDITIONAL_LIGHT_SHADOWS)) && !defined(_RECEIVE_SHADOWS_OFF)

#define SHADOWS_SCREEN 1
#define _MAIN_LIGHT_SHADOWS

#define MainLightRealtimeShadow _p4lwrp_discard_MainLightRealtimeShadow
#define AdditionalLightRealtimeShadow _p4lwrp_discard_AdditionalLightRealtimeShadow

#include "../UnityMacros.cginc"
#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Shadows.hlsl"

half __P4LWRP_CheckFunctionsExist(float4 shadowCoord, int lightIndex, float3 positionWS)
{
	half a = MainLightRealtimeShadow(shadowCoord);
	half b = AdditionalLightRealtimeShadow(lightIndex, positionWS);
	return a + b;
}
#undef MainLightRealtimeShadow
#undef AdditionalLightRealtimeShadow

sampler2D p4lwrp_ShadowBufferTex;
sampler2D p4lwrp_AdditionalShadowBufferTex;
fixed4 p4lwrp_MainLightShadowChannelMask;
fixed4 p4lwrp_AdditionalLightShadowChannelMask[MAX_VISIBLE_LIGHTS];

static fixed4 g_p4lwrp_additionalLightShadowColor;
void FetchAdditionalLightShadow(float4 shadowCoord)
{
#if !defined(P4LWRP_ADDITIONAL_LIGHT_SHADOWS)
	g_p4lwrp_additionalLightShadowColor = tex2Dproj(p4lwrp_AdditionalShadowBufferTex, UNITY_PROJ_COORD(shadowCoord));
#endif
}

fixed MainLightRealtimeShadow(float4 shadowCoord)
{
	FetchAdditionalLightShadow(shadowCoord); // assuming that MainLightRealtimeShadow is called before AdditionalLightRealtimeShadow
#if !defined(P4LWRP_MAIN_LIGHT_SHADOWS)
	return 1.0h;
#endif
#if defined(P4LWRP_ADDITIONAL_LIGHT_SHADOWS_SINGLE_TEX) && defined(P4LWRP_ADDITIONAL_LIGHT_SHADOWS)
	fixed4 shadowTex = g_p4lwrp_additionalLightShadowColor;
#else
	fixed4 shadowTex = tex2Dproj(p4lwrp_ShadowBufferTex, UNITY_PROJ_COORD(shadowCoord));
#endif
	return dot(shadowTex, p4lwrp_MainLightShadowChannelMask);
}

half AdditionalLightRealtimeShadow(int lightIndex, float3 positionWS)
{
#if !defined(P4LWRP_ADDITIONAL_LIGHT_SHADOWS)
	return 1.0h;
#endif
	fixed4 shadowTex = g_p4lwrp_additionalLightShadowColor;
	return dot(shadowTex, p4lwrp_AdditionalLightShadowChannelMask[lightIndex]);
}
#else
#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Shadows.hlsl"
#endif // defined(P4LWRP_SHADOWBUFFER_SHADOWS)

#endif // !defined(P4LWRP_OVERRIDESHADOWFUNCTIONS_HLSL_INCLUDED)
