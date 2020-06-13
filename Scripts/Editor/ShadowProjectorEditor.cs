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
    [CustomEditor(typeof(ShadowProjectorForLWRP))]
    public class ShadowProjectorEditor : ProjectorEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
			// check projector material
			ShadowProjectorForLWRP projector = target as ShadowProjectorForLWRP;
			Projector unityProjector = projector.GetComponent<Projector>();
			Material material = unityProjector.material;
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
				ShadowMaterialProperties shadowMaterialProperties = projector.GetComponent<ShadowMaterialProperties>();
				if (projectorType == "Shadow")
				{
					if (shadowMaterialProperties == null)
					{
						GUILayout.TextArea("<color=red>This projector has a shadow projector material. Please press the button below to add a Shadow Material Properties component</color>", errorStyle);
						if (GUILayout.Button("Add Shadow Material Properties component"))
						{
							Undo.AddComponent<ShadowMaterialProperties>(projector.gameObject);
						}
					}
				}
				else if (shadowMaterialProperties != null)
				{
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
