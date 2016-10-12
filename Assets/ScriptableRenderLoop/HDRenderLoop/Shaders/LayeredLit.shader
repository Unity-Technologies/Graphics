Shader "Unity/LayeredLit"
{
    Properties
    {
        // Following set of parameters represent the parameters node inside the MaterialGraph.
        // They are use to fill a SurfaceData. With a MaterialGraph this should not exist.

        // Reminder. Color here are in linear but the UI (color picker) do the conversion sRGB to linear
        _BaseColor0("BaseColor0", Color) = (1,1,1,1)
        _BaseColor1("BaseColor1", Color) = (1, 1, 1, 1)
        _BaseColor2("BaseColor2", Color) = (1, 1, 1, 1)
        _BaseColor3("BaseColor3", Color) = (1, 1, 1, 1)

        _BaseColorMap0("BaseColorMap0", 2D) = "white" {}
        _BaseColorMap1("BaseColorMap1", 2D) = "white" {}
        _BaseColorMap2("BaseColorMap2", 2D) = "white" {}
        _BaseColorMap3("BaseColorMap3", 2D) = "white" {}

        _Metalic0("Metalic0", Range(0.0, 1.0)) = 0
        _Metalic1("Metalic1", Range(0.0, 1.0)) = 0
        _Metalic2("Metalic2", Range(0.0, 1.0)) = 0
        _Metalic3("Metalic3", Range(0.0, 1.0)) = 0

        _Smoothness0("Smoothness0", Range(0.0, 1.0)) = 0.5
        _Smoothness1("Smoothness1", Range(0.0, 1.0)) = 0.5
        _Smoothness2("Smoothness2", Range(0.0, 1.0)) = 0.5
        _Smoothness3("Smoothness3", Range(0.0, 1.0)) = 0.5

        _MaskMap0("MaskMap0", 2D) = "white" {}
        _MaskMap1("MaskMap1", 2D) = "white" {}
        _MaskMap2("MaskMap2", 2D) = "white" {}
        _MaskMap3("MaskMap3", 2D) = "white" {}

        _SpecularOcclusionMap0("SpecularOcclusion0", 2D) = "white" {}
        _SpecularOcclusionMap1("SpecularOcclusion1", 2D) = "white" {}
        _SpecularOcclusionMap2("SpecularOcclusion2", 2D) = "white" {}
        _SpecularOcclusionMap3("SpecularOcclusion3", 2D) = "white" {}

        _NormalMap0("NormalMap0", 2D) = "bump" {}
        _NormalMap1("NormalMap1", 2D) = "bump" {}
        _NormalMap2("NormalMap2", 2D) = "bump" {}
        _NormalMap3("NormalMap3", 2D) = "bump" {}

        [Enum(TangentSpace, 0, ObjectSpace, 1)] _NormalMapSpace("NormalMap space", Float) = 0

        _HeightMap0("HeightMap0", 2D) = "black" {}
        _HeightMap1("HeightMap1", 2D) = "black" {}
        _HeightMap2("HeightMap2", 2D) = "black" {}
        _HeightMap3("HeightMap3", 2D) = "black" {}

        _HeightScale0("Height Scale0", Float) = 1
        _HeightScale1("Height Scale1", Float) = 1
        _HeightScale2("Height Scale2", Float) = 1
        _HeightScale3("Height Scale3", Float) = 1

        _HeightBias0("Height Bias0", Float) = 0
        _HeightBias1("Height Bias1", Float) = 0
        _HeightBias2("Height Bias2", Float) = 0
        _HeightBias3("Height Bias3", Float) = 0

        [Enum(Parallax, 0, Displacement, 1)] _HeightMapMode("Heightmap usage", Float) = 0

        _EmissiveColor0("EmissiveColor0", Color) = (0, 0, 0)
        _EmissiveColor1("EmissiveColor1", Color) = (0, 0, 0)
        _EmissiveColor2("EmissiveColor2", Color) = (0, 0, 0)
        _EmissiveColor3("EmissiveColor3", Color) = (0, 0, 0)

        _EmissiveColorMap0("EmissiveColorMap0", 2D) = "white" {}
        _EmissiveColorMap1("EmissiveColorMap1", 2D) = "white" {}
        _EmissiveColorMap2("EmissiveColorMap2", 2D) = "white" {}
        _EmissiveColorMap3("EmissiveColorMap3", 2D) = "white" {}

        _EmissiveIntensity0("EmissiveIntensity0", Float) = 0
        _EmissiveIntensity1("EmissiveIntensity1", Float) = 0
        _EmissiveIntensity2("EmissiveIntensity2", Float) = 0
        _EmissiveIntensity3("EmissiveIntensity3", Float) = 0

        _LayerMaskMap("LayerMaskMap", 2D) = "white" {}

        [ToggleOff]     _DistortionOnly("Distortion Only", Float) = 0.0
        [ToggleOff]     _DistortionDepthTest("Distortion Only", Float) = 0.0

        [ToggleOff]  _AlphaCutoffEnable("Alpha Cutoff Enable", Float) = 0.0

        _AlphaCutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        // Blending state
        [HideInInspector] _SurfaceType("__surfacetype", Float) = 0.0
        [HideInInspector] _BlendMode ("__blendmode", Float) = 0.0
        [HideInInspector] _SrcBlend ("__src", Float) = 1.0
        [HideInInspector] _DstBlend ("__dst", Float) = 0.0
        [HideInInspector] _ZWrite ("__zw", Float) = 1.0
        [HideInInspector] _CullMode("__cullmode", Float) = 2.0
        // Material Id
        [HideInInspector] _MaterialId("_MaterialId", FLoat) = 0

        [HideInInspector] _LayerCount("__layerCount", Float) = 2.0

        [Enum(Mask Alpha, 0, BaseColor Alpha, 1)] _SmoothnessTextureChannel("Smoothness texture channel", Float) = 1
        [Enum(Use Emissive Color, 0, Use Emissive Mask, 1)] _EmissiveColorMode("Emissive color mode", Float) = 1
        [Enum(None, 0, DoubleSided, 1, DoubleSidedLigthingFlip, 2, DoubleSidedLigthingMirror, 3)] _DoubleSidedMode("Double sided mode", Float) = 0
    }

    HLSLINCLUDE

    #pragma target 5.0
    #pragma only_renderers d3d11 // TEMP: unitl we go futher in dev

    #pragma shader_feature _ALPHATEST_ON
    #pragma shader_feature _ _DOUBLESIDED_LIGHTING_FLIP _DOUBLESIDED_LIGHTING_MIRROR
    #pragma shader_feature _NORMALMAP
    #pragma shader_feature _NORMALMAP_TANGENT_SPACE
    #pragma shader_feature _MASKMAP
    #pragma shader_feature _SPECULAROCCLUSIONMAP
    #pragma shader_feature _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
    #pragma shader_feature _EMISSIVE_COLOR
    #pragma shader_feature _EMISSIVE_COLOR_MAP
    #pragma shader_feature _HEIGHTMAP
    #pragma shader_feature _HEIGHTMAP_AS_DISPLACEMENT
    #pragma shader_feature _LAYERMASKMAP
    #pragma shader_feature _ _LAYEREDLIT_3_LAYERS _LAYEREDLIT_4_LAYERS

    #include "LayeredLitTemplate.hlsl"

    ENDHLSL

    SubShader
    {
        Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
        LOD 300

        // ------------------------------------------------------------------
        //  Deferred pass
        Pass
        {
            Name "GBuffer"  // Name is not used
            Tags { "LightMode" = "GBuffer" } // This will be only for opaque object based on the RenderQueue index

            Cull  [_CullMode]

            HLSLPROGRAM

            #pragma vertex VertDefault
            #pragma fragment FragDeferred

            #if SHADER_STAGE_FRAGMENT

            void FragDeferred(  PackedVaryings packedInput,
                                OUTPUT_GBUFFER(outGBuffer)
                                #ifdef VELOCITY_IN_GBUFFER
                                , OUTPUT_GBUFFER_VELOCITY(outGBuffer)
                                #endif
                                , OUTPUT_GBUFFER_BAKE_LIGHTING(outGBuffer)
                                )
            {
                Varyings input = UnpackVaryings(packedInput);
                SurfaceData surfaceData;
                BuiltinData builtinData;
                GetSurfaceAndBuiltinData(input, surfaceData, builtinData);

                ENCODE_INTO_GBUFFER(surfaceData, outGBuffer);
                #ifdef VELOCITY_IN_GBUFFER
                ENCODE_VELOCITY_INTO_GBUFFER(builtinData.velocity, outGBuffer);
                #endif
                ENCODE_BAKE_LIGHTING_INTO_GBUFFER(GetBakedDiffuseLigthing(surfaceData, builtinData), outGBuffer);
            }

            #endif

            ENDHLSL
        }

        // ------------------------------------------------------------------
        //  Debug pass
        Pass
        {
            Name "Debug"
            Tags{ "LightMode" = "DebugView" }

            Cull[_CullMode]

            HLSLPROGRAM

            #pragma vertex VertDefault
            #pragma fragment FragDebug

            #include "Assets/ScriptableRenderLoop/ShaderLibrary/Color.hlsl"

            int _DebugViewMaterial;

            #if SHADER_STAGE_FRAGMENT

            float4 FragDebug(PackedVaryings packedInput) : SV_Target
            {
                Varyings input = UnpackVaryings(packedInput);
                SurfaceData surfaceData;
                BuiltinData builtinData;
                GetSurfaceAndBuiltinData(input, surfaceData, builtinData);

                BSDFData bsdfData = ConvertSurfaceDataToBSDFData(surfaceData);

                float3 result = float3(1.0, 1.0, 0.0);
                bool needLinearToSRGB = false;

                GetVaryingsDataDebug(_DebugViewMaterial, input, result, needLinearToSRGB);
                GetBuiltinDataDebug(_DebugViewMaterial, builtinData, result, needLinearToSRGB);
                GetSurfaceDataDebug(_DebugViewMaterial, surfaceData, result, needLinearToSRGB);
                GetBSDFDataDebug(_DebugViewMaterial, bsdfData, result, needLinearToSRGB); // TODO: This required to initialize all field from BSDFData...

                // TEMP!
                // For now, the final blit in the backbuffer performs an sRGB write
                // So in the meantime we apply the inverse transform to linear data to compensate.
                if (!needLinearToSRGB)
                    result = SRGBToLinear(max(0, result));

                return float4(result, 0.0);
            }

            #endif

            ENDHLSL
        }

        // ------------------------------------------------------------------
        //  forward pass
        Pass
        {
            Name "Forward" // Name is not used
            Tags{ "LightMode" = "Forward" } // This will be only for transparent object based on the RenderQueue index

            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            Cull[_CullMode]

            HLSLPROGRAM

            #pragma vertex VertDefault
            #pragma fragment FragForward

            #if SHADER_STAGE_FRAGMENT

            float4 FragForward(PackedVaryings packedInput) : SV_Target
            {
                Varyings input = UnpackVaryings(packedInput);
                float3 V = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float3 positionWS = input.positionWS;

                SurfaceData surfaceData;
                BuiltinData builtinData;
                GetSurfaceAndBuiltinData(input, surfaceData, builtinData);

                BSDFData bsdfData = ConvertSurfaceDataToBSDFData(surfaceData);

                float4 diffuseLighting;
                float4 specularLighting;
                ForwardLighting(V, positionWS, bsdfData, diffuseLighting, specularLighting);

                diffuseLighting.rgb += GetBakedDiffuseLigthing(surfaceData, builtinData);

                return float4(diffuseLighting.rgb + specularLighting.rgb, builtinData.opacity);
            }

            #endif

            ENDHLSL
        }
    }

    CustomEditor "LayeredLitGUI"
}
