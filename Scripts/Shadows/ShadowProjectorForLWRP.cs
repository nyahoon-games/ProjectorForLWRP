﻿//
// ShadowProjectorForLWRP.cs
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
	public class ShadowProjectorForLWRP : ProjectorForLWRP
	{
		[SerializeField]
		private ShadowBuffer m_shadowBuffer = null;

		public ShadowBuffer shadowBuffer
		{
			get { return m_shadowBuffer; }
			set { m_shadowBuffer = value; }
		}

		private static bool s_isInitialized = false;
		static ShadowProjectorForLWRP()
		{
			StaticInitialize();
		}
		static protected new void StaticInitialize()
		{
			if (!s_isInitialized)
			{
				ProjectorForLWRP.StaticInitialize();
				s_isInitialized = true;
			}
		}

		private ShadowMaterialProperties m_shadowProperties;
		protected override void Initialize()
		{
			base.Initialize();
			m_shadowProperties = GetComponent<ShadowMaterialProperties>();
		}

		protected override void AddProjectorToRenderer(Camera camera)
		{
			if (m_shadowBuffer != null && m_shadowBuffer.isActiveAndEnabled)
			{
				m_shadowBuffer.AddShadowProjector(camera, this);
			}
			else
			{
				base.AddProjectorToRenderer(camera);
			}
		}

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

			PerObjectData requiredPerObjectData = PerObjectData.None;
			if (m_shadowProperties != null)
			{
				if (!m_shadowProperties.UpdateMaterialProperties(material, ref renderingData, out requiredPerObjectData))
				{
					return;
				}
			}
			if (useStencilTest)
			{
				WriteFrustumStencil(context);
			}
			requiredPerObjectData |= perObjectData;
			SetupCullingResultsForRendering(ref renderingData, ref cullingResults, requiredPerObjectData);
			DrawingSettings drawingSettings;
			FilteringSettings filteringSettings;
			RenderStateBlock renderStateBlock;
			GetDefaultDrawSettings(ref renderingData, material, out drawingSettings, out filteringSettings, out renderStateBlock);
			drawingSettings.perObjectData = requiredPerObjectData;
			context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);
		}
		internal void CollectShadows(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			CullingResults cullingResults;
			if (!TryGetCullingResults(renderingData.cameraData.camera, out cullingResults))
			{
				return;
			}
			Material material = GetDuplicatedProjectorMaterial();
			EnableProjectorForLWRPKeyword(material);
			P4LWRPShaderProperties.p4lwrp_ColorWriteMask.Set(material, shadowBuffer.colorWriteMask);
			SetupProjectorMatrix(material);

			if (useStencilTest)
			{
				WriteFrustumStencil(context);
			}

			DrawingSettings drawingSettings;
			FilteringSettings filteringSettings;
			RenderStateBlock renderStateBlock;
			GetDefaultDrawSettings(ref renderingData, material, out drawingSettings, out filteringSettings, out renderStateBlock);
			context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);
		}
		internal void ApplyShadowBuffer(ScriptableRenderContext context, ref RenderingData renderingData, Material material, PerObjectData requiredPerObjectData, int stencilMask)
		{
			CullingResults cullingResults;
			if (!TryGetCullingResults(renderingData.cameraData.camera, out cullingResults))
			{
				return;
			}
			SetupProjectorMatrix(material);

			requiredPerObjectData |= perObjectData;
			SetupCullingResultsForRendering(ref renderingData, ref cullingResults, requiredPerObjectData);
			DrawingSettings drawingSettings;
			FilteringSettings filteringSettings;
			RenderStateBlock renderStateBlock;
			GetDefaultDrawSettings(ref renderingData, material, out drawingSettings, out filteringSettings, out renderStateBlock);

			drawingSettings.perObjectData = requiredPerObjectData;
			drawingSettings.overrideMaterial = material;

			renderStateBlock.mask = RenderStateMask.Stencil;
			renderStateBlock.stencilReference = stencilMask;
			renderStateBlock.stencilState = new StencilState(true, (byte)stencilMask, (byte)stencilMask, CompareFunction.NotEqual, StencilOp.Replace, StencilOp.Keep, StencilOp.Keep);

			context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);
		}
	}
}
