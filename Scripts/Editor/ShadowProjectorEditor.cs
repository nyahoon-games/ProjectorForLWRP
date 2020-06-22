//
// ShadowProjectorEditor.cs
//
// Projector For LWRP
//
// Copyright (c) 2020 NYAHOON GAMES PTE. LTD.
//

using UnityEngine;
using UnityEditor;

namespace ProjectorForLWRP.Editor
{
    [CustomEditor(typeof(ShadowProjectorForLWRP))]
    public class ShadowProjectorEditor : ProjectorEditor
    {
		private SerializedProperty m_stencilPassProperty;
		private ShadowMaterialProperties m_shadowMaterialProperties;
		private Projector m_unityProjector;
		protected override void OnEnable()
		{
			base.OnEnable();
			ShadowProjectorForLWRP projector = target as ShadowProjectorForLWRP;
			m_unityProjector = projector.GetComponent<Projector>();
			m_shadowMaterialProperties = projector.GetComponent<ShadowMaterialProperties>();
		}

		public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
			// check projector material
			ShadowProjectorForLWRP projector = target as ShadowProjectorForLWRP;
			Material material = m_unityProjector.material;
			if (material != null)
			{
				string projectorType = material.GetTag("P4LWRPProjectorType", false);
				if (projector.shadowBuffer != null)
				{
					// projector type should be "CollectShadowBuffer"
					if (projectorType != "CollectShadowBuffer")
					{
						GUILayout.TextArea("<color=red>This projector is being rendered to a Shadow Buffer but the material doesn't have Collect Shadow Buffer shader.</color>", errorStyle);
					}
				}
				if (projectorType == "Shadow")
				{
					if (m_shadowMaterialProperties == null)
					{
						GUILayout.TextArea("<color=red>This projector has a shadow projector material. Please press the button below to add a Shadow Material Properties component</color>", errorStyle);
						if (GUILayout.Button("Add Shadow Material Properties component"))
						{
							m_shadowMaterialProperties = Undo.AddComponent<ShadowMaterialProperties>(projector.gameObject);
						}
					}
					else
					{
						if (m_shadowMaterialProperties.lightSource != null)
						{
							ShadowBuffer lightSourceShadowBuffer = m_shadowMaterialProperties.lightSource.GetComponent<ShadowBuffer>();
							if (lightSourceShadowBuffer != projector.shadowBuffer)
							{
								if (projector.shadowBuffer == null)
								{
									GUILayout.TextArea("<color=red>The Light Source has a Shadow Buffer. Please press the button below to set the Shadow Buffer.</color>", errorStyle);
									if (GUILayout.Button("Set Shadow Buffer"))
									{
										serializedObject.FindProperty("m_shadowBuffer").objectReferenceValue = lightSourceShadowBuffer;
									}
								}
								else
								{
									GUILayout.TextArea("<color=red>Shadow Buffer is inconsistent with Shadow Material Property setting.</color>", errorStyle);
								}
							}
						}
					}
				}
				else if (m_shadowMaterialProperties != null)
				{
					GUILayout.TextArea("<color=red>This projector doesn't have a shadow projector material. Do you want to remove Shadow Material Properties component?</color>", errorStyle);
					if (GUILayout.Button("Remove Shadow Material Properties component"))
					{
						Undo.DestroyObjectImmediate(m_shadowMaterialProperties);
						m_shadowMaterialProperties = null;
					}
				}
			}
		}
	}
}
