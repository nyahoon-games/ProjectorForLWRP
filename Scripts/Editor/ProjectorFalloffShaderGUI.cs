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
                P4LWRPShaderKeywords.FalloffType falloff = HelperFunctions.MaterialKeywordSelectGUI<P4LWRPShaderKeywords.FalloffType>(material, "Falloff");
                if (falloff == P4LWRPShaderKeywords.FalloffType.Texture)
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
