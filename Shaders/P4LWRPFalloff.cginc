//
// P4LWRPFallOff.cginc
//
// Projector For LWRP
//
// Copyright (c) 2020 NYAHOON GAMES PTE. LTD.
//

//
// [Keywords usded in this file]
//
// #pragma shader_feature_local P4LWRP_FALLOFF_TEXTURE P4LWRP_FALLOFF_LINEAR P4LWRP_FALLOFF_SQUARE P4LWRP_FALLOFF_INV_SQUARE P4LWRP_FALLOFF_NONE
//   the keywords indicate how to decrease projection intensity according to the distance from the near clip plane of the projector.
//     P4LWRP_FALLOFF_TEXTURE   : intensity = _FalloffTex(z).a;  z = 0 at near clip, z = 1 at far clip. _FalloffTex(0) and _FalloffTex(1) must be zero.
//     P4LWRP_FALLOFF_LINEAR    : intensity = 1 - z
//     P4LWRP_FALLOFF_SQUARE    : intensity = 1 - z*z
//     P4LWRP_FALLOFF_INV_SQUARE: intensity = (1 - z)*(1 - z)
//     P4LWRP_FALLOFF_NONE      : intensity = 1, if 0 < z 
//

#if !defined(_P4LWRP_FALLOFF_CGINC_INCLUDED)
#define _P4LWRP_FALLOFF_CGINC_INCLUDED

sampler2D _FalloffTex; // this doesn't follow our naming convention p4lwrp_variableName. _FalloffTex is commonly used by Unity projector shaders.

fixed P4LWRP_GetFalloff(float4 uvShadow)
{
#if defined(P4LWRP_FALLOFF_TEXTURE)
    return tex2D(_FalloffTex, (uvShadow).zz).a;
#else
    fixed nearClip = (0 < uvShadow.z) ? 1 : 0;
#if defined(P4LWRP_FALLOFF_SQUARE)
    fixed z = saturate(uvShadow.z);
    return nearClip * (1.0f - z * z);
#elif defined(P4LWRP_FALLOFF_INV_SQUARE)
    fixed falloff = 1.0f - saturate(uvShadow.z);
    return nearClip * falloff * falloff;
#elif defined(P4LWRP_FALLOFF_LINEAR)
    fixed falloff = 1.0f - saturate(uvShadow.z);
    return nearClip * falloff;
#else // no falloff
    return nearClip;
#endif
#endif
}

fixed P4LWRP_ApplyFalloff(fixed shadow, fixed falloff)
{
	return 1.0f - falloff + falloff * shadow;
}

fixed P4LWRP_ApplyFalloff(fixed3 shadow, fixed falloff)
{
	return 1.0f - falloff + falloff * shadow;
}

#endif // !defined(_P4LWRP_FALLOFF_CGINC_INCLUDED)
