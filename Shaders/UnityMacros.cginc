//
// UnityMacros.cginc
//
// Projector For LWRP
//
// Defines Unity Macros as defined in UnityCG.cginc using LWRP ShaderLibrary
//
// Copyright 2020 NYAHOON GAMES PTE. LTD. All Rights Reserved.
//

// UNITY_SHADER_NO_UPGRADE

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

// copied from UnityShaderUtilities.cginc
float3 ODSOffset(float3 worldPos, float ipd)
{
    //based on google's omni-directional stereo rendering thread
    const float EPSILON = 2.4414e-4;
    float3 worldUp = float3(0.0, 1.0, 0.0);
    float3 camOffset = worldPos.xyz - _WorldSpaceCameraPos.xyz;
    float4 direction = float4(camOffset.xyz, dot(camOffset.xyz, camOffset.xyz));
    direction.w = max(EPSILON, direction.w);
    direction *= rsqrt(direction.w);

    float3 tangent = cross(direction.xyz, worldUp.xyz);
    if (dot(tangent, tangent) < EPSILON)
        return float3(0, 0, 0);
    tangent = normalize(tangent);

    float directionMinusIPD = max(EPSILON, direction.w*direction.w - ipd*ipd);
    float a = ipd * ipd / direction.w;
    float b = ipd / direction.w * sqrt(directionMinusIPD);
    float3 offset = -a*direction + b*tangent;
    return offset;
}

inline float4 UnityObjectToClipPosODS(float3 inPos)
{
    float4 clipPos;
    float3 posWorld = mul(unity_ObjectToWorld, float4(inPos, 1.0)).xyz;
#if defined(STEREO_CUBEMAP_RENDER_ON)
    float3 offset = ODSOffset(posWorld, unity_HalfStereoSeparation.x);
    clipPos = mul(UNITY_MATRIX_VP, float4(posWorld + offset, 1.0));
#else
    clipPos = mul(UNITY_MATRIX_VP, float4(posWorld, 1.0));
#endif
    return clipPos;
}

inline float4 UnityWorldToClipPos( in float3 pos )
{
    // UnityCG.cginc uses ODSOffset only in UnityObjectToClipPos. why????
    return TransformWorldToHClip(pos);
}

// experimental. not actually used.
//#define P4LWRP_MINIMIZE_WORLDPOS_ROUNDERROR

#if defined(P4LWRP_MINIMIZE_WORLDPOS_ROUNDERROR)
inline float4 P4LWRP_GetTranslationVectorFrom4x4Matrix(float4x4 mat)
{
    return mat._14_24_34_44;
}

inline float4 PS4LWRP_Multiply4x3Matrix(float4x4 mat, float3 vec)
{
#if defined(SHADER_API_GLES)
    return  mul(mat, float4(vec,0));
#else
    return  mul((float4x3)mat, vec);
#endif
}
#endif // defined(P4LWRP_MINIMIZE_WORLDPOS_ROUNDERROR)

inline float4 UnityObjectToClipPos(in float3 pos)
{
#if defined(STEREO_CUBEMAP_RENDER_ON)
    return UnityObjectToClipPosODS(pos);
#elif defined(P4LWRP_MINIMIZE_WORLDPOS_ROUNDERROR)
    float3 worldPosOffset = mul((float3x3)unity_ObjectToWorld, pos);
    float3 worldPosCenter = P4LWRP_GetTranslationVectorFrom4x4Matrix(unity_ObjectToWorld);
    // the first term below may have a large rounding error, but it is constant for all vertices in the object.
    return UnityWorldToClipPos(worldPosCenter) + PS4LWRP_Multiply4x3Matrix(UNITY_MATRIX_VP, worldPosOffset);
#else
    return TransformObjectToHClip(pos);
#endif
}

inline float4 UnityViewToClipPos( in float3 pos )
{
    return TransformWViewToHClip(pos);
}

inline float3 UnityObjectToViewPos( in float3 pos )
{
    return mul(GetWorldToViewMatrix(), mul(GetObjectToWorldMatrix(), float4(pos, 1.0))).xyz;
}
inline float3 UnityObjectToViewPos(float4 pos) // overload for float4; avoids "implicit truncation" warning for existing shaders
{
    return UnityObjectToViewPos(pos.xyz);
}

inline float3 UnityWorldToViewPos( in float3 pos )
{
    return TransformWorldToView(pos);
}

inline float3 UnityObjectToWorldDir( in float3 dir )
{
    return TransformObjectToWorldDir(dir);
}

inline float3 UnityWorldToObjectDir( in float3 dir )
{
    return TransformWorldToObjectDir(dir);
}

inline float3 UnityObjectToWorldNormal( in float3 norm )
{
    return TransformObjectToWorldNormal(norm);
}

inline void P4LWRP_TransformObjectToWorldAndClip(in float3 pos, out float3 posWorld, out float4 clipPos)
{
#if defined(P4LWRP_MINIMIZE_WORLDPOS_ROUNDERROR) && !defined(STEREO_CUBEMAP_RENDER_ON)
    float3 worldPosOffset = mul((float3x3)unity_ObjectToWorld, pos);
    float3 worldPosCenter = P4LWRP_GetTranslationVectorFrom4x4Matrix(unity_ObjectToWorld);
    posWorld = worldPosCenter + worldPosOffset;
    // the first term below may have a large rounding error, but it is constant for all vertices in the object.
    clipPos = UnityWorldToClipPos(worldPosCenter) + PS4LWRP_Multiply4x3Matrix(UNITY_MATRIX_VP, worldPosOffset);
#else
    posWorld = mul(unity_ObjectToWorld, float4(pos, 1.0)).xyz;
#if defined(STEREO_CUBEMAP_RENDER_ON)
    float3 offset = ODSOffset(posWorld, unity_HalfStereoSeparation.x);
    clipPos = mul(UNITY_MATRIX_VP, float4(posWorld + offset, 1.0));
#endif
    clipPos = mul(UNITY_MATRIX_VP, float4(posWorld, 1.0));
#endif
}

#endif // !defined(P4LWRP_UNITYMACROS_CGINC_DEFINED)
