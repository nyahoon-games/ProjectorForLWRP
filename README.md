
# Projector For LWRP

## Overview
This project provides Unity C# scripts and shaders to use [Projector](https://docs.unity3d.com/Manual/class-Projector.html) component with [Lightweight Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.lightweight@4.0/manual/index.html).

[Online Document](https://nyahoon.com/products/projector-for-lwrp)

## Verified LWRP version
5.7.2

## Install
Clone (or submodule add) this repository into the Assets folder in your Unity Project.

### Clone:
	cd Pass-to-Your-Unity-Project/Assets
	git clone https://github.com/nyahoon-games/ProjectorForLWRP.git

### Submodule Add:
	cd Pass-to-Your-Unity-Project
	git submodule add https://github.com/nyahoon-games/ProjectorForLWRP.git Assets/ProjectorForLWRP

## Setup
If you already have a `ForwardRendererData` asset and assigned it to the `LightweightRenderPipelineAsset`, add a `ProjectorRendererFeature` to your `ForwardRendererData`.
![](https://nyahoon.com/wp/wp-content/uploads/2019/05/addrendererfeature.png)

If you don’t have a `ForwardRendererData` asset yet, you can use `Assets/ProjectorForLWRP/Data/ForwardRendererWithProjectorPass`. Go to Graphics Settings and double click `LightweightRenderPipelineAsset` in Scriptable Render Pipeline Settings. Then, in Inspector View, change `Renderer Type` to `custom` and assign  Assets/ProjectorForLWRP/Data/ForwardRendererWithProjectorPass to `Data`.
![](https://nyahoon.com/wp/wp-content/uploads/2019/05/selectforwardrendererdata.png)

## How to Use
1. Select an existing `GameObject` that has Projector component, or create a new empty `GameObject`.
2. Press `Add Component` button in Inspector View, and select `Scripts` > `ProjectorForLWRP` > `Projector For LWRP`.
3. The `GameObject` will contain `Projector` component and `Projector For LWRP` component. `Projector` component is automatically disabled by Projector for LWRP component though, you still need to setup `Projector` properties as usual. One thing that is different from usual settings is that you cannot use the projector shaders in Standard Assets. Please use one of the shaders in this project, or create a custom shaders if needed. (Shaders in [Dynamic Shadow Projector](https://nyahoon.com/products/dynamic-shadow-projector) and [Fast Shadow Receiver](https://nyahoon.com/products/fast-shadow-receiver) will be available after update).
4. In addition to setting up Projector properties, you might need to setup the properties of Projector For LWRP component.

## Properties of Projector For LWRP component
| Property | Description |
|:---|:---|
| Cameras | An array of cameras in which the projector is rendered. If it is empty, <code>Camera.main</code> will be used. To add a camera to the array, increase `Size` first, then put the camera to the last element of the array. |
| Shader Tag List | An array of `LightMode` tag values. Only the renderers whose material has a shader that contains a pass whose `LightMode` tag value is identical to one of the values in the array can receive projection. If a shader pass doesn't have `LightMode` tag, it's  `LightMode` tag value will be considered as `SRPDefaultUnlit`. To add a value, please increase `Size` first. |
| Render Queue Lower/Upper Bound | Only the renderers of which the render queue values of their materials are within this range can receive projection. |
| Render Pass Event | An event in which projector render pass is inserted. Please be aware that the render queue value of the projector's material is ignored. |
| Per Object Data | Kinds of per object data (other than transform matrix) that are required by the projector's material. |
| Use Stencil Test | There is a chance to improve the rendering performance if stencil test is enabled. Just try and see if it is effective or not. Don't need to use stencil test, if the projector is used with [Fast Shadow Receiver](https://nyahoon.com/products/fast-shadow-receiver). |

## Projector Shaders
If you need a custom projector shader, please include "Assets/ProjectorForLWRP/Shaders/P4LWRT.cginc" and use `fsrTransformVertex` function to transform vertex and projection uv. The shader must be compiled with `FSR_PROJECTOR_FOR_LWRP` keyword.

### Sample Code:

	Shader "Custom/Projector/Shadow" 
	{
		Properties {
			[NoScaleOffset] _ShadowTex ("Cookie", 2D) = "gray" {}
			[NoScaleOffset] _FalloffTex ("FallOff", 2D) = "white" {}
			_Offset ("Offset", Range (-1, -10)) = -1.0
		}
		SubShader
		{
			Tags {"Queue"="Transparent-1"}
			Pass
			{
				ZWrite Off
				Fog { Color (1, 1, 1) }
				ColorMask RGB
				Blend DstColor Zero
				Offset -1, [_Offset]
	
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile _ FSR_RECEIVER FSR_PROJECTOR_FOR_LWRP
				#pragma multi_compile_fog
				#include "UnityCG.cginc"
				#include "Assets/ProjectorForLWRP/Shaders/P4LWRT.cginc"
	
				P4LWRT_V2F_PROJECTOR vert(float4 vertex : POSITION)
				{
					P4LWRT_V2F_PROJECTOR o;
					fsrTransformVertex(vertex, o.pos, o.uvShadow);
					UNITY_TRANSFER_FOG(o, o.pos);
					return o;
				}

				fixed4 frag(P4LWRT_V2F_PROJECTOR i) : SV_Target
				{
					fixed4 col;
 					fixed falloff = tex2D(_FalloffTex, i.uvShadow.zz).a;
					col.rgb = tex2Dproj(_ShadowTex, UNITY_PROJ_COORD(i.uvShadow)).rgb;
					col.a = 1.0f;
					col.rgb = lerp(fixed3(1,1,1), col.rgb, falloff);
					UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(1,1,1,1));
					return col;
				}
	
				ENDCG
			}
		} 
	}
