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
				++EditorGUI.indentLevel;
				EditorGUILayout.PropertyField(serializedObject.FindProperty("m_stencilRef"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("m_stencilMask"));
				--EditorGUI.indentLevel;
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
			// check projector material
			Projector unityProjector = projector.GetComponent<Projector>();
			Material material = unityProjector.material;
			if (material != null) {
				string projectorType = material.GetTag("ProjectorType", false);
				if (projector.shadowBuffer != null) {
					// projector type should be "CollectShadowBuffer"
					if (projectorType != "CollectShadowBuffer")
					{
						GUILayout.TextArea("<color=red>This projector is being rendered to a Shadow Buffer but the material doesn't have Collect Shadow Buffer shader.</color>", errorStyle);
					}
				}
				ShadowMaterialProperties shadowMaterialProperties = projector.GetComponent<ShadowMaterialProperties>();
				if (projectorType == "Shadow")
				{
					if (shadowMaterialProperties == null) {
						GUILayout.TextArea("<color=red>This projector has a shadow projector material. Please press the button below to add a Shadow Material Properties component</color>", errorStyle);
						if (GUILayout.Button("Add Shadow Material Properties component"))
						{
							Undo.AddComponent<ShadowMaterialProperties>(projector.gameObject);
						}
					}
				}
				else if (shadowMaterialProperties != null) {
					GUILayout.TextArea("<color=red>This projector doesn't have a shadow projector material. Do you want to remove Shadow Material Properties component?</color>", errorStyle);
					if (GUILayout.Button("Remove Shadow Material Properties component"))
					{
						Undo.DestroyObjectImmediate(shadowMaterialProperties);
					}
				}
			}
		}
	}
}
