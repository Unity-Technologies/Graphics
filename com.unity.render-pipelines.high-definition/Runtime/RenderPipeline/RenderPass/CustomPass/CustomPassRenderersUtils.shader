Shader "Hidden/HDRP/CustomPassRenderersUtils"
{
    Properties
    {
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    // #pragma enable_d3d11_debug_symbols

    //enable GPU instancing support
    #pragma multi_compile_instancing
    #pragma multi_compile _ DOTS_INSTANCING_ON

    // List all the attributes needed in your shader (will be passed to the vertex shader)
    // you can see the complete list of these attributes in VaryingMesh.hlsl
    #define ATTRIBUTES_NEED_TEXCOORD0
    #define ATTRIBUTES_NEED_NORMAL
    #define ATTRIBUTES_NEED_TANGENT

    // List all the varyings needed in your fragment shader
    #define VARYINGS_NEED_TEXCOORD0
    #define VARYINGS_NEED_TANGENT_TO_WORLD

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassRenderers.hlsl"

    #pragma vertex Vert
    #pragma fragment Frag

    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "DepthToColorPass"
            Tags { "LightMode" = "DepthToColorPass" }

            Blend Off
            ZWrite Off
            ZTest LEqual

            Cull Back

            HLSLPROGRAM

            // Put the code to render the objects in your custom pass in this function
            void GetSurfaceAndBuiltinData(FragInputs fragInputs, float3 viewDirection, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
            {
                // Write back the data to the output structures
                ZERO_INITIALIZE(BuiltinData, builtinData); // No call to InitBuiltinData as we don't have any lighting
                builtinData.opacity = 1;
                builtinData.emissiveColor = 0;

                // Outline linear eye depth to the color
                surfaceData.color = LinearEyeDepth(fragInputs.positionSS.z, _ZBufferParams);
                surfaceData.normalWS = 0.0;
            }

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForwardUnlit.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "DepthPass"
            Tags { "LightMode" = "DepthPass" }

            Blend Off
            ZWrite On
            ZTest LEqual
            ColorMask 0

            Cull Back

            HLSLPROGRAM

            // Put the code to render the objects in your custom pass in this function
            void GetSurfaceAndBuiltinData(FragInputs fragInputs, float3 viewDirection, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
            {
                // Write back the data to the output structures
                ZERO_INITIALIZE(BuiltinData, builtinData); // No call to InitBuiltinData as we don't have any lighting
                builtinData.opacity = 1;
                builtinData.emissiveColor = 0;
                surfaceData.color = 0;
                surfaceData.normalWS = 0.0;
            }

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForwardUnlit.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "NormalToColorPass"
            Tags { "LightMode" = "NormalToColorPass" }

            Blend Off
            ZWrite Off
            ZTest LEqual

            Cull Back

            HLSLPROGRAM

            // Put the code to render the objects in your custom pass in this function
            void GetSurfaceAndBuiltinData(FragInputs fragInputs, float3 viewDirection, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
            {
                // Write back the data to the output structures
                ZERO_INITIALIZE(BuiltinData, builtinData); // No call to InitBuiltinData as we don't have any lighting
                builtinData.opacity = 1;
                builtinData.emissiveColor = 0;
                surfaceData.color = fragInputs.tangentToWorld[2].xyz;
                surfaceData.normalWS = 0.0;
            }

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForwardUnlit.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "TangentToColorPass"
            Tags { "LightMode" = "TangentToColorPass" }

            Blend Off
            ZWrite Off
            ZTest LEqual

            Cull Back

            HLSLPROGRAM

            // Put the code to render the objects in your custom pass in this function
            void GetSurfaceAndBuiltinData(FragInputs fragInputs, float3 viewDirection, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
            {
                // Write back the data to the output structures
                ZERO_INITIALIZE(BuiltinData, builtinData); // No call to InitBuiltinData as we don't have any lighting
                builtinData.opacity = 1;
                builtinData.emissiveColor = 0;
                surfaceData.color = fragInputs.tangentToWorld[0].xyz;
                surfaceData.normalWS = 0.0;
            }

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForwardUnlit.hlsl"

            ENDHLSL
        }
    }
}
