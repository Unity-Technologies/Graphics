# Write a depth-only pass in a Universal Render Pipeline shader

To create a depth prepass that writes the depth of objects before the opaque and transparent passes, add a shader pass that has the `DepthOnly` tag.

Add this pass if you enable a setting that requires a depth prepass, otherwise Unity might render incorrectly. For example, if you enable **Depth Priming** in the [Universal Render Pipleine (URP) Asset](urp-universal-renderer.md), opaque objects are invisible.

**Important**: To render correctly, make sure that every shader pass renders the same number of fragments in the same positions. For example, use an include or a macro to share the same code between passes, especially if you change the positions of vertices, use alpha clipping, or use effects like dithering. 

## Add a depth-only pass

The following example shows how to add a depth-only pass to a shader:

```lang-hlsl
Shader "Example/CustomShader"
{
    SubShader
    {

        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        // Add a depth-only pass
        Pass
        {
            Name "DepthOnlyPass"
            Tags { "LightMode" = "DepthOnly" }

            // Write depth to the depth buffer
            ZWrite On

            // Don't write to the color buffer
            ColorMask 0 

            ...
        }

        // The forward pass. This pass also writes depth by default.
        Pass
        {
            Name "ForwardPass"
            Tags { "LightMode" = "UniversalForward" }

            ...
        }
    }
}
```

## Add a depth and normals pass

The recommended best practice is to also add a pass that has the `DepthNormals` tag, to write both the depth and normals of objects. Otherwise, if you enable features like screen space ambient occlusion (SSAO), Unity might not render objects correctly.

For more information about outputting normals, refer to [Visualize normal vectors in a shader in URP](writing-shaders-urp-unlit-normals.md).

For example:

```lang-hlsl
Pass
{
    Name "DepthNormalsPass"
    Tags { "LightMode" = "DepthNormals" }

    // Write depth to the depth buffer
    ZWrite On

    ...

    float4 frag(Varyings input) : SV_TARGET
    {
        // Return the normal as a value between 0 and 1
        float3 normalWS = normalize(input.normalWS);
        return float4(normalWS * 0.5 + 0.5, 1);
    }}
```

## Additional resources

- [ShaderLab Pass tags](urp-shaders/urp-shaderlab-pass-tags.md)
- [Shader methods](use-built-in-shader-methods.md)
- [Universal Render Pipleine (URP) Asset](urp-universal-renderer.md)
