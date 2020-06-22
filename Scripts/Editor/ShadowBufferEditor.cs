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
		private GUIContent m_applyMethodLabel;
		private GUIContent m_projectorMaterialLabel;
		private GUIContent m_layerMaskLablel;
		private GUIContent m_renderingLayerMaskLablel;
		private ShadowBuffer m_shadowBuffer;
		private Light m_light;
		private LightProjectorForLWRP m_lightProjector;
		private SerializedProperty m_applyMethodPropety;
		private SerializedProperty m_realtimeShadowReceiverLayersProperty;
		private SerializedProperty m_realtimeShadowReceiverRenderingLayerMaskProperty;
		private SerializedProperty m_collectRealtimeShadowsProperty;
		private SerializedProperty m_materialProperty;
		private SerializedProperty m_shadowTextureNameProperty;
		private SerializedProperty m_renderPassEventProperty;
		private SerializedProperty m_perObjectDataProperty;
		private void OnEnable()
		{
			m_applyMethodLabel = new GUIContent("Apply Shadow Buffer");
			m_projectorMaterialLabel = new GUIContent("Projector Material");
			m_layerMaskLablel = new GUIContent("Layer Mask");
			m_renderingLayerMaskLablel = new GUIContent("Rendering Layer Mask");
			m_shadowBuffer = target as ShadowBuffer;
			m_light = m_shadowBuffer.GetComponent<Light>();
			m_lightProjector = m_shadowBuffer.GetComponent<LightProjectorForLWRP>();
			m_applyMethodPropety = serializedObject.FindProperty("m_applyMethod");
			m_collectRealtimeShadowsProperty = serializedObject.FindProperty("m_collectRealtimeShadows");
			m_realtimeShadowReceiverLayersProperty = serializedObject.FindProperty("m_realtimeShadowReceiverLayers");
			m_realtimeShadowReceiverRenderingLayerMaskProperty = serializedObject.FindProperty("m_realtimeShadowReceiverRenderingLayerMask");
			m_materialProperty = serializedObject.FindProperty("m_material");
			m_shadowTextureNameProperty = serializedObject.FindProperty("m_shadowTextureName");
			m_renderPassEventProperty = serializedObject.FindProperty("m_renderPassEvent");
			m_perObjectDataProperty = serializedObject.FindProperty("m_perObjectData");
		}
		private bool CheckApplyMethodEnabled(System.Enum enumValue)
		{
			ShadowBuffer.ApplyMethod applyMethod = (ShadowBuffer.ApplyMethod)enumValue;
			if (m_light != null)
			{
				return applyMethod != ShadowBuffer.ApplyMethod.ByLightProjectors;
			}
			else if (m_lightProjector != null)
			{
				return applyMethod == ShadowBuffer.ApplyMethod.ByLightProjectors;
			}
			else
			{
				return applyMethod != ShadowBuffer.ApplyMethod.ByLitShaders;
			}
		}
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			ShadowBuffer.ApplyMethod method = m_shadowBuffer.applyMethod;
			ShadowBuffer.ApplyMethod newMethod = (ShadowBuffer.ApplyMethod)EditorGUILayout.EnumPopup(m_applyMethodLabel, method, CheckApplyMethodEnabled, false);
			if (newMethod != method)
			{
				m_applyMethodPropety.intValue = (int)newMethod;
				method = newMethod;
			}
			if (m_light != null)
			{
				// check light type
				if (m_light.type == LightType.Disc || m_light.type == LightType.Rectangle)
				{
					EditorGUILayout.TextArea("<color=red>Area light is not supported. Please remove Shadow Buffer component.</color>", textStyle);
					if (GUILayout.Button("Remove this component"))
					{
						Undo.DestroyObjectImmediate(m_shadowBuffer);
					}
					return;
				}
			}
			if (method == ShadowBuffer.ApplyMethod.ByLitShaders)
			{
				if (m_shadowBuffer.shadowColor == ShadowBuffer.ShadowColor.Colored)
				{
					EditorGUILayout.TextArea("<color=red>Lit shader does not support colored shadow! Shadow projector will be used instead.</color>", textStyle);
				}
				else if (m_light != null && m_light.bakingOutput.isBaked && m_light.bakingOutput.lightmapBakeType == LightmapBakeType.Baked)
				{
					EditorGUILayout.TextArea("<color=red>The light is baked only. Shadow projector will be used instead.</color>", textStyle);
				}
				else
				{
					EditorGUILayout.TextArea("<color=blue>[IMPORTANT] Shadows are collected from the associated Shadow Projectors, "
										   + "but they will no be shown unless the receiver objects use Lit Shaders chosen from 'Projector For LWRP/Lit/' "
										   + "or customized shaders as described in the document.</color>", textStyle);
					if (m_shadowBuffer.realtimeShadowsEnabled)
					{
						EditorGUILayout.PropertyField(m_collectRealtimeShadowsProperty);
						if (m_collectRealtimeShadowsProperty.boolValue)
						{
							++EditorGUI.indentLevel;
							EditorGUILayout.PropertyField(m_realtimeShadowReceiverLayersProperty, m_layerMaskLablel);
							EditorGUILayout.PropertyField(m_realtimeShadowReceiverRenderingLayerMaskProperty, m_renderingLayerMaskLablel);
							--EditorGUI.indentLevel;
						}
					}
					EditorGUILayout.TextArea("<color=blue>In case that Lit Shaders could not handle this Shadow Buffer (Lit Shaders can handle up to 8 shadow buffers), "
										   + "the collected shadows are casted by the asscociated Shadow Projectors "
										   + "with the following settings.</color>", textStyle);
				}
			}
			else if (method == ShadowBuffer.ApplyMethod.ByShadowProjectors)
			{
				EditorGUILayout.TextArea("<color=blue>Shadows are collected from the associated Shadow Projectors, "
									   + "and they will be casted on the receivers by the same Shadow Projectors "
									   + "with the following settings.</color>", textStyle);
			}
			else
			{
				EditorGUILayout.TextArea("<color=blue>Shadows are collected from the associated Shadow Projectors, "
									   + "but they will not be shown unless this shadow buffer is set to light projectors.</color>", textStyle);
			}
			if (method != ShadowBuffer.ApplyMethod.ByLightProjectors)
			{
				EditorGUILayout.PropertyField(m_materialProperty, m_projectorMaterialLabel);
				EditorGUILayout.PropertyField(m_shadowTextureNameProperty);
				EditorGUILayout.PropertyField(m_renderPassEventProperty);
				if (m_light == null)
				{
					EditorGUILayout.PropertyField(m_perObjectDataProperty);
				}
				bool isMaterialValid = true;
				if (m_shadowBuffer.material != null)
				{
					string projectorType = m_shadowBuffer.material.GetTag("P4LWRPProjectorType", false);
					if (projectorType != "ApplyShadowBuffer")
					{
						isMaterialValid = false;
						EditorGUILayout.TextArea("<color=red>" + m_shadowBuffer.material.name + " material is not available for Shadow Buffer. Please set a valid material whose 'P4LWRPProjectorType' tag is 'ApplyShadowBuffer'.</color>", textStyle);
					}
					else if (m_shadowBuffer.IsShadowMaterial())
					{
						if (m_light == null)
						{
							isMaterialValid = false;
							EditorGUILayout.TextArea("<color=red>" + m_shadowBuffer.material.name + " material is not available without Light component. Please add ShadowBuffer component to a Light object to use the material.</color>", textStyle);
						}
					}
				}
				else
				{
					isMaterialValid = false;
					EditorGUILayout.TextArea("<color=red>Please set a valid material.</color>", textStyle);
				}
				if (!isMaterialValid)
				{
					if (GUILayout.Button("Set Dedault Material"))
					{
						m_materialProperty.objectReferenceValue = m_shadowBuffer.GetDefaultMaterial();
					}
				}
			}
			serializedObject.ApplyModifiedProperties();
		}
	}
}
