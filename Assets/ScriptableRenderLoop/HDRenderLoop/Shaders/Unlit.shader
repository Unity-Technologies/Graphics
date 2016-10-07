Shader "Unity/Unlit"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1) 
        _ColorMap("ColorMap", 2D) = "white" {}

        _EmissiveColor("EmissiveColor", Color) = (0, 0, 0)
        _EmissiveColorMap("EmissiveColorMap", 2D) = "white" {}
        _EmissiveIntensity("EmissiveIntensity", Float) = 0

        [ToggleOff]		_DistortionOnly("Distortion Only", Float) = 0.0
        [ToggleOff]		_DistortionDepthTest("Distortion Only", Float) = 0.0

        [ToggleOff]  _AlphaCutoffEnable("Alpha Cutoff Enable", Float) = 0.0
        _AlphaCutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        // Blending state
        [HideInInspector] _SurfaceType("__surfacetype", Float) = 0.0
        [HideInInspector] _BlendMode ("__blendmode", Float) = 0.0
        [HideInInspector] _SrcBlend ("__src", Float) = 1.0
        [HideInInspector] _DstBlend ("__dst", Float) = 0.0
        [HideInInspector] _ZWrite ("__zw", Float) = 1.0
        [HideInInspector] _CullMode("__cullmode", Float) = 2.0

        [Enum(None, 0, DoubleSided)] _DoubleSidedMode("Double sided mode", Float) = 0
    }

    HLSLINCLUDE

    #pragma target 5.0
    #pragma only_renderers d3d11 // TEMP: unitl we go futher in dev

    #pragma shader_feature _ALPHATEST_ON
    #pragma shader_feature _EMISSIVE_COLOR_MAP

    #include "Assets/ScriptableRenderLoop/ShaderLibrary/Common.hlsl"
    #define UNITY_MATERIAL_UNLIT
    #include "Material/Material.hlsl"
    #include "ShaderVariables.hlsl"

    float4	_Color;
    sampler2D _ColorMap;
    float4 _EmissiveColor;
    sampler2D _EmissiveColorMap;
    float _EmissiveIntensity;

    ENDHLSL

    SubShader
    {
        Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
        LOD 300

        // ------------------------------------------------------------------
        //  forward pass
        Pass
        {
            Name "Forward" // Name is not used
            Tags { "LightMode" = "ForwardUnlit" } // This will be only for transparent object based on the RenderQueue index

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_CullMode]

            HLSLPROGRAM
        
            #pragma vertex VertDefault
            #pragma fragment FragForward

            // Forward
            struct Attributes
            {
                float3 positionOS	: POSITION;
                float2 uv0			: TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHS;
                float2 texCoord0;
            };

            struct PackedVaryings
            {
                float4 positionHS : SV_Position;
                float4 interpolators[1] : TEXCOORD0;
            };

            // Function to pack data to use as few interpolator as possible, the ShaderGraph should generate these functions
            PackedVaryings PackVaryings(Varyings input)
            {
                PackedVaryings output;
                output.positionHS = input.positionHS;
                output.interpolators[0].xy = input.texCoord0.xy;
                output.interpolators[0].zw = float2(0.0, 0.0);

                return output;
            }

            Varyings UnpackVaryings(PackedVaryings input)
            {
                Varyings output;
                output.positionHS = input.positionHS;
                output.texCoord0.xy = input.interpolators[0].xy;

                return output;
            }

            PackedVaryings VertDefault(Attributes input)
            {
                Varyings output;

                float3 positionWS = TransformObjectToWorld(input.positionOS);
                // TODO deal with camera center rendering and instancing (This is the reason why we always perform tow steps transform to clip space + instancing matrix)
                output.positionHS = TransformWorldToHClip(positionWS);

                output.texCoord0 = input.uv0;

                return PackVaryings(output);
            }


            void GetSurfaceAndBuiltinData(Varyings input, out SurfaceData surfaceData, out BuiltinData builtinData)
            {
                surfaceData.color = tex2D(_ColorMap, input.texCoord0).rgb * _Color.rgb;
                float alpha = tex2D(_ColorMap, input.texCoord0).a * _Color.rgb;

                #ifdef _ALPHATEST_ON
                clip(alpha - _AlphaCutoff);
                #endif

                builtinData.opacity = alpha;

                // Builtin Data
                builtinData.bakeDiffuseLighting = float3(0.0, 0.0, 0.0);

                #ifdef _EMISSIVE_COLOR_MAP
                builtinData.emissiveColor = tex2D(_EmissiveColorMap, input.texCoord0).rgb * _EmissiveColor;
                #else
                builtinData.emissiveColor = _EmissiveColor;
                #endif			

                builtinData.emissiveIntensity = _EmissiveIntensity;

                builtinData.velocity = float2(0.0, 0.0);

                builtinData.distortion = float2(0.0, 0.0);
                builtinData.distortionBlur = 0.0;
            }

            #if SHADER_STAGE_FRAGMENT

            float4 FragForward(PackedVaryings packedInput) : SV_Target
            {
                Varyings input = UnpackVaryings(packedInput);

                SurfaceData surfaceData;
                BuiltinData builtinData;
                GetSurfaceAndBuiltinData(input, surfaceData, builtinData);

                BSDFData bsdfData = ConvertSurfaceDataToBSDFData(surfaceData);

                return float4(bsdfData.color, builtinData.opacity);
            }

            #endif

            ENDHLSL
        }
    }
}
