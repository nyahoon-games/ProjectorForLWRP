//
// UnityMacros.cginc
//
// Projector For LWRP
//
// Defines Unity Macros as defined in UnityCG.cginc using LWRP ShaderLibrary
//
// Copyright 2020 NYAHOON GAMES PTE. LTD. All Rights Reserved.
//

#if !defined(P4LWRP_UNITYMACROS_CGINC_DEFINED)
#define P4LWRP_UNITYMACROS_CGINC_DEFINED

#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"

#if !defined(SHADER_API_GLES) && !defined(SHADER_API_PSSL) && !defined(SHADER_API_GLES3) && !defined(SHADER_API_VULKAN) && !defined(SHADER_API_METAL) && !defined(SHADER_API_SWITCH)
#if !defined(fixed)
#define fixed half
#define fixed2 half2
#define fixed3 half3
#define fixed4 half4
#define fixed4x4 half4x4
#define fixed3x3 half3x3
#define fixed2x2 half2x2
#endif
#endif

#if defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN) || (defined(SHADER_API_MOBILE) && defined(SHADER_API_METAL)) || defined(SHADER_API_SWITCH)
// with HLSLcc, use DX11.1 partial precision for translation
// we specifically define fixed to be float16 (same as half) as all new GPUs seems to agree on float16 being minimal precision float
#if !defined(fixed)
#define fixed min16float
#define fixed2 min16float2
#define fixed3 min16float3
#define fixed4 min16float4
#define fixed4x4 min16float4x4
#define fixed3x3 min16float3x3
#define fixed2x2 min16float2x2
#endif
#if !defined(half)
#define half min16float
#define half2 min16float2
#define half3 min16float3
#define half4 min16float4
#define half2x2 min16float2x2
#define half3x3 min16float3x3
#define half4x4 min16float4x4
#endif
#endif

#if (!defined(SHADER_API_MOBILE) && defined(SHADER_API_METAL))
#if !defined(fixed)
#define fixed float
#define fixed2 float2
#define fixed3 float3
#define fixed4 float4
#define fixed4x4 float4x4
#define fixed3x3 float3x3
#define fixed2x2 float2x2
#endif
#if !defined(half)
#define half float
#define half2 float2
#define half3 float3
#define half4 float4
#define half2x2 float2x2
#define half3x3 float3x3
#define half4x4 float4x4
#endif
#endif

#define UNITY_PROJ_COORD(a) a

#define UNITY_FOG_COORDS_PACKED(idx, vectype) vectype fogCoord : TEXCOORD##idx;

#if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
    #define UNITY_FOG_COORDS(idx) UNITY_FOG_COORDS_PACKED(idx, float1)
    #define UNITY_TRANSFER_FOG(o,outpos) o.fogCoord.x = ComputeFogFactor((outpos).z)
    #define UNITY_APPLY_FOG_COLOR(coord,col,fogCol) col = MixFogColor(col,fogCol,(coord).x);
#else
    #define UNITY_FOG_COORDS(idx)
    #define UNITY_TRANSFER_FOG(o,outpos)
    #define UNITY_APPLY_FOG_COLOR(coord,col,fogCol)
#endif

#ifdef UNITY_PASS_FORWARDADD
    #define UNITY_APPLY_FOG(coord,col) UNITY_APPLY_FOG_COLOR(coord,col,fixed4(0,0,0,0))
#else
    #define UNITY_APPLY_FOG(coord,col) UNITY_APPLY_FOG_COLOR(coord,col,unity_FogColor)
#endif

inline float4 UnityObjectToClipPos(in float3 pos)
{
    return TransformObjectToHClip(pos);
}

// Tranforms position from world to homogenous space
inline float4 UnityWorldToClipPos( in float3 pos )
{
    return TransformWorldToHClip(pos);
}

// Tranforms position from view to homogenous space
inline float4 UnityViewToClipPos( in float3 pos )
{
    return TransformWViewToHClip(pos);
}

// Tranforms position from object to camera space
inline float3 UnityObjectToViewPos( in float3 pos )
{
    return mul(GetWorldToViewMatrix(), mul(GetObjectToWorldMatrix(), float4(pos, 1.0))).xyz;
}
inline float3 UnityObjectToViewPos(float4 pos) // overload for float4; avoids "implicit truncation" warning for existing shaders
{
    return UnityObjectToViewPos(pos.xyz);
}

// Tranforms position from world to camera space
inline float3 UnityWorldToViewPos( in float3 pos )
{
    return TransformWorldToView(pos);
}

// Transforms direction from object to world space
inline float3 UnityObjectToWorldDir( in float3 dir )
{
    return TransformObjectToWorldDir(dir);
}

// Transforms direction from world to object space
inline float3 UnityWorldToObjectDir( in float3 dir )
{
    return TransformWorldToObjectDir(dir);
}

// Transforms normal from object to world space
inline float3 UnityObjectToWorldNormal( in float3 norm )
{
    return TransformObjectToWorldNormal(norm);
}

#endif // !defined(P4LWRP_UNITYMACROS_CGINC_DEFINED)
