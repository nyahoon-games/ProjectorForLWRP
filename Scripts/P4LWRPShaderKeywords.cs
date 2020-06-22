//
// P4LWRPShaderKeywords.cs
//
// Projector For LWRP
//
// Copyright (c) 2020 NYAHOON GAMES PTE. LTD.
//

using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectorForLWRP
{
	public static class ShaderKeywords
	{
		// ------------------------------
		// Projector Keywords
		// ------------------------------
		public static class Projector
		{
			// Falloff Keywords
			public enum FalloffType
			{
				Texture = 0,
				Linear = 1,
				Square = 2,
				InvSquare = 3,
				Flat = 4
			}
			static readonly string[] FALLOFF_KEYWORDS = { "P4LWRP_FALLOFF_TEXTURE", "P4LWRP_FALLOFF_LINEAR", "P4LWRP_FALLOFF_SQUARE", "P4LWRP_FALLOFF_INV_SQUARE", "P4LWRP_FALLOFF_NONE" };

			// Shadow Texture Channel
			public enum ShadowTextureChannel
			{
				RGB = 0,
				R = 1,
				G = 2,
				B = 3,
				A = 4
			}
			static readonly string[] SHADOWTEX_CHANNEL_KEYWORDS = { "P4LWRP_SHADOWTEX_CHANNEL_RGB", "P4LWRP_SHADOWTEX_CHANNEL_R", "P4LWRP_SHADOWTEX_CHANNEL_G", "P4LWRP_SHADOWTEX_CHANNEL_B", "P4LWRP_SHADOWTEX_CHANNEL_A" };

			static Projector()
			{
				ShaderUtils.ShaderKeywordGroup<FalloffType>.s_keywords = FALLOFF_KEYWORDS;
				ShaderUtils.ShaderKeywordGroup<ShadowTextureChannel>.s_keywords = SHADOWTEX_CHANNEL_KEYWORDS;
			}

			// call static constructor
			public static void Activate()
			{
			}
		}


		// ------------------------------
		// Shadow Material Keywords
		// ------------------------------
		public static class Shadow
		{
			// Mixed lighting shadow type
			public enum MixedLightingType
			{
				None = 0,
				Subtractive = 1,
				Shadowmask = 2
			}
			static readonly string[] MIXED_LIGHT_TYPE_KEYWORDS = { null, "P4LWRP_MIXED_LIGHT_SUBTRACTIVE", "P4LWRP_MIXED_LIGHT_SHADOWMASK" };

			// shadows light source
			public enum ShadowLightSource
			{
				MainLight = 0,
				AdditionalLight = 1
			}
			static readonly string[] SHADOW_LIGHT_SOURCE_KEYWORDS = { null, "P4LWRP_ADDITIONAL_LIGHT_SHADOW" };

			// Is main light baked?
			public enum MainLightBaked
			{
				No = 0,
				Yes = 1
			}
			static readonly string[] MAINLIGHT_BAKED_KEYWORDS = { null, "P4LWRP_MAINLIGHT_BAKED" };

			// Are additional lights baked?
			public enum AdditionalLightsBaked
			{
				No = 0,
				Yes = 1
			}
			static readonly string[] ADDITIONALLIGHTS_BAKED_KEYWORDS = { null, "P4LWRP_ADDITIONALLIGHTS_BAKED" };

			// Ambient source
			public enum AmbientSourceType
			{
				SHOnly = 0,
				SHAndAdditionalLights = 1,
			}
			static readonly string[] AMBIENT_INCLUDE_KEYWORDS = { null, "P4LWRP_AMBIENT_INCLUDE_ADDITIONAL_LIGHT" };

			// Light source type
			public enum LightSourceType
			{
				DirectionalLight = 0,
				PointLight = 1,
				SpotLight = 2
			}
			static readonly string[] LIGHTSOURCE_KEYWORDS = { null, "P4LWRP_LIGHTSOURCE_POINT", "P4LWRP_LIGHTSOURCE_SPOT" };

			static Shadow()
			{
				ShaderUtils.ShaderKeywordGroup<MixedLightingType>.s_keywords = MIXED_LIGHT_TYPE_KEYWORDS;
				ShaderUtils.ShaderKeywordGroup<ShadowLightSource>.s_keywords = SHADOW_LIGHT_SOURCE_KEYWORDS;
				ShaderUtils.ShaderKeywordGroup<MainLightBaked>.s_keywords = MAINLIGHT_BAKED_KEYWORDS;
				ShaderUtils.ShaderKeywordGroup<AdditionalLightsBaked>.s_keywords = ADDITIONALLIGHTS_BAKED_KEYWORDS;
				ShaderUtils.ShaderKeywordGroup<AmbientSourceType>.s_keywords = AMBIENT_INCLUDE_KEYWORDS;
				ShaderUtils.ShaderKeywordGroup<LightSourceType>.s_keywords = LIGHTSOURCE_KEYWORDS;
			}

			// call static constructor
			public static void Activate()
			{
			}
		}

		// ------------------------------
		// Lit Shader Keywords (Forward Lighting Pass)
		// ------------------------------
		public class LitShader
		{
			// main light shadows
			public enum MainLightShadows
			{
				Off = 0,
				On = 1
			}
			static readonly string[] MAINLIGHT_SHADOWS_KEYWORDS = { null, "P4LWRP_MAIN_LIGHT_SHADOWS" };

			// Additional light shadows
			public enum AdditionalLightShadowsTexture
			{
				None = 0,
				AdditionalTexture = 1,
				MainLightTexture = 2,
				Both = 3
			}
			static readonly string[] ADDITIONAL_LIGHT_SHADOWS_KEYWORDS = { null, "P4LWRP_ADDITIONAL_LIGHT_SHADOWS", "P4LWRP_ADDITIONAL_LIGHT_SHADOWS_SINGLE_TEX", "P4LWRP_ADDITIONAL_LIGHT_SHADOWS_DOUBLE_TEX" };

			// ------------------------------
			// Lit Shader Keywords (Collect Shadows Pass)
			// ------------------------------
			public enum CollectMainLightShadows
			{
				Off = 0,
				On = 1
			}
			static readonly string[] COLLECT_MAINLIGHT_SHADOWS_KEYWORDS = { null, "P4LWRP_COLLECT_MAINLIGHT_SHADOWS" };

			public enum CollectAdditionalLightShadows
			{
				Off = 0,
				On = 1
			}
			static readonly string[] COLLECT_ADDITIONALLIGHT_SHADOWS_KEYWORDS = { null, "P4LWRP_COLLECT_ADDITIONALLIGHT_SHADOWS" };

			public enum CollectShadowmaskR
			{
				Off = 0,
				On = 1
			}
			static readonly string[] COLLECT_SHADOWMASK_R_KEYWORDS = { null, "P4LWRP_COLLECT_SHADOWMASK_R" };

			public enum CollectShadowmaskG
			{
				Off = 0,
				On = 1
			}
			static readonly string[] COLLECT_SHADOWMASK_G_KEYWORDS = { null, "P4LWRP_COLLECT_SHADOWMASK_G" };

			public enum CollectShadowmaskB
			{
				Off = 0,
				On = 1
			}
			static readonly string[] COLLECT_SHADOWMASK_B_KEYWORDS = { null, "P4LWRP_COLLECT_SHADOWMASK_B" };

			public enum CollectShadowmaskA
			{
				Off = 0,
				On = 1
			}
			static readonly string[] COLLECT_SHADOWMASK_A_KEYWORDS = { null, "P4LWRP_COLLECT_SHADOWMASK_R" };

			static LitShader()
			{
				ShaderUtils.ShaderKeywordGroup<MainLightShadows>.s_keywords = MAINLIGHT_SHADOWS_KEYWORDS;
				ShaderUtils.ShaderKeywordGroup<AdditionalLightShadowsTexture>.s_keywords = ADDITIONAL_LIGHT_SHADOWS_KEYWORDS;
				ShaderUtils.ShaderKeywordGroup<CollectMainLightShadows>.s_keywords = COLLECT_MAINLIGHT_SHADOWS_KEYWORDS;
				ShaderUtils.ShaderKeywordGroup<CollectAdditionalLightShadows>.s_keywords = COLLECT_ADDITIONALLIGHT_SHADOWS_KEYWORDS;
				ShaderUtils.ShaderKeywordGroup<CollectShadowmaskR>.s_keywords = COLLECT_SHADOWMASK_R_KEYWORDS;
				ShaderUtils.ShaderKeywordGroup<CollectShadowmaskG>.s_keywords = COLLECT_SHADOWMASK_G_KEYWORDS;
				ShaderUtils.ShaderKeywordGroup<CollectShadowmaskB>.s_keywords = COLLECT_SHADOWMASK_B_KEYWORDS;
				ShaderUtils.ShaderKeywordGroup<CollectShadowmaskA>.s_keywords = COLLECT_SHADOWMASK_A_KEYWORDS;
			}
			public static void Activate()
			{
				// call static constructor.
			}
		}
	}
}