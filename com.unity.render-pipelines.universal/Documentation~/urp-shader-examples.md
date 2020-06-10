# URP unlit shader examples

This section contains URP-compatible shader examples that help you to get started with writing shaders for URP.

The section contains the following topics:

* [Creating a sample scene](#prerequisites)
* [URP basic shader](#urp-unlit-basic-shader)
* [URP unlit shader with color input](#urp-unlit-color-shader)
* [Visualizing normal vectors](#urp-unlit-normals-shader)
* [Drawing a texture](#urp-unlit-normals-shader)

Each example covers some extra information compared to the basic shader example, and contains the explanation of that information.

<a name="prerequisites"></a>

## Creating a sample scene

To follow the examples in this section:

1. Create a new project using the [__Universal Project Template__](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@8.0/manual/creating-a-new-project-with-urp.html).

2. In the sample Scene, create a GameObject to test the shaders on, for example, a capsule.
    ![Sample GameObject](Images/shader-examples/urp-template-sample-object.jpg)

3. Create a new Material and assign it to the capsule.

4. Create a new Shader asset and assign it to the Material of the capsule. When following an example, replace the code in the Shader asset with the code in the example.

<a name="urp-unlit-basic-shader"></a>

## URP unlit basic shader

This example describes the basic URP-compatible shader.

The shader fills the mesh shape with a color predefined in the shader code.

### Shader code 

```c++
// This shader fills the mesh shape with a color predefined in the code.
Shader "Example/URPUnlitShaderBasic"
{
    // The properties block of the shader. In this example this block is empty since 
    // the output color is predefined in the fragment shader code.
    Properties
    { }

    // The SubShader block containing the Shader code. 
    SubShader
    {
        // Tags define when and under which conditions a SubShader block or a pass
        // is executed.
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline" }
        
        Pass
        {
            // The HLSL code block. Unity SRP uses the HLSL language.
            HLSLPROGRAM
            // This line defines the name of the vertex shader. 
            #pragma vertex vert
            // This line defines the name of the fragment shader. 
            #pragma fragment frag

            // The Core.hlsl file contains definitions of frequently used HLSL macros and
            // functions, and also contains #include references to other HLSL files 
            // (for example, Common.hlsl, SpaceTransforms.hlsl, etc.).
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"            
            
            // The structure definition defines which variables it contains.
            // This example uses the Attributes structure as an input structure in the
            // vertex shader.
            struct Attributes
            {
                // The positionOS variable contains the vertex positions.
                float4 positionOS   : POSITION;                 
            };

            struct Varyings
            {
                // The positions in this struct must have the SV_POSITION semantic.
                float4 positionHCS  : SV_POSITION;
            };            

            // The vertex shader definition with paroperties defined in the Varyings 
            // structure. The type of the vert function must match the type (struct) that it
            // returns.
            Varyings vert(Attributes IN)
            {
                // Declaring the output object (OUT) with the Varyings struct.
                Varyings OUT;
                // The TransformObjectToHClip function transforms positions from object
                // space to homogenous space
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                // Returning the output.
                return OUT;
            }

            // The fragment shader definition.
            half4 frag() : SV_Target
            {
                // Defining the color variable and returning it.
                half4 customColor;
                customColor = half4(0.5, 0, 0, 1);
                return customColor;
            }
            ENDHLSL
        }
    }
}
```

### Shader code description

TODO

```c++
    Properties
    { }
```

In this example the block is empty since the output color is predefined in the fragment shader code.

<a name="urp-unlit-color-shader"></a>
## URP unlit shader with color input


<a name="urp-unlit-normals-shader"></a>
## Visualizing normal vectors


<a name="urp-unlit-texture-shader"></a>
## Drawing a texture

