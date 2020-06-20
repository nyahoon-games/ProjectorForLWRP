//
// ShadowMaterialProperties.cs
//
// Projector For LWRP
//
// Copyright (c) 2020 NYAHOON GAMES PTE. LTD.
//

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
        private bool m_calculateShadowColorInFragmentShader = false;

        public Light lightSource
        {
            get { return m_lightSource; }
            set { m_lightSource = value; }
        }

        static ShadowMaterialProperties()
        {
            P4LWRPShaderKeywords.Activate();
        }

        public int FindLightSourceIndex(ref RenderingData renderingData, out int additionalLightIndex)
        {
            var lightIndexMap = renderingData.cullResults.GetLightIndexMap(Unity.Collections.Allocator.Temp);
            int index = FindLightSourceIndex(ref renderingData, ref lightIndexMap, out additionalLightIndex);
            lightIndexMap.Dispose();
            return index;
        }
        public int FindLightSourceIndex(ref RenderingData renderingData, ref Unity.Collections.NativeArray<int> lightIndexMap, out int additionalLightIndex)
        {
            additionalLightIndex = -1;
            if (m_lightSource == null)
            {
                return renderingData.lightData.mainLightIndex;
            }
            var visibleLights = renderingData.lightData.visibleLights;
            int visibleLightCount = visibleLights.Length;
            for (int i = 0; i < visibleLightCount; ++i)
            {
                if (m_lightSource == visibleLights[i].light)
                {
                    if (i != renderingData.lightData.mainLightIndex && 0 <= renderingData.lightData.additionalLightsCount)
                    {
                        additionalLightIndex = lightIndexMap[i];
                    }
                    return i;
                }
            }
            return -1;
        }
        static bool IsLightBaked(Light light)
        {
            if (light.bakingOutput.isBaked)
            {
                if (light.lightmapBakeType == LightmapBakeType.Baked)
                {
                    return true;
                }
                else if (light.lightmapBakeType == LightmapBakeType.Mixed && light.bakingOutput.mixedLightingMode == MixedLightingMode.Subtractive)
                {
                    return true;
                }
            }
            return false;
        }
        static P4LWRPShaderKeywords.MainLightBaked IsMainLightBaked(ref RenderingData renderingData)
        {
            int mainLightIndex = renderingData.lightData.mainLightIndex;
            var visibleLights = renderingData.lightData.visibleLights;
            int visibleLightCount = visibleLights.Length;
            if (0 <= mainLightIndex && mainLightIndex < visibleLightCount)
            {
                Light mainLight = visibleLights[mainLightIndex].light;
                return IsLightBaked(mainLight) ? P4LWRPShaderKeywords.MainLightBaked.Yes : P4LWRPShaderKeywords.MainLightBaked.No;
            }
            return P4LWRPShaderKeywords.MainLightBaked.No;
        }
        static P4LWRPShaderKeywords.AdditionalLightsBaked IsAdditionalLightsBaked(ref RenderingData renderingData)
        {
            if (0 < renderingData.lightData.additionalLightsCount) {
                var visibleLights = renderingData.lightData.visibleLights;
                int visibleLightCount = visibleLights.Length;
                for (int i = 0; i < visibleLightCount; ++i)
                {
                    Light light = visibleLights[i].light;
                    if (IsLightBaked(light))
                    {
                        using (var lightIndexMap = renderingData.cullResults.GetLightIndexMap(Unity.Collections.Allocator.Temp))
                        {
                            if (0 <= lightIndexMap[i])
                            {
                                return P4LWRPShaderKeywords.AdditionalLightsBaked.Yes;
                            }
                        }
                    }
                }
            }
            return P4LWRPShaderKeywords.AdditionalLightsBaked.No;
        }
        public bool UpdateMaterialProperties(Material targetMaterial, ref RenderingData renderingData, out PerObjectData perObjectData)
        {
            SetupMixedLightingShadow(targetMaterial);
            perObjectData = PerObjectData.LightData | PerObjectData.LightProbe | PerObjectData.Lightmaps | PerObjectData.OcclusionProbe;
            int additionalLightIndex;
            int lightIndex = FindLightSourceIndex(ref renderingData, out additionalLightIndex);
            if (lightIndex < 0)
            {
                return false;
            }
            int additionalLightsCount = renderingData.lightData.additionalLightsCount;
            if (0 < additionalLightsCount)
            {
                perObjectData |= PerObjectData.LightIndices;
            }

            ShaderUtils.SetMaterialKeyword(targetMaterial, IsMainLightBaked(ref renderingData));
            ShaderUtils.SetMaterialKeyword(targetMaterial, IsAdditionalLightsBaked(ref renderingData));

            if (lightIndex == renderingData.lightData.mainLightIndex)
            {
                SetupMainLightShadow(targetMaterial, additionalLightsCount);
                return true;
            }
            else if (0 <= additionalLightIndex)
            {
                SetupAdditionalLightShadow(targetMaterial, additionalLightIndex, additionalLightsCount);
                return true;
            }
            else
            {
                return false;
            }
        }

        private void SetupMixedLightingShadow(Material targetMaterial)
        {
            P4LWRPShaderKeywords.MixedLightingType mixedLightingType = P4LWRPShaderKeywords.MixedLightingType.None;
            Light light = m_lightSource;
            if (light == null)
            {
                light = RenderSettings.sun;
            }
            if (light != null && light.lightmapBakeType != LightmapBakeType.Realtime && light.bakingOutput.isBaked)
            {
                if (light.lightmapBakeType == LightmapBakeType.Mixed && light.bakingOutput.mixedLightingMode == MixedLightingMode.Shadowmask)
                {
                    // implement shadowmask mixed lighting. Lightweight RP does not support it though...
                    if (0 <= light.bakingOutput.occlusionMaskChannel && light.bakingOutput.occlusionMaskChannel < 4)
                    {
                        mixedLightingType = P4LWRPShaderKeywords.MixedLightingType.Shadowmask;
                        Vector4 shadowMaskChannel = new Vector4(0, 0, 0, 0);
                        shadowMaskChannel[light.bakingOutput.occlusionMaskChannel] = 1;
                        P4LWRPShaderProperties.p4lwrp_ShadowMaskSelector.Set(targetMaterial, shadowMaskChannel);
                    }
                }
                else if (light.lightmapBakeType == LightmapBakeType.Mixed && light.bakingOutput.mixedLightingMode == MixedLightingMode.IndirectOnly)
                {
                    // we use projector shadow instead of shadowmap. thus, we don't support indirect mixed lighting.
                    mixedLightingType = P4LWRPShaderKeywords.MixedLightingType.None;
                }
                else
                {
                    // in other cases, we use subtractive mixed lighting
                    mixedLightingType = P4LWRPShaderKeywords.MixedLightingType.Subtractive;
                }
            }
            ShaderUtils.SetMaterialKeyword(targetMaterial, mixedLightingType);
        }
        private void SetupMainLightShadow(Material targetMaterial, int additionalLightCount)
        {
            P4LWRPShaderKeywords.ShadowLightSource shadowLightSource = P4LWRPShaderKeywords.ShadowLightSource.MainLight;
            P4LWRPShaderKeywords.AmbientSourceType ambientSource = P4LWRPShaderKeywords.AmbientSourceType.SHOnly;
            if (0 < additionalLightCount)
            {
                ambientSource = P4LWRPShaderKeywords.AmbientSourceType.SHAndAdditionalLights;
            }
            ShaderUtils.SetMaterialKeyword(targetMaterial, shadowLightSource);
            ShaderUtils.SetMaterialKeyword(targetMaterial, ambientSource);
            ShaderUtils.SetMaterialKeyword(targetMaterial, P4LWRPShaderKeywords.LightSourceType.DirectionalLight);
        }
        private void SetupAdditionalLightShadow(Material targetMaterial, int index, int additionalLightCount)
        {
            P4LWRPShaderProperties.p4lwrp_ShadowLightIndex.Set(targetMaterial, index);
            P4LWRPShaderKeywords.ShadowLightSource shadowLightSource = P4LWRPShaderKeywords.ShadowLightSource.AdditionalVertexLight;
            if (m_calculateShadowColorInFragmentShader || m_lightSource.type != LightType.Directional)
            {
                shadowLightSource = P4LWRPShaderKeywords.ShadowLightSource.AdditionalLight;
            }
            P4LWRPShaderKeywords.AmbientSourceType ambientSource = P4LWRPShaderKeywords.AmbientSourceType.SHOnly;
            if (1 < additionalLightCount)
            {
                ambientSource = P4LWRPShaderKeywords.AmbientSourceType.SHAndAdditionalLights;
            }
            ShaderUtils.SetMaterialKeyword(targetMaterial, shadowLightSource);
            ShaderUtils.SetMaterialKeyword(targetMaterial, ambientSource);

            switch (m_lightSource.type)
            {
                case LightType.Directional:
                    ShaderUtils.SetMaterialKeyword(targetMaterial, P4LWRPShaderKeywords.LightSourceType.DirectionalLight);
                    break;
                case LightType.Point:
                    ShaderUtils.SetMaterialKeyword(targetMaterial, P4LWRPShaderKeywords.LightSourceType.PointLight);
                    break;
                case LightType.Spot:
                    ShaderUtils.SetMaterialKeyword(targetMaterial, P4LWRPShaderKeywords.LightSourceType.SpotLight);
                    break;
            }
        }
    }
}
