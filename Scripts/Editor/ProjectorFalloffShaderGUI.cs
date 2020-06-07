//
// ProjectorFalloffShaderGUI.cs
//
// Projector For LWRP
//
// Copyright (c) 2020 NYAHOON GAMES PTE. LTD.
//

using UnityEngine;
using UnityEditor;

namespace ProjectorForLWRP
{
    public class ProjectorFalloffShaderGUI : ShaderGUI
    {
        enum Type
        {
            Texture = 0,
            Linear = 1,
            Square = 2,
            InvSquare = 3,
            Flat = 4
        }
        static readonly string[] FALLOFF_KEYWORDS = { "P4LWRP_FALLOFF_TEXTURE", "P4LWRP_FALLOFF_LINEAR", "P4LWRP_FALLOFF_SQUARE", "P4LWRP_FALLOFF_INV_SQUARE", "P4LWRP_FALLOFF_NONE" };
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            base.OnGUI(materialEditor, properties);

            Material material = materialEditor.target as Material;
            Type currentFalloffType = Type.Flat;
            for (int i = 0; i < FALLOFF_KEYWORDS.Length; ++i) {
                if (material.IsKeywordEnabled(FALLOFF_KEYWORDS[i]))
                {
                    currentFalloffType = (Type)i;
                }
            }
            Type newFalloffType = (Type)EditorGUILayout.EnumPopup("Falloff", currentFalloffType);
            if (currentFalloffType != newFalloffType) {
                for (int i = 0; i < FALLOFF_KEYWORDS.Length; ++i)
                {
                    if (i == (int)newFalloffType)
                    {
                        material.EnableKeyword(FALLOFF_KEYWORDS[i]);
                    }
                    else
                    {
                        material.DisableKeyword(FALLOFF_KEYWORDS[i]);
                    }
                }
                currentFalloffType = newFalloffType;
            }
            if (currentFalloffType == Type.Texture)
            {
                MaterialProperty fallOffTexture = FindProperty("_FalloffTex", properties, true);
                materialEditor.TextureProperty(fallOffTexture, "Falloff Texture");
            }
        }
    }
}
