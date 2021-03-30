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
		private SerializedProperty m_stencilPassProperty;
		private SerializedProperty m_stencilOptionProperty;
		private void OnEnable()
		{
			m_stencilOptionProperty = null;
			m_stencilPassProperty = null;
		}
		public override void OnInspectorGUI()
		{
			ProjectorForLWRP projector = target as ProjectorForLWRP;
			DrawDefaultInspector();
			bool useStencil = EditorGUILayout.Toggle("Use Stencil Test", projector.useStencilTest);
			if (useStencil)
			{
				if (m_stencilOptionProperty == null)
				{
					m_stencilOptionProperty = serializedObject.FindProperty("m_stencilTestOptions");
				}
				EditorGUI.indentLevel++;
				bool clearStencil = (m_stencilOptionProperty.intValue & (int)ProjectorForLWRP.StencilTestOptions.ClearStencil) != 0;
				bool newClearStencil = EditorGUILayout.Toggle("Clear stencil after draw", clearStencil);
				if (clearStencil != newClearStencil)
				{
					if (newClearStencil)
					{
						m_stencilOptionProperty.intValue = m_stencilOptionProperty.intValue | (int)ProjectorForLWRP.StencilTestOptions.ClearStencil;
					}
					else
					{
						m_stencilOptionProperty.intValue = m_stencilOptionProperty.intValue & ~(int)ProjectorForLWRP.StencilTestOptions.ClearStencil;
					}
				}
				EditorGUI.indentLevel--;
			}
			if (useStencil != projector.useStencilTest)
			{
				if (m_stencilPassProperty == null)
				{
					m_stencilPassProperty = serializedObject.FindProperty("m_stencilPass");
				}
				if (useStencil)
				{
					if (projector.stencilPassMaterial == null)
					{
						Shader stencilPassShader = Shader.Find("Hidden/ProjectorForLWRP/StencilPass");
						string path = AssetDatabase.GetAssetPath(stencilPassShader);
						path = path.Substring(0, path.Length - 6); // remove "shader" extension
						path += "mat"; // add "mat" extension
						m_stencilPassProperty.objectReferenceValue = AssetDatabase.LoadAssetAtPath(path, typeof(Material)) as Material;
					}
				}
				else
				{
					m_stencilPassProperty.objectReferenceValue = null;
				}
			}
			serializedObject.ApplyModifiedProperties();
		}
	}
}
