# Use the camera in a custom URP shader

To use the camera in a custom Universal Render Pipeline (URP) shader, follow these steps:

1. Add `#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"` inside the `HLSLPROGRAM` in your shader file. The `Core.hlsl` file imports the `ShaderVariablesFunction.hlsl` file.
2. Use one of the following methods from the `ShaderVariablesFunction.hlsl` file.

| **Method** | **Syntax** | **Description** |
|-|-|-|
| `GetCameraPositionWS` | `float3 GetCameraPositionWS()` | Returns the world space position of the camera. |
| `GetScaledScreenParams` | `float4 GetScaledScreenParams()` | Returns the width and height of the screen in pixels. |
| `GetViewForwardDir` | `float3 GetViewForwardDir()` | Returns the forward direction of the view in world space. |
| `IsPerspectiveProjection` | `bool IsPerspectiveProjection()` | Returns `true` if the camera projection is set to perspective. |
| `LinearDepthToEyeDepth` | `half LinearDepthToEyeDepth(half linearDepth)` | Converts a linear depth buffer value to view depth. Refer to [Cameras and depth textures](https://docs.unity3d.com/Manual/SL-CameraDepthTexture.html) for more information. |
| `TransformScreenUV` | `void TransformScreenUV(inout float2 screenSpaceUV)` | Flips the y coordinate of the screen space position, if Unity uses an upside-down coordinate space. You can also input both a `uv`, and the screen height as a `float`, so the method outputs the position scaled to the screen size in pixels. |

## Example

The following URP shader draws object surfaces with colors that represent the direction from the surface to the camera.

```hlsl
Shader "Custom/DirectionToCamera"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv: TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv: TEXCOORD0;
                float3 viewDirection : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // Get the positions of the vertex in different coordinate spaces
                VertexPositionInputs positions = GetVertexPositionInputs(IN.positionOS);
                OUT.positionCS = positions.positionCS;

                // Get the direction from the vertex to the camera, in world space
                OUT.viewDirection = GetCameraPositionWS() - positions.positionWS.xyz;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Set the fragment color to the direction vector
                return float4(IN.viewDirection, 1);
            }
            ENDHLSL
        }
    }
}
```

## Additional resources

- [Cameras in URP](cameras/camera-differences-in-urp.md)
- [Writing custom shaders](writing-custom-shaders-urp.md)
- [Upgrade custom shaders for URP compatibility](urp-shaders/birp-urp-custom-shader-upgrade-guide.md)
- [HLSL in Unity](https://docs.unity3d.com/Manual/SL-ShaderPrograms.html)
