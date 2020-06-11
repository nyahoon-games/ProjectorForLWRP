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
		private int m_usedStencilMask = 0;
		private int FindUnusedStencilMask()
		{
			for (int i = 1; i < 8; ++i)
			{
				int mask = (1 << i);
				if ((m_usedStencilMask & mask) == 0)
				{
					return mask;
				}
			}
			if ((m_usedStencilMask & 1) != 0) // 1 is the last option. Projectors use it by default.
			{
				return 1;
			}
			return 2; // default value
		}
		private GUIContent m_applyMethodLabel;
		private GUIContent m_projectorMaterial;
		private void OnEnable()
		{
			m_shadowBuffers = Object.FindObjectsOfType<ShadowBuffer>();
			foreach (ShadowBuffer shadowBuffer in m_shadowBuffers)
			{
				if (shadowBuffer != target)
				{
					if (shadowBuffer.applyMethod != ShadowBuffer.ApplyMethod.ByLitShaders)
					{
						m_usedStencilMask |= shadowBuffer.stencilMask;
					}
				}
			}
			ShadowBuffer targetShadowBuffer = target as ShadowBuffer;
			if (targetShadowBuffer.stencilMask == 0 && targetShadowBuffer.applyMethod != ShadowBuffer.ApplyMethod.ByLitShaders)
			{
				targetShadowBuffer.stencilMask = FindUnusedStencilMask();
			}
			m_applyMethodLabel = new GUIContent("Apply Shadow Buffer");
			m_projectorMaterial = new GUIContent("Projector Material");
		}
		public override void OnInspectorGUI()
		{
			ShadowBuffer shadowBuffer = target as ShadowBuffer;
			ShadowBuffer.ApplyMethod method = shadowBuffer.applyMethod;
			SerializedProperty applyMethod = serializedObject.FindProperty("applyMethod");
			if (EditorGUILayout.PropertyField(applyMethod))
			{
				method = (ShadowBuffer.ApplyMethod)applyMethod.intValue;
				SerializedProperty stencil = serializedObject.FindProperty("m_stencilMask");
				if (method == ShadowBuffer.ApplyMethod.ByLitShaders)
				{
					stencil.intValue = 0;
				}
				else if (stencil.intValue == 0) {
					stencil.intValue = FindUnusedStencilMask();
				}
			}
			if (method != ShadowBuffer.ApplyMethod.ByShadowProjectors)
			{
				EditorGUILayout.TextArea("<color=blue>[IMPORTANT] Lit Shaders of the receiver materials must be chosen from 'Projector For LWRP/Lit/' or customized shaders as described in the document.</color>", textStyle);
			}
			if (method == ShadowBuffer.ApplyMethod.Both)
			{
				EditorGUILayout.PropertyField(serializedObject.FindProperty("additionalIgnoreLayers"));
			}
			if (method != ShadowBuffer.ApplyMethod.ByLitShaders)
			{
				EditorGUILayout.PropertyField(serializedObject.FindProperty("material"), m_projectorMaterial);
				EditorGUILayout.PropertyField(serializedObject.FindProperty("renderPassEvent"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("perObjectData"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("m_stencilMask"));
				if ((shadowBuffer.stencilMask & m_usedStencilMask) != 0)
				{
					EditorGUILayout.TextArea("<color=red>The value of Stencil Mask is used by another ShadowBuffer. Please choose another one.</color>", textStyle);
				}
			}
			serializedObject.ApplyModifiedProperties();
		}
	}
}
