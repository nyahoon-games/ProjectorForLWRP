Shader "Projector For LWRP/Projector/Dynamic/Collect Mipmapped Shadow"
{
	Properties {
		_Alpha ("Shadow Strength", Range (0, 2)) = 1.0
		_Offset ("Offset", Range (-1, -10)) = -1.0
		_OffsetSlope ("Offset Slope Factor", Range (0, -1)) = -1.0
        [HideInInspector] _ColorWriteMask ("Color Write Mask", Float) = 1
	}
	Subshader {
		Tags {"Queue"="Transparent-1" "P4LWRPProjectorType"="CollectShadowBuffer"}
		UsePass "Projector For LWRP/Projector/Collect Mipmapped Shadow/PASS"
	}
	CustomEditor "ProjectorForLWRP.Editor.ProjectorFalloffShaderGUI"
}
