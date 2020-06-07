//
// P4LWRP.cginc
//
// Projector For LWRP
//
// Copyright (c) 2019 NYAHOON GAMES PTE. LTD.
//

#if !defined(P4LWRP_CGINC_INCLUDED)
#define P4LWRP_CGINC_INCLUDED

#include "UnityMacros.cginc"

struct P4LWRP_V2F_PROJECTOR {
	float4 uvShadow : TEXCOORD0;
	UNITY_FOG_COORDS(1)
	float4 pos : SV_POSITION;
};

#if defined(FSR_RECEIVER) // FSR_RECEIVER keyword is used by Projection Receiver Renderer component which is contained in Fast Shadow Receiver.

CBUFFER_START(ProjectorTransform)
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

CBUFFER_START(ProjectorTransform)
uniform float4x4 _FSRWorldToProjector;
uniform float4 _FSRWorldProjectDir;
CBUFFER_END

void fsrTransformVertex(float4 v, out float4 clipPos, out float4 shadowUV)
{
	float4 worldPos;
	worldPos.xyz = mul(unity_ObjectToWorld, v).xyz;
	worldPos.w = 1.0f;
	clipPos = TransformWorldToHClip(worldPos.xyz);
	shadowUV = mul(_FSRWorldToProjector, worldPos);
}
float3 fsrProjectorDir()
{
	return UnityWorldToObjectDir(_FSRWorldProjectDir.xyz);
}

#else // !defined(FSR_RECEIVER)

CBUFFER_START(ProjectorTransform)
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

P4LWRP_V2F_PROJECTOR p4lwrp_vert_projector(float4 vertex : POSITION)
{
	P4LWRP_V2F_PROJECTOR o;
	fsrTransformVertex(vertex, o.pos, o.uvShadow);
	UNITY_TRANSFER_FOG(o, o.pos);
	return o;
}

#endif // !defined(P4LWRP_CGINC_INCLUDED)
