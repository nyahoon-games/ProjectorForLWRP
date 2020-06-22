//
// RenderPassManagerTemplate.cs
//
// Projector For LWRP
//
// Copyright (c) 2020 NYAHOON GAMES PTE. LTD.
//

using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectorForLWRP
{
    public abstract class RenderPassManagerTemplate<T> where T : new()
    {
		public static T staticInstance { get; private set; }

		protected abstract void OnEndCameraRendering(ScriptableRenderContext context, Camera camera);

		static RenderPassManagerTemplate()
		{
			staticInstance = new T();
		}
		public RenderPassManagerTemplate()
		{
			RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
		}
		~RenderPassManagerTemplate()
		{
			RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
		}
	}
}
