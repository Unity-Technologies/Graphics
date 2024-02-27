# Upgrade custom shaders for URP compatibility

Custom Shaders written for the Built-In Render Pipeline are not compatible with the Universal Render Pipeline (URP), and you can't upgrade them automatically with the Render Pipeline Converter. Instead, you must rewrite the incompatible sections of shader code to work with URP.

You can also recreate custom shaders in Shader Graph. For more information, refer to documentation on [ShaderGraph](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest).

> **Note**: You can identify any materials in a scene that use custom shaders when you upgrade to URP as they turn magenta (bright pink) to indicate an error.

This guide demonstrates how to upgrade a custom unlit shader from Built-In Render Pipeline to be fully compatible with URP through the following sections:

* [Example Built-In Render Pipeline custom shader](#example-built-in-render-pipeline-custom-shader)
* [Make the custom shader URP compatible](#make-the-custom-shader-urp-compatible)
* [Enable tiling and offset for the shader](#enable-tiling-and-offset-for-the-shader)
* [Complete shader code](#complete-shader-code)

## Example Built-In Render Pipeline custom shader

The following shader is a simple unlit shader that works with the Built-In Render Pipeline. This guide demonstrates how to upgrade this shader to be compatible with URP.

```c++
Shader "Custom/UnlitShader"
{
    Properties
    {
        [NoScaleOffset] _MainTex("Main Texture", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct v2f
            {
                float4 position : SV_POSITION;
                float2 uv: TEXCOORD0;
            };

            float4 _Color;
            sampler2D _MainTex;

            v2f vert(appdata_base v)
            {
                v2f o;

                o.position = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 texel = tex2D(_MainTex, i.uv);
                return texel * _Color;
            }
            ENDCG
        }
    }
}
```

## Make the custom shader URP compatible

Built-In Render Pipeline shaders have two issues, which you can see in the Inspector window:

* A warning that **Material property is found in another cbuffer**.
* The **SRP Batcher** property displays **not compatible**.

The following steps show how to solve these issues and make a shader compatible with URP and the SRP Batcher.

1. Change `CGPROGRAM` and `ENDCG` to `HLSLPROGRAM` and `ENDHLSL`.
2. Update the include statement to reference the `Core.hlsl` file.

    ```c++
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    ```

    > **Note**: `Core.hlsl` includes the core SRP library, URP shader variables, and matrix defines and transformations, but it does not include lighting functions or default structs.

3. Add `"RenderPipeline" = "UniversalPipeline"` to the shader tags.

    ```c++
    Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
    ```

    > **Note**: URP does not support all ShaderLab tags. For more information on which tags URP supports, refer to [URP ShaderLab Pass tags](./urp-shaderlab-pass-tags.md).

4. Replace the `struct v2f` code block with the following `struct Varyings` code block. This changes the struct to use the URP naming convention of `Varyings` instead of `v2f`, and updates the shader to use the correct variables for URP.

    ```c++
    struct Varyings
    {
        // The positions in this struct must have the SV_POSITION semantic.
        float4 positionHCS  : SV_POSITION;
        float2 uv : TEXCOORD0;
    };
    ```

5. Beneath the include statement and above the `Varyings` struct, define a new struct with the name `Attributes`. This is equivalent to the Built-In Render Pipeline's appdata structs but with the new URP naming conventions.
6. Add the variables shown below to the `Attributes` struct.

    ```c++
    struct Attributes
    {
        float4 positionOS   : POSITION;
        float2 uv : TEXCOORD0;
    };
    ```

7. Update the `v2f vert` function definition to use the new `Varyings` struct and take an instance of the `Attributes` struct as an input, as shown below.

    ```c++
    Varyings vert(Attributes IN)
    ```

8. Update the vert function to output an instance of the `Varyings` struct and use the `TransformObjectToHClip` function to convert from object space to clip space. The function also needs to take the input `Attributes` UV and pass it to the output `Varyings` UV.

    ```c++
    Varyings vert(Attributes IN)
    {
        Varyings OUT;

        OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
        OUT.uv = IN.uv;

        return OUT;
    }
    ```

    > **Note**: URP shaders use suffixes to indicate the space. `OS` means object space, and `HCS` means homogeneous clip space.

9. Place a `CBUFFER` code block around the properties the shader uses, along with the `UnityPerMaterial` parameter.

    ```c++
    CBUFFER_START(UnityPerMaterial)
    float4 _Color;
    sampler2D _MainTex;
    CBUFFER_END
    ```

    > **Note**: For a shader to be SRP Batcher compatible, you must declare all material properties within a `CBUFFER` code block. Even if a shader has multiple passes, all passes must use the same `CBUFFER` block.

10. Update the `frag` function to use the `Varyings` input and the type `half4`, as shown below. The `frag` function must now use this type, as URP shaders do not support fixed types.

    ```c++
    half4 frag(Varyings IN) : SV_Target
    {
        half4 texel = tex2D(_MainTex, IN.uv);
        return texel * _Color;
    }
    ```

This custom unlit shader is now compatible with the SRP Batcher and ready for use within URP. You can check this in the Inspector window:

* The warning that **Material property is found in another cbuffer** no longer appears.
* The **SRP Batcher** property displays **compatible**.

## Enable tiling and offset for the shader

Although the shader is now compatible with URP and the SRP Batcher, you can't use use the **Tiling** and **Offset** properties without further changes. To add this functionality to the custom unlit shader, use the following steps.

1. Rename the property `_MainTex` to `_BaseMap` along with any references to this property. This brings the shader code closer to standard URP shader conventions.
2. Remove the `[NoScaleOffset]` ShaderLab attribute from the `_BaseMap` property. You can now see **Tiling** and **Offset** properties in the shader's Inspector window.
3. Add the `[MainTexture]` ShaderLab attribute to the `_BaseMap` property and the `[MainColor]` attribute to the `_Color` property. This tells the Editor which property to return when you request the main texture or main color from another part of your project or in the Editor. The `Properties` section of your shader should now look as follows:

    ```c++
    Properties
    {
        [MainTexture] _BaseMap("Main Texture", 2D) = "white" {}
        [MainColor] _Color("Color", Color) = (1,1,1,1)
    }
    ```

4. Add the `TEXTURE2D(_BaseMap)` and `SAMPLER(sampler_BaseMap)` macros above the `CBUFFER` block. These macros define the texture and sampler state variables for use later. For more information on sampler states, refer to [Using sampler states](xref:SL-SamplerStates).

    ```c++
    TEXTURE2D(_BaseMap);
    SAMPLER(sampler_BaseMap);
    ```

5. Change the `sampler2D _BaseMap` variable inside the `CBUFFER` block to `float4 _BaseMap_ST`. This variable now stores the tiling and offset values set in the Inspector.

    ```c++
    CBUFFER_START(UnityPerMaterial)
    float4 _Color;
    float4 _BaseMap_ST;
    CBUFFER_END
    ```

6. Change the `frag` function to access the texture with a macro instead of `tex2D` directly. To do this, replace `tex2D` with the `SAMPLE_TEXTURE2D` macro and add `sampler_BaseMap` as an additional parameter, as shown below:

    ```c++
    half4 frag(Varyings IN) : SV_Target
    {
        half4 texel = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
        return texel * _Color;
    }
    ```

7. In the `vert` function, change `OUT.uv` to use a macro instead of passing the texture coordinates as `IN.uv` directly. To do this, replace `IN.uv` with `TRANSFORM_TEX(IN.uv, _BaseMap)`. Your `vert` function should now look like the following example:

    ```c++
    Varyings vert(Attributes IN)
    {
        Varyings OUT;

        OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
        OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);

        return OUT;
    }
    ```

    > **Note**: It's important that you define the `vert` function after the `CBUFFER` block, as the `TRANSFORM_TEX` macro uses the parameter with the `_ST` suffix.

This shader now has a texture, modified by a color, and is fully SRP Batcher compatible. It also fully supports the **Tiling** and **Offset** properties.

To see an example of the complete shader code, refer to the [Complete shader code](#complete-shader-code) section of this page.

## Complete shader code

```c++
Shader "Custom/UnlitShader"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
    }

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
                float4 positionOS   : POSITION;
                float2 uv: TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv: TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float4 _BaseMap_ST;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 texel = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                return texel * _Color;
            }
            ENDHLSL
        }
    }
}
```
