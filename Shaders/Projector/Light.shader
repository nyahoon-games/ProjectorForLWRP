Shader "Projector For LWRP/Projector/Light" 
{
	Properties {
		_Color ("Light Color", Color) = (1,1,1,1)
		_Alpha ("Intensity", Range (0, 2)) = 1.0
		[NoScaleOffset] _LightTex ("Cookie", 2D) = "gray" {}
		[HideInInspector][NoScaleOffset] _FalloffTex ("FallOff", 2D) = "white" {}
		_Offset ("Offset", Range (0, -10)) = -1.0
		_OffsetSlope ("Offset Slope Factor", Range (0, -1)) = -1.0
	}
	SubShader
	{
		Tags {"Queue"="Transparent-1" "P4LWRPProjectorType"="Light"}
        // Shader code
		Pass
        {
			ZWrite Off
			Fog { Color (0, 0, 0) }
			ColorMask RGB
			Blend DstColor One
			Offset [_OffsetSlope], [_Offset]

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma shader_feature_local FSR_PROJECTOR_FOR_LWRP
            #pragma shader_feature_local P4LWRP_FALLOFF_TEXTURE P4LWRP_FALLOFF_LINEAR P4LWRP_FALLOFF_SQUARE P4LWRP_FALLOFF_INV_SQUARE P4LWRP_FALLOFF_NONE
			// no color channel definition means no shadows. later we will add P4LWRP_SHADOWTEX_CHANNEL_DEPTH for depth shadows
            #pragma multi_compile_local _ P4LWRP_SHADOWTEX_CHANNEL_R P4LWRP_SHADOWTEX_CHANNEL_G P4LWRP_SHADOWTEX_CHANNEL_B P4LWRP_SHADOWTEX_CHANNEL_A P4LWRP_SHADOWTEX_CHANNEL_RGB // P4LWRP_SHADOWTEX_CHANNEL_DEPTH
			#pragma multi_compile_fog
            #pragma multi_compile_instancing

			#if (defined(P4LWRP_SHADOWTEX_CHANNEL_R) || defined(P4LWRP_SHADOWTEX_CHANNEL_G) || defined(P4LWRP_SHADOWTEX_CHANNEL_B) || defined(P4LWRP_SHADOWTEX_CHANNEL_A) || defined(P4LWRP_SHADOWTEX_CHANNEL_RGB))
			#define _P4LWRP_SHADOWBUFFER_SHADOWS_ON
			#endif

			#include "../P4LWRP.cginc"
			#include "../P4LWRPFalloff.cginc"

			CBUFFER_START(UnityPerMaterial)
			uniform fixed4 _Color;
            uniform half _Alpha;
			CBUFFER_END

			sampler2D _LightTex;
			sampler2D _ShadowTex;

			struct VertexAttributes {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput {
				float4 uvProj   : TEXCOORD0;
#if defined(_P4LWRP_SHADOWBUFFER_SHADOWS_ON)
				float4 uvScreen : TEXCOORD1;
#endif
#if defined(_P4LWRP_FOG_ON)
				fixed2 ndotL  : TEXCOORD2;
#else
				fixed1 ndotL  : TEXCOORD2;
#endif
				float4 pos : SV_POSITION;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			VertexOutput vert(VertexAttributes v)
			{
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				VertexOutput o;
                float3 worldPos;
                float4 clipPos;
            	P4LWRP_TransformObjectToWorldAndClip(v.vertex.xyz, worldPos, clipPos);

			    o.uvProj = P4LWRP_CalculateProjectorUV(v.vertex.xyz, worldPos);
#if defined(_P4LWRP_SHADOWBUFFER_SHADOWS_ON)
				o.uvScreen = ComputeScreenPos(clipPos);
#endif
				o.ndotL.x = -dot(v.normal, fsrProjectorDir());
				o.pos = clipPos;
				P4LWRP_TRANSFER_FOGCOORD(o.ndotL.y, o.pos);
				return o;
			}
			fixed4 frag(VertexOutput i) : SV_Target
			{
				fixed4 col;
				col.rgb = _Color.rgb * tex2Dproj(_LightTex, UNITY_PROJ_COORD(i.uvProj)).rgb;
				col.a = 1.0f;
				col.rgb *= _Alpha * P4LWRP_GetFalloff(i.uvProj) * saturate(i.ndotL.x);
#if defined(_P4LWRP_SHADOWBUFFER_SHADOWS_ON)
				col.rgb *= tex2Dproj(_ShadowTex, UNITY_PROJ_COORD(i.uvScreen)).P4LWRP_SHADOWTEX_CHANNELMASK;
#endif
				UNITY_APPLY_FOG_COLOR(i.ndotL.y, col, fixed4(0,0,0,0));
				return col;
			}
			ENDHLSL
		}
	} 
	CustomEditor "ProjectorForLWRP.Editor.ProjectorFalloffShaderGUI"
}
