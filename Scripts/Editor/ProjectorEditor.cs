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
		private Projector m_baseProjector;
		protected virtual void OnEnable()
		{
			ProjectorForLWRP projector = target as ProjectorForLWRP;
			m_baseProjector = projector.GetComponent<Projector>();
		}
		public override void OnInspectorGUI()
		{
			ProjectorForLWRP projector = target as ProjectorForLWRP;
			DrawDefaultInspector();
			bool useStencil = EditorGUILayout.Toggle("Use Stencil Test", projector.useStencilTest);
			if (useStencil != projector.useStencilTest)
			{
				if (useStencil)
				{
					if (projector.stencilPassMaterial == null)
					{
						serializedObject.FindProperty("m_stencilPass").objectReferenceValue = HelperFunctions.FindMaterial("Hidden/ProjectorForLWRP/StencilPass");
					}
				}
				else
				{
					serializedObject.FindProperty("m_stencilPass").objectReferenceValue = null;
				}
			}
			serializedObject.ApplyModifiedProperties();

			projector.UpdateShaderTagIdList();

			Material material = m_baseProjector.material;
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
	[CustomEditor(typeof(LightProjectorForLWRP))]
	public class LightProjectorEditor : ProjectorEditor
	{
	}
}
