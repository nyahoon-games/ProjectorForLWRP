//
// P4LWRP.cginc
//
// Projector For LWRP
//
// Copyright (c) 2019 NYAHOON GAMES PTE. LTD.
//

// [NOTE}
// The prefix 'FSR' used here stands for Fast Shadow Receiver, our first asset published on Unity Asset Store.
// We keep using the following keywords and shader constants to make Fast Shadow Receiver available.
//
// [Keywords]
// FSR_RECEIVER
// FSR_PROJECTOR_FOR_LWRP
//
// [Shader constants]
// _FSRProjector
// _FSRProjectDir
// _FSRWorldToProjector
// _FSRWorldProjectDir
//
// Other than the above, we use the following naming rules:
//
// keyword         : Start with P4LWRP_ followed by upper snake case. ex) P4LWRP_KEYWORD_NAME
// struct          : Start with P4LWRP_ followed by upper camel case. ex) P4LWRP_StructName
// functions       : Start with P4LWRP_ followed by upper camel case. ex) P4LWRP_FunctionName
// shader constants: Start with p4lwrp_ followed by lower camel case. ex) p4lwrp_shaderConstantName
// macro function  : Start with P4LWRP_ followed by upper snake case. ex) P4LWRP_MACRO_FUNCTION
// type/semantic   : Start with P4LWRP_ followed by upper snake case. ex) P4LWRP_TYPE_NAME
// constant value  : Start with P4LWRP_ followed by upper snake case. ex) P4LWRP_CONSTANT_VALUE
// cbuffer name    : Start with P4LWRP followed by upper camel case.  ex) P4LWRPConstantBufferName
// conditional compilation macro (other than keywords): Start with _P4LWRP_ followed by upper snake case.
//
// Locally defined identifiers do not follow the above rules.
//

//
// [Keywords usded in this file]
// 
// #pragma shader_feature_local _ FSR_RECEIVER FSR_PROJECTOR_FOR_LWRP
//                           _ : projector shader for Unity default render pipeline
//                FSR_RECEIVER : projector shader for Fast Shadow Receiver
//      FSR_PROJECTOR_FOR_LWRP : projector shader for Lightweight render pipeline
//
// #pragma shader_feature_local P4LWRP_SHADOWTEX_CHANNEL_RGB P4LWRP_SHADOWTEX_CHANNEL_R P4LWRP_SHADOWTEX_CHANNEL_G P4LWRP_SHADOWTEX_CHANNEL_B P4LWRP_SHADOWTEX_CHANNEL_A
//    P4LWRP_SHADOWTEX_CHANNEL_RGB: use rgb channel for shadow texture
//    P4LWRP_SHADOWTEX_CHANNEL_R  : use red channel only for shadow texture
//    P4LWRP_SHADOWTEX_CHANNEL_G  : use green channel only for shadow texture
//    P4LWRP_SHADOWTEX_CHANNEL_B  : use blue channel only for shadow texture
//    P4LWRP_SHADOWTEX_CHANNEL_A  : use alpha channel only for shadow texture
// 

#if !defined(_P4LWRP_CGINC_INCLUDED)
#define _P4LWRP_CGINC_INCLUDED

#include "P4LWRPUnityMacros.cginc"

struct P4LWRP_ProjectorVertexAttributes {
    float4 vertex : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct P4LWRP_ProjectorVertexOutput {
	float4 uvShadow : TEXCOORD0;
	UNITY_FOG_COORDS(1)
	float4 pos : SV_POSITION;
    UNITY_VERTEX_OUTPUT_STEREO
};

#if defined(FSR_RECEIVER) // FSR_RECEIVER keyword is used by Projection Receiver Renderer component which is contained in Fast Shadow Receiver.

CBUFFER_START(P4LWRPProjectorTransform)
float4x4 _FSRProjector;
float4 _FSRProjectDir;
CBUFFER_END

void fsrTransformVertex(float4 v, out float4 clipPos, out float4 shadowUV)
{
	clipPos = UnityObjectToClipPos(v);
	shadowUV = mul(_FSRProjector, v);
}
float3 fsrProjectorDir()
{
	return _FSRProjectDir.xyz;
}

#elif defined(FSR_PROJECTOR_FOR_LWRP)

CBUFFER_START(P4LWRPProjectorTransform)
uniform float4x4 _FSRWorldToProjector;
uniform float4 _FSRWorldProjectDir;
CBUFFER_END

void fsrTransformVertex(float4 v, out float4 clipPos, out float4 shadowUV)
{
	float3 worldPos;
	P4LWRP_TransformObjectToWorldAndClip(v, worldPos, clipPos);
	shadowUV = mul(_FSRWorldToProjector, fixed4(worldPos, 1.0f));
}
float3 fsrProjectorDir()
{
	return UnityWorldToObjectDir(_FSRWorldProjectDir.xyz);
}

#else // !defined(FSR_RECEIVER)

CBUFFER_START(P4LWRPProjectorTransform)
float4x4 unity_Projector;
float4x4 unity_ProjectorClip;
CBUFFER_END

void fsrTransformVertex(float4 v, out float4 clipPos, out float4 shadowUV)
{
	clipPos = UnityObjectToClipPos(v);
	shadowUV = mul (unity_Projector, v);
	shadowUV.z = mul (unity_ProjectorClip, v).x;
}
float3 fsrProjectorDir()
{
	return normalize(float3(unity_Projector[2][0],unity_Projector[2][1], unity_Projector[2][2]));
}

#endif // FSR_RECEIVER

#if defined(P4LWRP_SHADOWTEX_CHANNEL_R)
#define P4LWRP_SHADOWTEX_CHANNELMASK r
#elif defined(P4LWRP_SHADOWTEX_CHANNEL_G)
#define P4LWRP_SHADOWTEX_CHANNELMASK g
#elif defined(P4LWRP_SHADOWTEX_CHANNEL_B)
#define P4LWRP_SHADOWTEX_CHANNELMASK b
#elif defined(P4LWRP_SHADOWTEX_CHANNEL_A)
#define P4LWRP_SHADOWTEX_CHANNELMASK a
#else
#define P4LWRP_SHADOWTEX_CHANNELMASK rgb
#endif

P4LWRP_ProjectorVertexOutput P4LWRPProjectorVertexFunc(P4LWRP_ProjectorVertexAttributes v)
{
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
	P4LWRP_ProjectorVertexOutput o;
	fsrTransformVertex(v.vertex, o.pos, o.uvShadow);
	UNITY_TRANSFER_FOG(o, o.pos);
	return o;
}

#endif // !defined(_P4LWRP_CGINC_INCLUDED)
