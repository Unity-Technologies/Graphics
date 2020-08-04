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

This example shows a basic URP-compatible shader. This shader fills the mesh shape with a color predefined in the shader code.

To see the shader in action, copy and paste the following ShadeLab code into the Shader asset.

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
                // The TransformObjectToHClip function transforms vertex positions from
                // object space to homogenous space
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

The shader paints the GameObject dark red.

![The shader paints the GameObject dark red](Images/shader-examples/unlit-shader-tutorial-basic-hardcoded-color.jpg)

The following sections introduce you to the structure of this basic shader.

The shader in this example has the following blocks:

* [Shader](#shader)
* [Properties](#properties)
* [SubShader](#subshader)
* [Pass](#pass)
* [HLSLPROGRAM](#hlsl)

<a name="shader"></a>

### Shader block

Unity shader assets are written in a Unity-specific language called [ShaderLab](https://docs.unity3d.com/Manual/SL-Shader.html). 

A ShaderLab file starts with the `Shader` declaration.

```c++
Shader "Example/URPUnlitShaderBasic"
```

The path in this declaration determines the location of the shader in the Shader menu on a Material.

![location of the shader in the Shader menu on a Material](Images/shader-examples/urp-material-ui-shader-path.png)

<a name="properties"></a>

### Properties block

The [Properties](https://docs.unity3d.com/Manual/SL-Properties.html) block contains the declarations of properties that users can set in the Inspector window on a Material.

In this example, the Properties block is empty, since this shader does not expose any Material properties that a user can define. TODO:reference to Color. 

### SubShader block

A ShaderLab file contains one or more [SubShader](https://docs.unity3d.com/Manual/SL-SubShader.html) blocks. When rendering a mesh, Unity selects the first SubShader block that is compatible with the GPU on the target device.

A SubShader block contains the __Tags__ element.

```
Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline" }
```

The `RenderPipeline` tag in this Shader instructs Unity to use this SubShader block only when the project is using the Universal Render Pipeline.

For more information on Tags, see [ShaderLab: SubShader Tags](https://docs.unity3d.com/Manual/SL-SubShaderTags.html).

### Pass block

In this example, there is one Pass block that contains the HLSL program code. For more information on Pass blocks, see [ShaderLab: Pass](https://docs.unity3d.com/Manual/SL-Pass.html).

### HLSLPROGRAM block

This block contains the HLSL program code.

> **NOTE**: SRP shaders support only the HLSL language.

This block contains the `#include` declaration with the reference to the `Core.hlsl` file.

```c++
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
```

The `Core.hlsl` file contains definitions of frequently used HLSL macros and functions, and also contains #include references to other HLSL files (for example, Common.hlsl, SpaceTransforms.hlsl, etc.).

For example the vertex shader in the HLSL program uses the `TransformObjectToHClip` function from the `SpaceTransforms.hlsl` file. The  function transforms vertex positions from object space to homogenous space:

```c++
Varyings vert(Attributes IN)
{
    Varyings OUT;
    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
    return OUT;
}
```

The fragment shader in this basic HLSL program outputs the single color predefined in the code:

```c++
half4 frag() : SV_Target
{
    half4 customColor;
    customColor = half4(0.5, 0, 0, 1);
    return customColor;
}
```

Section [URP unlit shader with color input](#urp-unlit-color-shader) shows how to add the editable color property in the Inspector window on the Material.

<a name="urp-unlit-color-shader"></a>

## URP unlit shader with color input

The shader in this example adds the __Base Color__ property to the Material. You can select the color using that property and the shader fills the mesh shape with the color.

Use the ShaderLab code from section [URP unlit basic shader](#urp-unlit-basic-shader) and make the following changes to it:

1. Add the `_BaseColor` property definition to the Properties block:
    
    ```c++
    Properties
    { 
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
    }
    ```
    
    This declaration adds the `_BaseColor` property with the label __Base Color__ to the Material:

    ![Base Color property on a Material](Images/shader-examples/urp-material-prop-base-color.png) 

    The `_BaseColor` property name is a reserved name. When you declare a property with this name, Unity uses this property as the [main color](https://docs.unity3d.com/ScriptReference/Material-color.html) of the Material. 

2. After declaring a property in the Properties block, it's necessary to declare it in the HLSL program block. 
    
    > __NOTE__: To ensure that the shader is SRP Batcher compatible, declare all Material properties inside a single `CBUFFER` block with the name `UnityPerMaterial`.
    
    Add the following code before the vertex shader:

    ```c++
    CBUFFER_START(UnityPerMaterial)
        half4 _BaseColor;            
    CBUFFER_END
    ```

3. Change the code in the fragment shader so that it returns the `_BaseColor` property.

    ```c++
    half4 frag() : SV_Target
    {
        return _BaseColor;
    }
    ```

Now you can select the color in the Base Color field in the Inspector window and the shader fills the mesh with that color.

![Base Color field on a Material](Images/shader-examples/unlit-shader-tutorial-color-field-with-scene.jpg)

Below is the complete ShaderLab code for this example.

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

            // To make the shader SRP Batcher compatible, declare all properties related to a Material 
            // in a a single CBUFFER block with the name UnityPerMaterial.
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

Section [Drawing a texture](#urp-unlit-texture-shader) shows how to draw a texture on the mesh.

<a name="urp-unlit-texture-shader"></a>

## Drawing a texture

The shader in this example draws a texture on the mesh.

Use the ShaderLab code from section [URP unlit shader with color input](#urp-unlit-color-shader) and make the following changes to it:

1. In the Properties block, replace the existing code with the `_BaseMap` property definition.

    ```c++
    Properties
    { 
        _BaseMap("Base Map", 2D) = "white"
    }
    ```

    This declaration adds the `_BaseMap` property with the label __Base Map__ to the Material. 
    
    The `_BaseMap` property name is a reserved name. When you declare a property with this name, Unity uses this property as the [main texture](https://docs.unity3d.com/ScriptReference/Material-mainTexture.html) of the Material. 

2. In `struct Attributes` and `struct Varyings`, add the `uv` variable for the UV coordinates on the texture:

    ```c++
    float2 uv           : TEXCOORD0;
    ```

3. Define the texture as a 2D texture and specify a sampler for it. Add the following lines before the CBUFFER block:

    ```c++
    TEXTURE2D(_BaseMap);
    SAMPLER(sampler_BaseMap);
    ```

    The TEXTURE2D and the SAMPLER macros are defined in one of the files referenced in `Core.hlsl`.

4. When you declare a texture property in the Properties block, Unity adds the Tiling and Offset controls to that property in the Inspector. For tiling and offset to work, it's necessary to declare the texture property with the `_ST` suffix in the 'CBUFFER' block. The `_ST` suffix is necessary because some macros (for example, `TRANSFORM_TEX`) use it.

    > __NOTE__: To ensure that the shader is SRP Batcher compatible, declare all Material properties inside a single `CBUFFER` block with the name `UnityPerMaterial`.
    
    ```c++
    CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;
    CBUFFER_END
    ```

5. To apply the tiling and offset transformation, add the following line in the vertex shader:

    ```c++
    OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
    ```

    The `TRANSFORM_TEX` macro is defined in the `Macros.hlsl` file. The `#include` declaration contains a reference to that file.

6. In the fragment shader, use the `SAMPLE_TEXTURE2D` macro to sample the texture:

    ```c++
    half4 frag(Varyings IN) : SV_Target
    {
        half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
        return color;
    }
    ```

Now you can select a texture in the __Base Map__ field in the Inspector window and the shader draws that texture on the mesh.

![Base Map texture on a Material](Images/shader-examples/unlit-shader-tutorial-texture-with-scene.jpg)

Below is the complete ShaderLab code for this example.

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
                // The uv variable contains the UV coordinate on the texture for the given
                // vertex.
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                // The uv variable contains the UV coordinate on the texture for the given
                // vertex.
                float2 uv           : TEXCOORD0;
            };

            // This macro declares _BaseMap as a Texture2D object.
            TEXTURE2D(_BaseMap);
            // This macro declares the sampler for the _BaseMap texture.
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                // The following line declares the _BaseMap_ST variable, so that you can use
                // the _BaseMap variable in the fragment shader.
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
                // The SAMPLE_TEXTURE2D marco samples the texture with the given sampler.
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                return color;
            }
            ENDHLSL
        }
    }
}
```

Section [Visualizing normal vectors](#urp-unlit-normals-shader) shows how to visualize normal vectors on the mesh.

<a name="urp-unlit-normals-shader"></a>

## Visualizing normal vectors

The shader in this example visualizes the normal vector values on the mesh.

Use the ShaderLab code from section [URP unlit basic shader](#urp-unlit-basic-shader) and make the following changes to it:

1. In `struct Attributes`, which is the input structure for the vertex shader in this example, declare the variable containing the normal vector for each vertex.

    ```c++
    struct Attributes
    {
        float4 positionOS   : POSITION;
        // Declaring the variable containing the normal vector for each vertex.
        half3 normal        : NORMAL;
    };
    ```

2. In `struct Varyings`, which is the input structure for the fragment shader in this example, declare the variable for storing the normal vector values for each fragment:

    ```c++
    struct Varyings
    {
        float4 positionHCS  : SV_POSITION;
        // The variable for storing the normal vector values.
        half3 normal        : TEXCOORD0;
    };
    ```

    This example uses the three components of the normal vector as RGB color values for each fragment.

3. To render the normal vector values on the mesh, use the following code as the fragment shader:

    ```c++
    half4 frag(Varyings IN) : SV_Target
    {                
        half4 color = 0;
        color.rgb = IN.normal;
        return color;
    }
    ```

4. Unity renders the normal vector values on the mesh:

    ![Rendering normals without compression](Images/shader-examples/unlit-shader-tutorial-normals-uncompressed.jpg)

    A part of the capsule is black. This is because in those points all three components of the normal vector are negative. The next step shows how to render values in those areas as well.

5. To render negative normal vector components, use the compression technique. To compress the range of normal component values `(-1..1)` to color value range `(0..1)`, change the following line:

    ```c++
    color.rgb = IN.normal;
    ```

    to this line:

    ```c++
    color.rgb = IN.normal * 0.5 + 0.5;
    ```

Now Unity renders the normal vector vales as colors on the mesh.

![Rendering normals with compression](Images/shader-examples/unlit-shader-tutorial-normals.jpg)

Below is the complete ShaderLab code for this example.

```c++
// This shader visuzlizes the normal vector values on the mesh.
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
                // Declaring the variable containing the normal vector for each vertex.
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
                // Using the TransformObjectToWorldNormal function to transform the normals 
                // from object to world space. This function is from the 
                // SpaceTransforms.hlsl file, which is referenced in Core.hlsl.
                OUT.normal = TransformObjectToWorldNormal(IN.normal);                
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {                
                half4 color = 0;
                // IN.normal is a 3D vector. Each vector component has the range -1..1.
                // To show all vector elements as color, including the negative values,
                // compress each value into the range 0..1.
                color.rgb = IN.normal * 0.5 + 0.5;                
                return color;
            }
            ENDHLSL
        }
    }
}
```
