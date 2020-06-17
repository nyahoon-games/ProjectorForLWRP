//
// LightProjectorForLWRP.cs
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
    public class LightProjectorForLWRP : ProjectorForLWRP
    {
		[SerializeField]
		private ShadowBuffer m_shadowBuffer = null;
		[SerializeField]
		private string m_shadowTexPropertyName = "_ShadowTex";

		public ShadowBuffer shadowBuffer
		{
			get { return m_shadowBuffer; }
			set { m_shadowBuffer = value; }
		}

		static LightProjectorForLWRP()
		{
			StaticInitialize();
		}

		int m_shadowTexPropertyId;
		protected override void Initialize()
		{
			base.Initialize();
			m_shadowTexPropertyId = Shader.PropertyToID(m_shadowTexPropertyName);
		}

		private void OnValidate()
		{
			m_shadowTexPropertyId = Shader.PropertyToID(m_shadowTexPropertyName);
		}

		static readonly string[] COLORCHANNEL_KEYWORDS = { "P4LWRP_SHADOWTEX_CHANNEL_A", "P4LWRP_SHADOWTEX_CHANNEL_B", "P4LWRP_SHADOWTEX_CHANNEL_G", "P4LWRP_SHADOWTEX_CHANNEL_R", "P4LWRP_SHADOWTEX_CHANNEL_RGB" };
		public override void Render(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			CullingResults cullingResults;
			if (!TryGetCullingResults(renderingData.cameraData.camera, out cullingResults))
			{
				return;
			}
			Material material = GetDuplicatedProjectorMaterial();
			EnableProjectorForLWRPKeyword(material);
			SetupProjectorMatrix(material);

			if (m_shadowBuffer != null && m_shadowBuffer.isActiveAndEnabled && m_shadowBuffer.GetTemporaryShadowTexture() != null)
			{
				int colorWriteMask = m_shadowBuffer.colorWriteMask;
				bool isMonochrome = false;
				for (int i = 0; i < 4; ++i)
				{
					if (colorWriteMask == (1 << i))
					{
						material.EnableKeyword(COLORCHANNEL_KEYWORDS[i]);
						isMonochrome = true;
					}
					else
					{
						material.DisableKeyword(COLORCHANNEL_KEYWORDS[i]);
					}
				}
				if (isMonochrome)
				{
					material.DisableKeyword(COLORCHANNEL_KEYWORDS[4]);
				}
				else
				{
					material.EnableKeyword(COLORCHANNEL_KEYWORDS[4]);
				}
				material.SetTexture(m_shadowTexPropertyId, m_shadowBuffer.GetTemporaryShadowTexture());
			}
			else
			{
				for (int i = 0, count = COLORCHANNEL_KEYWORDS.Length; i < count; ++i)
				{
					material.DisableKeyword(COLORCHANNEL_KEYWORDS[i]);
				}
			}

			if (useStencilTest)
			{
				WriteFrustumStencil(context);
			}

			SetupCullingResultsForRendering(ref renderingData, ref cullingResults, perObjectData);
			DrawingSettings drawingSettings;
			FilteringSettings filteringSettings;
			RenderStateBlock renderStateBlock;
			GetDefaultDrawSettings(ref renderingData, material, out drawingSettings, out filteringSettings, out renderStateBlock);
			context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);
		}
	}
}
