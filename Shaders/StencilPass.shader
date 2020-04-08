Shader "Hidden/ProjectorForLWRP/StencilPass"
{
	Properties {
		[HideInInspector] P4LWRP_StencilRef ("Stencil Ref", Float) = 1
		[HideInInspector] P4LWRP_StencilMask ("Stencil Mask", Float) = 1
	}
	SubShader
	{
		Pass
		{
			Cull Back
			ZWrite Off
			ColorMask 0
			Stencil {
				Ref [P4LWRP_StencilRef]
				WriteMask [P4LWRP_StencilMask]
				Comp Always
				Pass Replace
				ZFail Keep
			}
			HLSLPROGRAM
			#include "EnableCbuffer.cginc"
			#include "UnityCG.cginc"
			float4 vert (float4 vertex : POSITION) : SV_POSITION
			{
				return UnityObjectToClipPos(vertex);
			}
			fixed4 frag () : SV_Target
			{
				return 0;
			}
			#pragma vertex vert
			#pragma fragment frag
			ENDHLSL
		}
		Pass
		{
			Cull Front
			ZWrite Off
			ColorMask 0
			Stencil {
				Ref [P4LWRP_StencilRef]
				WriteMask [P4LWRP_StencilMask]
				Comp Always
				Pass Zero
				ZFail Keep
			}
			HLSLPROGRAM
			#include "EnableCbuffer.cginc"
			#include "UnityCG.cginc"
			float4 vert (float4 vertex : POSITION) : SV_POSITION
			{
				return UnityObjectToClipPos(vertex);
			}
			fixed4 frag (float4 pos : SV_POSITION) : SV_Target
			{
				return 0;
			}
			#pragma vertex vert
			#pragma fragment frag
			ENDHLSL
		}
	}
}
