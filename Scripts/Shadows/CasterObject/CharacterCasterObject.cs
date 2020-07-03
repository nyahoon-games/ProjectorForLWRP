//
// CharacterCasterObject.cs
//
// Projector For LWRP For LWRP
//
// Copyright (c) 2020 NYAHOON GAMES PTE. LTD.
//

using UnityEngine;

namespace ProjectorForLWRP
{
	public class CharacterCasterObject : CasterObject
	{
		[System.Serializable]
		private struct ShadowAnchor
		{
			public Transform transform;
			public Vector3 localPos;
			public float radius;

			internal Vector3 worldPos { get; private set; }
			internal void UpdateWorldPos(Transform defaultTransform)
			{
				if (transform != null)
				{
					defaultTransform = transform;
				}
				worldPos = defaultTransform.TransformPoint(localPos);
			}
		}
		[System.Serializable]
		private struct BodySlice
		{
			public float positionY;
			public float radiusX;
			public float radiusZ;
		}
		[SerializeField]
		private Quaternion m_bodyRotation = Quaternion.identity;
		[SerializeField]
		private ShadowAnchor m_head = new ShadowAnchor()
		{
			transform = null,
			localPos = new Vector3(0, 1, 0),
			radius = 0.2f,
		};
		[SerializeField]
		private BodySlice[] m_bodySlices = new BodySlice[] {
			new BodySlice() {
				positionY = 0.5f,
				radiusX = 0.5f,
				radiusZ = 0.5f
			}
		};
		[SerializeField]
		private ShadowAnchor m_foot = new ShadowAnchor()
		{
			transform = null,
			localPos = new Vector3(0, 0, 0),
			radius = 0.2f,
		};

		const float EPSILON = 0.0001f;

		private BodySlice m_largestSlice = new BodySlice() { positionY = 0, radiusX = 0, radiusZ = 0 };
		protected void Initialize()
		{
			float lagestArea;
			if (m_head.radius < m_foot.radius)
			{
				m_largestSlice.positionY = 0;
				m_largestSlice.radiusX = m_foot.radius;
				m_largestSlice.radiusZ = m_foot.radius;
				lagestArea = m_foot.radius * m_foot.radius;
			}
			else
			{
				m_largestSlice.positionY = 1;
				m_largestSlice.radiusX = m_head.radius;
				m_largestSlice.radiusZ = m_head.radius;
				lagestArea = m_head.radius * m_head.radius;
			}
			for (int i = 0; i < m_bodySlices.Length; ++i)
			{
				ref BodySlice slice = ref m_bodySlices[i];
				float sliceArea = slice.radiusX * slice.radiusZ;
				if (lagestArea < sliceArea)
				{
					m_largestSlice = slice;
				}
			}
		}

		private void OnValidate()
		{
			Initialize();
			m_objectUpdated = true;
		}

		private bool m_objectUpdated = true;
		private void Update()
		{
			m_objectUpdated = true;
		}

		private struct CasterCoordinates
		{
			public Vector3 right;
			public Vector3 up;
			public Vector3 forward;
			public float height;
			public float sliceScaleX;
			public float sliceScaleZ;
		}
		private struct ProjectorCoordinates
		{
			public Vector3 position;
			public Vector3 forward;
			public Vector3 up;
			public Vector3 right;
			public Vector3 forwardOnSlice;
			public Vector3 upOnSlice;
			public Vector3 rightOnSlice;
			public Vector2 sliceForward;
			public Vector2 sliceRight;
			public bool isOrthographic;
		}
		private CasterCoordinates m_casterCoordinates = new CasterCoordinates();
		public override void CalculateShadowBounds(Transform lightTransform, bool isDirectional, out ShadowBounds shadowBounds, out ProjectorBases projectorBases)
		{
			UpdateCasterCoordinates();

			Vector3 lightDir;
			if (isDirectional)
			{
				lightDir = lightTransform.forward;
			}
			else
			{
				Vector3 posCenter = 0.5f * (m_head.worldPos + m_foot.worldPos);
				lightDir = (posCenter - lightTransform.position).normalized;
			}
			// calculate projector coordinates
			ProjectorCoordinates projCoords = new ProjectorCoordinates();
			projCoords.position = lightTransform.position;
			projCoords.isOrthographic = isDirectional;
			CalculateProjectorCoordinates(ref projCoords, lightDir);

			// calculate shadow bounds
			CalculateShadowBoundsInWorldSpace(ref projCoords, m_head.worldPos, m_casterCoordinates.sliceScaleX * m_head.radius, m_casterCoordinates.sliceScaleZ * m_head.radius, out shadowBounds);
			ShadowBounds sliceBounds;
			CalculateShadowBoundsInWorldSpace(ref projCoords, m_foot.worldPos, m_casterCoordinates.sliceScaleX * m_foot.radius, m_casterCoordinates.sliceScaleZ * m_foot.radius, out sliceBounds);
			shadowBounds.Merge(ref sliceBounds);
			foreach (BodySlice slice in m_bodySlices)
			{
				Vector3 posSlice = Vector3.Lerp(m_foot.worldPos, m_head.worldPos, slice.positionY);
				CalculateShadowBoundsInWorldSpace(ref projCoords, posSlice, m_casterCoordinates.sliceScaleX * slice.radiusX, m_casterCoordinates.sliceScaleZ * slice.radiusZ, out sliceBounds);
				shadowBounds.Merge(ref sliceBounds);
			}

			projectorBases.forward = projCoords.forward;
			projectorBases.up = projCoords.up;
			projectorBases.right = projCoords.right;
		}
		private void UpdateCasterCoordinates()
		{
			if (m_largestSlice.radiusX == 0 && m_largestSlice.radiusX == 0)
			{
				Initialize();
			}
			if (!m_objectUpdated)
			{
				return;
			}
			m_objectUpdated = false;
			m_head.UpdateWorldPos(transform);
			m_foot.UpdateWorldPos(transform);
			m_casterCoordinates.up = m_head.worldPos - m_foot.worldPos;
			m_casterCoordinates.height = m_casterCoordinates.up.magnitude;
			m_casterCoordinates.up *= 1.0f / m_casterCoordinates.height;
			Quaternion bodyRotation = transform.rotation * m_bodyRotation;
			m_casterCoordinates.forward = bodyRotation * Vector3.forward;
			m_casterCoordinates.right = Vector3.Cross(m_casterCoordinates.forward, m_casterCoordinates.up);
			if (m_casterCoordinates.right.sqrMagnitude < EPSILON)
			{
				float sign = (Vector3.Dot(m_casterCoordinates.forward, m_casterCoordinates.up) < 0) ? -1 : 1;
				m_casterCoordinates.right = sign * Vector3.Cross(m_casterCoordinates.up, bodyRotation * Vector3.up);
			}
			else
			{
				m_casterCoordinates.right.Normalize();
			}
			m_casterCoordinates.forward = Vector3.Cross(m_casterCoordinates.up, m_casterCoordinates.right);
			Quaternion invRotation = Quaternion.Inverse(transform.rotation);
			m_casterCoordinates.sliceScaleX = transform.TransformVector(invRotation * m_casterCoordinates.right).magnitude;
			m_casterCoordinates.sliceScaleZ = transform.TransformVector(invRotation * m_casterCoordinates.forward).magnitude;
		}
		private void CalculateProjectorCoordinates(ref ProjectorCoordinates projCoords, Vector3 projDir)
		{
			projCoords.forward = projDir;
			Vector3 projRight = Vector3.Cross(projDir, m_casterCoordinates.up);
			if (projRight.sqrMagnitude < EPSILON)
			{
				// projection dir is almost parallel to target up vector.
				// calculate projector up vector based on target forwad vector.
				projRight = Vector3.Cross(m_casterCoordinates.forward, projDir);
				Vector3 projRightOnSlice = projRight - Vector3.Dot(projRight, m_casterCoordinates.up) * m_casterCoordinates.up;
				projCoords.right = projRight.normalized;
				projCoords.up = Vector3.Cross(projCoords.right, projDir);
				projCoords.rightOnSlice = projRightOnSlice.normalized;
				projCoords.upOnSlice = Vector3.Cross(projCoords.right, m_casterCoordinates.forward);
				projCoords.forwardOnSlice = Vector3.zero;
				projCoords.sliceForward = Vector2.zero;
				projCoords.sliceRight.x = Vector3.Dot(m_casterCoordinates.right, projCoords.rightOnSlice);
				projCoords.sliceRight.y = Vector3.Dot(m_casterCoordinates.forward, projCoords.rightOnSlice);
			}
			else
			{
				// calculate projector up vector based on target up vector.
				projCoords.right = projRight.normalized;
				projCoords.up = Vector3.Cross(projCoords.right, projDir);
				projCoords.rightOnSlice = projCoords.right;
				projCoords.upOnSlice = Vector3.Cross(m_casterCoordinates.up, projCoords.right);
				projCoords.forwardOnSlice = (projDir - Vector3.Dot(projDir, m_casterCoordinates.up) * m_casterCoordinates.up).normalized;
				projCoords.sliceForward.x = Vector3.Dot(m_casterCoordinates.right, projCoords.forwardOnSlice);
				projCoords.sliceForward.y = Vector3.Dot(m_casterCoordinates.forward, projCoords.forwardOnSlice);

				// if projected largest slice height is greater than head to foot projected height, smoothly shift to forward base or right base up vector.
				float dirDotUp = Mathf.Abs(Vector3.Dot(projDir, m_casterCoordinates.up));
				float dirDotRight = Mathf.Abs(Vector3.Dot(projDir, m_casterCoordinates.right));
				float dirDotForward = Mathf.Abs(Vector3.Dot(projDir, m_casterCoordinates.forward));
				float projectedBaseSize = dirDotUp * (m_head.radius + m_foot.radius);
				if (Mathf.Approximately(m_casterCoordinates.sliceScaleX, m_casterCoordinates.sliceScaleZ))
				{
					projectedBaseSize *= m_casterCoordinates.sliceScaleX;
				}
				else
				{
					float xx = projCoords.sliceForward.x * m_casterCoordinates.sliceScaleX;
					float yz = projCoords.sliceForward.y * m_casterCoordinates.sliceScaleZ;
					projectedBaseSize *= Mathf.Sqrt(xx * xx + yz * yz);
				}
				float projectedHeight = Mathf.Sqrt(1 - dirDotUp * dirDotUp) * m_casterCoordinates.height + projectedBaseSize;
				float projectedSliceWidth = 2.0f * Mathf.Sqrt(1.0f - dirDotRight * dirDotRight) * m_largestSlice.radiusX * m_casterCoordinates.sliceScaleX;
				float projectedSliceDepth = 2.0f * Mathf.Sqrt(1.0f - dirDotForward * dirDotForward) * m_largestSlice.radiusZ * m_casterCoordinates.sliceScaleZ;
				float projectedMaxSliceSize = Mathf.Max(projectedSliceWidth, projectedSliceDepth);
				if (projectedHeight < projectedMaxSliceSize)
				{
					float radiusX = m_largestSlice.radiusX * m_casterCoordinates.sliceScaleX;
					float radiusZ = m_largestSlice.radiusZ * m_casterCoordinates.sliceScaleZ;
					float projectedSliceHeight;
					if (Mathf.Approximately(radiusX, radiusZ))
					{
						projectedSliceHeight = dirDotUp * radiusX;
					}
					else
					{
						float xx = projCoords.sliceForward.x * radiusX;
						float yz = projCoords.sliceForward.y * radiusZ;
						projectedSliceHeight = dirDotUp * Mathf.Sqrt(xx * xx + yz * yz);
					}
					float transitionBaseSize = Mathf.Min(Mathf.Min(projectedSliceWidth, projectedSliceDepth), projectedSliceHeight);
					transitionBaseSize = Mathf.Max(transitionBaseSize, projectedBaseSize);
					// smooth transition parameter
					float t = (projectedHeight - transitionBaseSize) / (projectedMaxSliceSize - transitionBaseSize);
					// calculate projector up vector based on target forward or right vector.
					float cosForward = Vector3.Dot(projCoords.upOnSlice, m_casterCoordinates.forward);
					float cosRight = Vector3.Dot(projCoords.upOnSlice, m_casterCoordinates.right);
					float cos, sin;
					if (Mathf.Abs(cosForward) < Mathf.Abs(cosRight))
					{
						cos = cosRight;
						sin = cosForward;
					}
					else
					{
						cos = cosForward;
						sin = cosRight;
					}
					sin = t * sin;
					float absCos = Mathf.Abs(1 - sin * sin);
					cos = cos < 0 ? -absCos : absCos;
					projCoords.rightOnSlice = cos * projCoords.rightOnSlice + sin * projCoords.upOnSlice;
					projCoords.upOnSlice = Vector3.Cross(m_casterCoordinates.up, projCoords.rightOnSlice);
				}
				projCoords.sliceRight.x = Vector3.Dot(m_casterCoordinates.right, projCoords.rightOnSlice);
				projCoords.sliceRight.y = Vector3.Dot(m_casterCoordinates.forward, projCoords.rightOnSlice);
			}
		}
		private void CalculateShadowBoundsInWorldSpace(ref ProjectorCoordinates projCoords, Vector3 slicePos, float sliceRadiusX, float sliceRadiusZ, out ShadowBounds shadowBounds)
		{
			float sliceHalfWidth, sliceHalfHeight, sliceHalfDepth;
			if (Mathf.Approximately(sliceRadiusX, sliceRadiusZ))
			{
				sliceHalfWidth = sliceHalfHeight = sliceHalfDepth = sliceRadiusX;
			}
			else
			{
				float xx = sliceRadiusX * projCoords.sliceRight.x;
				float zy = sliceRadiusZ * projCoords.sliceRight.y;
				float xy = sliceRadiusX * projCoords.sliceRight.y;
				float zx = sliceRadiusZ * projCoords.sliceRight.x;
				sliceHalfWidth = Mathf.Sqrt(xx * xx + zy * zy);
				sliceHalfHeight = Mathf.Sqrt(xy * xy + zx * zx);
				float xfx = sliceRadiusX * projCoords.sliceForward.x;
				float zfy = sliceRadiusZ * projCoords.sliceForward.y;
				sliceHalfDepth = Mathf.Sqrt(xfx * xfx + zfy * zfy);
			}
			slicePos -= projCoords.position;
			Vector3 up = sliceHalfHeight * projCoords.upOnSlice;
			Vector3 right = sliceHalfWidth * projCoords.rightOnSlice;
			Vector3 posTop = slicePos + up;
			Vector3 posBottom = slicePos - up;
			Vector3 posLeft = slicePos - right;
			Vector3 posRight = slicePos + right;
			float depth = sliceHalfDepth * Mathf.Abs(Vector3.Dot(projCoords.forwardOnSlice, projCoords.forward));
			float centerDepth = Vector3.Dot(slicePos, projCoords.forward);
			shadowBounds.near = centerDepth - depth;
			shadowBounds.far = centerDepth + depth;

			shadowBounds.top = Vector3.Dot(projCoords.up, posTop);
			shadowBounds.bottom = Vector3.Dot(projCoords.up, posBottom);
			shadowBounds.left = Vector3.Dot(projCoords.right, posLeft);
			shadowBounds.right = Vector3.Dot(projCoords.right, posRight);
			if (!projCoords.isOrthographic)
			{
				shadowBounds.top /= Vector3.Dot(projCoords.forward, posTop);
				shadowBounds.bottom /= Vector3.Dot(projCoords.forward, posBottom);
				shadowBounds.left /= Vector3.Dot(projCoords.forward, posLeft);
				shadowBounds.right /= Vector3.Dot(projCoords.forward, posRight);
			}
		}
#if UNITY_EDITOR
		private Mesh m_gizmoMesh;
		private Vector3[] m_gizmoVertices;
		private Vector3[] m_gizmoNormals;
		private int[] m_gizmoTriangles;
		public bool drawGizmo { get; set; } = true;
		const int VERTEX_COUNT_PER_SLICE = 16;
		System.Collections.Generic.List<BodySlice> m_bodySliceList;
		private class BodySlicePositionComparer : System.Collections.Generic.IComparer<BodySlice>
		{
			public int Compare(BodySlice x, BodySlice y)
			{
				return y.positionY.CompareTo(x.positionY);
			}
		};
		private static BodySlicePositionComparer s_bodySlicePositionComparer = new BodySlicePositionComparer();
		private void UpdateGizmoMesh()
		{
			UpdateCasterCoordinates();
			if (m_gizmoMesh == null)
			{
				m_gizmoMesh = new Mesh();
			}
			int vertexCount = 2 + (2 + m_bodySlices.Length) * 2 * VERTEX_COUNT_PER_SLICE;
			int indexCount = 6 * VERTEX_COUNT_PER_SLICE * (2 + m_bodySlices.Length);
			if (m_gizmoVertices == null || m_gizmoVertices.Length != vertexCount)
			{
				m_gizmoVertices = new Vector3[vertexCount];
				m_gizmoNormals = new Vector3[vertexCount];
				m_gizmoTriangles = new int[indexCount];
				int triangleIndex = 0;
				// head disc triangles
				for (int i = 0; i < VERTEX_COUNT_PER_SLICE - 1; ++i)
				{
					m_gizmoTriangles[triangleIndex++] = 0;
					m_gizmoTriangles[triangleIndex++] = i + 1;
					m_gizmoTriangles[triangleIndex++] = (i + 2);
				}
				m_gizmoTriangles[triangleIndex++] = 0;
				m_gizmoTriangles[triangleIndex++] = VERTEX_COUNT_PER_SLICE;
				m_gizmoTriangles[triangleIndex++] = 1;
				// body triangles
				for (int i = 0; i < m_bodySlices.Length + 1; ++i)
				{
					int baseIndex = 1 + VERTEX_COUNT_PER_SLICE + 2 * i * VERTEX_COUNT_PER_SLICE;
					for (int j = 0; j < VERTEX_COUNT_PER_SLICE - 1; ++j)
					{
						m_gizmoTriangles[triangleIndex++] = baseIndex + j;
						m_gizmoTriangles[triangleIndex++] = baseIndex + j + VERTEX_COUNT_PER_SLICE;
						m_gizmoTriangles[triangleIndex++] = baseIndex + j + 1;
						m_gizmoTriangles[triangleIndex++] = baseIndex + j + 1;
						m_gizmoTriangles[triangleIndex++] = baseIndex + j + VERTEX_COUNT_PER_SLICE;
						m_gizmoTriangles[triangleIndex++] = baseIndex + j + VERTEX_COUNT_PER_SLICE + 1;
					}
					m_gizmoTriangles[triangleIndex++] = baseIndex + VERTEX_COUNT_PER_SLICE - 1;
					m_gizmoTriangles[triangleIndex++] = baseIndex + 2 * VERTEX_COUNT_PER_SLICE - 1;
					m_gizmoTriangles[triangleIndex++] = baseIndex;
					m_gizmoTriangles[triangleIndex++] = baseIndex;
					m_gizmoTriangles[triangleIndex++] = baseIndex + 2 * VERTEX_COUNT_PER_SLICE - 1;
					m_gizmoTriangles[triangleIndex++] = baseIndex + VERTEX_COUNT_PER_SLICE;
				}
				// foot disc triangles
				for (int i = 0; i < VERTEX_COUNT_PER_SLICE - 1; ++i)
				{
					m_gizmoTriangles[triangleIndex++] = vertexCount - 1;
					m_gizmoTriangles[triangleIndex++] = vertexCount - i - 2;
					m_gizmoTriangles[triangleIndex++] = vertexCount - i - 3;
				}
				m_gizmoTriangles[triangleIndex++] = vertexCount - 1;
				m_gizmoTriangles[triangleIndex++] = vertexCount - VERTEX_COUNT_PER_SLICE - 1;
				m_gizmoTriangles[triangleIndex++] = vertexCount - VERTEX_COUNT_PER_SLICE - 2;
				Debug.Assert(triangleIndex == indexCount);
			}
			if (m_objectUpdated)
			{
				m_head.UpdateWorldPos(transform);
				m_foot.UpdateWorldPos(transform);
				UpdateCasterCoordinates();
				m_objectUpdated = false;
			}

			// calculate gizmo vertices in world coordinates.
			int index = 0;
			m_gizmoVertices[index] = m_head.worldPos;
			m_gizmoNormals[index] = m_casterCoordinates.up;
			++index;
			float lastRadiusX = m_head.radius;
			float lastRadiusZ = m_head.radius;
			float lastSlicePosY = 1;
			FillGizmoSliceVertices(ref index, lastRadiusX, lastRadiusZ, m_head.worldPos, m_casterCoordinates.up);
			if (m_bodySliceList == null)
			{
				m_bodySliceList = new System.Collections.Generic.List<BodySlice>();
			}
			m_bodySliceList.Clear();
			m_bodySliceList.AddRange(m_bodySlices);
			HelperFunctions.GarbageFreeSort(m_bodySliceList, s_bodySlicePositionComparer);
			for (int i = 0; i < m_bodySliceList.Count; ++i)
			{
				BodySlice slice = m_bodySliceList[i];
				FillGizmoSliceVertices(ref index, slice.radiusX, slice.radiusZ, slice.positionY, lastRadiusX, lastRadiusZ, lastSlicePosY);
				lastRadiusX = slice.radiusX;
				lastRadiusZ = slice.radiusZ;
				lastSlicePosY = slice.positionY;
			}
			FillGizmoSliceVertices(ref index, m_foot.radius, m_foot.radius, 0, lastRadiusX, lastRadiusZ, lastSlicePosY);
			FillGizmoSliceVertices(ref index, m_foot.radius, m_foot.radius, m_foot.worldPos, -m_casterCoordinates.up);
			m_gizmoVertices[index] = m_foot.worldPos;
			m_gizmoNormals[index] = -m_casterCoordinates.up;
			++index;
			Debug.Assert(index == vertexCount);
			m_gizmoMesh.vertices = m_gizmoVertices;
			m_gizmoMesh.normals = m_gizmoNormals;
			m_gizmoMesh.triangles = m_gizmoTriangles;
		}
		private void FillGizmoSliceVertices(ref int index, float radiusX, float radiusZ, Vector3 pos, Vector3 normal)
		{
			Vector3 vecX = radiusX * m_casterCoordinates.sliceScaleX * m_casterCoordinates.right;
			Vector3 vecZ = radiusZ * m_casterCoordinates.sliceScaleZ * m_casterCoordinates.forward;
			float deltaTheta = 2.0f * Mathf.PI / VERTEX_COUNT_PER_SLICE;
			for (int i = 0; i < VERTEX_COUNT_PER_SLICE; ++i, ++index)
			{
				float x = Mathf.Cos(i * deltaTheta);
				float z = Mathf.Sin(i * deltaTheta);
				m_gizmoVertices[index] = pos + x * vecX + z * vecZ;
				m_gizmoNormals[index] = normal;
			}
		}
		private void FillGizmoSliceVertices(ref int index, float radiusX, float radiusZ, float slicePosY, float lastRadiusX, float lastRadiusZ, float lastSlicePosY)
		{
			Vector3 pos0 = m_foot.worldPos + lastSlicePosY * m_casterCoordinates.height * m_casterCoordinates.up;
			Vector3 vecX0 = lastRadiusX * m_casterCoordinates.sliceScaleX * m_casterCoordinates.right;
			Vector3 vecZ0 = lastRadiusZ * m_casterCoordinates.sliceScaleZ * m_casterCoordinates.forward;
			Vector3 pos1 = m_foot.worldPos + slicePosY * m_casterCoordinates.height * m_casterCoordinates.up;
			Vector3 vecX1 = radiusX * m_casterCoordinates.sliceScaleX * m_casterCoordinates.right;
			Vector3 vecZ1 = radiusZ * m_casterCoordinates.sliceScaleZ * m_casterCoordinates.forward;
			float deltaTheta = 2.0f * Mathf.PI / VERTEX_COUNT_PER_SLICE;
			for (int i = 0; i < VERTEX_COUNT_PER_SLICE; ++i, ++index)
			{
				float x = Mathf.Cos(i * deltaTheta);
				float z = Mathf.Sin(i * deltaTheta);
				m_gizmoVertices[index] = pos0 + x * vecX0 + z * vecZ0;
				m_gizmoVertices[index + VERTEX_COUNT_PER_SLICE] = pos1 + x * vecX1 + z * vecZ1;
				Vector3 binormal = (m_gizmoVertices[index] - m_gizmoVertices[index + VERTEX_COUNT_PER_SLICE]);
				Vector3 tangent0 = (x * vecZ0 - z * vecX0);
				Vector3 tangent1 = (x * vecZ0 - z * vecX0);
				m_gizmoNormals[index] = Vector3.Cross(tangent0, binormal);
				m_gizmoNormals[index + VERTEX_COUNT_PER_SLICE] = Vector3.Cross(tangent1, binormal);
			}
			index += VERTEX_COUNT_PER_SLICE;
		}
		private void OnDrawGizmosSelected()
		{
			if (drawGizmo)
			{
				UpdateGizmoMesh();
				Gizmos.color = new Color(1.0f, 0.5f, 0.0f);
				Gizmos.DrawMesh(m_gizmoMesh);
			}
		}
#endif
	}
}
