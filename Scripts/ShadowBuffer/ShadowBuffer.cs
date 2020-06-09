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

namespace ProjectorForLWRP
{
    [RequireComponent(typeof(ShadowMaterialProperties))]
    public class ShadowBuffer : MonoBehaviour, System.IComparable<ShadowBuffer>
    {
        public Material material;
        public string shadowTextureName = "_ShadowTex";
        public int stencilMask = 0x2;
        public UnityEngine.Rendering.LWRP.RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        public PerObjectData perObjectData = PerObjectData.None;
        public bool applyToLightingPass = false;
        public LayerMask ignoreLayersIfLightPassAvailable = -1;

        private Dictionary<Camera, List<ProjectorForLWRP>> m_projectors = new Dictionary<Camera, List<ProjectorForLWRP>>();
        private ApplyShadowBufferPass m_applyPass;
        private ShadowMaterialProperties m_shadowMaterialProperties;
        private int m_shadowTextureId;
        private void Initialize()
        {
            m_applyPass = new ApplyShadowBufferPass(this);
            m_shadowTextureId = Shader.PropertyToID(shadowTextureName);
            m_shadowMaterialProperties = GetComponent<ShadowMaterialProperties>();
        }
        ApplyShadowBufferPass applyPass
        {
            get
            {
                if (m_applyPass == null)
                {
                    Initialize();
                }
                return m_applyPass;
            }
        }
        ShadowMaterialProperties shadowMaterialProperties
        {
            get
            {
                if (m_shadowMaterialProperties == null)
                {
                    Initialize();
                }
                return m_shadowMaterialProperties;
            }
        }
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (material == null)
            {
                material = HelperFunctions.FindMaterial("Projector For LWRP/ShadowBuffer/Apply Shadow Buffer");
            }
        }
#endif
        internal int GetSortIndex(ref RenderingData renderingData)
        {
            int additionalLightIndex;
            int lightIndex = shadowMaterialProperties.FindLightSourceIndex(ref renderingData, out additionalLightIndex);
            if (0 <= additionalLightIndex)
            {
                return additionalLightIndex;
            }
            else if (lightIndex == renderingData.lightData.mainLightIndex)
            {
                return renderingData.lightData.additionalLightsCount;
            }
            else if (0 < lightIndex)
            {
                return renderingData.lightData.additionalLightsCount + 1 + lightIndex;
            }
            else
            {
                return renderingData.lightData.additionalLightsCount + 1 + renderingData.lightData.visibleLights.Length;
            }
        }

        // use IComparable for sorting shadow buffer list because lambda expression cannot use 'ref'.
        // This can avoid making a copy of RenderingData.
        private int m_sortIndex;
        internal void CalculateSortIndex(ref RenderingData renderingData)
        {
            m_sortIndex = GetSortIndex(ref renderingData);
        }
        public int CompareTo(ShadowBuffer rhs)
        {
            return m_sortIndex - rhs.m_sortIndex;
        }
        
		internal void RegisterProjector(Camera cam, ProjectorForLWRP projector)
        {
            if (material == null)
            {
                return;
            }
            List<ProjectorForLWRP> projectors;
            if (!m_projectors.TryGetValue(cam, out projectors))
            {
                projectors = new List<ProjectorForLWRP>();
                m_projectors.Add(cam, projectors);
            }
            projectors.Add(projector);
        }
        internal void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData, out int applyPassCount)
        {
            List<ProjectorForLWRP> projectors;
            applyPassCount = 0;
            if (m_projectors.TryGetValue(renderingData.cameraData.camera, out projectors))
            {
                if (0 < projectors.Count)
                {
                    applyPass.renderPassEvent = renderPassEvent;
                    renderer.EnqueuePass(applyPass);
                    ++applyPassCount;
                }
            }
        }
        private bool m_appliedToLightPass = false;
        private CollectShadowBufferPass.RenderTextureRef m_shadowTextureRef = null;
        private int m_shadowTextureColorChannelIndex = 0;
        internal void CollectShadowBuffer(ScriptableRenderContext context, ref RenderingData renderingData, CollectShadowBufferPass.RenderTextureRef textureRef, int channelIndex)
        {
            m_shadowTextureRef = textureRef;
            m_shadowTextureColorChannelIndex = channelIndex;
            textureRef.Retain((ColorWriteMask)colorWriteMask);
            List < ProjectorForLWRP> projectors;
            if (m_projectors.TryGetValue(renderingData.cameraData.camera, out projectors))
            {
                if (projectors != null)
                {
                    for (int i = 0; i < projectors.Count; ++i)
                    {
                        projectors[i].Render(context, ref renderingData);
                    }
                    m_appliedToLightPass = false;
                    if (applyToLightingPass) {
                        int additionalLightIndex;
                        int lightIndex = shadowMaterialProperties.FindLightSourceIndex(ref renderingData, out additionalLightIndex);
                        if (0 <= lightIndex)
                        {
                            if (lightIndex == renderingData.lightData.mainLightIndex)
                            {
                                LitShaderState.SetMainLightShadow(m_shadowTextureRef.renderTexture, m_shadowTextureColorChannelIndex);
                                m_appliedToLightPass = true;
                            }
                            else if (0 <= additionalLightIndex)
                            {
                                m_appliedToLightPass = LitShaderState.SetAdditionalLightShadow(additionalLightIndex, m_shadowTextureRef.renderTexture, m_shadowTextureColorChannelIndex);
                            }
                        }
                    }
                }
            }
        }
        static readonly string[] KEYWORD_SHADOWTEX_CHANNELS = { "P4LWRP_SHADOWTEX_CHANNEL_A", "P4LWRP_SHADOWTEX_CHANNEL_B", "P4LWRP_SHADOWTEX_CHANNEL_G", "P4LWRP_SHADOWTEX_CHANNEL_R" };
        internal void ApplyShadowBuffer(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            PerObjectData requiredPerObjectData;

            bool appliedToLightPass = m_appliedToLightPass;
            m_appliedToLightPass = false;
            if (appliedToLightPass && ignoreLayersIfLightPassAvailable == -1)
            {
                return;
            }

            if (!shadowMaterialProperties.UpdateMaterialProperties(material, ref renderingData, out requiredPerObjectData))
            {
                return;
            }
            for (int i = 0; i < KEYWORD_SHADOWTEX_CHANNELS.Length; ++i)
            {
                if (m_shadowTextureColorChannelIndex == i)
                {
                    material.EnableKeyword(KEYWORD_SHADOWTEX_CHANNELS[i]);
                }
                else
                {
                    material.DisableKeyword(KEYWORD_SHADOWTEX_CHANNELS[i]);
                }
            }
            requiredPerObjectData |= perObjectData;
            List<ProjectorForLWRP> projectors;
            if (m_projectors.TryGetValue(renderingData.cameraData.camera, out projectors))
            {
                if (projectors != null)
                {
                    material.SetTexture(m_shadowTextureId, GetTemporaryShadowTexture());
                    for (int i = 0; i < projectors.Count; ++i)
                    {
                        projectors[i].Render(context, ref renderingData, this, requiredPerObjectData, appliedToLightPass ? (int)ignoreLayersIfLightPassAvailable : 0);
                    }
                }
            }
        }
        internal void ClearProjectosForCamera(Camera camera)
        {
            List<ProjectorForLWRP> projectors;
            if (m_projectors.TryGetValue(camera, out projectors))
            {
                if (projectors != null)
                {
                    projectors.Clear();
                }
            }
        }
        internal int colorWriteMask
        {
            get { return 1 << m_shadowTextureColorChannelIndex; }
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
