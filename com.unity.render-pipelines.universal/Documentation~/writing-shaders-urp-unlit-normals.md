# Visualizing normal vectors

The Unity shader in this example visualizes the normal vector values on the mesh.

Use the Unity shader source file from section [URP unlit basic shader](writing-shaders-urp-basic-unlit-structure.md) and make the following changes to the ShaderLab code:

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

    ![Rendering normals without compression](Images/shader-examples/unlit-shader-tutorial-normals-uncompressed.png)

    A part of the capsule is black. This is because in those points, all three components of the normal vector are negative. The next step shows how to render values in those areas as well.

5. To render negative normal vector components, use the compression technique. To compress the range of normal component values `(-1..1)` to color value range `(0..1)`, change the following line:

    ```c++
    color.rgb = IN.normal;
    ```

    to this line:

    ```c++
    color.rgb = IN.normal * 0.5 + 0.5;
    ```

Now Unity renders the normal vector values as colors on the mesh.

![Rendering normals with compression](Images/shader-examples/unlit-shader-tutorial-normals.png)

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
                // Declaring the variable containing the normal vector for each
                // vertex.
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
                // Use the TransformObjectToWorldNormal function to transform the
                // normals from object to world space. This function is from the 
                // SpaceTransforms.hlsl file, which is referenced in Core.hlsl.
                OUT.normal = TransformObjectToWorldNormal(IN.normal);                
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {                
                half4 color = 0;
                // IN.normal is a 3D vector. Each vector component has the range
                // -1..1. To show all vector elements as color, including the
                // negative values, compress each value into the range 0..1.
                color.rgb = IN.normal * 0.5 + 0.5;                
                return color;
            }
            ENDHLSL
        }
    }
}
```
