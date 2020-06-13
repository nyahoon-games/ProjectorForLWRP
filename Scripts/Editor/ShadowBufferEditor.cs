//
// ShadowBufferEditor.cs
//
// Projector For LWRP
//
// Copyright (c) 2020 NYAHOON GAMES PTE. LTD.
//

using UnityEngine;
using UnityEditor;

namespace ProjectorForLWRP
{
	[CustomEditor(typeof(ShadowBuffer))]
	public class ShadowBufferEditor : Editor
	{
		GUIStyle m_textStype;
		protected GUIStyle textStyle
		{
			get
			{
				if (m_textStype == null)
				{
					m_textStype = new GUIStyle();
					m_textStype.richText = true;
					m_textStype.wordWrap = true;
				}
				return m_textStype;
			}
		}
		private ShadowBuffer[] m_shadowBuffers;
		private GUIContent m_applyMethodLabel;
		private GUIContent m_projectorMaterial;
		private void OnEnable()
		{
			m_applyMethodLabel = new GUIContent("Apply Shadow Buffer");
			m_projectorMaterial = new GUIContent("Projector Material");
		}
		public override void OnInspectorGUI()
		{
			ShadowBuffer shadowBuffer = target as ShadowBuffer;
			ShadowBuffer.ApplyMethod method = shadowBuffer.applyMethod;
			SerializedProperty applyMethod = serializedObject.FindProperty("applyMethod");
			if (EditorGUILayout.PropertyField(applyMethod, m_applyMethodLabel))
			{
				method = (ShadowBuffer.ApplyMethod)applyMethod.intValue;
			}
			if (method != ShadowBuffer.ApplyMethod.ByShadowProjectors)
			{
				EditorGUILayout.TextArea("<color=blue>[IMPORTANT] Lit Shaders of the receiver materials must be chosen from 'Projector For LWRP/Lit/' or customized shaders as described in the document.</color>", textStyle);
				EditorGUILayout.PropertyField(serializedObject.FindProperty("collectRealtimeShadows"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("shadowReceiverLayers"));
			}
			if (method != ShadowBuffer.ApplyMethod.ByLitShaders)
			{
				EditorGUILayout.PropertyField(serializedObject.FindProperty("material"), m_projectorMaterial);
				EditorGUILayout.PropertyField(serializedObject.FindProperty("renderPassEvent"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("perObjectData"));
			}
			serializedObject.ApplyModifiedProperties();
		}
	}
}
