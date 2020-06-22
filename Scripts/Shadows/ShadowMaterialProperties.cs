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
    [DisallowMultipleComponent]
    public class ShadowMaterialProperties : MonoBehaviour
    {
        [SerializeField]
        private Light m_lightSource = null;

        public Light lightSource
        {
            get { return m_lightSource; }
            set { m_lightSource = value; }
        }

        static ShadowMaterialProperties()
        {
            ShaderKeywords.Shadow.Activate();
        }
        public void FindAndSetLightSource(bool searchAncestors)
        {
            Light light = GetComponent<Light>();
            if (searchAncestors)
            {
                Transform parent = transform.parent;
                while (light == null && parent != null)
                {
                    light = parent.GetComponent<Light>();
                    parent = parent.parent;
                }
            }
            if (light != null)
            {
                m_lightSource = light;
            }
        }
        public static int FindLightSourceIndex(Light lightSource, ref RenderingData renderingData, ref Unity.Collections.NativeArray<int> lightIndexMap, out int additionalLightIndex)
        {
            additionalLightIndex = -1;
            if (lightSource == null)
            {
                return renderingData.lightData.mainLightIndex;
            }
            var visibleLights = renderingData.lightData.visibleLights;
            int visibleLightCount = visibleLights.Length;
            for (int i = 0; i < visibleLightCount; ++i)
            {
                if (lightSource == visibleLights[i].light)
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
        public static int FindLightSourceIndex(Light lightSource, ref RenderingData renderingData, out int additionalLightIndex)
        {
            var lightIndexMap = renderingData.cullResults.GetLightIndexMap(Unity.Collections.Allocator.Temp);
            int index = FindLightSourceIndex(lightSource, ref renderingData, ref lightIndexMap, out additionalLightIndex);
            lightIndexMap.Dispose();
            return index;
        }
        public int FindLightSourceIndex(ref RenderingData renderingData, out int additionalLightIndex)
        {
            return FindLightSourceIndex(m_lightSource, ref renderingData, out additionalLightIndex);
        }
        public int FindLightSourceIndex(ref RenderingData renderingData, ref Unity.Collections.NativeArray<int> lightIndexMap, out int additionalLightIndex)
        {
            return FindLightSourceIndex(m_lightSource, ref renderingData, ref lightIndexMap, out additionalLightIndex);
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
        static ShaderKeywords.Shadow.MainLightBaked IsMainLightBaked(ref RenderingData renderingData)
        {
            int mainLightIndex = renderingData.lightData.mainLightIndex;
            var visibleLights = renderingData.lightData.visibleLights;
            int visibleLightCount = visibleLights.Length;
            if (0 <= mainLightIndex && mainLightIndex < visibleLightCount)
            {
                Light mainLight = visibleLights[mainLightIndex].light;
                return IsLightBaked(mainLight) ? ShaderKeywords.Shadow.MainLightBaked.Yes : ShaderKeywords.Shadow.MainLightBaked.No;
            }
            return ShaderKeywords.Shadow.MainLightBaked.No;
        }
        static ShaderKeywords.Shadow.AdditionalLightsBaked IsAdditionalLightsBaked(ref RenderingData renderingData)
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
                                return ShaderKeywords.Shadow.AdditionalLightsBaked.Yes;
                            }
                        }
                    }
                }
            }
            return ShaderKeywords.Shadow.AdditionalLightsBaked.No;
        }
        public bool UpdateMaterialProperties(Material targetMaterial, ref RenderingData renderingData, out PerObjectData perObjectData)
        {
            return UpdateMaterialProperties(m_lightSource, targetMaterial, ref renderingData, out perObjectData);
        }
        public static bool UpdateMaterialProperties(Light lightSource, Material targetMaterial, ref RenderingData renderingData, out PerObjectData perObjectData)
        {
            SetupMixedLightingShadow(lightSource, targetMaterial);
            perObjectData = PerObjectData.LightData | PerObjectData.LightProbe | PerObjectData.Lightmaps | PerObjectData.OcclusionProbe;
            int additionalLightIndex;
            int lightIndex = FindLightSourceIndex(lightSource, ref renderingData, out additionalLightIndex);
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
                SetupAdditionalLightShadow(lightSource, targetMaterial, additionalLightIndex, additionalLightsCount);
                return true;
            }
            else
            {
                return false;
            }
        }

        private static void SetupMixedLightingShadow(Light lightSource, Material targetMaterial)
        {
            ShaderKeywords.Shadow.MixedLightingType mixedLightingType = ShaderKeywords.Shadow.MixedLightingType.None;
            if (lightSource == null)
            {
                lightSource = RenderSettings.sun;
            }
            if (lightSource != null && lightSource.lightmapBakeType != LightmapBakeType.Realtime && lightSource.bakingOutput.isBaked)
            {
                if (lightSource.lightmapBakeType == LightmapBakeType.Mixed && lightSource.bakingOutput.mixedLightingMode == MixedLightingMode.Shadowmask)
                {
                    // implement shadowmask mixed lighting. Lightweight RP does not support it though...
                    if (0 <= lightSource.bakingOutput.occlusionMaskChannel && lightSource.bakingOutput.occlusionMaskChannel < 4)
                    {
                        mixedLightingType = ShaderKeywords.Shadow.MixedLightingType.Shadowmask;
                        Vector4 shadowMaskChannel = new Vector4(0, 0, 0, 0);
                        shadowMaskChannel[lightSource.bakingOutput.occlusionMaskChannel] = 1;
                        P4LWRPShaderProperties.p4lwrp_ShadowMaskSelector.Set(targetMaterial, shadowMaskChannel);
                    }
                }
                else if (lightSource.lightmapBakeType == LightmapBakeType.Mixed && lightSource.bakingOutput.mixedLightingMode == MixedLightingMode.IndirectOnly)
                {
                    // we use projector shadow instead of shadowmap. thus, we don't support indirect mixed lighting.
                    mixedLightingType = ShaderKeywords.Shadow.MixedLightingType.None;
                }
                else
                {
                    // in other cases, we use subtractive mixed lighting
                    mixedLightingType = ShaderKeywords.Shadow.MixedLightingType.Subtractive;
                }
            }
            ShaderUtils.SetMaterialKeyword(targetMaterial, mixedLightingType);
        }
        private static void SetupMainLightShadow(Material targetMaterial, int additionalLightCount)
        {
            ShaderKeywords.Shadow.ShadowLightSource shadowLightSource = ShaderKeywords.Shadow.ShadowLightSource.MainLight;
            ShaderKeywords.Shadow.AmbientSourceType ambientSource = ShaderKeywords.Shadow.AmbientSourceType.SHOnly;
            if (0 < additionalLightCount)
            {
                ambientSource = ShaderKeywords.Shadow.AmbientSourceType.SHAndAdditionalLights;
            }
            ShaderUtils.SetMaterialKeyword(targetMaterial, shadowLightSource);
            ShaderUtils.SetMaterialKeyword(targetMaterial, ambientSource);
            ShaderUtils.SetMaterialKeyword(targetMaterial, ShaderKeywords.Shadow.LightSourceType.DirectionalLight);
        }
        private static void SetupAdditionalLightShadow(Light lightSource, Material targetMaterial, int index, int additionalLightCount)
        {
            P4LWRPShaderProperties.p4lwrp_ShadowLightIndex.Set(targetMaterial, index);
            ShaderKeywords.Shadow.ShadowLightSource shadowLightSource = ShaderKeywords.Shadow.ShadowLightSource.AdditionalLight;
            ShaderKeywords.Shadow.AmbientSourceType ambientSource = ShaderKeywords.Shadow.AmbientSourceType.SHOnly;
            if (1 < additionalLightCount)
            {
                ambientSource = ShaderKeywords.Shadow.AmbientSourceType.SHAndAdditionalLights;
            }
            ShaderUtils.SetMaterialKeyword(targetMaterial, shadowLightSource);
            ShaderUtils.SetMaterialKeyword(targetMaterial, ambientSource);

            switch (lightSource.type)
            {
                case LightType.Directional:
                    ShaderUtils.SetMaterialKeyword(targetMaterial, ShaderKeywords.Shadow.LightSourceType.DirectionalLight);
                    break;
                case LightType.Point:
                    ShaderUtils.SetMaterialKeyword(targetMaterial, ShaderKeywords.Shadow.LightSourceType.PointLight);
                    break;
                case LightType.Spot:
                    ShaderUtils.SetMaterialKeyword(targetMaterial, ShaderKeywords.Shadow.LightSourceType.SpotLight);
                    break;
            }
        }
    }
}
