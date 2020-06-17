Shader "Projector For LWRP/Projector/Collect Shadow"
{
    Properties
    {
 		[NoScaleOffset] _ShadowTex ("Cookie", 2D) = "gray" {}
		[HideInInspector][NoScaleOffset] _FalloffTex ("FallOff", 2D) = "white" {}
		_Alpha ("Shadow Strength", Range (0, 2)) = 1.0
		_Offset ("Offset", Range (-1, -10)) = -1.0
		_OffsetSlope ("Offset Slope Factor", Range (0, -1)) = -1.0
        [HideInInspector] _ColorWriteMask ("Color Write Mask", Float) = 1
    }
    SubShader
    {
		Tags {"Queue"="Transparent-1" "P4LWRPProjectorType"="CollectShadowBuffer"}
        Pass
        {
			Name "PASS"
			ZWrite Off
			Blend DstColor Zero
			Offset [_OffsetSlope], [_Offset]
			ColorMask [_ColorWriteMask]
			Fog { Mode Off }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma shader_feature_local _ FSR_PROJECTOR_FOR_LWRP
            #pragma shader_feature_local P4LWRP_FALLOFF_TEXTURE P4LWRP_FALLOFF_LINEAR P4LWRP_FALLOFF_SQUARE P4LWRP_FALLOFF_INV_SQUARE P4LWRP_FALLOFF_NONE
            #pragma shader_feature_local P4LWRP_SHADOWTEX_CHANNEL_R P4LWRP_SHADOWTEX_CHANNEL_G P4LWRP_SHADOWTEX_CHANNEL_B P4LWRP_SHADOWTEX_CHANNEL_A
            #pragma multi_compile_instancing

            #if !defined(P4LWRP_SHADOWTEX_CHANNEL_R) && !defined(P4LWRP_SHADOWTEX_CHANNEL_G) && !defined(P4LWRP_SHADOWTEX_CHANNEL_B) && !defined(P4LWRP_SHADOWTEX_CHANNEL_A)
            #define P4LWRP_SHADOWTEX_CHANNEL_R // Collect Shadow shader requires a monochrome shadow texture. Use R channel by default.
            #endif

			#include "../P4LWRP.cginc"
			#include "../P4LWRPFalloff.cginc"

            CBUFFER_START(UnityPerMaterial)
            uniform fixed _Alpha;
            CBUFFER_END

            UNITY_VERTEX_INPUT_INSTANCE_ID

            struct v2f {
	            float4 uvShadow : TEXCOORD0;
	            float4 pos : SV_POSITION;
            };

            v2f vert (P4LWRP_ProjectorVertexAttributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
            	v2f o;
            	fsrTransformVertex(v.vertex , o.pos, o.uvShadow);
            	return o;
            }

			sampler2D _ShadowTex;

            fixed4 frag (v2f i) : SV_Target
            {
				fixed alpha = saturate(_Alpha*P4LWRP_GetFalloff(i.uvShadow));
				fixed shadow = tex2Dproj(_ShadowTex, UNITY_PROJ_COORD(i.uvShadow)).P4LWRP_SHADOWTEX_CHANNELMASK;
				return P4LWRP_ApplyFalloff(shadow, alpha);
            }
            ENDHLSL
        }
    }
	CustomEditor "ProjectorForLWRP.Editor.ProjectorFalloffShaderGUI"
}
