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
    public class ShadowBuffer : MonoBehaviour
    {
        public Material material;
        public string shadowTextureName = "_ShadowTex";
        public int stencilMask = 0x2;
        public UnityEngine.Rendering.LWRP.RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        public PerObjectData perObjectData = PerObjectData.None;
        public bool insertDepthOnlyPassIfNecessary = true;

        private Dictionary<Camera, List<ProjectorForLWRP>> m_projectors = new Dictionary<Camera, List<ProjectorForLWRP>>();
        private CollectShadowBufferPass m_collectPass;
        private ApplyShadowBufferPass m_applyPass;
        private ShadowMaterialProperties m_shadowMaterialProperties;
        private int m_shadowTextureId;
        private void Initialize()
        {
            m_collectPass = new CollectShadowBufferPass(this);
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
        CollectShadowBufferPass collectPass
        {
            get
            {
                if (m_collectPass == null)
                {
                    Initialize();
                }
                return m_collectPass;
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
        internal void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            List<ProjectorForLWRP> projectors;
            if (m_projectors.TryGetValue(renderingData.cameraData.camera, out projectors))
            {
                if (0 < projectors.Count)
                {
                    applyPass.renderPassEvent = renderPassEvent;
                    renderer.EnqueuePass(collectPass);
                    renderer.EnqueuePass(applyPass);
                }
            }
        }
        internal void CollectShadowBuffer(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            List<ProjectorForLWRP> projectors;
            if (m_projectors.TryGetValue(renderingData.cameraData.camera, out projectors))
            {
                if (projectors != null)
                {
                    for (int i = 0; i < projectors.Count; ++i)
                    {
                        projectors[i].Render(context, ref renderingData);
                    }
                }
            }
        }
        static readonly string[] KEYWORD_SHADOWTEX_CHANNELS = { "P4LWRP_SHADOWTEX_CHANNEL_A", "P4LWRP_SHADOWTEX_CHANNEL_B", "P4LWRP_SHADOWTEX_CHANNEL_G", "P4LWRP_SHADOWTEX_CHANNEL_R" };
        internal void ApplyShadowBuffer(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            PerObjectData requiredPerObjectData;
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
                        projectors[i].Render(context, ref renderingData, this, requiredPerObjectData);
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
        internal class RenderTextureRef
        {
            public RenderTexture renderTexture = null;
            public int refCount = 0;
            public int releaseCount = 0;
        }
        private static List<RenderTextureRef> s_tempRenderTextureList = new List<RenderTextureRef>();
        internal static bool IsFirstCollectPass()
        {
            return s_tempRenderTextureList.Count == 0 || s_tempRenderTextureList[0].refCount == 1;
        }
        private RenderTextureRef m_shadowTextureRef = null;
        private int m_shadowTextureColorChannelIndex = 0;
        internal int colorWriteMask
        {
            get { return 1 << m_shadowTextureColorChannelIndex; }
        }
        internal RenderTextureRef CreateTemporaryShadowTexture(int width, int height)
        {
            RenderTextureRef textureRef = null;
            for (int i = 0; i < s_tempRenderTextureList.Count; ++i)
            {
                int refCount = s_tempRenderTextureList[i].refCount;
                RenderTexture renderTexture = s_tempRenderTextureList[i].renderTexture;
                if (renderTexture == null || (refCount < 4 && renderTexture.width == width && renderTexture.height == height))
                {
                    textureRef = s_tempRenderTextureList[i];
                    break;
                }
            }
            if (textureRef == null)
            {
                textureRef = new RenderTextureRef();
                s_tempRenderTextureList.Add(textureRef);
            }
            if (textureRef.renderTexture == null)
            {
                textureRef.renderTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            }
            int colorChannelIndex = textureRef.refCount++;
            m_shadowTextureColorChannelIndex = colorChannelIndex;
            m_shadowTextureRef = textureRef;
            return textureRef;
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
                if (++m_shadowTextureRef.releaseCount == m_shadowTextureRef.refCount)
                {
                    RenderTexture.ReleaseTemporary(m_shadowTextureRef.renderTexture);
                    m_shadowTextureRef.renderTexture = null;
                    m_shadowTextureRef.refCount = 0;
                    m_shadowTextureRef.releaseCount = 0;
                }
                m_shadowTextureRef = null;
            }
        }
    }
}
