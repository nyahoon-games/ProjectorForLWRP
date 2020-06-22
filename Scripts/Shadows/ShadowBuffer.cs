//
// ShadowBuffer.cs
//
// Projector For LWRP
//
// Copyright (c) 2020 NYAHOON GAMES PTE. LTD.
//

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;
using Unity.Collections;

namespace ProjectorForLWRP
{
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class ShadowBuffer : MonoBehaviour
    {
        public enum ShadowColor
        {
            Monochrome = 0,
            Colored
        };
        public enum ApplyMethod
        {
            ByShadowProjectors = 0,
            ByLitShaders = 1,
            ByLightProjectors = 2,
        };
        const RenderPassEvent INVALID_RENDER_PASS_EVENT = (RenderPassEvent)(-1);
        const RenderPassEvent DEFAULT_RENDER_PASS_EVENT = RenderPassEvent.AfterRenderingOpaques;
        [SerializeField]
        [HideInInspector]
        private Material m_material;
        [SerializeField]
        [HideInInspector]
        private string m_shadowTextureName = "_ShadowTex";
        [SerializeField]
        [HideInInspector]
        private ShadowColor m_shadowColor = ShadowColor.Monochrome;
        [SerializeField]
        [HideInInspector]
        private ApplyMethod m_applyMethod = ApplyMethod.ByShadowProjectors;
        [SerializeField]
        [HideInInspector]
        private RenderPassEvent m_renderPassEvent = INVALID_RENDER_PASS_EVENT;
        [SerializeField]
        [HideInInspector]
        private PerObjectData m_perObjectData = PerObjectData.None;
        [SerializeField]
        [HideInInspector]
        private LayerMask m_realtimeShadowReceiverLayers = -1;
        [SerializeField]
        [HideInInspector]
        private RenderingLayerMask m_realtimeShadowReceiverRenderingLayerMask = RenderingLayerMask.Everything;
        [SerializeField]
        [HideInInspector]
        private bool m_collectRealtimeShadows = true;

        public Material material { get { return m_material; } }
        public ShadowColor shadowColor { get { return m_shadowColor; } }
        public ApplyMethod applyMethod { get { return m_applyMethod; } }
        public RenderPassEvent renderPassEvent { get { return m_renderPassEvent; } set { m_renderPassEvent = value; } }
        public PerObjectData perObjectData { get { return m_perObjectData; } set { m_perObjectData = value; } }
        public bool collectRealtimeShadows
        {
            get
            {
                if (m_light == null || m_applyMethod != ApplyMethod.ByLitShaders || !m_collectRealtimeShadows)
                {
                    return false;
                }
                if (m_realtimeShadowReceiverLayers == 0 || m_realtimeShadowReceiverRenderingLayerMask == 0)
                {
                    return false;
                }
                return realtimeShadowsEnabled;
            }
        }

        static ShadowBuffer()
        {
            ShaderKeywords.Projector.Activate();
        }

        private ApplyShadowBufferPass m_applyPass;
        private int m_shadowTextureId;
        private Light m_light;
        private void Initialize()
        {
            m_shadowTextureId = Shader.PropertyToID(m_shadowTextureName);
            m_light = GetComponent<Light>();
            if (m_renderPassEvent == INVALID_RENDER_PASS_EVENT)
            {
                InitialSetup();
            }
            if (applyMethod != ApplyMethod.ByLightProjectors && m_applyPass == null)
            {
                m_applyPass = new ApplyShadowBufferPass(this);
            }
        }
        private void InitialSetup()
        {
            m_renderPassEvent = DEFAULT_RENDER_PASS_EVENT;
            LightProjectorForLWRP lightProjector = GetComponent<LightProjectorForLWRP>();
            if (m_light != null)
            {
                m_applyMethod = ApplyMethod.ByShadowProjectors;
            }
            else if (lightProjector != null)
            {
                m_applyMethod = ApplyMethod.ByLightProjectors;
                if (lightProjector.shadowBuffer == null)
                {
                    lightProjector.shadowBuffer = this;
                }
            }
            else
            {
                m_applyMethod = ApplyMethod.ByShadowProjectors;
            }
#if UNITY_EDITOR
            if (applyMethod != ApplyMethod.ByLightProjectors)
            {
                m_material = GetDefaultMaterial();
            }
#endif
        }
        private void OnEnable()
        {
            Initialize();
            RenderPipelineManager.beginFrameRendering += OnBeginFrameRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }
        private void OnDisable()
        {
            RenderPipelineManager.beginFrameRendering -= OnBeginFrameRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        }

        public bool IsShadowMaterial()
        {
            return material != null && (material.GetTag("P4LWRPApplyShadowBufferType", false) == "Shadow");
        }
#if UNITY_EDITOR
        public Material GetDefaultMaterial()
        {
            if (m_light != null)
            {
                return HelperFunctions.FindMaterial("Projector For LWRP/ShadowBuffer/Apply Shadow Buffer");
            }
            else
            {
                return HelperFunctions.FindMaterial("Projector For LWRP/ShadowBuffer/Apply Custom Shadow Buffer");
            }
        }
        private void OnValidate()
        {
            Initialize();
        }
#endif

        //
        // rendering loop functions (called every frame)
        //
        private ObjectPool<Collections.AutoClearList<ShadowProjectorForLWRP>>.Map<Camera> m_cameraToProjectorList = new ObjectPool<Collections.AutoClearList<ShadowProjectorForLWRP>>.Map<Camera>();
        private void OnBeginFrameRendering(ScriptableRenderContext context, Camera[] cameras)
        {
            m_shadowTextureRef = null; // no longer valid.
            if (applyMethod == ApplyMethod.ByLitShaders && collectRealtimeShadows && isActiveAndEnabled)
            {
                foreach (Camera camera in cameras)
                {
                    if ((camera.cullingMask & (1 << gameObject.layer)) != 0)
                    {
                        CollectShadowBufferPassManager.staticInstance.AddShadowBuffer(camera, this);
                    }
                }
            }
        }

        private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            m_cameraToProjectorList.Remove(camera);
            if (m_shadowTextureRef != null)
            {
                m_shadowTextureRef.Release(m_shadowTextureColorWriteMask);
            }
        }

        internal void AddShadowProjector(Camera camera, ShadowProjectorForLWRP projector)
        {
            if (material == null && applyMethod == ApplyMethod.ByShadowProjectors)
            {
                return;
            }
            List<ShadowProjectorForLWRP> projectors = m_cameraToProjectorList[camera];
            if (projectors.Count == 0)
            {
                CollectShadowBufferPassManager.staticInstance.AddShadowBuffer(camera, this);
                if (applyMethod != ApplyMethod.ByLightProjectors && m_applyPass != null && material != null)
                {
                    // in case that there are more than 8 'ByLitShaders' shadow buffers,
                    // ByLitShaders shadow buffer also add apply pass. 
                    m_applyPass.renderPassEvent = renderPassEvent;
                    ProjectorRendererFeature.AddRenderPass(camera, m_applyPass);
                }
            }
            projectors.Add(projector);
        }


        internal int sortIndex { get; private set; }

        public bool realtimeShadowsEnabled
        {
            get
            {
                if (m_light == null)
                {
                    return false;
                }
                if (m_light.shadows == LightShadows.None)
                {
                    return false;
                }
                if (m_light.type == LightType.Rectangle || m_light.type == LightType.Disc)
                {
                    // no area light shadows
                    return false;
                }
                if (m_light.type == LightType.Point)
                {
                    // LWRP does not support point light shadows
                    return false;
                }
                if (m_light.bakingOutput.isBaked && m_light.bakingOutput.lightmapBakeType == LightmapBakeType.Baked)
                {
                    return false;
                }
                if (QualitySettings.shadows == UnityEngine.ShadowQuality.Disable)
                {
                    return false;
                }
                return true;
            }
        }
        private int m_additionalLightIndex;
        internal int visibleLightIndex { get; private set; }
        internal int additionalLightIndex { get { return m_additionalLightIndex; } }
        internal bool isMainLight { get; private set; }
        internal bool isVisible
        {
            get
            {
                if (applyMethod == ApplyMethod.ByLightProjectors)
                {
                    return true;
                }
                if (applyMethod == ApplyMethod.ByLitShaders && m_light == null)
                {
                    return false;
                }
                if (applyMethod == ApplyMethod.ByShadowProjectors && material != null)
                {
                    return true;
                }
                return visibleLightIndex != -1 || additionalLightIndex != -1;
            }
        }
        internal void SetupLightSource(ref RenderingData renderingData, ref NativeArray<int> lightIndexMap)
        {
            if (m_light != null)
            {
                visibleLightIndex = ShadowMaterialProperties.FindLightSourceIndex(m_light, ref renderingData, ref lightIndexMap, out m_additionalLightIndex);
            }
            else
            {
                visibleLightIndex = m_additionalLightIndex = -1;
            }
            isMainLight = (visibleLightIndex != -1 && visibleLightIndex == renderingData.lightData.mainLightIndex);
            sortIndex = CalculateSortIndex(ref renderingData);
        }

        private int CalculateSortIndex(ref RenderingData renderingData)
        {
            if (applyMethod == ApplyMethod.ByLitShaders && shadowColor == ShadowColor.Monochrome && m_light != null)
            {
                // shadow buffers that collect realtime shadows have the highest priority.
                // it is better to combine them into a single texture.
                // main light shadows must be written into alpha channel.
                if (collectRealtimeShadows)
                {
                    if (0 <= additionalLightIndex)
                    {
                        return additionalLightIndex + 1;
                    }
                    else if (isMainLight)
                    {
                        return 0;
                    }
                }
                // then, shadow buffers used by lit shaders but doesn't collect realtime shadows come next.
                // if the number of the additional light shadow buffers is less than 5, they must be combined together.
                // additional light shadow buffers must come first.
                else
                {
                    if (0 <= additionalLightIndex)
                    {
                        return additionalLightIndex + renderingData.lightData.additionalLightsCount + 1;
                    }
                    else if (isMainLight)
                    {
                        return 2 * renderingData.lightData.additionalLightsCount + 1;
                    }
                }
            }
            // for others, don't care order but need to minimize the number of texture.
            // to prevent combining monochrome shadow buffers together and leaving a color shadow buffer alone,
            // sort color shadow buffers first.
            int index = 2 * renderingData.lightData.additionalLightsCount + 1;
            if (shadowColor == ShadowColor.Monochrome)
            {
                index += 1;
            }
            if (!isVisible)
            {
                index += 100;
            }
            return index;
        }

        private bool m_appliedToLightPass = false;
        private CollectShadowBufferPass.RenderTextureRef m_shadowTextureRef = null;
        private ColorWriteMask m_shadowTextureColorWriteMask = 0;
        internal void CollectShadowBuffer(ScriptableRenderContext context, ref RenderingData renderingData, CollectShadowBufferPass.RenderTextureRef textureRef, ColorWriteMask writeMask)
        {
            m_shadowTextureRef = textureRef;
            m_shadowTextureColorWriteMask = writeMask;
            textureRef.Retain(writeMask);
            List<ShadowProjectorForLWRP> projectors = m_cameraToProjectorList[renderingData.cameraData.camera];
            bool collectedProjectorShadows = 0 < projectors.Count;
            foreach (ShadowProjectorForLWRP projector in projectors)
            {
                projector.CollectShadows(context, ref renderingData);
            }
            m_appliedToLightPass = false;
            if (applyMethod == ApplyMethod.ByLitShaders && shadowColor == ShadowColor.Monochrome && (collectedProjectorShadows || collectRealtimeShadows))
            {
                if (0 <= visibleLightIndex)
                {
                    bool collect = collectRealtimeShadows;
                    if (collect)
                    {
                        Light light = renderingData.lightData.visibleLights[visibleLightIndex].light;
                        if (light.shadows == LightShadows.None)
                        {
                            collect = false;
                        }
                        else if (light.bakingOutput.isBaked && light.bakingOutput.lightmapBakeType == LightmapBakeType.Baked)
                        {
                            collect = false;
                        }
                    }
                    if (collectedProjectorShadows || collect)
                    {
                        if (isMainLight)
                        {
                            LitShaderState.SetMainLightShadow(m_shadowTextureRef.renderTexture, m_realtimeShadowReceiverLayers, m_realtimeShadowReceiverRenderingLayerMask);
                            m_appliedToLightPass = true;
                        }
                        else if (0 <= additionalLightIndex)
                        {
                            int channelIndex = 0;
                            for (int i = 0; i < 4; ++i)
                            {
                                if ((writeMask & (ColorWriteMask)(1 << i)) != 0)
                                {
                                    channelIndex = i;
                                    break;
                                }
                            }
                            m_appliedToLightPass = LitShaderState.SetAdditionalLightShadow(additionalLightIndex, m_shadowTextureRef.renderTexture, channelIndex, m_realtimeShadowReceiverLayers, m_realtimeShadowReceiverRenderingLayerMask);
                        }
                    }
                }
            }
        }
        static readonly ShaderKeywords.Projector.ShadowTextureChannel[] SHADOW_TEXTURE_CHANNELS = { ShaderKeywords.Projector.ShadowTextureChannel.A, ShaderKeywords.Projector.ShadowTextureChannel.B, ShaderKeywords.Projector.ShadowTextureChannel.G, ShaderKeywords.Projector.ShadowTextureChannel.R, ShaderKeywords.Projector.ShadowTextureChannel.RGB };
#if UNITY_EDITOR
        Material m_copiedMaterial = null;
#endif
        internal void ApplyShadowBuffer(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            PerObjectData requiredPerObjectData = PerObjectData.None;

            bool appliedToLightPass = m_appliedToLightPass;
            m_appliedToLightPass = false;
            if (appliedToLightPass)
            {
                return;
            }
            Material applyShadowMaterial = material;
#if UNITY_EDITOR
            // do not use the original material so as not to make it dirty.
            if (m_copiedMaterial == null)
            {
                m_copiedMaterial = new Material(material);
            }
            applyShadowMaterial = m_copiedMaterial;
#endif
            if (m_light != null && !ShadowMaterialProperties.UpdateMaterialProperties(m_light, applyShadowMaterial, ref renderingData, out requiredPerObjectData))
            {
                return;
            }
            if (shadowColor == ShadowColor.Colored)
            {
                ShaderUtils.SetMaterialKeyword(applyShadowMaterial, SHADOW_TEXTURE_CHANNELS[4]);
            }
            else
            {
                ShaderKeywords.Projector.ShadowTextureChannel shadowTextureChannel = ShaderKeywords.Projector.ShadowTextureChannel.R;
                for (int i = 0; i < 4; ++i)
                {
                    if (m_shadowTextureColorWriteMask == (ColorWriteMask)(1 << i))
                    {
                        shadowTextureChannel = SHADOW_TEXTURE_CHANNELS[i];
                        break;
                    }
                }
                ShaderUtils.SetMaterialKeyword(applyShadowMaterial, shadowTextureChannel);
            }
            requiredPerObjectData |= perObjectData;
            List<ShadowProjectorForLWRP> projectors = m_cameraToProjectorList[renderingData.cameraData.camera];
            int stencilMask = StencilMaskAllocator.AllocateSingleBit();
            if (stencilMask == 0)
            {
#if UNITY_EDITOR
                Debug.LogError("No more available stencil bit. Skip shadow projector rendering.");
#endif
                return;
            }
            applyShadowMaterial.SetTexture(m_shadowTextureId, GetTemporaryShadowTexture());
            foreach (ShadowProjectorForLWRP projector in projectors)
            {
                projector.ApplyShadowBuffer(context, ref renderingData, applyShadowMaterial, requiredPerObjectData, stencilMask);
            }
        }
        internal int colorWriteMask
        {
            get { return (int)m_shadowTextureColorWriteMask; }
        }
        internal Texture GetTemporaryShadowTexture()
        {
            if (m_shadowTextureRef == null)
            {
                return null;
            }
            return m_shadowTextureRef.renderTexture;
        }
        internal void ReleaseTemporaryShadowTexture()
        {
            if (m_shadowTextureRef != null)
            {
                m_shadowTextureRef.Release((ColorWriteMask)colorWriteMask);
            }
        }
    }
}
