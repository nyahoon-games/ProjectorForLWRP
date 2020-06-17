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
		private void OnEnable()
		{
			ProjectorForLWRP projector = target as ProjectorForLWRP;
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
					Shader stencilPassShader = Shader.Find("Hidden/ProjectorForLWRP/StencilPass");
					string path = AssetDatabase.GetAssetPath(stencilPassShader);
					path = path.Substring(0, path.Length - 6); // remove "shader" extension
					path += "mat"; // add "mat" extension
					serializedObject.FindProperty("m_stencilPass").objectReferenceValue = AssetDatabase.LoadAssetAtPath(path, typeof(Material)) as Material;
				}
			}
			else
			{
				serializedObject.FindProperty("m_stencilPass").objectReferenceValue = null;
			}
			projector.UpdateShaderTagIdList();
			serializedObject.ApplyModifiedProperties();
		}
	}
}
