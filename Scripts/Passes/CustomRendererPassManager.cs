//
// CustomRendererPassManager.cs
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
    public class CustomRendererPassManager : RenderPassManagerTemplate<CustomRendererPassManager>
	{
		//
		// public members
		//
		public void AddCustomRenderer(Camera camera, ICustomRenderer customRenderer)
		{
			CustomRendererPass pass = m_cameraToPassMap[camera][customRenderer.renderPassEvent];
			if (pass.rendererCount == 0)
			{
				ProjectorRendererFeature.AddRenderPass(camera, pass);
			}
			pass.AddRenderer(customRenderer);
		}

		//
		// private members
		//
		private ObjectPool<ObjectPool<CustomRendererPass>.AutoClearMap<RenderPassEvent>>.Map<Camera> m_cameraToPassMap = new ObjectPool<ObjectPool<CustomRendererPass>.AutoClearMap<RenderPassEvent>>.Map<Camera>();
		protected override void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
		{
			m_cameraToPassMap.Remove(camera);
		}
	}
}
