//
// ShadowBufferEditor.cs
//
// Projector For LWRP
//
// Copyright (c) 2020 NYAHOON GAMES PTE. LTD.
//

using UnityEngine;
using UnityEditor;

namespace ProjectorForLWRP.Editor
{
	[CustomEditor(typeof(ShadowBuffer))]
	public class ShadowBufferEditor : UnityEditor.Editor
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
			DrawDefaultInspector();
			ShadowBuffer shadowBuffer = target as ShadowBuffer;
			ShadowBuffer.ApplyMethod method = shadowBuffer.applyMethod;
			SerializedProperty applyMethod = serializedObject.FindProperty("m_applyMethod");
			if (EditorGUILayout.PropertyField(applyMethod, m_applyMethodLabel))
			{
				method = (ShadowBuffer.ApplyMethod)applyMethod.intValue;
			}
			bool shadowPropertiesRequired = false;
			if (method == ShadowBuffer.ApplyMethod.ByLitShaders)
			{
				shadowPropertiesRequired = true;
				if (shadowBuffer.shadowColor == ShadowBuffer.ShadowColor.Colored)
				{
					EditorGUILayout.TextArea("<color=red>Lit shader does not support colored shadow! Shadow projector will be used instead.</color>", textStyle);
				}
				else
				{
					EditorGUILayout.TextArea("<color=blue>[IMPORTANT] Shadows are collected from the associated Shadow Projectors, "
					                       + "but they will no be shown unless the receiver objects use Lit Shaders chosen from 'Projector For LWRP/Lit/' "
					                       + "or customized shaders as described in the document.</color>", textStyle);
					EditorGUILayout.PropertyField(serializedObject.FindProperty("m_shadowReceiverLayers"));
					if (shadowBuffer.shadowMaterialProperties.lightSource == null)
					{
						EditorGUILayout.TextArea("<color=red>Please set 'Light Source' property in Shadow Material Properties component.</color>", textStyle);
					}
					else if (shadowBuffer.realtimeShadowsEnabled)
					{
						EditorGUILayout.PropertyField(serializedObject.FindProperty("m_collectRealtimeShadows"));
					}
				}
			}
			else if (method == ShadowBuffer.ApplyMethod.ByShadowProjectors)
			{
				EditorGUILayout.TextArea("<color=blue>Shadows are collected from the associated Shadow Projectors, "
				                       + "and they will be casted on the receivers by the same Shadow Projectors "
				                       + "with the following settings.</color>", textStyle);
				EditorGUILayout.PropertyField(serializedObject.FindProperty("m_material"), m_projectorMaterial);
				EditorGUILayout.PropertyField(serializedObject.FindProperty("m_shadowTextureName"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("m_renderPassEvent"));
				if (shadowBuffer.IsShadowMaterial())
				{
					shadowPropertiesRequired = true;
					if (shadowBuffer.shadowMaterialProperties.lightSource == null)
					{
						EditorGUILayout.TextArea("<color=red>Please set 'Light Source' property in Shadow Material Properties component.</color>", textStyle);
					}
				}
				else
				{
					EditorGUILayout.PropertyField(serializedObject.FindProperty("m_perObjectData"));
				}
			}
			else // if (method == ShadowBuffer.ApplyMethod.ByLightProjectors)
			{
				EditorGUILayout.TextArea("<color=blue>Shadows are collected from the associated Shadow Projectors, "
				                       + "but they will not be shown unless this shadow buffer is set to light projectors.</color>", textStyle);
			}
			if (!shadowPropertiesRequired) {
				ShadowMaterialProperties shadowProperties;
				if (shadowBuffer.TryGetComponent(out shadowProperties))
				{
					EditorGUILayout.TextArea("<color=red>The above settings doesn't require the Shadow Material Properties component. "
										   + "Press the button below if you want to remove it.</color>", textStyle);
					if (GUILayout.Button("Remove Shadow Material Properties component"))
					{
						Undo.DestroyObjectImmediate(shadowProperties);
					}
				}
			}
			serializedObject.ApplyModifiedProperties();
		}
	}
}
