Shader "Projector For LWRP/Projector/Multiply Add" 
{
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
		[NoScaleOffset] _ShadowTex ("Cookie", 2D) = "gray" {}
		[HideInInspector][NoScaleOffset] _FalloffTex ("FallOff", 2D) = "white" {}
		_Offset ("Offset", Range (0, -10)) = -1.0
		_OffsetSlope ("Offset Slope Factor", Range (0, -1)) = -1.0
	}
	SubShader
	{
		Tags {"Queue"="Transparent-1"}
        // Shader code
		Pass
        {
			ZWrite Off
			Fog { Color (0, 0, 0) }
			ColorMask RGB
			Blend DstColor One
			Offset [_OffsetSlope], [_Offset]

			HLSLPROGRAM
			#pragma vertex P4LWRPProjectorVertexFunc
			#pragma fragment frag
			#pragma shader_feature_local FSR_PROJECTOR_FOR_LWRP
            #pragma shader_feature_local P4LWRP_FALLOFF_TEXTURE P4LWRP_FALLOFF_LINEAR P4LWRP_FALLOFF_SQUARE P4LWRP_FALLOFF_INV_SQUARE P4LWRP_FALLOFF_NONE
			#pragma multi_compile_fog
            #pragma multi_compile_instancing
			#include "../P4LWRP.cginc"
			#include "../P4LWRPFalloff.cginc"

			CBUFFER_START(UnityPerMaterial)
			uniform fixed4 _Color;
			CBUFFER_END

			sampler2D _ShadowTex;

			fixed4 frag(P4LWRP_ProjectorVertexOutput i) : SV_Target
			{
				fixed4 col;
				fixed alpha = P4LWRP_GetFalloff(i.uvShadow);
				col.rgb = _Color.rgb * tex2Dproj(_ShadowTex, UNITY_PROJ_COORD(i.uvShadow)).rgb;
				col.a = 1.0f;
				col.rgb *= alpha;
				UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(0,0,0,0));
				return col;
			}
			ENDHLSL
		}
	} 
	CustomEditor "ProjectorForLWRP.ProjectorFalloffShaderGUI"
}
