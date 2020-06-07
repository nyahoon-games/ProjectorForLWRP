Shader "Projector For LWRP/ShadowBuffer/Collect Mipmapped Shadow"
{
    Properties
    {
 		[NoScaleOffset] _ShadowTex ("Cookie", 2D) = "gray" {}
		_Alpha ("Shadow Darkness", Range (0, 2)) = 1.0
		_DSPMipLevel ("Max Mip Level", float) = 4.0
		_Offset ("Offset", Range (-1, -10)) = -1.0
		_OffsetSlope ("Offset Slope Factor", Range (0, -1)) = -1.0
        [HideInInspector] _ColorWriteMask ("Color Write Mask", Float) = 1
    }
    SubShader
    {
		Tags {"Queue"="Transparent-1"}
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
            #pragma shader_feature_local P4LWRP_SHADOWTEX_CHANNEL_R P4LWRP_SHADOWTEX_CHANNEL_G P4LWRP_SHADOWTEX_CHANNEL_B P4LWRP_SHADOWTEX_CHANNEL_A

            #if !defined(P4LWRP_SHADOWTEX_CHANNEL_R) && !defined(P4LWRP_SHADOWTEX_CHANNEL_G) && !defined(P4LWRP_SHADOWTEX_CHANNEL_B) && !defined(P4LWRP_SHADOWTEX_CHANNEL_A)
            #define P4LWRP_SHADOWTEX_CHANNEL_R // Collect Shadow shader requires a monochrome shadow texture. Use R channel by default.
            #endif

			#include "../P4LWRP.cginc"

            CBUFFER_START(UnityPerMaterial)
            uniform fixed _Alpha;
            uniform half _DSPMipLevel;
            CBUFFER_END

            struct v2f {
	            float4 uvShadow : TEXCOORD0;
	            float4 pos : SV_POSITION;
            };

            v2f vert (float4 vertex : POSITION)
            {
            	v2f o;
	            fsrTransformVertex(vertex, o.pos, o.uvShadow);
	            float z = o.uvShadow.z;
	            o.uvShadow.z = _DSPMipLevel * z;
            	return o;
            }

			sampler2D _ShadowTex;

            fixed4 frag (v2f i) : SV_Target
            {
				fixed alpha = (0 < i.uvShadow.z) ? _Alpha : 0;
                float3 uv;
                uv.xy = saturate(i.uvShadow.xy/i.uvShadow.w);
                uv.z = i.uvShadow.z;
                fixed shadow = tex2Dlod(_ShadowTex, uv.xyzz).P4LWRP_SHADOWTEX_CHANNELMASK;
				return 1.0f - alpha + alpha * shadow;
            }
            ENDHLSL
        }
    }
}
