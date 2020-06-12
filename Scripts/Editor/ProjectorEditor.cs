//
// ProjectorEditor.cs
//
// Projector For LWRP
//
// Copyright (c) 2019 NYAHOON GAMES PTE. LTD.
//

using UnityEngine;
using UnityEditor;

namespace ProjectorForLWRP
{
	[CustomEditor(typeof(ProjectorForLWRP))]
	public class ProjectorEditor : Editor
	{
		GUIStyle m_errorStyle;
		protected GUIStyle errorStyle
		{
			get
			{
				if (m_errorStyle == null)
				{
					m_errorStyle = new GUIStyle();
					m_errorStyle.richText = true;
					m_errorStyle.wordWrap = true;
				}
				return m_errorStyle;
			}
		}
		bool m_isOrthographic;
		float m_orthographicSize;
		float m_aspect;
		float m_fov;
		float m_far;
		float m_near;
		private void OnEnable()
		{
			ProjectorForLWRP projector = target as ProjectorForLWRP;
			Projector baseProjector = projector.GetComponent<Projector>();
			m_isOrthographic = baseProjector.orthographic;
			m_orthographicSize = baseProjector.orthographicSize;
			m_aspect = baseProjector.aspectRatio;
			m_fov = baseProjector.fieldOfView;
			m_far = baseProjector.farClipPlane;
			m_near = baseProjector.nearClipPlane;
		}
		public override void OnInspectorGUI()
		{
			ProjectorForLWRP projector = target as ProjectorForLWRP;
			DrawDefaultInspector();
			bool useStencil = EditorGUILayout.Toggle("Use Stencil Test", projector.useStencilTest);
			if (useStencil)
			{
				if (projector.stencilPassMaterial == null)
				{
					projector.stencilPassMaterial = HelperFunctions.FindMaterial("Hidden/ProjectorForLWRP/StencilPass");
				}
				Material projectorMaterial = projector.GetComponent<Projector>().material;
			}
			else
			{
				projector.stencilPassMaterial = null;
			}
			projector.UpdateShaderTagIdList();
			serializedObject.ApplyModifiedProperties();
			Projector baseProjector = projector.GetComponent<Projector>();
			if (m_isOrthographic != baseProjector.orthographic
				|| m_orthographicSize != baseProjector.orthographicSize
				|| m_aspect != baseProjector.aspectRatio
				|| m_fov != baseProjector.fieldOfView
				|| m_far != baseProjector.farClipPlane
				|| m_near != baseProjector.nearClipPlane)
			{
				m_isOrthographic = baseProjector.orthographic;
				m_orthographicSize = baseProjector.orthographicSize;
				m_aspect = baseProjector.aspectRatio;
				m_fov = baseProjector.fieldOfView;
				m_far = baseProjector.farClipPlane;
				m_near = baseProjector.nearClipPlane;
				projector.UpdateFrustum();
			}
		}
	}
}
