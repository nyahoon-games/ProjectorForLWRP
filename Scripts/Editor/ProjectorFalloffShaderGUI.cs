//
// ProjectorFalloffShaderGUI.cs
//
// Projector For LWRP
//
// Copyright (c) 2020 NYAHOON GAMES PTE. LTD.
//

using UnityEngine;
using UnityEditor;

namespace ProjectorForLWRP.Editor
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

        public static void ShowProjectorFallOffGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            MaterialProperty fallOffTexture = null;
            try
            {
                fallOffTexture = FindProperty("_FalloffTex", properties, true);
            }
            catch (System.ArgumentException)
            {
            }
            if (fallOffTexture != null)
            {
                Material material = materialEditor.target as Material;
                FalloffType falloff = HelperFunctions.MaterialKeywordSelectGUI(material, "Falloff", FALLOFF_KEYWORDS, FALLOFF_VALUES);
                if (falloff == FalloffType.Texture)
                {
                    materialEditor.TextureProperty(fallOffTexture, "Falloff Texture");
                }
            }
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            base.OnGUI(materialEditor, properties);
            ShowProjectorFallOffGUI(materialEditor, properties);
        }
    }
}
