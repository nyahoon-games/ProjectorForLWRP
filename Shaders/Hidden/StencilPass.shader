Shader "Hidden/ProjectorForLWRP/StencilPass"
{
	Properties {
		[HideInInspector] p4lwrp_StencilRef ("Stencil Ref", Float) = 1
		[HideInInspector] p4lwrp_StencilMask ("Stencil Mask", Float) = 1
	}
	SubShader
	{
		Pass
		{
			Cull Back
			ZWrite Off
			ColorMask 0
			Stencil {
				Ref [p4lwrp_StencilRef]
				WriteMask [p4lwrp_StencilMask]
				Comp Always
				Pass Replace
				ZFail Keep
			}
			HLSLPROGRAM
			#include "../P4LWRP.cginc"
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
				Ref [p4lwrp_StencilRef]
				WriteMask [p4lwrp_StencilMask]
				Comp Always
				Pass Zero
				ZFail Keep
			}
			HLSLPROGRAM
			#include "../P4LWRP.cginc"
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
