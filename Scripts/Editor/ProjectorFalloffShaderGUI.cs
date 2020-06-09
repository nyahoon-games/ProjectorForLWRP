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
        enum FalloffType
        {
            Texture,
            Linear,
            Square,
            InvSquare,
            Flat
        }
        static readonly string[] FALLOFF_KEYWORDS = { "P4LWRP_FALLOFF_TEXTURE", "P4LWRP_FALLOFF_LINEAR", "P4LWRP_FALLOFF_SQUARE", "P4LWRP_FALLOFF_INV_SQUARE", "P4LWRP_FALLOFF_NONE" };
        static readonly FalloffType[] FALLOFF_VALUES = { FalloffType.Texture, FalloffType.Linear, FalloffType.Square, FalloffType.InvSquare, FalloffType.Flat };

        public static void ShowProjectorFallOffGUI(UnityEditor.MaterialEditor materialEditor, UnityEditor.MaterialProperty[] properties)
        {
            Material material = materialEditor.target as Material;
            FalloffType falloff = HelperFunctions.MaterialKeywordSelectGUI(material, "Falloff", FALLOFF_KEYWORDS, FALLOFF_VALUES);
            if (falloff == FalloffType.Texture)
            {
                UnityEditor.MaterialProperty fallOffTexture = UnityEditor.ShaderGUI.FindProperty("_FalloffTex", properties, true);
                materialEditor.TextureProperty(fallOffTexture, "Falloff Texture");
            }
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            base.OnGUI(materialEditor, properties);
            ShowProjectorFallOffGUI(materialEditor, properties);
        }
    }
}
