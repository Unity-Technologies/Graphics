Shader "HDRP/OcclusionCulling"
{
    Properties
    {
        // Mandatory or we get yelled by the runtime
        _MainTex("Albedo", 2D) = "white" {}
        _Color("Color", Color) = (0,0,0,0)
    }

    HLSLINCLUDE

    #pragma target 4.5
    // #pragma enable_d3d11_debug_symbols

    //-------------------------------------------------------------------------------------
    // Variant
    //-------------------------------------------------------------------------------------


    //-------------------------------------------------------------------------------------
    // Include
    //-------------------------------------------------------------------------------------

    #define DOTS_INSTANCING_ON
    #define UNITY_DONT_INSTANCE_OBJECT_MATRICES
    #define SHADERPASS SHADERPASS_VISIBILITY_OCCLUSION_CULLING

    #pragma multi_compile _ DEBUG_OUTPUT

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Visibility/OcclusionCommon.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"

    //-------------------------------------------------------------------------------------
    // variable declaration
    //-------------------------------------------------------------------------------------

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Visibility/VisibilityProperties.hlsl"

    ENDHLSL

    SubShader
    {
        // This tags allow to use the shader replacement features
        Tags{ "RenderPipeline"="HDRenderPipeline" }

        Pass
        {
            Name "VBuffer"
            Tags { "LightMode" = "VBufferOcclusionCulling" } // This will be only for opaque object based on the RenderQueue index

            Cull Off
            ZTest Less
            ZWrite Off
            Offset -1, -1

            HLSLPROGRAM

            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

            #define PROCEDURAL_CUBE 1

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Visibility/VisibilityCommon.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Visibility/VisibilityPass.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassOcclusionCulling.hlsl"

            #pragma vertex VertOcclusion
            #pragma fragment FragOcclusion

            ENDHLSL
        }

        Pass
        {
            Name "VBufferCubeMesh"
            Tags { "LightMode" = "VBufferOcclusionCulling" } // This will be only for opaque object based on the RenderQueue index

            Cull Off
            ZTest Less
            ZWrite Off
            Offset -1, -1

            HLSLPROGRAM

            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Visibility/VisibilityCommon.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Visibility/VisibilityPass.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassOcclusionCulling.hlsl"

            #pragma vertex VertOcclusion
            #pragma fragment FragOcclusion

            ENDHLSL
        }
    }
}
