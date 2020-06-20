//
// CollectShadowBufferPassManager.cs
//
// Projector For LWRP
//
// Copyright (c) 2020 NYAHOON GAMES PTE. LTD.
//

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;
using System.Collections.Generic;

namespace ProjectorForLWRP
{
	public class CollectShadowBufferPassManager : RenderPassManagerTemplate<CollectShadowBufferPassManager>
	{
		//
		// public members
		//
		public void AddShadowBuffer(Camera camera, ShadowBuffer shadowBuffer)
		{
			List<ShadowBuffer> shadowBufferList = m_activeShadowBufferList[camera];
			if (shadowBufferList.Count == 0)
			{
				// add collect pass only once a frame per camera.
				ProjectorRendererFeature.AddRenderPass(camera, m_collectShadowBufferPass);
			}
			if (!shadowBufferList.Contains(shadowBuffer))
			{
				shadowBufferList.Add(shadowBuffer);
			}
		}

		public CollectShadowBufferPassManager()
		{
			m_activeShadowBufferList = new CameraToShadowBufferListMap();
			m_collectShadowBufferPass = new CollectShadowBufferPass(m_activeShadowBufferList);
		}

		//
		// private members
		//
		private class CameraToShadowBufferListMap : ObjectPool<Collections.AutoClearList<ShadowBuffer>>.Map<Camera>, CollectShadowBufferPass.ICameraToShadowBufferListMap
		{
			IList<ShadowBuffer> CollectShadowBufferPass.ICameraToShadowBufferListMap.this[Camera camera]
			{
				get
				{
					return base[camera];
				}
			}
		}
		private CollectShadowBufferPass m_collectShadowBufferPass;
		private CameraToShadowBufferListMap m_activeShadowBufferList = new CameraToShadowBufferListMap();

		protected override void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
		{
			if (m_activeShadowBufferList.ContainsKey(camera))
			{
				CommandBuffer cmd = CommandBufferPool.Get();
				if (LitShaderState.ClearStates(cmd))
				{
					context.ExecuteCommandBuffer(cmd);
					cmd.Clear();
					CommandBufferPool.Release(cmd);
				}
				m_activeShadowBufferList.Remove(camera);
			}
		}
	}
}
