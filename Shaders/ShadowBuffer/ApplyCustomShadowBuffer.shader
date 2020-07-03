Shader "Projector For LWRP/ShadowBuffer/Apply Custom Shadow Buffer"
{
    Properties
    {
		_ShadowColor ("Shadow Color", Color) = (0.2,0.2,0.2,1)
		_Offset ("Offset", Range (-1, -10)) = -1.0
		_OffsetSlope ("Offset Slope Factor", Range (0, -1)) = -1.0
    }
    SubShader
    {
		Tags {"Queue"="Transparent-1" "P4LWRPProjectorType"="ApplyShadowBuffer" "P4LWRPApplyShadowBufferType"="Custom"}
        Pass
        {
			ZWrite Off
			ColorMask RGB
			Blend DstColor Zero
			Offset [_OffsetSlope], [_Offset]
			Fog { Color (1, 1, 1) }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_local P4LWRP_SHADOWTEX_CHANNEL_R P4LWRP_SHADOWTEX_CHANNEL_G P4LWRP_SHADOWTEX_CHANNEL_B P4LWRP_SHADOWTEX_CHANNEL_A P4LWRP_SHADOWTEX_CHANNEL_RGB

            // keywords defined by Unity 
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #define FSR_PROJECTOR_FOR_LWRP
            #include "../P4LWRPShadow.cginc"

            struct VertexAttributes {
                float4 vertex : POSITION;
                float4 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct VertexOutput
            {
                float4 uvShadow : TEXCOORD0;
#if defined(_P4LWRP_FOG_ON)
                fixed4 shadowColor : TEXCOORD1;
#else
                fixed3 shadowColor : TEXCOORD1;
#endif
                float4 pos : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
            uniform fixed4 _ShadowColor;
            CBUFFER_END

            VertexOutput vert (VertexAttributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);

                float3 worldPos;
                float4 clipPos;
            	P4LWRP_TransformObjectToWorldAndClip(v.vertex.xyz, worldPos, clipPos);
                fixed nDotL = saturate(-dot(v.normal, fsrProjectorDir()));
                half3 totalColor = max(_ShadowColor.rgb + nDotL, half3(0.01,0.01,0.01));
                VertexOutput o;
                o.uvShadow = ComputeScreenPos(clipPos);
                o.pos = clipPos;
                o.shadowColor.rgb = _ShadowColor.rgb / totalColor;
				P4LWRP_TRANSFER_FOGCOORD(o.shadowColor.w, o.pos);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                return o;
            }

			sampler2D _ShadowTex;

            fixed4 frag (VertexOutput i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                fixed alpha = (0 < i.uvShadow.z) ? 1 : 0;
                fixed4 color;
                color.rgb = lerp(i.shadowColor.rgb, fixed3(1,1,1), alpha * tex2Dproj(_ShadowTex, UNITY_PROJ_COORD(i.uvShadow)).P4LWRP_SHADOWTEX_CHANNELMASK);
                color.a = 1;
				UNITY_APPLY_FOG_COLOR(i.shadowColor.w, color, fixed4(1,1,1,1));
                return color;
            }
            ENDHLSL
        }
    }
}
