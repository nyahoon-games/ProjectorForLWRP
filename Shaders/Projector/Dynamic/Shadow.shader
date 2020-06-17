Shader "Projector For LWRP/Projector/Dynamic/Shadow"
{
	Properties {
		[HideInInspector][NoScaleOffset] _FalloffTex ("FallOff", 2D) = "white" {}
		_Alpha ("Shadow Strength", Range (0, 2)) = 1.0
		_Offset ("Offset", Range (-1, -10)) = -1.0
		_OffsetSlope ("Offset Slope Factor", Range (0, -1)) = -1.0
	}
	Subshader {
		Tags {"Queue"="Transparent-1" "P4LWRPProjectorType"="Shadow"}
		UsePass "Projector For LWRP/Projector/Shadow/PASS"
	}
	CustomEditor "ProjectorForLWRP.Editor.ProjectorShadowShaderGUI"
}
