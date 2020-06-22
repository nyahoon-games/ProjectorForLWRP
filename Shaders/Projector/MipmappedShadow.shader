Shader "Projector For LWRP/Projector/Mipmapped Shadow"
{
	Properties {
		[NoScaleOffset] _ShadowTex ("Cookie", 2D) = "gray" {}
		_Alpha ("Shadow Strength", Range (0, 2)) = 1.0
		_Offset ("Offset", Range (0, -10)) = -1.0
		_OffsetSlope ("Offset Slope Factor", Range (0, -1)) = -1.0
	}
    SubShader
    {
		Tags {"Queue"="Transparent-1" "P4LWRPProjectorType"="Shadow"}
        // Shader code
		Pass
        {
			Name "PASS"
			ZWrite Off
			Fog { Color (1, 1, 1) }
			ColorMask RGB
			Blend DstColor Zero
			Offset [_OffsetSlope], [_Offset]

			HLSLPROGRAM
            #pragma target 3.0

			#pragma vertex vert
			#pragma fragment frag
			#pragma shader_feature_local FSR_PROJECTOR_FOR_LWRP
            #pragma shader_feature_local P4LWRP_FALLOFF_TEXTURE P4LWRP_FALLOFF_LINEAR P4LWRP_FALLOFF_SQUARE P4LWRP_FALLOFF_INV_SQUARE P4LWRP_FALLOFF_NONE
            #pragma shader_feature_local P4LWRP_SHADOWTEX_CHANNEL_RGB P4LWRP_SHADOWTEX_CHANNEL_R P4LWRP_SHADOWTEX_CHANNEL_G P4LWRP_SHADOWTEX_CHANNEL_B P4LWRP_SHADOWTEX_CHANNEL_A
            #pragma multi_compile_local _ P4LWRP_MIXED_LIGHT_SUBTRACTIVE P4LWRP_MIXED_LIGHT_SHADOWMASK
            #pragma multi_compile_local _ P4LWRP_ADDITIONAL_LIGHT_SHADOW
            #pragma multi_compile_local _ P4LWRP_MAINLIGHT_BAKED
			#pragma multi_compile_local _ P4LWRP_ADDITIONALLIGHTS_BAKED
            #pragma multi_compile_local _ P4LWRP_AMBIENT_INCLUDE_ADDITIONAL_LIGHT
            #pragma multi_compile_local _ P4LWRP_LIGHTSOURCE_POINT P4LWRP_LIGHTSOURCE_SPOT

            // keywards defined by Lightweight RP 
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

            // keywords defined by Unity 
            #pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile _ UNITY_HDR_ON
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

			#include "../P4LWRPShadow.cginc"
			#include "../P4LWRPFalloff.cginc"

            CBUFFER_START(UnityPerMaterial)
            uniform fixed _Alpha;
            uniform half _DSPMipLevel;
            CBUFFER_END

			sampler2D _ShadowTex;

			P4LWRP_ShadowProjectorVertexOutput vert(P4LWRP_ShadowProjectorVertexAttributes v)
			{
				P4LWRP_ShadowProjectorVertexOutput o = P4LWRP_ShadowProjectorVertexFunc(v);
				o.uvShadow.z *= _DSPMipLevel;
				return o;
			}

			fixed4 frag(P4LWRP_ShadowProjectorVertexOutput i) : SV_Target
			{
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float3 uv;
                uv.xy = saturate(i.uvShadow.xy/i.uvShadow.w);
                uv.z = i.uvShadow.z;

#if defined(P4LWRP_SHADOWTEX_CHANNEL_RGB)
                fixed3 shadow = tex2Dlod(_ShadowTex, uv.xyzz).P4LWRP_SHADOWTEX_CHANNELMASK;
#else
                fixed shadow = tex2Dlod(_ShadowTex, uv.xyzz).P4LWRP_SHADOWTEX_CHANNELMASK;
#endif
				fixed alpha = (0 < i.uvShadow.z) ? _Alpha : 0;
				shadow = P4LWRP_ApplyFalloff(shadow, alpha);
                return P4LWRP_CalculateShadowProjectorFragmentOutput(i, shadow);
			}

			ENDHLSL
		}
	} 
	CustomEditor "ProjectorForLWRP.Editor.ProjectorShadowShaderGUI"
	Fallback "Projector For LWRP/Projector/Shadow"
}
