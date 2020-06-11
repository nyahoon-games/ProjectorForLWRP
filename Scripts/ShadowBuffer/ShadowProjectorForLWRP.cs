//
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
		private static int s_shaderPropIdColorWriteMask = -1;
		static ShadowProjectorForLWRP()
		{
			StaticInitialize();
		}
		static protected new void StaticInitialize()
		{
			if (!s_isInitialized)
			{
				ProjectorForLWRP.StaticInitialize();
				s_shaderPropIdColorWriteMask = Shader.PropertyToID("_ColorWriteMask");
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
			ProjectorRendererFeature.AddShadowProjector(this, camera);
		}

		public override void Render(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			CullingResults cullingResults;
			if (!GetCullingResults(renderingData.cameraData.camera, out cullingResults))
			{
				return;
			}
			Material material = GetTemporaryProjectorMaterial();
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
			if (!GetCullingResults(renderingData.cameraData.camera, out cullingResults))
			{
				return;
			}
			Material material = GetTemporaryProjectorMaterial();
			material.SetInt(s_shaderPropIdColorWriteMask, shadowBuffer.colorWriteMask);
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
		internal void ApplyShadowBuffer(ScriptableRenderContext context, ref RenderingData renderingData, PerObjectData requiredPerObjectData, int additionalIgnoreLayers = 0)
		{
			CullingResults cullingResults;
			if (!GetCullingResults(renderingData.cameraData.camera, out cullingResults))
			{
				return;
			}
			Material material = shadowBuffer.material;
			SetupProjectorMatrix(material);

			requiredPerObjectData |= perObjectData;
			SetupCullingResultsForRendering(ref renderingData, ref cullingResults, requiredPerObjectData);
			DrawingSettings drawingSettings;
			FilteringSettings filteringSettings;
			RenderStateBlock renderStateBlock;
			GetDefaultDrawSettings(ref renderingData, material, out drawingSettings, out filteringSettings, out renderStateBlock);

			drawingSettings.perObjectData = requiredPerObjectData;
			drawingSettings.overrideMaterial = material;
			filteringSettings.layerMask &= ~additionalIgnoreLayers;

			StencilState stencilState = renderStateBlock.stencilState;
			if (useStencilTest)
			{
#if UNITY_EDIOR
				if (shadowBufferStencilMask <= stencilMask)
				{
					Debug.LogWarning("The stencil mask value of the shadow buffer must be greater than the one of this projector.", this);
				}
#endif
				WriteFrustumStencil(context);

				stencilState.readMask |= (byte)shadowBuffer.stencilMask;
				stencilState.writeMask |= (byte)shadowBuffer.stencilMask;
			}
			else
			{
				renderStateBlock.mask = RenderStateMask.Stencil;
				renderStateBlock.stencilReference = shadowBuffer.stencilMask;
				stencilState = new StencilState(true, (byte)shadowBuffer.stencilMask, (byte)shadowBuffer.stencilMask, CompareFunction.NotEqual, StencilOp.Replace, StencilOp.Keep, StencilOp.Keep);
			}
			renderStateBlock.stencilState = stencilState;
			context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);
		}
	}
}
