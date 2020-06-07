//
// P4LWRPFallOff.cginc
//
// Projector For LWRP
//
// Copyright (c) 2020 NYAHOON GAMES PTE. LTD.
//

#if !defined(P4LWRPFALLOFF_CGINC_INCLUDED)
#define P4LWRPFALLOFF_CGINC_INCLUDED

sampler2D _FalloffTex;

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

#endif // !defined(P4LWRPFALLOFF_CGINC_INCLUDED)
