# Reconstruct the world space positions of pixels from the depth texture

The Unity shader in this example reconstructs the world space positions for pixels using a depth texture and screen space UV coordinates. The shader draws a checkerboard pattern on a mesh to visualize the positions.

The following illustration shows the end result:

![Checkerboard pattern visualizing the reconstructed world space positions.](Images/shader-examples/urp-shader-tutorial-reconstruct-world-positions-from-depth.png) 

## Create the sample scene:

Create the sample scene to follow the steps in this section:

1. Install URP into an existing Unity project, or create a new project using the [__Universal Project Template__](creating-a-new-project-with-urp.md).

2. In the sample Scene, create a plane GameObject and place it so that it occludes some of the GameObjects.

    ![Create a plane](Images/shader-examples/urp-shader-tutorial-create-place-gameobj.png)

3. Create a new Material and assign it to the plane.

4. Create a new shader and assign it to the material. Copy and paste the Unity shader source code from the page [URP unlit basic shader](writing-shaders-urp-basic-unlit-structure.md).

5. Select the URP Asset. If you created the project using the Universal Render Pipeline template, the URP Asset path is `Assets/Settings/UniversalRP-HighQuality`.

6. In the URP Asset, in the General section, enable `Depth Texture`.

    ![In URP Asset, enable Depth Texture](Images/shader-examples/urp-asset-depth-texture.png)

7. Open the shader you created on step 4.

## Edit the ShaderLab code

Make the following changes to the ShaderLab code: 

TODO: START HERE.

1. Add a new include for a depth texture shader header. You can place it under the existing include for `Core.hlsl`.
    
    ```c++
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl`"
    ```
    The DeclareDepthTexture.hlsl file contains utilities for sampling camera depth texture which are needed for sampling the Z coordiante for the pixel.

2. Add input Varyings struct for the fragment shader. 
    
    ```c++
    half4 frag(Varyings IN) : SV_Target
    ```
    
    TODO: put this in a different place:
    Varyings are per vertex values (output from the vertex shader) that are interpolated across the triangles to provide per pixel value in the fragment shader.
    
    ```c++
    struct Varyings
    {
        // The positions in this struct must have the SV_POSITION semantic.
        float4 positionHCS  : SV_POSITION;
    };
    ```
    TODO: same as above:
    The Varyings struct has a member `float4 positionHCS : SV_POSITION;` For fragment shader the varying field with `SV_POSITION` will provide a pixel location.

3. To calculate the UV coordinates for sampling the depth buffer, the pixel location is divided with render target resolution.

    ```c++
    float2 UV = IN.positionHCS.xy / _ScaledScreenParams.xy;
    ```
    The _ScaledScreenParams.xy will take into account any scaling of the render target (such as Dynamic Resolution).

4. Sample the depth buffer.
    ```c++
    #if UNITY_REVERSED_Z
    real depth = SampleSceneDepth(UV);
    #else
    // Adjust z to match NDC for OpenGL
    real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(UV));
    #endif
    ```
   `SampleSceneDepth` is defined in the `DeclareDepthTexture.hlsl`. It will return Z value in the range of `[0, 1]`.
   The depth value needs to be in NDC space for the reconstruction function. For Z in D3D it is `[0,1]`, but in OpenGL it is `[-1, 1]`.
   
   `UNITY_REVERSED_Z` is used to detect the platform difference and adjust the Z value range. More detailed explanation in step 6.
   
   `UNITY_NEAR_CLIP_VALUE` is a platform independent near plane value for clip space.

   TODO: Also add the link from Erik.

5. Reconstruct world position from the UV and Z coordinates.
    ```c++
    float3 worldPos = ComputeWorldSpacePosition(UV, depth, UNITY_MATRIX_I_VP);
    ```
    `ComputeWorldSpacePosition` is a utility function that computes the world position from UV and Z depth, defined in `Common.hlsl`.
    `UNITY_MATRIX_I_VP` is an inverse view projection matrix which transforms points from the clip space to the world space.

6. Create the 3D checkboard effect.

    ```c++
    uint scale = 10;
    uint3 worldIntPos = uint3(abs(worldPos.xyz * scale));
    ```
    The `scale` is the inverse scale of the checkboard pattern size.
    The `abs` mirrors the pattern to negative coordinate side.
    The `uint3` snaps the coordinate positions into integers.
    
    ```c++
    bool white = (worldIntPos.x & 1) ^ (worldIntPos.y & 1) ^ (worldIntPos.z & 1);
    half4 color = white ? half4(1,1,1,1) : half4(0,0,0,1);
    ```
    The `AND` operator `<integer value> & 1` checks if the value is even (0) or odd (1). It is used to divide the coordinates into squares.
    The `XOR` operator `<integer value> ^ <integer value>` is used to flip the square color.
    
    ```c++
    #if UNITY_REVERSED_Z
    if(depth < 0.0001)
        return half4(0,0,0,1);
    #else
    if(depth > 0.9999)
        return half4(0,0,0,1);
    #endif
    ```
    The depth buffer might not have a valid value for areas where no geometry is rendered. This part will mask parts of the image close to the `far plane` as black.
    Some platforms use reversed Z values (0 == far vs. 1 == far), to improve Z-buffer value distribution. `UNITY_REVERSED_Z` is used to handle all platforms correctly.
    
The end result should look like this:
![3D Checkerboard](Images/shader-examples/urp-shader-tutorial-reconstruct-world-positions-from-depth.png) 

Below is the complete ShaderLab code for this example.

```c++
// This shader fills the mesh shape with a color predefined in the code.
Shader "Example/URPReconstructWorldPos"
{
    // The properties block of the Unity shader. In this example this block is empty
    // because the output color is predefined in the fragment shader code.
    Properties
    { }

    // The SubShader block containing the Shader code. 
    SubShader
    {
        // SubShader Tags define when and under which conditions a SubShader block or
        // a pass is executed.
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        
        Pass
        {
            // The HLSL code block. Unity SRP uses the HLSL language.
            HLSLPROGRAM
            // This line defines the name of the vertex shader. 
            #pragma vertex vert
            // This line defines the name of the fragment shader. 
            #pragma fragment frag

            // The Core.hlsl file contains definitions of frequently used HLSL
            // macros and functions, and also contains #include references to other
            // HLSL files (for example, Common.hlsl, SpaceTransforms.hlsl, etc.).
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // The DeclareDepthTexture.hlsl file contains utilities for sampling camera depth texture.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            
            // The structure definition defines which variables it contains.
            // This example uses the Attributes structure as an input structure in
            // the vertex shader.
            struct Attributes
            {
                // The positionOS variable contains the vertex positions in object
                // space.
                float4 positionOS   : POSITION;                 
            };

            struct Varyings
            {
                // The positions in this struct must have the SV_POSITION semantic.
                float4 positionHCS  : SV_POSITION;
            };            

            // The vertex shader definition with properties defined in the Varyings 
            // structure. The type of the vert function must match the type (struct)
            // that it returns.
            Varyings vert(Attributes IN)
            {
                // Declaring the output object (OUT) with the Varyings struct.
                Varyings OUT;
                // The TransformObjectToHClip function transforms vertex positions
                // from object space to homogenous clip space.
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                // Returning the output.
                return OUT;
            }

            // The fragment shader definition.
            // The varyings contain interpolated values from vertex shader
            half4 frag(Varyings IN) : SV_Target
            {
                // Compute the pixel UV. Screen pixel pos / screen size.
                float2 UV = IN.positionHCS.xy / _ScaledScreenParams.xy;
                
                // Sample the depth from the camera depth texture.
            #if UNITY_REVERSED_Z
                real depth = SampleSceneDepth(UV);
            #else
                // Adjust z to match NDC for OpenGL [-1, 1]
                real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(UV));
            #endif

                // Reconstruct world position.
                float3 worldPos = ComputeWorldSpacePosition(UV, depth, UNITY_MATRIX_I_VP);

                // Compute the checkerboard effect.
                // Scale is the inverse size of the squares. (The coordinates are scaled)
                uint scale = 10;
                // Scale, mirror and snap the coordinates.
                uint3 worldIntPos = uint3(abs(worldPos.xyz * scale));
                // Split the coordinates into squares and compute a color id value
                bool white = ((worldIntPos.x) & 1) ^ (worldIntPos.y & 1) ^ (worldIntPos.z & 1);
                // Color the square based on the id value, black or white.
                half4 color = white ? half4(1,1,1,1) : half4(0,0,0,1);

                // Set the color black close to the far plane
            #if UNITY_REVERSED_Z
                // Platforms with REVERSED_Z, such as D3D
                if(depth < 0.0001)
                    return half4(0,0,0,1);
            #else
                // Platforms without REVERSED_Z, such as OpenGL
                if(depth > 0.9999)
                    return half4(0,0,0,1);
            #endif
                
                return color;
            }
            ENDHLSL
        }
    }
}
```
