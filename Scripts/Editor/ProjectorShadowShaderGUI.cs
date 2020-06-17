//
// ProjectorShadowShaderGUI.cs
//
// Projector For LWRP
//
// Copyright (c) 2020 NYAHOON GAMES PTE. LTD.
//

using UnityEngine;
using UnityEditor;

namespace ProjectorForLWRP.Editor
{
    public class ProjectorShadowShaderGUI : ShaderGUI
    {
        enum ColorChannel
        {
            RGB,
            R,
            G,
            B,
            A
        }
        static readonly string[] COLORCHANNEL_KEYWORDS = { "P4LWRP_SHADOWTEX_CHANNEL_RGB", "P4LWRP_SHADOWTEX_CHANNEL_R", "P4LWRP_SHADOWTEX_CHANNEL_G", "P4LWRP_SHADOWTEX_CHANNEL_B", "P4LWRP_SHADOWTEX_CHANNEL_A" };
        static readonly ColorChannel[] COLLORCHANNEL_VALUES = { ColorChannel.RGB, ColorChannel.R, ColorChannel.G, ColorChannel.B, ColorChannel.A };
        public static void ShowColorChannelSelectGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            Material material = materialEditor.target as Material;
            HelperFunctions.MaterialKeywordSelectGUI(material, "Color Channel", COLORCHANNEL_KEYWORDS, COLLORCHANNEL_VALUES);
        }
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            base.OnGUI(materialEditor, properties);
            ShowColorChannelSelectGUI(materialEditor, properties);
            ProjectorFalloffShaderGUI.ShowProjectorFallOffGUI(materialEditor, properties);
        }
    }
}
