//
// P4LWRPOverrideShadowFunctions.hlsl
//
// Projector For LWRP
//
// Copyright (c) 2020 NYAHOON GAMES PTE. LTD.
//

//
// [Keywords]
// main light shadows:
//     P4LWRP_MAIN_LIGHT_SHADOWS : cast main light shadows using shadow buffer texture
// addotional light shadows:
//     P4LWRP_ADDITIONAL_LIGHT_SHADOWS            : cast additional shadows using 2nd shadow buffer texture
//     P4LWRP_ADDITIONAL_LIGHT_SHADOWS_SINGLE_TEX : cast additional shadows using a single shadow buffer texture shared with main light
//

#if !defined(_P4LWRP_OVERRIDESHADOWFUNCTIONS_HLSL_INCLUDED)
#define _P4LWRP_OVERRIDESHADOWFUNCTIONS_HLSL_INCLUDED

#include "../P4LWRPUnityMacros.cginc"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

#if defined(P4LWRP_ADDITIONAL_LIGHT_SHADOWS_SINGLE_TEX) && !defined(P4LWRP_ADDITIONAL_LIGHT_SHADOWS)
#define P4LWRP_ADDITIONAL_LIGHT_SHADOWS
#endif

#if (defined(P4LWRP_MAIN_LIGHT_SHADOWS) || defined(P4LWRP_ADDITIONAL_LIGHT_SHADOWS)) && !defined(_RECEIVE_SHADOWS_OFF)
#define SHADOWS_SCREEN 1
#define _MAIN_LIGHT_SHADOWS // add shadowCode into Varyings

#define MainLightRealtimeShadow _p4lwrp_discard_MainLightRealtimeShadow
#define AdditionalLightRealtimeShadow _p4lwrp_discard_AdditionalLightRealtimeShadow

#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Shadows.hlsl"

half __P4LWRP_CheckFunctionsExist(float4 shadowCoord, int lightIndex, float3 positionWS)
{
	half a = MainLightRealtimeShadow(shadowCoord);
	half b = AdditionalLightRealtimeShadow(lightIndex, positionWS);
	return a + b;
}
#undef MainLightRealtimeShadow
#undef AdditionalLightRealtimeShadow

sampler2D p4lwrp_shadowBufferTex;
sampler2D p4lwrp_additionalShadowBufferTex;
fixed4 p4lwrp_mainLightShadowChannelMask;
fixed4 p4lwrp_additionalLightShadowChannelMask[MAX_VISIBLE_LIGHTS];
fixed4 p4lwrp_additionalLightShadowAttenuationBase[(MAX_VISIBLE_LIGHTS + 3)/4];

static fixed4 g_p4lwrp_additionalLightShadowColor;
void FetchAdditionalLightShadow(float4 shadowCoord)
{
#if defined(P4LWRP_ADDITIONAL_LIGHT_SHADOWS)
	g_p4lwrp_additionalLightShadowColor = tex2Dproj(p4lwrp_additionalShadowBufferTex, UNITY_PROJ_COORD(shadowCoord));
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
	fixed4 shadowTex = tex2Dproj(p4lwrp_shadowBufferTex, UNITY_PROJ_COORD(shadowCoord));
#endif
	return dot(shadowTex, p4lwrp_mainLightShadowChannelMask);
}

half AdditionalLightRealtimeShadow(int lightIndex, float3 positionWS)
{
#if !defined(P4LWRP_ADDITIONAL_LIGHT_SHADOWS)
	return 1.0h;
#endif
	fixed4 shadowTex = g_p4lwrp_additionalLightShadowColor;
	return dot(shadowTex, p4lwrp_additionalLightShadowChannelMask[lightIndex]) + p4lwrp_additionalLightShadowAttenuationBase[lightIndex/4][lightIndex&3];
}
#else
#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Shadows.hlsl"
#endif // (defined(P4LWRP_MAIN_LIGHT_SHADOWS) || defined(P4LWRP_ADDITIONAL_LIGHT_SHADOWS)) && !defined(_RECEIVE_SHADOWS_OFF)

#endif // !defined(_P4LWRP_OVERRIDESHADOWFUNCTIONS_HLSL_INCLUDED)
