//
// CasterObject.cs
//
// Projector For LWRP For LWRP
//
// Copyright (c) 2020 NYAHOON GAMES PTE. LTD.
//

using UnityEngine;

namespace ProjectorForLWRP
{
	public abstract class CasterObject : MonoBehaviour
	{
		public struct ShadowBounds
		{
			public float top;
			public float bottom;
			public float left;
			public float right;
			public float near;
			public float far;
			public void Merge(ref ShadowBounds otherBounds)
			{
				top = Mathf.Max(top, otherBounds.top);
				bottom = Mathf.Min(bottom, otherBounds.bottom);
				left = Mathf.Min(left, otherBounds.left);
				right = Mathf.Max(right, otherBounds.right);
				near = Mathf.Min(near, otherBounds.near);
				far = Mathf.Max(far, otherBounds.far);
			}
		}
		public struct ProjectorBases
		{
			public Vector3 forward;
			public Vector3 up;
			public Vector3 right;
		}
		public abstract void CalculateShadowBounds(Transform lightTransform, bool isDirectional, out ShadowBounds shadowBounds, out ProjectorBases projectorBases);
	}
}
