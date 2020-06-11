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

This example contains the basic URP-compatible shader.

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

<a name="urp-unlit-color-shader"></a>

## URP unlit shader with color input

The shader in this example adds the __Base Color__ field in the Inspector window. You can change the color using that field, and the shaders fills the mesh shape with that color.

```c++
// This shader fills the mesh shape with a color that a user can change using the Inspector window on a Material.
Shader "Example/URPUnlitShaderColor"
{    
    // The _BaseColor variable is visible as a field called Base Color in the Inspector window on a Material.
    // This variable has the default value, and you can select a custom color using the Base Color field.
    Properties
    { 
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
    }
    
    SubShader
    {        
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline" }
                
        Pass
        {            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"            
            
            struct Attributes
            {
                float4 positionOS   : POSITION;                 
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
            };

            // To make the Shader SRP Batcher compatible, declare all properties related to a Material 
            // in a UnityPerMaterial CBUFFER.
            CBUFFER_START(UnityPerMaterial)
                // The following line declares the _BaseColor variable, so that you can use it in the fragment shader.
                half4 _BaseColor;            
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag() : SV_Target
            {
                // Returning the _BaseColor value.                
                return _BaseColor;
            }
            ENDHLSL
        }
    }
}
```

<a name="urp-unlit-normals-shader"></a>

## Visualizing normal vectors

The shader in this example visualizes the normal vector values on the mesh.

```c++
// This shader visualizes the normal vector values on the mesh.
Shader "Example/URPUnlitShaderNormal"
{    
    Properties
    { }
    
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline" }
                
        Pass
        {            
            HLSLPROGRAM            
            #pragma vertex vert            
            #pragma fragment frag
                        
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"            
                        
            struct Attributes
            {
                float4 positionOS   : POSITION;
                // Declaring the normal variable containing the normal vector for each vertex.
                half3 normal        : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                half3 normal        : TEXCOORD0;
            };                                   
            
            Varyings vert(Attributes IN)
            {                
                Varyings OUT;                
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);       
                // Using the TransformObjectToWorldNormal function to transform the normals from object to world space.
                // This function is from the SpaceTransforms.hlsl file, which is in one of the #include definitions referenced in Core.hlsl.
                OUT.normal = TransformObjectToWorldNormal(IN.normal);                
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {                
                half4 color = 0;
                // IN.normal is a 3D vector. Each vector component has the range -1..1.
                // To show the vector elements as color, compress each value into the range 0..1.                
                color.rgb = IN.normal * 0.5 + 0.5;                
                return color;
            }
            ENDHLSL
        }
    }
}
```

<a name="urp-unlit-texture-shader"></a>

## Drawing a texture

The shader in this example draws a texture on the mesh.

```c++
// This shader draws a texture on the mesh.
Shader "Example/URPUnlitShaderTexture"
{
    // The _BaseMap variable is visible as a field called Base Map in the Inspector window on a Material.  
    Properties
    { 
        _BaseMap("Base Map", 2D) = "white"
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline" }
                
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"            
            
            struct Attributes
            {
                float4 positionOS   : POSITION;
                // The uv variable contains the UV coordinate on the texture for the given vertex.
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                // The uv variable contains the UV coordinate on the texture for the given vertex.
                float2 uv           : TEXCOORD0;
            };

            // This macro declares _BaseMap as a Texture2D object.
            TEXTURE2D(_BaseMap);
            // This macro declares the sampler for the _BaseMap texture.
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                // The following line declares the _BaseMap_ST variable, so that you can use the _BaseMap variable in the fragment shader.
                // The _ST suffix is necessary for the tiling and offset function to work.
                float4 _BaseMap_ST;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                // The TRANSFORM_TEX macro performs the tiling and offset transformation.
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // The SAMPLE_TEXTURE2D marco samples the given texture with the given sampler.
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                return color;
            }
            ENDHLSL
        }
    }
}
```
