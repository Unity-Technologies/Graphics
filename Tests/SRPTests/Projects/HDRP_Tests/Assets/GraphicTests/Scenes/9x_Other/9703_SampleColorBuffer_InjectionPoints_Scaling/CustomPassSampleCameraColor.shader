Shader "Renderers/CustomPassSampleCameraColor"
{
    Properties
    {
        _BlendMode("Blend Mode", Int) = 0
        _Color("Color", Color) = (1, 1, 1, 1)
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    // #pragma enable_d3d11_debug_symbols

    //enable GPU instancing support
    #pragma multi_compile_instancing
    #pragma multi_compile _ DOTS_INSTANCING_ON

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode" = "ForwardOnly" }

            Blend Off
            ZWrite Off
            ZTest LEqual

            Cull Back

            HLSLPROGRAM

            // Toggle the alpha test
            #define _ALPHATEST_ON

            // Toggle transparency
            // #define _SURFACE_TYPE_TRANSPARENT

            // Toggle fog on transparent
            #define _ENABLE_FOG_ON_TRANSPARENT

            // List all the attributes needed in your shader (will be passed to the vertex shader)
            // you can see the complete list of these attributes in VaryingMesh.hlsl
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT

            // List all the varyings needed in your fragment shader
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_TANGENT_TO_WORLD

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            // Declare properties in the UnityPerMaterial cbuffer to make the shader compatible with SRP Batcher.
CBUFFER_START(UnityPerMaterial)
            float _AlphaCutoff;
            float _BlendMode;
            float4 _Color;
CBUFFER_END


            #define SHADERPASS SHADERPASS_FORWARD_UNLIT

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VaryingMesh.hlsl"

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/SampleUVMapping.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassSampling.hlsl"

            // If you need to modify the vertex datas, you can uncomment this code
            // Note: all the transformations here are done in object space
            // #define HAVE_MESH_MODIFICATION
            // AttributesMesh ApplyMeshModification(AttributesMesh input, float3 timeParameters)
            // {
            //     input.positionOS += input.normalOS * 0.0001; // inflate a bit the mesh to avoid z-fight
            //     return input;
            // }

            // Put the code to render the objects in your custom pass in this function
            void GetSurfaceAndBuiltinData(FragInputs fragInputs, float3 viewDirection, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
            {
                // Write back the data to the output structures
                ZERO_BUILTIN_INITIALIZE(builtinData); // No call to InitBuiltinData as we don't have any lighting
                ZERO_INITIALIZE(SurfaceData, surfaceData);
                builtinData.opacity = 1;
                builtinData.emissiveColor = float3(0, 0, 0);
                surfaceData.color = CustomPassSampleCameraColor(posInput.positionNDC.xy, 0) * _Color;
            }

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForwardUnlit.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            ENDHLSL
        }
    }
}
