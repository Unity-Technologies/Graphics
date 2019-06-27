Shader "Hidden/HDRP/PupilSizeEstimation"
{
    HLSLINCLUDE

    #pragma enable_d3d11_debug_symbols

    #pragma target 4.5
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
    // Supported shadow modes per light type
    #pragma multi_compile SHADOW_LOW SHADOW_MEDIUM SHADOW_HIGH SHADOW_VERY_HIGH

    #define USE_CLUSTERED_LIGHTLIST // There is not FPTL lighting when using transparent

    #define SHADERPASS SHADERPASS_FORWARD
    #define HAS_LIGHTLOOP
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl"

    TEXTURE2D_X(_DepthTexture);

    struct Attributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord   : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
        output.texcoord = GetNormalizedFullScreenTriangleTexCoord(input.vertexID);
        return output;
    }


    float Frag(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        // Get distortion values
        float depthValue = LOAD_TEXTURE2D_X(_DepthTexture, input.positionCS.xy);

        PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw, depthValue, UNITY_MATRIX_I_VP, UNITY_MATRIX_V, 0);
        float3 V = GetWorldSpaceNormalizeViewDir(posInput.positionWS);

        // Decode the world space normal
        NormalData normalData;
        DecodeFromNormalBuffer(input.positionCS.xy, normalData);

        // Read the bsdf data and builtin data from the gbuffer
        BSDFData bsdfData;
        ZERO_INITIALIZE(BSDFData, bsdfData);
        bsdfData.materialFeatures = MATERIALFEATUREFLAGS_LIT_STANDARD;
        bsdfData.diffuseColor = float3(1.0, 1.0, 1.0);
        bsdfData.fresnel0 = float3(0.04, 0.04, 0.04);
        bsdfData.ambientOcclusion = 1.0;
        bsdfData.specularOcclusion = 1.0;
        bsdfData.normalWS = normalData.normalWS;
        bsdfData.perceptualRoughness = 1.0;
        bsdfData.coatMask = 0.0;
        bsdfData.roughnessT = 1.0;

        BuiltinData builtinData;
        ZERO_INITIALIZE(BuiltinData, builtinData);
        builtinData.opacity = 1.0;
        builtinData.renderingLayers = DEFAULT_LIGHT_LAYERS;
        builtinData.shadowMask0 = 1.0;
        builtinData.shadowMask1 = 1.0;
        builtinData.shadowMask2 = 1.0;
        builtinData.shadowMask3 = 1.0;
        builtinData.bakeDiffuseLighting = SAMPLE_TEXTURECUBE_ARRAY_LOD(_SkyTexture, s_trilinear_clamp_sampler, normalData.normalWS, 0.0, UNITY_SPECCUBE_LOD_STEPS - 1).xyz;
        
        PreLightData preLightData = GetPreLightData(V, posInput, bsdfData);

        uint featureFlags = LIGHT_FEATURE_MASK_FLAGS_OPAQUE;
        float3 diffuseLighting;
        float3 specularLighting;
        LightLoop(V, posInput, preLightData, bsdfData, builtinData, featureFlags, diffuseLighting, specularLighting);
        float luminance = Luminance(diffuseLighting);
        return luminance / (1 + luminance);

    }
    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            Stencil
            {
                WriteMask 64
                ReadMask 64 // StencilBitMask.SMAA
                Ref  64     // StencilBitMask.SMAA
                Comp Equal
                Pass Zero   // We can clear the bit since we won't need anymore.
            }

            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag
            ENDHLSL
        }
    }
    Fallback Off
}
