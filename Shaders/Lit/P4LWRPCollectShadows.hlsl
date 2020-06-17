//
// P4LWRPCollectShadows.hlsl
//
// Projector For LWRP
//
// Copyright (c) 2020 NYAHOON GAMES PTE. LTD.
//

// [P4LWRP Keywords]
// P4LWRP_COLLECT_MAINLIGHT_SHADOWS       : defined if collect main light shadows
// P4LWRP_COLLECT_ADDITIONALLIGHT_SHADOWS : defined if collect additional ligight shadows
// P4LWRP_COLLECT_SHADOWMASK_R            : defined if collect R channel of the shadowmask (NOTE: LWRP doesn't support shadowmask)
// P4LWRP_COLLECT_SHADOWMASK_G            : defined if collect G channel of the shadowmask (NOTE: LWRP doesn't support shadowmask)
// P4LWRP_COLLECT_SHADOWMASK_B            : defined if collect B channel of the shadowmask (NOTE: LWRP doesn't support shadowmask)
// P4LWRP_COLLECT_SHADOWMASK_A            : defined if collect A channel of the shadowmask (NOTE: LWRP doesn't support shadowmask)

// [Unity Keywords]
// LIGHTMAP_ON
// SHADOWS_SHADOWMASK

// [LWRP Keywords]
// _MAIN_LIGHT_SHADOWS
// _MAIN_LIGHT_SHADOWS_CASCADE
// _ADDITIONAL_LIGHT_SHADOWS
// _SHADOWS_SOFT

// [Material Keywords]
// _RECEIVE_SHADOWS_OFF

// [Locally degined macros]
// _P4LWRP_COLLECT_SHADOWMASK_ON : defined if collect shadow mask and shadow mask is used for the object
// _P4LWRP_COLLECT_NO_SHADOWS    : defined if the object doesn't receive any shadows collected

#if !defined(_P4LWRP_COLLECTSHADOWS_HLSL_INCLUDED)
#define _P4LWRP_COLLECTSHADOWS_HLSL_INCLUDED

#include "../P4LWRPUnityMacros.cginc"
#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Lighting.hlsl"

#if defined(SHADOWS_SHADOWMASK) && defined(LIGHTMAP_ON) && (defined(P4LWRP_COLLECT_SHADOWMASK_R) || defined(P4LWRP_COLLECT_SHADOWMASK_G) || defined(P4LWRP_COLLECT_SHADOWMASK_B) || defined(P4LWRP_COLLECT_SHADOWMASK_A))
#define _P4LWRP_COLLECT_SHADOWMASK_ON
#endif

#if defined(P4LWRP_COLLECT_ADDITIONALLIGHT_SHADOWS) && !defined(_ADDITIONAL_LIGHT_SHADOWS)
#undef P4LWRP_COLLECT_ADDITIONALLIGHT_SHADOWS
#endif

#if defined(_RECEIVE_SHADOWS_OFF) || !(defined(P4LWRP_COLLECT_MAINLIGHT_SHADOWS) || defined(P4LWRP_COLLECT_SHADOWMASK) || defined(P4LWRP_COLLECT_ADDITIONALLIGHT_SHADOWS))
#define _P4LWRP_COLLECT_NO_SHADOWS
#endif

struct P4LWRP_CollectShadowsVertexAttributes {
	float4 vertex : POSITION;
#if defined(_P4LWRP_COLLECT_SHADOWMASK_ON)
	float2 lightmapUV : TEXCOORD1;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct P4LWRP_CollectShadowsVertexOutput {
#if !defined(_P4LWRP_COLLECT_NO_SHADOWS)

#if defined(_P4LWRP_COLLECT_SHADOWMASK_ON)
    half2 lightmapUV : TEXCOORD0;
#endif

#if defined(P4LWRP_COLLECT_MAINLIGHT_SHADOWS)
	float4 mainLightShadowCoord : TEXCOORD1;
#endif

#if defined(P4LWRP_COLLECT_ADDITIONALLIGHT_SHADOWS)
	float3 worldPos : TEXCOORD2;
#endif

#endif // !defined(_P4LWRP_COLLECT_NO_SHADOWS)

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO

	float4 pos : SV_POSITION;
};

P4LWRP_CollectShadowsVertexOutput P4LWRP_CollectShadowsVertexFunc(P4LWRP_CollectShadowsVertexAttributes v)
{
	P4LWRP_CollectShadowsVertexOutput o;

    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    UNITY_TRANSFER_INSTANCE_ID(v, o);

#if defined(_P4LWRP_COLLECT_NO_SHADOWS)
	// put the vertex behind the view clip box (use vertex attribute to avoid some graphics driver issues)
	o.pos = float4(v.vertex.x,v.vertex.y,-1,-1);
#else

	float3 worldPos;
    float4 clipPos;
    P4LWRP_TransformObjectToWorldAndClip(v.vertex.xyz, worldPos, clipPos);
	o.pos = clipPos;

#if defined(_P4LWRP_COLLECT_SHADOWMASK_ON)
	o.lightmapUV = lightmapUV.xy * unity_LightmapST.xy + unity_LightmapST.zw;
#endif

#if defined(P4LWRP_COLLECT_MAINLIGHT_SHADOWS)
	#if SHADOWS_SCREEN
		o.mainLightShadowCoord = ComputeScreenPos(clipPos);
	#else
		o.mainLightShadowCoord = TransformWorldToShadowCoord(worldPos);
	#endif
#endif

#if defined(P4LWRP_COLLECT_ADDITIONALLIGHT_SHADOWS)
	o.worldPos = worldPos;
#endif

#endif // !defined(_RECEIVE_SHADOWS_OFF)

	return o;
}

CBUFFER_START(P4LWRPCollectShadows)
fixed4 p4lwrp_shadowMaskWriteMasks[4];
fixed4 p4lwrp_shadowMaskWriteMasksInv[4]; // 1 - p4lwrp_shadowMaskWriteMasks
fixed4 p4lwrp_additionalLightShadowWriteMask[MAX_VISIBLE_LIGHTS];
CBUFFER_END

fixed4 P4LWRP_ApplyWriteMaskToShadow(fixed4 mask, fixed4 mask_inv, fixed shadow)
{
	return mask_inv + shadow*mask;
}

fixed4 P4LWRP_ApplyWriteMaskToShadow(fixed4 mask, fixed shadow)
{
	return 1 - (1 - shadow) * mask;
}

// TODO: Use multi render target if available
fixed4 P4LWRP_CollectShadowsFragmentFunc(P4LWRP_CollectShadowsVertexOutput i) : SV_Target
{
#if defined(_P4LWRP_COLLECT_NO_SHADOWS)
	return 0;
#else
	fixed4 shadows = 1;

#if defined(_P4LWRP_COLLECT_SHADOWMASK_ON)
    fixed4 shadowMask = UNITY_SAMPLE_TEX2D(unity_ShadowMask, i.lightmapUV);
#if defined(P4LWRP_COLLECT_SHADOWMASK_R)
	shadows *= P4LWRP_ApplyWriteMaskToShadow(p4lwrp_shadowMaskWriteMasks[0], p4lwrp_shadowMaskWriteMasksInv[0], shadowMask.r);
#endif
#if defined(P4LWRP_COLLECT_SHADOWMASK_G)
	shadows *= P4LWRP_ApplyWriteMaskToShadow(p4lwrp_shadowMaskWriteMasks[1], p4lwrp_shadowMaskWriteMasksInv[1], shadowMask.g);
#endif
#if defined(P4LWRP_COLLECT_SHADOWMASK_B)
	shadows *= P4LWRP_ApplyWriteMaskToShadow(p4lwrp_shadowMaskWriteMasks[2], p4lwrp_shadowMaskWriteMasksInv[2], shadowMask.b);
#endif
#if defined(P4LWRP_COLLECT_SHADOWMASK_A)
	shadows *= P4LWRP_ApplyWriteMaskToShadow(p4lwrp_shadowMaskWriteMasks[3], p4lwrp_shadowMaskWriteMasksInv[3], shadowMask.a);
#endif
#endif // defined(P4LWRP_COLLECT_SHADOWMASK_ON)

#if defined(P4LWRP_COLLECT_MAINLIGHT_SHADOWS)
	shadows.a *= MainLightRealtimeShadow(i.mainLightShadowCoord);
#endif

#if defined(P4LWRP_COLLECT_ADDITIONALLIGHT_SHADOWS)
    int pixelLightCount = GetAdditionalLightsCount();
    for (int n = 0; n < pixelLightCount; ++n)
    {
        int index = GetPerObjectLightIndex(n);
		fixed additionalShadow = AdditionalLightRealtimeShadow(index, i.worldPos);
		shadows *= P4LWRP_ApplyWriteMaskToShadow(p4lwrp_additionalLightShadowWriteMask[index], additionalShadow);
    }
#endif

	return shadows;
#endif
}

#endif // !defined(_P4LWRP_COLLECTSHADOWS_HLSL_INCLUDED)
