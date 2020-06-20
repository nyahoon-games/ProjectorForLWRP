using UnityEngine;

namespace ProjectorForLWRP
{
    public static class P4LWRPShaderProperties
    {
        // ------------------------------
        // Shadow Material Properties
        // ------------------------------
        public class p4lwrp_ShadowMaskSelector : ShaderUtils.ShaderProperty<Vector4, p4lwrp_ShadowMaskSelector> { }
        public class p4lwrp_ShadowLightIndex : ShaderUtils.ShaderProperty<int, p4lwrp_ShadowLightIndex> { }

        // ------------------------------
        // Lit Shader Properties (Forward Lighting Pass)
        // ------------------------------
        public class p4lwrp_shadowBufferTex : ShaderUtils.ShaderProperty<Texture, p4lwrp_shadowBufferTex> { }
        public class p4lwrp_additionalShadowBufferTex : ShaderUtils.ShaderProperty<Texture, p4lwrp_additionalShadowBufferTex> { }
        public class p4lwrp_additionalLightShadowChannelIndex : ShaderUtils.ShaderProperty<Vector4[], p4lwrp_additionalLightShadowChannelIndex> { }
        public class p4lwrp_shadowMaskWriteMasks : ShaderUtils.ShaderProperty<Vector4[], p4lwrp_shadowMaskWriteMasks> { }
        public class p4lwrp_shadowMaskWriteMasksInv : ShaderUtils.ShaderProperty<Vector4[], p4lwrp_shadowMaskWriteMasksInv> { }
        public class p4lwrp_additionalLightShadowWriteMask : ShaderUtils.ShaderProperty<Vector4[], p4lwrp_additionalLightShadowWriteMask> { }

        // ------------------------------
        // Collect Shadow Shader Properties
        // ------------------------------
        public class p4lwrp_ColorWriteMask : ShaderUtils.ShaderProperty<int, p4lwrp_ColorWriteMask> { }

        // ------------------------------
        // Stencil Pass Shader Properties
        // ------------------------------
        public class p4lwrp_StencilRef : ShaderUtils.ShaderProperty<int, p4lwrp_StencilRef> { }
        public class p4lwrp_StencilMask : ShaderUtils.ShaderProperty<int, p4lwrp_StencilMask> { }
    }
}
