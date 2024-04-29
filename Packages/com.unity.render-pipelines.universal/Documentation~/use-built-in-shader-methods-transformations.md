# Transform positions in a custom URP shader

To transform positions in a custom Universal Render Pipeline (URP) shader, follow these steps:

1. Add `#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"` inside the `HLSLPROGRAM` in your shader file. The `Core.hlsl` file imports the `ShaderVariablesFunction.hlsl` file.
2. Use one of the following methods from the `ShaderVariablesFunction.hlsl` file.

| **Method** | **Syntax** | **Description** |
|-|-|-|
| `GetNormalizedScreenSpaceUV` | `float2 GetNormalizedScreenSpaceUV(float2 positionInClipSpace)` | Converts a position in clip space to screen space. |
| `GetObjectSpaceNormalizeViewDir` | `half3 GetObjectSpaceNormalizeViewDir(float3 positionInObjectSpace)` | Converts a position in object space to the normalized direction towards the viewer. |
| `GetVertexNormalInputs` | `VertexNormalInputs GetVertexNormalInputs(float3 normalInObjectSpace)` | Converts the normal of a vertex in object space to a tangent, bitangent, and normal in world space. You can also input both the normal and a `float4` tangent in object space. |
| `GetVertexPositionInputs` | `VertexPositionInputs GetVertexPositionInputs(float3 positionInObjectSpace)` | Converts the position of a vertex in object space to positions in world space, view space, clip space, and normalized device coordinates. |
| `GetWorldSpaceNormalizeViewDir` | `half3 GetWorldSpaceNormalizeViewDir(float3 positionInWorldSpace)` | Returns the direction from a position in world space to the viewer, and normalizes the direction. |
| `GetWorldSpaceViewDir` | `float3 GetWorldSpaceViewDir(float3 positionInWorldSpace)` | Returns the direction from a position in world space to the viewer. |

## Structs

### VertexPositionInputs

Use the `GetVertexNormalInputs` method to get this struct.

| **Field** | **Description** |
|-------|-------------|
| `float3 positionWS` | The position in world space. |
| `float3 positionVS` | The position in view space. |
| `float4 positionCS` | The position in clip space. |
| `float4 positionNDC` | The position as normalized device coordinates (NDC). |

### VertexNormalInputs

Use the `GetVertexNormalInputs` method to get this struct.

| **Field** | **Description** |
|---------|-------------|
| `real3 tangentWS` | The tangent in world space. |
| `real3 bitangentWS` | The bitangent in world space. |
| `float3 normalWS` | The normal in world space. |

## Example

The following URP shader draws object surfaces with colors that represent their position in screen space.

```hlsl
Shader "Custom/ScreenSpacePosition"
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
                float3 positionWS : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);

                // Get the position of the vertex in different spaces
                VertexPositionInputs positions = GetVertexPositionInputs(IN.positionOS);

                // Set positionWS to the screen space position of the vertex
                OUT.positionWS = positions.positionWS.xyz;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Set the fragment color to the screen space position vector
                return float4(IN.positionWS.xy, 0, 1);
            }
            ENDHLSL
        }
    }
}
```

## Additional resources

- [Writing custom shaders](writing-custom-shaders-urp.md)
- [Upgrade custom shaders for URP compatibility](urp-shaders/birp-urp-custom-shader-upgrade-guide.md)
- [HLSL in Unity](https://docs.unity3d.com/Manual/SL-ShaderPrograms.html)
- [Shader semantics](https://docs.unity3d.com/Manual/SL-ShaderSemantics.html)



