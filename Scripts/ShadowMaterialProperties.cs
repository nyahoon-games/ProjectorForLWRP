using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;

namespace ProjectorForLWRP
{
    public class ShadowMaterialProperties : MonoBehaviour
    {
        [SerializeField]
        private Light m_lightSource = null;
        [SerializeField]
        public bool m_calculateShadowColorInFragmentShader = false;

        const string KEYWORD_MAINLIGHT_BAKED = "P4LWRP_MAINLIGHT_BAKED";
        const string KEYWORD_MIXED_LIGHTING_SUBTRACTIVE = "P4LWRP_MIXED_LIGHT_SUBTRACTIVE";
        const string KEYWORD_MIXED_LIGHTING_SHADOWMASK = "P4LWRP_MIXED_LIGHT_SHADOWMASK";
        const string KEYWORD_ADDITIONALLIGHT_SHADOW = "P4LWRP_ADDITIONAL_LIGHT_SHADOW";
        const string KEYWORD_AMBIENT_INCLUDE_ADDITIONALLIGHT = "P4LWRP_AMBIENT_INCLUDE_ADDITIONAL_LIGHT";
        const string KEYWORD_LIGHTSOURCE_POINT = "P4LWRP_LIGHTSOURCE_POINT";
        const string KEYWORD_LIGHTSOURCE_SPOT = "P4LWRP_LIGHTSOURCE_SPOT";
        const string KEYWORD_LIGHTSOURCE_PERPIXEL_DIRECTIONAL = "P4LWRP_LIGHTSOURCE_PERPIXEL_DIRECTIONAL";
        const string KEYWORD_SHADOWTEX_CHANNEL_R = "P4LWRP_SHADOWTEX_CHANNEL_R";
        const string KEYWORD_SHADOWTEX_CHANNEL_G = "P4LWRP_SHADOWTEX_CHANNEL_G";
        const string KEYWORD_SHADOWTEX_CHANNEL_B = "P4LWRP_SHADOWTEX_CHANNEL_B";
        const string KEYWORD_SHADOWTEX_CHANNEL_A = "P4LWRP_SHADOWTEX_CHANNEL_A";

        static int SHADER_CONST_ID_SHADOWMASKSELECTOR;
        static int SHADER_CONST_ID_ADDITIONALLIGHT_INDEX;
        static ShadowMaterialProperties()
        {
            SHADER_CONST_ID_SHADOWMASKSELECTOR = Shader.PropertyToID("p4lwrp_ShadowMaskSelector");
            SHADER_CONST_ID_ADDITIONALLIGHT_INDEX = Shader.PropertyToID("p4lwrp_ShadowLightIndex");
        }

        public bool UpdateMaterialProperties(Material targetMaterial, ref RenderingData renderingData, out PerObjectData perObjectData)
        {
            SetupMixedLightingShadow(targetMaterial);
            var visibleLights = renderingData.lightData.visibleLights;
            int visibleLightCount = visibleLights.Length;
            int additionalLightsCount = renderingData.lightData.additionalLightsCount;
            int mainLightIndex = renderingData.lightData.mainLightIndex;
            bool mainLightBaked = false;
            perObjectData = PerObjectData.LightData | PerObjectData.LightProbe | PerObjectData.Lightmaps | PerObjectData.OcclusionProbe;
            if (0 < additionalLightsCount)
            {
                perObjectData |= PerObjectData.LightIndices;
            }
            if (0 <= mainLightIndex && mainLightIndex < visibleLightCount)
            {
                Light mainLight = visibleLights[mainLightIndex].light;
                if (mainLight.bakingOutput.isBaked)
                {
                    if (mainLight.lightmapBakeType == LightmapBakeType.Baked)
                    {
                        mainLightBaked = true;
                    }
                    else if (mainLight.lightmapBakeType == LightmapBakeType.Mixed && mainLight.bakingOutput.mixedLightingMode == MixedLightingMode.Subtractive)
                    {
                        mainLightBaked = true;
                    }
                }
            }
            if (mainLightBaked)
            {
                targetMaterial.EnableKeyword(KEYWORD_MAINLIGHT_BAKED);
            }
            else
            {
                targetMaterial.DisableKeyword(KEYWORD_MAINLIGHT_BAKED);
            }
            if (m_lightSource == null)
            {
                SetupMainLightShadow(targetMaterial, additionalLightsCount);
                return true;
            }
            if (0 <= mainLightIndex && mainLightIndex < visibleLightCount && m_lightSource == visibleLights[mainLightIndex].light)
            {
                SetupMainLightShadow(targetMaterial, additionalLightsCount);
                return true;
            }
            if (additionalLightsCount == 0)
            {
                return false;
            }
            using (var lightIndexMap = renderingData.cullResults.GetLightIndexMap(Unity.Collections.Allocator.Temp))
            {
                for (int i = 0; i < visibleLightCount; ++i)
                {
                    if (m_lightSource == visibleLights[i].light)
                    {
                        if (0 <= lightIndexMap[i])
                        {
                            SetupAdditionalLightShadow(targetMaterial, lightIndexMap[i], additionalLightsCount);
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
            return false;
        }

        static private bool IsLightmapOn()
        {
            return (LightmapSettings.lightmaps != null && 0 < LightmapSettings.lightmaps.Length);
        }
        private void SetupMixedLightingShadow(Material targetMaterial)
        {
            Light light = m_lightSource;
            if (light == null)
            {
                light = RenderSettings.sun;
            }
            if (light != null && IsLightmapOn() && light.lightmapBakeType != LightmapBakeType.Realtime && light.bakingOutput.isBaked)
            {
                if (light.lightmapBakeType == LightmapBakeType.Mixed && light.bakingOutput.mixedLightingMode == MixedLightingMode.Shadowmask)
                {
                    // implement shadowmask mixed lighting. LIghtweight RP does not support it though...
                    targetMaterial.DisableKeyword(KEYWORD_MIXED_LIGHTING_SUBTRACTIVE);
                    if (0 <= light.bakingOutput.occlusionMaskChannel && light.bakingOutput.occlusionMaskChannel < 4)
                    {
                        targetMaterial.EnableKeyword(KEYWORD_MIXED_LIGHTING_SHADOWMASK);
                        Vector4 shadowMaskChannel = new Vector4(0, 0, 0, 0);
                        shadowMaskChannel[light.bakingOutput.occlusionMaskChannel] = 1;
                        targetMaterial.SetVector(SHADER_CONST_ID_SHADOWMASKSELECTOR, shadowMaskChannel);
                    }
                    else
                    {
                        targetMaterial.DisableKeyword(KEYWORD_MIXED_LIGHTING_SHADOWMASK);
                    }
                }
                else if (light.lightmapBakeType == LightmapBakeType.Mixed && light.bakingOutput.mixedLightingMode == MixedLightingMode.IndirectOnly)
                {
                    // we use projector shadow instead of shadowmap. thus, we don't support indirect mixed lighting.
                    targetMaterial.DisableKeyword(KEYWORD_MIXED_LIGHTING_SHADOWMASK);
                    targetMaterial.DisableKeyword(KEYWORD_MIXED_LIGHTING_SUBTRACTIVE);
                }
                else
                {
                    // in other cases, we use subtractive mixed lighting
                    targetMaterial.DisableKeyword(KEYWORD_MIXED_LIGHTING_SHADOWMASK);
                    targetMaterial.EnableKeyword(KEYWORD_MIXED_LIGHTING_SUBTRACTIVE);
                }
            }
            else
            {
                targetMaterial.DisableKeyword(KEYWORD_MIXED_LIGHTING_SHADOWMASK);
                targetMaterial.DisableKeyword(KEYWORD_MIXED_LIGHTING_SUBTRACTIVE);
            }
        }
        private void SetupMainLightShadow(Material targetMaterial, int additionalLightCount)
        {
            targetMaterial.DisableKeyword(KEYWORD_ADDITIONALLIGHT_SHADOW);
            if (0 < additionalLightCount)
            {
                targetMaterial.EnableKeyword(KEYWORD_AMBIENT_INCLUDE_ADDITIONALLIGHT);
            }
            else
            {
                targetMaterial.DisableKeyword(KEYWORD_AMBIENT_INCLUDE_ADDITIONALLIGHT);
            }
            targetMaterial.DisableKeyword(KEYWORD_LIGHTSOURCE_POINT);
            targetMaterial.DisableKeyword(KEYWORD_LIGHTSOURCE_SPOT);
            if (m_calculateShadowColorInFragmentShader)
            {
                targetMaterial.EnableKeyword(KEYWORD_LIGHTSOURCE_PERPIXEL_DIRECTIONAL);
            }
            else
            {
                targetMaterial.DisableKeyword(KEYWORD_LIGHTSOURCE_PERPIXEL_DIRECTIONAL);
            }
        }
        private void SetupAdditionalLightShadow(Material targetMaterial, int index, int additionalLightCount)
        {
            targetMaterial.SetInt(SHADER_CONST_ID_ADDITIONALLIGHT_INDEX, index);
            targetMaterial.EnableKeyword(KEYWORD_ADDITIONALLIGHT_SHADOW);
            if (1 < additionalLightCount)
            {
                targetMaterial.EnableKeyword(KEYWORD_AMBIENT_INCLUDE_ADDITIONALLIGHT);
            }
            else
            {
                targetMaterial.DisableKeyword(KEYWORD_AMBIENT_INCLUDE_ADDITIONALLIGHT);
            }
            switch (m_lightSource.type)
            {
                case LightType.Directional:
                    if (m_calculateShadowColorInFragmentShader)
                    {
                        targetMaterial.EnableKeyword(KEYWORD_LIGHTSOURCE_PERPIXEL_DIRECTIONAL);
                    }
                    else
                    {
                        targetMaterial.DisableKeyword(KEYWORD_LIGHTSOURCE_PERPIXEL_DIRECTIONAL);
                    }
                    targetMaterial.DisableKeyword(KEYWORD_LIGHTSOURCE_POINT);
                    targetMaterial.DisableKeyword(KEYWORD_LIGHTSOURCE_SPOT);
                    break;
                case LightType.Point:
                    targetMaterial.EnableKeyword(KEYWORD_LIGHTSOURCE_POINT);
                    targetMaterial.DisableKeyword(KEYWORD_LIGHTSOURCE_SPOT);
                    targetMaterial.DisableKeyword(KEYWORD_LIGHTSOURCE_PERPIXEL_DIRECTIONAL);
                    break;
                case LightType.Spot:
                    targetMaterial.EnableKeyword(KEYWORD_LIGHTSOURCE_SPOT);
                    targetMaterial.DisableKeyword(KEYWORD_LIGHTSOURCE_POINT);
                    targetMaterial.DisableKeyword(KEYWORD_LIGHTSOURCE_PERPIXEL_DIRECTIONAL);
                    break;
            }
        }
    }
}
