//
// ProjectorEditor.cs
//
// Projector For LWRP
//
// Copyright (c) 2019 NYAHOON GAMES PTE. LTD.
//

using UnityEngine;
using UnityEditor;

namespace ProjectorForLWRP.Editor
{
	[CustomEditor(typeof(ProjectorForLWRP))]
	public class ProjectorEditor : UnityEditor.Editor
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
			serializedObject.ApplyModifiedProperties();

			projector.UpdateShaderTagIdList();

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

			Material material = baseProjector.material;
			if (material != null)
			{
				string projectorType = material.GetTag("P4LWRPProjectorType", false);
				if (projectorType == "Shadow" || projectorType == "CollectShadowBuffer")
				{
					if (!(projector is ShadowProjectorForLWRP))
					{
						GUILayout.TextArea("<color=red>This projector has a shadow projector material. Please press the button below to replace this component with a Shadow Projector For LWRP component.</color>", errorStyle);
						DrawSwitchProjectorGUI<ShadowProjectorForLWRP>("Switch to Shadow Projector For LWRP");
					}
				}
				else if (projectorType == "Light")
				{
					if (projector is LightProjectorForLWRP)
					{
						LightProjectorForLWRP lightProjector = projector as LightProjectorForLWRP;
						if (lightProjector.shadowBuffer == null)
						{
							GUILayout.TextArea("<color=blue>If you wish to mask the light with shadow buffer shadows, please set the Shadow Buffer property.</color>", errorStyle);
							GUILayout.TextArea("<color=blue>If not, you can use Projector For LWRP instead.</color>", errorStyle);
							DrawSwitchProjectorGUI<ProjectorForLWRP>("Switch to Projector For LWRP");
						}
					}
					else
					{
						GUILayout.TextArea("<color=blue>This projector has a light projector material. If you wish to mask the light with shadow buffer shadows, please press the button below.</color>", errorStyle);
						DrawSwitchProjectorGUI<ProjectorForLWRP>("Switch to Light Projector For LWRP");
					}
				}
				else
				{
					if ((projector is ShadowProjectorForLWRP) || (projector is ShadowProjectorForLWRP))
					{
						if (projector is ShadowProjectorForLWRP)
						{
							GUILayout.TextArea("<color=blue>This projector does not have a shadow projector material. Do you want switch to Projector For LWRP component?</color>", errorStyle);
						}
						else
						{
							GUILayout.TextArea("<color=blue>This projector does not have a light projector material. Do you want switch to Projector For LWRP component?</color>", errorStyle);
						}
						DrawSwitchProjectorGUI<ProjectorForLWRP>("Switch to Projector For LWRP");
					}
				}
			}
		}
		protected void DrawSwitchProjectorGUI<NewProjectorType>(string label) where NewProjectorType : ProjectorForLWRP
		{
			if (GUILayout.Button(label))
			{
				ProjectorForLWRP projector = target as ProjectorForLWRP;
				Undo.SetCurrentGroupName(label);
				int undoGroup = Undo.GetCurrentGroup();
				ProjectorForLWRP newProjector = Undo.AddComponent<NewProjectorType>(projector.gameObject);
				newProjector.CopySerializedPropertiesFrom(projector);
				Undo.DestroyObjectImmediate(projector);
				Undo.CollapseUndoOperations(undoGroup);
			}
		}
	}
}
