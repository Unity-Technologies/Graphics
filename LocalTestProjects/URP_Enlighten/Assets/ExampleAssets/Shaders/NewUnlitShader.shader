Shader "CUSTOM Paint1G_WAnim_Shader"
{
    Properties
    {
        Vector1_2EE2CB80("Speed", Float) = 0.1
        Vector2_C848BFFB("Center", Vector) = (0.25, 0.25, 0, 0)
        Color_A2AAE5B5("PaintColor_01", Color) = (0.1137255, 0.627451, 0.8352941, 0)
        Color_23C9B10D("PaintColor_02", Color) = (0.04561231, 0.144953, 0.3867925, 0)
        [NoScaleOffset]Texture2D_C69EB180("Label", 2D) = "white" {}
        [NonModifiableTextureData][NoScaleOffset]_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1("Texture2D", 2D) = "white" {}
        [NonModifiableTextureData][NoScaleOffset]_SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1("Texture2D", 2D) = "white" {}
        [NonModifiableTextureData][NoScaleOffset]_SampleTexture2D_aef34a082e463b8992e5f66de0345979_Texture_1("Texture2D", 2D) = "bump" {}
        [NonModifiableTextureData][NoScaleOffset]_SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_Texture_1("Texture2D", 2D) = "white" {}
        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "UniversalMaterialType" = "Lit"
            "Queue"="Geometry"
        }
        Pass
        {
            Name "Universal Forward"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            // Render State
            Cull Back
        Blend One Zero
        ZTest LEqual
        ZWrite On

            // Debug
            // <None>

            // --------------------------------------------------
            // Pass

            HLSLPROGRAM

            // Pragmas
            #pragma target 4.5
        #pragma exclude_renderers gles gles3 glcore
        #pragma multi_compile_instancing
        #pragma multi_compile_fog
        #pragma multi_compile _ DOTS_INSTANCING_ON
        #pragma vertex vert
        #pragma fragment frag

            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>

            // Keywords
            #pragma multi_compile _ _SCREEN_SPACE_OCCLUSION
        #pragma multi_compile _ LIGHTMAP_ON
        #pragma multi_compile _ DYNAMICLIGHTMAP_ON
        #pragma multi_compile _ DIRLIGHTMAP_COMBINED
        #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
        #pragma multi_compile _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS _ADDITIONAL_OFF
        #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
        #pragma multi_compile _ _SHADOWS_SOFT
        #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
        #pragma multi_compile _ SHADOWS_SHADOWMASK
            // GraphKeywords: <None>

            // Defines
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define ATTRIBUTES_NEED_TEXCOORD2
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_TANGENT_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_VIEWDIRECTION_WS
            #define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_FORWARD
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */

            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            // --------------------------------------------------
            // Structs and Packing

            struct Attributes
        {
            float3 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float4 uv0 : TEXCOORD0;
            float4 uv1 : TEXCOORD1;
            float4 uv2 : TEXCOORD2;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS;
            float3 normalWS;
            float4 tangentWS;
            float4 texCoord0;
            float3 viewDirectionWS;
            #if defined(LIGHTMAP_ON)
            float2 staticLightmapUV;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
            float2 dynamicLightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
            float3 sh;
            #endif
            float4 fogFactorAndVertexLight;
            float4 shadowCoord;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
            float3 TangentSpaceNormal;
            float4 uv0;
            float3 TimeParameters;
        };
        struct VertexDescriptionInputs
        {
            float3 ObjectSpaceNormal;
            float3 ObjectSpaceTangent;
            float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
            float4 positionCS : SV_POSITION;
            float3 interp0 : TEXCOORD0;
            float3 interp1 : TEXCOORD1;
            float4 interp2 : TEXCOORD2;
            float4 interp3 : TEXCOORD3;
            float3 interp4 : TEXCOORD4;
            #if defined(LIGHTMAP_ON)
            float2 interp5 : TEXCOORD5;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
            float2 interp5 : TEXCOORD5;
            #endif
            #if !defined(LIGHTMAP_ON)
            float3 interp6 : TEXCOORD6;
            #endif
            float4 interp7 : TEXCOORD7;
            float4 interp8 : TEXCOORD8;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };

            PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            output.positionCS = input.positionCS;
            output.interp0.xyz =  input.positionWS;
            output.interp1.xyz =  input.normalWS;
            output.interp2.xyzw =  input.tangentWS;
            output.interp3.xyzw =  input.texCoord0;
            output.interp4.xyz =  input.viewDirectionWS;
            #if defined(LIGHTMAP_ON)
            output.interp5.xy =  input.staticLightmapUV;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
            output.interp5.zw =  input.dynamicLightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
            output.interp6.xyz =  input.sh;
            #endif
            output.interp7.xyzw =  input.fogFactorAndVertexLight;
            output.interp8.xyzw =  input.shadowCoord;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.positionWS = input.interp0.xyz;
            output.normalWS = input.interp1.xyz;
            output.tangentWS = input.interp2.xyzw;
            output.texCoord0 = input.interp3.xyzw;
            output.viewDirectionWS = input.interp4.xyz;
            #if defined(LIGHTMAP_ON)
            output.staticLightmapUV = input.interp5.xy;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
            output.dynamicLightmapUV = input.interp5.zw;
            #endif
            #if !defined(LIGHTMAP_ON)
            output.sh = input.interp6.xyz;
            #endif
            output.fogFactorAndVertexLight = input.interp7.xyzw;
            output.shadowCoord = input.interp8.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }

            // --------------------------------------------------
            // Graph

            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
        float Vector1_2EE2CB80;
        float2 Vector2_C848BFFB;
        float4 Color_A2AAE5B5;
        float4 Color_23C9B10D;
        float4 Texture2D_C69EB180_TexelSize;
        float4 _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1_TexelSize;
        float4 _SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1_TexelSize;
        float4 _SampleTexture2D_aef34a082e463b8992e5f66de0345979_Texture_1_TexelSize;
        float4 _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_Texture_1_TexelSize;
        CBUFFER_END

        // Object and Global properties
        TEXTURE2D(Texture2D_C69EB180);
        SAMPLER(samplerTexture2D_C69EB180);
        TEXTURE2D(_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1);
        SAMPLER(sampler_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1);
        SAMPLER(SamplerState_Linear_Repeat);
        SAMPLER(SamplerState_Linear_Clamp);
        TEXTURE2D(_SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1);
        SAMPLER(sampler_SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1);
        TEXTURE2D(_SampleTexture2D_aef34a082e463b8992e5f66de0345979_Texture_1);
        SAMPLER(sampler_SampleTexture2D_aef34a082e463b8992e5f66de0345979_Texture_1);
        TEXTURE2D(_SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_Texture_1);
        SAMPLER(sampler_SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_Texture_1);

            // Graph Functions

        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }

        void Unity_Multiply_float(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }

        void Unity_Multiply_float(float A, float B, out float Out)
        {
            Out = A * B;
        }

        void Unity_Fraction_float(float In, out float Out)
        {
            Out = frac(In);
        }

        void Unity_Rotate_Degrees_float(float2 UV, float2 Center, float Rotation, out float2 Out)
        {
            //rotation matrix
            Rotation = Rotation * (3.1415926f/180.0f);
            UV -= Center;
            float s = sin(Rotation);
            float c = cos(Rotation);

            //center rotation matrix
            float2x2 rMatrix = float2x2(c, -s, s, c);
            rMatrix *= 0.5;
            rMatrix += 0.5;
            rMatrix = rMatrix*2 - 1;

            //multiply the UVs by the rotation matrix
            UV.xy = mul(UV.xy, rMatrix);
            UV += Center;

            Out = UV;
        }

        void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
        {
            Out = lerp(A, B, T);
        }

        void Unity_OneMinus_float(float In, out float Out)
        {
            Out = 1 - In;
        }

            // Graph Vertex
            struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };

        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }

            // Graph Pixel
            struct SurfaceDescription
        {
            float3 BaseColor;
            float3 NormalTS;
            float3 Emission;
            float Metallic;
            float Smoothness;
            float Occlusion;
        };

        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            float4 _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1).samplerstate, IN.uv0.xy);
            float _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_R_4 = _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0.r;
            float _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_G_5 = _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0.g;
            float _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_B_6 = _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0.b;
            float _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_A_7 = _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0.a;
            UnityTexture2D _Property_416a2417a5f16e86998a65bdfbfe105b_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_C69EB180);
            float2 _TilingAndOffset_5e3ed56d4eb5468ea3ee52c90f3fbe01_Out_3;
            Unity_TilingAndOffset_float(IN.uv0.xy, float2 (8, 32), float2 (-3.11, -6.86), _TilingAndOffset_5e3ed56d4eb5468ea3ee52c90f3fbe01_Out_3);
            float4 _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0 = SAMPLE_TEXTURE2D(_Property_416a2417a5f16e86998a65bdfbfe105b_Out_0.tex, UnityBuildSamplerStateStruct(SamplerState_Linear_Clamp).samplerstate, _TilingAndOffset_5e3ed56d4eb5468ea3ee52c90f3fbe01_Out_3);
            float _SampleTexture2D_53ef635328a52180babf003f88901af1_R_4 = _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0.r;
            float _SampleTexture2D_53ef635328a52180babf003f88901af1_G_5 = _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0.g;
            float _SampleTexture2D_53ef635328a52180babf003f88901af1_B_6 = _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0.b;
            float _SampleTexture2D_53ef635328a52180babf003f88901af1_A_7 = _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0.a;
            float4 _Multiply_098a1a5adc36458f9a9db8a89d29e5ee_Out_2;
            Unity_Multiply_float(_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0, _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0, _Multiply_098a1a5adc36458f9a9db8a89d29e5ee_Out_2);
            float4 _Property_c17c21327159a18ca8b8664a8b068016_Out_0 = Color_A2AAE5B5;
            float4 _Property_411901199da6388abd170ed7f1572505_Out_0 = Color_23C9B10D;
            float2 _Property_cdccdf9bd2b88d8ea1a0586c033ac4c8_Out_0 = Vector2_C848BFFB;
            float _Property_aeae242db5457e849304b95d8b1865e5_Out_0 = Vector1_2EE2CB80;
            float _Multiply_66801fb5ead25582bcac188fd69ff0d5_Out_2;
            Unity_Multiply_float(IN.TimeParameters.x, _Property_aeae242db5457e849304b95d8b1865e5_Out_0, _Multiply_66801fb5ead25582bcac188fd69ff0d5_Out_2);
            float _Fraction_f1b6b975af985484be20b419784bde3c_Out_1;
            Unity_Fraction_float(_Multiply_66801fb5ead25582bcac188fd69ff0d5_Out_2, _Fraction_f1b6b975af985484be20b419784bde3c_Out_1);
            float _Multiply_6598b1de072a41869510b57b1b24cfb5_Out_2;
            Unity_Multiply_float(_Fraction_f1b6b975af985484be20b419784bde3c_Out_1, 360, _Multiply_6598b1de072a41869510b57b1b24cfb5_Out_2);
            float2 _Rotate_6f00503a0a9f8e8ca3540eadb1831c6b_Out_3;
            Unity_Rotate_Degrees_float(IN.uv0.xy, _Property_cdccdf9bd2b88d8ea1a0586c033ac4c8_Out_0, _Multiply_6598b1de072a41869510b57b1b24cfb5_Out_2, _Rotate_6f00503a0a9f8e8ca3540eadb1831c6b_Out_3);
            float2 _TilingAndOffset_53b5cba0214a2c8fa262c98e44a577eb_Out_3;
            Unity_TilingAndOffset_float(_Rotate_6f00503a0a9f8e8ca3540eadb1831c6b_Out_3, float2 (4, 4), float2 (0, 0), _TilingAndOffset_53b5cba0214a2c8fa262c98e44a577eb_Out_3);
            float4 _SampleTexture2D_e60950bba990008885c246fd6823bc78_RGBA_0 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1).samplerstate, _TilingAndOffset_53b5cba0214a2c8fa262c98e44a577eb_Out_3);
            float _SampleTexture2D_e60950bba990008885c246fd6823bc78_R_4 = _SampleTexture2D_e60950bba990008885c246fd6823bc78_RGBA_0.r;
            float _SampleTexture2D_e60950bba990008885c246fd6823bc78_G_5 = _SampleTexture2D_e60950bba990008885c246fd6823bc78_RGBA_0.g;
            float _SampleTexture2D_e60950bba990008885c246fd6823bc78_B_6 = _SampleTexture2D_e60950bba990008885c246fd6823bc78_RGBA_0.b;
            float _SampleTexture2D_e60950bba990008885c246fd6823bc78_A_7 = _SampleTexture2D_e60950bba990008885c246fd6823bc78_RGBA_0.a;
            float4 _Lerp_eb1d105d1ff35a88b4a5066f2def8324_Out_3;
            Unity_Lerp_float4(_Property_c17c21327159a18ca8b8664a8b068016_Out_0, _Property_411901199da6388abd170ed7f1572505_Out_0, (_SampleTexture2D_e60950bba990008885c246fd6823bc78_R_4.xxxx), _Lerp_eb1d105d1ff35a88b4a5066f2def8324_Out_3);
            float4 _Lerp_4e0883bd978e1085b325579ae4bceddf_Out_3;
            Unity_Lerp_float4(_Multiply_098a1a5adc36458f9a9db8a89d29e5ee_Out_2, _Lerp_eb1d105d1ff35a88b4a5066f2def8324_Out_3, (_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_A_7.xxxx), _Lerp_4e0883bd978e1085b325579ae4bceddf_Out_3);
            float4 _SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_SampleTexture2D_aef34a082e463b8992e5f66de0345979_Texture_1).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2D_aef34a082e463b8992e5f66de0345979_Texture_1).samplerstate, IN.uv0.xy);
            _SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0.rgb = UnpackNormal(_SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0);
            float _SampleTexture2D_aef34a082e463b8992e5f66de0345979_R_4 = _SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0.r;
            float _SampleTexture2D_aef34a082e463b8992e5f66de0345979_G_5 = _SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0.g;
            float _SampleTexture2D_aef34a082e463b8992e5f66de0345979_B_6 = _SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0.b;
            float _SampleTexture2D_aef34a082e463b8992e5f66de0345979_A_7 = _SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0.a;
            float4 _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_RGBA_0 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_Texture_1).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_Texture_1).samplerstate, IN.uv0.xy);
            float _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_R_4 = _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_RGBA_0.r;
            float _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_G_5 = _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_RGBA_0.g;
            float _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_B_6 = _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_RGBA_0.b;
            float _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_A_7 = _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_RGBA_0.a;
            float _OneMinus_b677fa8cf63d2d8792ac7886710be407_Out_1;
            Unity_OneMinus_float(_SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_A_7, _OneMinus_b677fa8cf63d2d8792ac7886710be407_Out_1);
            surface.BaseColor = (_Lerp_4e0883bd978e1085b325579ae4bceddf_Out_3.xyz);
            surface.NormalTS = (_SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0.xyz);
            surface.Emission = float3(0, 0, 0);
            surface.Metallic = _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_R_4;
            surface.Smoothness = _OneMinus_b677fa8cf63d2d8792ac7886710be407_Out_1;
            surface.Occlusion = _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_G_5;
            return surface;
        }

            // --------------------------------------------------
            // Build Graph Inputs

            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);

            output.ObjectSpaceNormal =           input.normalOS;
            output.ObjectSpaceTangent =          input.tangentOS;
            output.ObjectSpacePosition =         input.positionOS;

            return output;
        }
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);



            output.TangentSpaceNormal =          float3(0.0f, 0.0f, 1.0f);


            output.uv0 =                         input.texCoord0;
            output.TimeParameters =              _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

            return output;
        }

            // --------------------------------------------------
            // Main

            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRForwardPass.hlsl"

            ENDHLSL
        }
        Pass
        {
            Name "GBuffer"
            Tags
            {
                "LightMode" = "UniversalGBuffer"
            }

            // Render State
            Cull Back
        Blend One Zero
        ZTest LEqual
        ZWrite On

            // Debug
            // <None>

            // --------------------------------------------------
            // Pass

            HLSLPROGRAM

            // Pragmas
            #pragma target 4.5
        #pragma exclude_renderers gles gles3 glcore
        #pragma multi_compile_instancing
        #pragma multi_compile_fog
        #pragma multi_compile _ DOTS_INSTANCING_ON
        #pragma vertex vert
        #pragma fragment frag

            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>

            // Keywords
            #pragma multi_compile _ LIGHTMAP_ON
        #pragma multi_compile _ DYNAMICLIGHTMAP_ON
        #pragma multi_compile _ DIRLIGHTMAP_COMBINED
        #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
        #pragma multi_compile _ _SHADOWS_SOFT
        #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
        #pragma multi_compile _ _GBUFFER_NORMALS_OCT
            // GraphKeywords: <None>

            // Defines
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define ATTRIBUTES_NEED_TEXCOORD2
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_TANGENT_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_VIEWDIRECTION_WS
            #define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_GBUFFER
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */

            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            // --------------------------------------------------
            // Structs and Packing

            struct Attributes
        {
            float3 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float4 uv0 : TEXCOORD0;
            float4 uv1 : TEXCOORD1;
            float4 uv2 : TEXCOORD2;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS;
            float3 normalWS;
            float4 tangentWS;
            float4 texCoord0;
            float3 viewDirectionWS;
            #if defined(LIGHTMAP_ON)
            float2 staticLightmapUV;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
            float2 dynamicLightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
            float3 sh;
            #endif
            float4 fogFactorAndVertexLight;
            float4 shadowCoord;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
            float3 TangentSpaceNormal;
            float4 uv0;
            float3 TimeParameters;
        };
        struct VertexDescriptionInputs
        {
            float3 ObjectSpaceNormal;
            float3 ObjectSpaceTangent;
            float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
            float4 positionCS : SV_POSITION;
            float3 interp0 : TEXCOORD0;
            float3 interp1 : TEXCOORD1;
            float4 interp2 : TEXCOORD2;
            float4 interp3 : TEXCOORD3;
            float3 interp4 : TEXCOORD4;
            #if defined(LIGHTMAP_ON)
            float2 interp5 : TEXCOORD5;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
            float2 interp5 : TEXCOORD5;
            #endif
            #if !defined(LIGHTMAP_ON)
            float3 interp6 : TEXCOORD6;
            #endif
            float4 interp7 : TEXCOORD7;
            float4 interp8 : TEXCOORD8;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };

            PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            output.positionCS = input.positionCS;
            output.interp0.xyz =  input.positionWS;
            output.interp1.xyz =  input.normalWS;
            output.interp2.xyzw =  input.tangentWS;
            output.interp3.xyzw =  input.texCoord0;
            output.interp4.xyz =  input.viewDirectionWS;
            #if defined(LIGHTMAP_ON)
            output.interp5.xy =  input.staticLightmapUV;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
            output.interp5.zw =  input.dynamicLightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
            output.interp6.xyz =  input.sh;
            #endif
            output.interp7.xyzw =  input.fogFactorAndVertexLight;
            output.interp8.xyzw =  input.shadowCoord;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.positionWS = input.interp0.xyz;
            output.normalWS = input.interp1.xyz;
            output.tangentWS = input.interp2.xyzw;
            output.texCoord0 = input.interp3.xyzw;
            output.viewDirectionWS = input.interp4.xyz;
            #if defined(LIGHTMAP_ON)
            output.staticLightmapUV = input.interp5.xy;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
            output.dynamicLightmapUV = input.interp5.zw;
            #endif
            #if !defined(LIGHTMAP_ON)
            output.sh = input.interp6.xyz;
            #endif
            output.fogFactorAndVertexLight = input.interp7.xyzw;
            output.shadowCoord = input.interp8.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }

            // --------------------------------------------------
            // Graph

            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
        float Vector1_2EE2CB80;
        float2 Vector2_C848BFFB;
        float4 Color_A2AAE5B5;
        float4 Color_23C9B10D;
        float4 Texture2D_C69EB180_TexelSize;
        float4 _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1_TexelSize;
        float4 _SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1_TexelSize;
        float4 _SampleTexture2D_aef34a082e463b8992e5f66de0345979_Texture_1_TexelSize;
        float4 _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_Texture_1_TexelSize;
        CBUFFER_END

        // Object and Global properties
        TEXTURE2D(Texture2D_C69EB180);
        SAMPLER(samplerTexture2D_C69EB180);
        TEXTURE2D(_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1);
        SAMPLER(sampler_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1);
        SAMPLER(SamplerState_Linear_Repeat);
        SAMPLER(SamplerState_Linear_Clamp);
        TEXTURE2D(_SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1);
        SAMPLER(sampler_SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1);
        TEXTURE2D(_SampleTexture2D_aef34a082e463b8992e5f66de0345979_Texture_1);
        SAMPLER(sampler_SampleTexture2D_aef34a082e463b8992e5f66de0345979_Texture_1);
        TEXTURE2D(_SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_Texture_1);
        SAMPLER(sampler_SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_Texture_1);

            // Graph Functions

        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }

        void Unity_Multiply_float(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }

        void Unity_Multiply_float(float A, float B, out float Out)
        {
            Out = A * B;
        }

        void Unity_Fraction_float(float In, out float Out)
        {
            Out = frac(In);
        }

        void Unity_Rotate_Degrees_float(float2 UV, float2 Center, float Rotation, out float2 Out)
        {
            //rotation matrix
            Rotation = Rotation * (3.1415926f/180.0f);
            UV -= Center;
            float s = sin(Rotation);
            float c = cos(Rotation);

            //center rotation matrix
            float2x2 rMatrix = float2x2(c, -s, s, c);
            rMatrix *= 0.5;
            rMatrix += 0.5;
            rMatrix = rMatrix*2 - 1;

            //multiply the UVs by the rotation matrix
            UV.xy = mul(UV.xy, rMatrix);
            UV += Center;

            Out = UV;
        }

        void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
        {
            Out = lerp(A, B, T);
        }

        void Unity_OneMinus_float(float In, out float Out)
        {
            Out = 1 - In;
        }

            // Graph Vertex
            struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };

        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }

            // Graph Pixel
            struct SurfaceDescription
        {
            float3 BaseColor;
            float3 NormalTS;
            float3 Emission;
            float Metallic;
            float Smoothness;
            float Occlusion;
        };

        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            float4 _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1).samplerstate, IN.uv0.xy);
            float _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_R_4 = _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0.r;
            float _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_G_5 = _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0.g;
            float _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_B_6 = _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0.b;
            float _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_A_7 = _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0.a;
            UnityTexture2D _Property_416a2417a5f16e86998a65bdfbfe105b_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_C69EB180);
            float2 _TilingAndOffset_5e3ed56d4eb5468ea3ee52c90f3fbe01_Out_3;
            Unity_TilingAndOffset_float(IN.uv0.xy, float2 (8, 32), float2 (-3.11, -6.86), _TilingAndOffset_5e3ed56d4eb5468ea3ee52c90f3fbe01_Out_3);
            float4 _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0 = SAMPLE_TEXTURE2D(_Property_416a2417a5f16e86998a65bdfbfe105b_Out_0.tex, UnityBuildSamplerStateStruct(SamplerState_Linear_Clamp).samplerstate, _TilingAndOffset_5e3ed56d4eb5468ea3ee52c90f3fbe01_Out_3);
            float _SampleTexture2D_53ef635328a52180babf003f88901af1_R_4 = _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0.r;
            float _SampleTexture2D_53ef635328a52180babf003f88901af1_G_5 = _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0.g;
            float _SampleTexture2D_53ef635328a52180babf003f88901af1_B_6 = _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0.b;
            float _SampleTexture2D_53ef635328a52180babf003f88901af1_A_7 = _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0.a;
            float4 _Multiply_098a1a5adc36458f9a9db8a89d29e5ee_Out_2;
            Unity_Multiply_float(_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0, _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0, _Multiply_098a1a5adc36458f9a9db8a89d29e5ee_Out_2);
            float4 _Property_c17c21327159a18ca8b8664a8b068016_Out_0 = Color_A2AAE5B5;
            float4 _Property_411901199da6388abd170ed7f1572505_Out_0 = Color_23C9B10D;
            float2 _Property_cdccdf9bd2b88d8ea1a0586c033ac4c8_Out_0 = Vector2_C848BFFB;
            float _Property_aeae242db5457e849304b95d8b1865e5_Out_0 = Vector1_2EE2CB80;
            float _Multiply_66801fb5ead25582bcac188fd69ff0d5_Out_2;
            Unity_Multiply_float(IN.TimeParameters.x, _Property_aeae242db5457e849304b95d8b1865e5_Out_0, _Multiply_66801fb5ead25582bcac188fd69ff0d5_Out_2);
            float _Fraction_f1b6b975af985484be20b419784bde3c_Out_1;
            Unity_Fraction_float(_Multiply_66801fb5ead25582bcac188fd69ff0d5_Out_2, _Fraction_f1b6b975af985484be20b419784bde3c_Out_1);
            float _Multiply_6598b1de072a41869510b57b1b24cfb5_Out_2;
            Unity_Multiply_float(_Fraction_f1b6b975af985484be20b419784bde3c_Out_1, 360, _Multiply_6598b1de072a41869510b57b1b24cfb5_Out_2);
            float2 _Rotate_6f00503a0a9f8e8ca3540eadb1831c6b_Out_3;
            Unity_Rotate_Degrees_float(IN.uv0.xy, _Property_cdccdf9bd2b88d8ea1a0586c033ac4c8_Out_0, _Multiply_6598b1de072a41869510b57b1b24cfb5_Out_2, _Rotate_6f00503a0a9f8e8ca3540eadb1831c6b_Out_3);
            float2 _TilingAndOffset_53b5cba0214a2c8fa262c98e44a577eb_Out_3;
            Unity_TilingAndOffset_float(_Rotate_6f00503a0a9f8e8ca3540eadb1831c6b_Out_3, float2 (4, 4), float2 (0, 0), _TilingAndOffset_53b5cba0214a2c8fa262c98e44a577eb_Out_3);
            float4 _SampleTexture2D_e60950bba990008885c246fd6823bc78_RGBA_0 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1).samplerstate, _TilingAndOffset_53b5cba0214a2c8fa262c98e44a577eb_Out_3);
            float _SampleTexture2D_e60950bba990008885c246fd6823bc78_R_4 = _SampleTexture2D_e60950bba990008885c246fd6823bc78_RGBA_0.r;
            float _SampleTexture2D_e60950bba990008885c246fd6823bc78_G_5 = _SampleTexture2D_e60950bba990008885c246fd6823bc78_RGBA_0.g;
            float _SampleTexture2D_e60950bba990008885c246fd6823bc78_B_6 = _SampleTexture2D_e60950bba990008885c246fd6823bc78_RGBA_0.b;
            float _SampleTexture2D_e60950bba990008885c246fd6823bc78_A_7 = _SampleTexture2D_e60950bba990008885c246fd6823bc78_RGBA_0.a;
            float4 _Lerp_eb1d105d1ff35a88b4a5066f2def8324_Out_3;
            Unity_Lerp_float4(_Property_c17c21327159a18ca8b8664a8b068016_Out_0, _Property_411901199da6388abd170ed7f1572505_Out_0, (_SampleTexture2D_e60950bba990008885c246fd6823bc78_R_4.xxxx), _Lerp_eb1d105d1ff35a88b4a5066f2def8324_Out_3);
            float4 _Lerp_4e0883bd978e1085b325579ae4bceddf_Out_3;
            Unity_Lerp_float4(_Multiply_098a1a5adc36458f9a9db8a89d29e5ee_Out_2, _Lerp_eb1d105d1ff35a88b4a5066f2def8324_Out_3, (_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_A_7.xxxx), _Lerp_4e0883bd978e1085b325579ae4bceddf_Out_3);
            float4 _SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_SampleTexture2D_aef34a082e463b8992e5f66de0345979_Texture_1).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2D_aef34a082e463b8992e5f66de0345979_Texture_1).samplerstate, IN.uv0.xy);
            _SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0.rgb = UnpackNormal(_SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0);
            float _SampleTexture2D_aef34a082e463b8992e5f66de0345979_R_4 = _SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0.r;
            float _SampleTexture2D_aef34a082e463b8992e5f66de0345979_G_5 = _SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0.g;
            float _SampleTexture2D_aef34a082e463b8992e5f66de0345979_B_6 = _SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0.b;
            float _SampleTexture2D_aef34a082e463b8992e5f66de0345979_A_7 = _SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0.a;
            float4 _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_RGBA_0 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_Texture_1).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_Texture_1).samplerstate, IN.uv0.xy);
            float _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_R_4 = _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_RGBA_0.r;
            float _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_G_5 = _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_RGBA_0.g;
            float _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_B_6 = _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_RGBA_0.b;
            float _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_A_7 = _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_RGBA_0.a;
            float _OneMinus_b677fa8cf63d2d8792ac7886710be407_Out_1;
            Unity_OneMinus_float(_SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_A_7, _OneMinus_b677fa8cf63d2d8792ac7886710be407_Out_1);
            surface.BaseColor = (_Lerp_4e0883bd978e1085b325579ae4bceddf_Out_3.xyz);
            surface.NormalTS = (_SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0.xyz);
            surface.Emission = float3(0, 0, 0);
            surface.Metallic = _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_R_4;
            surface.Smoothness = _OneMinus_b677fa8cf63d2d8792ac7886710be407_Out_1;
            surface.Occlusion = _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_G_5;
            return surface;
        }

            // --------------------------------------------------
            // Build Graph Inputs

            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);

            output.ObjectSpaceNormal =           input.normalOS;
            output.ObjectSpaceTangent =          input.tangentOS;
            output.ObjectSpacePosition =         input.positionOS;

            return output;
        }
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);



            output.TangentSpaceNormal =          float3(0.0f, 0.0f, 1.0f);


            output.uv0 =                         input.texCoord0;
            output.TimeParameters =              _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

            return output;
        }

            // --------------------------------------------------
            // Main

            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRGBufferPass.hlsl"

            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            // Render State
            Cull Back
        Blend One Zero
        ZTest LEqual
        ZWrite On
        ColorMask 0

            // Debug
            // <None>

            // --------------------------------------------------
            // Pass

            HLSLPROGRAM

            // Pragmas
            #pragma target 4.5
        #pragma exclude_renderers gles gles3 glcore
        #pragma multi_compile_instancing
        #pragma multi_compile _ DOTS_INSTANCING_ON
        #pragma vertex vert
        #pragma fragment frag

            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>

            // Keywords
            #pragma multi_compile _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            // GraphKeywords: <None>

            // Defines
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_SHADOWCASTER
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */

            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            // --------------------------------------------------
            // Structs and Packing

            struct Attributes
        {
            float3 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
        };
        struct VertexDescriptionInputs
        {
            float3 ObjectSpaceNormal;
            float3 ObjectSpaceTangent;
            float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
            float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };

            PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            output.positionCS = input.positionCS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }

            // --------------------------------------------------
            // Graph

            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
        float Vector1_2EE2CB80;
        float2 Vector2_C848BFFB;
        float4 Color_A2AAE5B5;
        float4 Color_23C9B10D;
        float4 Texture2D_C69EB180_TexelSize;
        CBUFFER_END

        // Object and Global properties
        TEXTURE2D(Texture2D_C69EB180);
        SAMPLER(samplerTexture2D_C69EB180);

            // Graph Functions
            // GraphFunctions: <None>

            // Graph Vertex
            struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };

        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }

            // Graph Pixel
            struct SurfaceDescription
        {
        };

        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            return surface;
        }

            // --------------------------------------------------
            // Build Graph Inputs

            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);

            output.ObjectSpaceNormal =           input.normalOS;
            output.ObjectSpaceTangent =          input.tangentOS;
            output.ObjectSpacePosition =         input.positionOS;

            return output;
        }
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);





        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

            return output;
        }

            // --------------------------------------------------
            // Main

            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl"

            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            // Render State
            Cull Back
        Blend One Zero
        ZTest LEqual
        ZWrite On
        ColorMask 0

            // Debug
            // <None>

            // --------------------------------------------------
            // Pass

            HLSLPROGRAM

            // Pragmas
            #pragma target 4.5
        #pragma exclude_renderers gles gles3 glcore
        #pragma multi_compile_instancing
        #pragma multi_compile _ DOTS_INSTANCING_ON
        #pragma vertex vert
        #pragma fragment frag

            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>

            // Keywords
            // PassKeywords: <None>
            // GraphKeywords: <None>

            // Defines
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_DEPTHONLY
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */

            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            // --------------------------------------------------
            // Structs and Packing

            struct Attributes
        {
            float3 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
        };
        struct VertexDescriptionInputs
        {
            float3 ObjectSpaceNormal;
            float3 ObjectSpaceTangent;
            float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
            float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };

            PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            output.positionCS = input.positionCS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }

            // --------------------------------------------------
            // Graph

            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
        float Vector1_2EE2CB80;
        float2 Vector2_C848BFFB;
        float4 Color_A2AAE5B5;
        float4 Color_23C9B10D;
        float4 Texture2D_C69EB180_TexelSize;
        CBUFFER_END

        // Object and Global properties
        TEXTURE2D(Texture2D_C69EB180);
        SAMPLER(samplerTexture2D_C69EB180);

            // Graph Functions
            // GraphFunctions: <None>

            // Graph Vertex
            struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };

        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }

            // Graph Pixel
            struct SurfaceDescription
        {
        };

        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            return surface;
        }

            // --------------------------------------------------
            // Build Graph Inputs

            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);

            output.ObjectSpaceNormal =           input.normalOS;
            output.ObjectSpaceTangent =          input.tangentOS;
            output.ObjectSpacePosition =         input.positionOS;

            return output;
        }
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);





        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

            return output;
        }

            // --------------------------------------------------
            // Main

            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl"

            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }

            // Render State
            Cull Back
        Blend One Zero
        ZTest LEqual
        ZWrite On

            // Debug
            // <None>

            // --------------------------------------------------
            // Pass

            HLSLPROGRAM

            // Pragmas
            #pragma target 4.5
        #pragma exclude_renderers gles gles3 glcore
        #pragma multi_compile_instancing
        #pragma multi_compile _ DOTS_INSTANCING_ON
        #pragma vertex vert
        #pragma fragment frag

            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>

            // Keywords
            // PassKeywords: <None>
            // GraphKeywords: <None>

            // Defines
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_TANGENT_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_DEPTHNORMALSONLY
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */

            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            // --------------------------------------------------
            // Structs and Packing

            struct Attributes
        {
            float3 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float4 uv0 : TEXCOORD0;
            float4 uv1 : TEXCOORD1;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 normalWS;
            float4 tangentWS;
            float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
            float3 TangentSpaceNormal;
            float4 uv0;
        };
        struct VertexDescriptionInputs
        {
            float3 ObjectSpaceNormal;
            float3 ObjectSpaceTangent;
            float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
            float4 positionCS : SV_POSITION;
            float3 interp0 : TEXCOORD0;
            float4 interp1 : TEXCOORD1;
            float4 interp2 : TEXCOORD2;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };

            PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            output.positionCS = input.positionCS;
            output.interp0.xyz =  input.normalWS;
            output.interp1.xyzw =  input.tangentWS;
            output.interp2.xyzw =  input.texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.normalWS = input.interp0.xyz;
            output.tangentWS = input.interp1.xyzw;
            output.texCoord0 = input.interp2.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }

            // --------------------------------------------------
            // Graph

            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
        float Vector1_2EE2CB80;
        float2 Vector2_C848BFFB;
        float4 Color_A2AAE5B5;
        float4 Color_23C9B10D;
        float4 Texture2D_C69EB180_TexelSize;
        float4 _SampleTexture2D_aef34a082e463b8992e5f66de0345979_Texture_1_TexelSize;
        CBUFFER_END

        // Object and Global properties
        TEXTURE2D(Texture2D_C69EB180);
        SAMPLER(samplerTexture2D_C69EB180);
        TEXTURE2D(_SampleTexture2D_aef34a082e463b8992e5f66de0345979_Texture_1);
        SAMPLER(sampler_SampleTexture2D_aef34a082e463b8992e5f66de0345979_Texture_1);
        SAMPLER(SamplerState_Linear_Repeat);

            // Graph Functions
            // GraphFunctions: <None>

            // Graph Vertex
            struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };

        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }

            // Graph Pixel
            struct SurfaceDescription
        {
            float3 NormalTS;
        };

        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            float4 _SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_SampleTexture2D_aef34a082e463b8992e5f66de0345979_Texture_1).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2D_aef34a082e463b8992e5f66de0345979_Texture_1).samplerstate, IN.uv0.xy);
            _SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0.rgb = UnpackNormal(_SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0);
            float _SampleTexture2D_aef34a082e463b8992e5f66de0345979_R_4 = _SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0.r;
            float _SampleTexture2D_aef34a082e463b8992e5f66de0345979_G_5 = _SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0.g;
            float _SampleTexture2D_aef34a082e463b8992e5f66de0345979_B_6 = _SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0.b;
            float _SampleTexture2D_aef34a082e463b8992e5f66de0345979_A_7 = _SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0.a;
            surface.NormalTS = (_SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0.xyz);
            return surface;
        }

            // --------------------------------------------------
            // Build Graph Inputs

            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);

            output.ObjectSpaceNormal =           input.normalOS;
            output.ObjectSpaceTangent =          input.tangentOS;
            output.ObjectSpacePosition =         input.positionOS;

            return output;
        }
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);



            output.TangentSpaceNormal =          float3(0.0f, 0.0f, 1.0f);


            output.uv0 =                         input.texCoord0;
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

            return output;
        }

            // --------------------------------------------------
            // Main

            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthNormalsOnlyPass.hlsl"

            ENDHLSL
        }
        Pass
        {
            Name "Meta"
            Tags
            {
                "LightMode" = "Meta"
            }

            // Render State
            Cull Off

            // Debug
            // <None>

            // --------------------------------------------------
            // Pass

            HLSLPROGRAM

            // Pragmas
            #pragma target 4.5
        #pragma exclude_renderers gles gles3 glcore
        #pragma vertex vert
        #pragma fragment frag

            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>

            // Keywords
            #pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            // GraphKeywords: <None>

            // Defines
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define ATTRIBUTES_NEED_TEXCOORD2
            #define VARYINGS_NEED_TEXCOORD0
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_META
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */

            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

            // --------------------------------------------------
            // Structs and Packing

            struct Attributes
        {
            float3 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float4 uv0 : TEXCOORD0;
            float4 uv1 : TEXCOORD1;
            float4 uv2 : TEXCOORD2;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
            float4 uv0;
            float3 TimeParameters;
        };
        struct VertexDescriptionInputs
        {
            float3 ObjectSpaceNormal;
            float3 ObjectSpaceTangent;
            float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
            float4 positionCS : SV_POSITION;
            float4 interp0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };

            PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            output.positionCS = input.positionCS;
            output.interp0.xyzw =  input.texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.interp0.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }

            // --------------------------------------------------
            // Graph

            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
        float Vector1_2EE2CB80;
        float2 Vector2_C848BFFB;
        float4 Color_A2AAE5B5;
        float4 Color_23C9B10D;
        float4 Texture2D_C69EB180_TexelSize;
        float4 _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1_TexelSize;
        float4 _SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1_TexelSize;
        CBUFFER_END

        // Object and Global properties
        TEXTURE2D(Texture2D_C69EB180);
        SAMPLER(samplerTexture2D_C69EB180);
        TEXTURE2D(_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1);
        SAMPLER(sampler_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1);
        SAMPLER(SamplerState_Linear_Repeat);
        SAMPLER(SamplerState_Linear_Clamp);
        TEXTURE2D(_SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1);
        SAMPLER(sampler_SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1);

            // Graph Functions

        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }

        void Unity_Multiply_float(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }

        void Unity_Multiply_float(float A, float B, out float Out)
        {
            Out = A * B;
        }

        void Unity_Fraction_float(float In, out float Out)
        {
            Out = frac(In);
        }

        void Unity_Rotate_Degrees_float(float2 UV, float2 Center, float Rotation, out float2 Out)
        {
            //rotation matrix
            Rotation = Rotation * (3.1415926f/180.0f);
            UV -= Center;
            float s = sin(Rotation);
            float c = cos(Rotation);

            //center rotation matrix
            float2x2 rMatrix = float2x2(c, -s, s, c);
            rMatrix *= 0.5;
            rMatrix += 0.5;
            rMatrix = rMatrix*2 - 1;

            //multiply the UVs by the rotation matrix
            UV.xy = mul(UV.xy, rMatrix);
            UV += Center;

            Out = UV;
        }

        void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
        {
            Out = lerp(A, B, T);
        }

            // Graph Vertex
            struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };

        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }

            // Graph Pixel
            struct SurfaceDescription
        {
            float3 BaseColor;
            float3 Emission;
        };

        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            float4 _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1).samplerstate, IN.uv0.xy);
            float _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_R_4 = _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0.r;
            float _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_G_5 = _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0.g;
            float _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_B_6 = _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0.b;
            float _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_A_7 = _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0.a;
            UnityTexture2D _Property_416a2417a5f16e86998a65bdfbfe105b_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_C69EB180);
            float2 _TilingAndOffset_5e3ed56d4eb5468ea3ee52c90f3fbe01_Out_3;
            Unity_TilingAndOffset_float(IN.uv0.xy, float2 (8, 32), float2 (-3.11, -6.86), _TilingAndOffset_5e3ed56d4eb5468ea3ee52c90f3fbe01_Out_3);
            float4 _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0 = SAMPLE_TEXTURE2D(_Property_416a2417a5f16e86998a65bdfbfe105b_Out_0.tex, UnityBuildSamplerStateStruct(SamplerState_Linear_Clamp).samplerstate, _TilingAndOffset_5e3ed56d4eb5468ea3ee52c90f3fbe01_Out_3);
            float _SampleTexture2D_53ef635328a52180babf003f88901af1_R_4 = _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0.r;
            float _SampleTexture2D_53ef635328a52180babf003f88901af1_G_5 = _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0.g;
            float _SampleTexture2D_53ef635328a52180babf003f88901af1_B_6 = _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0.b;
            float _SampleTexture2D_53ef635328a52180babf003f88901af1_A_7 = _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0.a;
            float4 _Multiply_098a1a5adc36458f9a9db8a89d29e5ee_Out_2;
            Unity_Multiply_float(_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0, _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0, _Multiply_098a1a5adc36458f9a9db8a89d29e5ee_Out_2);
            float4 _Property_c17c21327159a18ca8b8664a8b068016_Out_0 = Color_A2AAE5B5;
            float4 _Property_411901199da6388abd170ed7f1572505_Out_0 = Color_23C9B10D;
            float2 _Property_cdccdf9bd2b88d8ea1a0586c033ac4c8_Out_0 = Vector2_C848BFFB;
            float _Property_aeae242db5457e849304b95d8b1865e5_Out_0 = Vector1_2EE2CB80;
            float _Multiply_66801fb5ead25582bcac188fd69ff0d5_Out_2;
            Unity_Multiply_float(IN.TimeParameters.x, _Property_aeae242db5457e849304b95d8b1865e5_Out_0, _Multiply_66801fb5ead25582bcac188fd69ff0d5_Out_2);
            float _Fraction_f1b6b975af985484be20b419784bde3c_Out_1;
            Unity_Fraction_float(_Multiply_66801fb5ead25582bcac188fd69ff0d5_Out_2, _Fraction_f1b6b975af985484be20b419784bde3c_Out_1);
            float _Multiply_6598b1de072a41869510b57b1b24cfb5_Out_2;
            Unity_Multiply_float(_Fraction_f1b6b975af985484be20b419784bde3c_Out_1, 360, _Multiply_6598b1de072a41869510b57b1b24cfb5_Out_2);
            float2 _Rotate_6f00503a0a9f8e8ca3540eadb1831c6b_Out_3;
            Unity_Rotate_Degrees_float(IN.uv0.xy, _Property_cdccdf9bd2b88d8ea1a0586c033ac4c8_Out_0, _Multiply_6598b1de072a41869510b57b1b24cfb5_Out_2, _Rotate_6f00503a0a9f8e8ca3540eadb1831c6b_Out_3);
            float2 _TilingAndOffset_53b5cba0214a2c8fa262c98e44a577eb_Out_3;
            Unity_TilingAndOffset_float(_Rotate_6f00503a0a9f8e8ca3540eadb1831c6b_Out_3, float2 (4, 4), float2 (0, 0), _TilingAndOffset_53b5cba0214a2c8fa262c98e44a577eb_Out_3);
            float4 _SampleTexture2D_e60950bba990008885c246fd6823bc78_RGBA_0 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1).samplerstate, _TilingAndOffset_53b5cba0214a2c8fa262c98e44a577eb_Out_3);
            float _SampleTexture2D_e60950bba990008885c246fd6823bc78_R_4 = _SampleTexture2D_e60950bba990008885c246fd6823bc78_RGBA_0.r;
            float _SampleTexture2D_e60950bba990008885c246fd6823bc78_G_5 = _SampleTexture2D_e60950bba990008885c246fd6823bc78_RGBA_0.g;
            float _SampleTexture2D_e60950bba990008885c246fd6823bc78_B_6 = _SampleTexture2D_e60950bba990008885c246fd6823bc78_RGBA_0.b;
            float _SampleTexture2D_e60950bba990008885c246fd6823bc78_A_7 = _SampleTexture2D_e60950bba990008885c246fd6823bc78_RGBA_0.a;
            float4 _Lerp_eb1d105d1ff35a88b4a5066f2def8324_Out_3;
            Unity_Lerp_float4(_Property_c17c21327159a18ca8b8664a8b068016_Out_0, _Property_411901199da6388abd170ed7f1572505_Out_0, (_SampleTexture2D_e60950bba990008885c246fd6823bc78_R_4.xxxx), _Lerp_eb1d105d1ff35a88b4a5066f2def8324_Out_3);
            float4 _Lerp_4e0883bd978e1085b325579ae4bceddf_Out_3;
            Unity_Lerp_float4(_Multiply_098a1a5adc36458f9a9db8a89d29e5ee_Out_2, _Lerp_eb1d105d1ff35a88b4a5066f2def8324_Out_3, (_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_A_7.xxxx), _Lerp_4e0883bd978e1085b325579ae4bceddf_Out_3);
            surface.BaseColor = (_Lerp_4e0883bd978e1085b325579ae4bceddf_Out_3.xyz);
            surface.Emission = float3(0, 0, 0);
            return surface;
        }

            // --------------------------------------------------
            // Build Graph Inputs

            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);

            output.ObjectSpaceNormal =           input.normalOS;
            output.ObjectSpaceTangent =          input.tangentOS;
            output.ObjectSpacePosition =         input.positionOS;

            return output;
        }
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);





            output.uv0 =                         input.texCoord0;
            output.TimeParameters =              _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

            return output;
        }

            // --------------------------------------------------
            // Main

            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/LightingMetaPass.hlsl"

            ENDHLSL
        }
        Pass
        {
            // Name: <None>
            Tags
            {
                "LightMode" = "Universal2D"
            }

            // Render State
            Cull Back
        Blend One Zero
        ZTest LEqual
        ZWrite On

            // Debug
            // <None>

            // --------------------------------------------------
            // Pass

            HLSLPROGRAM

            // Pragmas
            #pragma target 4.5
        #pragma exclude_renderers gles gles3 glcore
        #pragma vertex vert
        #pragma fragment frag

            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>

            // Keywords
            // PassKeywords: <None>
            // GraphKeywords: <None>

            // Defines
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define VARYINGS_NEED_TEXCOORD0
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_2D
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */

            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            // --------------------------------------------------
            // Structs and Packing

            struct Attributes
        {
            float3 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float4 uv0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
            float4 uv0;
            float3 TimeParameters;
        };
        struct VertexDescriptionInputs
        {
            float3 ObjectSpaceNormal;
            float3 ObjectSpaceTangent;
            float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
            float4 positionCS : SV_POSITION;
            float4 interp0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };

            PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            output.positionCS = input.positionCS;
            output.interp0.xyzw =  input.texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.interp0.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }

            // --------------------------------------------------
            // Graph

            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
        float Vector1_2EE2CB80;
        float2 Vector2_C848BFFB;
        float4 Color_A2AAE5B5;
        float4 Color_23C9B10D;
        float4 Texture2D_C69EB180_TexelSize;
        float4 _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1_TexelSize;
        float4 _SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1_TexelSize;
        CBUFFER_END

        // Object and Global properties
        TEXTURE2D(Texture2D_C69EB180);
        SAMPLER(samplerTexture2D_C69EB180);
        TEXTURE2D(_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1);
        SAMPLER(sampler_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1);
        SAMPLER(SamplerState_Linear_Repeat);
        SAMPLER(SamplerState_Linear_Clamp);
        TEXTURE2D(_SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1);
        SAMPLER(sampler_SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1);

            // Graph Functions

        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }

        void Unity_Multiply_float(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }

        void Unity_Multiply_float(float A, float B, out float Out)
        {
            Out = A * B;
        }

        void Unity_Fraction_float(float In, out float Out)
        {
            Out = frac(In);
        }

        void Unity_Rotate_Degrees_float(float2 UV, float2 Center, float Rotation, out float2 Out)
        {
            //rotation matrix
            Rotation = Rotation * (3.1415926f/180.0f);
            UV -= Center;
            float s = sin(Rotation);
            float c = cos(Rotation);

            //center rotation matrix
            float2x2 rMatrix = float2x2(c, -s, s, c);
            rMatrix *= 0.5;
            rMatrix += 0.5;
            rMatrix = rMatrix*2 - 1;

            //multiply the UVs by the rotation matrix
            UV.xy = mul(UV.xy, rMatrix);
            UV += Center;

            Out = UV;
        }

        void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
        {
            Out = lerp(A, B, T);
        }

            // Graph Vertex
            struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };

        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }

            // Graph Pixel
            struct SurfaceDescription
        {
            float3 BaseColor;
        };

        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            float4 _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1).samplerstate, IN.uv0.xy);
            float _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_R_4 = _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0.r;
            float _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_G_5 = _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0.g;
            float _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_B_6 = _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0.b;
            float _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_A_7 = _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0.a;
            UnityTexture2D _Property_416a2417a5f16e86998a65bdfbfe105b_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_C69EB180);
            float2 _TilingAndOffset_5e3ed56d4eb5468ea3ee52c90f3fbe01_Out_3;
            Unity_TilingAndOffset_float(IN.uv0.xy, float2 (8, 32), float2 (-3.11, -6.86), _TilingAndOffset_5e3ed56d4eb5468ea3ee52c90f3fbe01_Out_3);
            float4 _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0 = SAMPLE_TEXTURE2D(_Property_416a2417a5f16e86998a65bdfbfe105b_Out_0.tex, UnityBuildSamplerStateStruct(SamplerState_Linear_Clamp).samplerstate, _TilingAndOffset_5e3ed56d4eb5468ea3ee52c90f3fbe01_Out_3);
            float _SampleTexture2D_53ef635328a52180babf003f88901af1_R_4 = _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0.r;
            float _SampleTexture2D_53ef635328a52180babf003f88901af1_G_5 = _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0.g;
            float _SampleTexture2D_53ef635328a52180babf003f88901af1_B_6 = _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0.b;
            float _SampleTexture2D_53ef635328a52180babf003f88901af1_A_7 = _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0.a;
            float4 _Multiply_098a1a5adc36458f9a9db8a89d29e5ee_Out_2;
            Unity_Multiply_float(_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0, _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0, _Multiply_098a1a5adc36458f9a9db8a89d29e5ee_Out_2);
            float4 _Property_c17c21327159a18ca8b8664a8b068016_Out_0 = Color_A2AAE5B5;
            float4 _Property_411901199da6388abd170ed7f1572505_Out_0 = Color_23C9B10D;
            float2 _Property_cdccdf9bd2b88d8ea1a0586c033ac4c8_Out_0 = Vector2_C848BFFB;
            float _Property_aeae242db5457e849304b95d8b1865e5_Out_0 = Vector1_2EE2CB80;
            float _Multiply_66801fb5ead25582bcac188fd69ff0d5_Out_2;
            Unity_Multiply_float(IN.TimeParameters.x, _Property_aeae242db5457e849304b95d8b1865e5_Out_0, _Multiply_66801fb5ead25582bcac188fd69ff0d5_Out_2);
            float _Fraction_f1b6b975af985484be20b419784bde3c_Out_1;
            Unity_Fraction_float(_Multiply_66801fb5ead25582bcac188fd69ff0d5_Out_2, _Fraction_f1b6b975af985484be20b419784bde3c_Out_1);
            float _Multiply_6598b1de072a41869510b57b1b24cfb5_Out_2;
            Unity_Multiply_float(_Fraction_f1b6b975af985484be20b419784bde3c_Out_1, 360, _Multiply_6598b1de072a41869510b57b1b24cfb5_Out_2);
            float2 _Rotate_6f00503a0a9f8e8ca3540eadb1831c6b_Out_3;
            Unity_Rotate_Degrees_float(IN.uv0.xy, _Property_cdccdf9bd2b88d8ea1a0586c033ac4c8_Out_0, _Multiply_6598b1de072a41869510b57b1b24cfb5_Out_2, _Rotate_6f00503a0a9f8e8ca3540eadb1831c6b_Out_3);
            float2 _TilingAndOffset_53b5cba0214a2c8fa262c98e44a577eb_Out_3;
            Unity_TilingAndOffset_float(_Rotate_6f00503a0a9f8e8ca3540eadb1831c6b_Out_3, float2 (4, 4), float2 (0, 0), _TilingAndOffset_53b5cba0214a2c8fa262c98e44a577eb_Out_3);
            float4 _SampleTexture2D_e60950bba990008885c246fd6823bc78_RGBA_0 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1).samplerstate, _TilingAndOffset_53b5cba0214a2c8fa262c98e44a577eb_Out_3);
            float _SampleTexture2D_e60950bba990008885c246fd6823bc78_R_4 = _SampleTexture2D_e60950bba990008885c246fd6823bc78_RGBA_0.r;
            float _SampleTexture2D_e60950bba990008885c246fd6823bc78_G_5 = _SampleTexture2D_e60950bba990008885c246fd6823bc78_RGBA_0.g;
            float _SampleTexture2D_e60950bba990008885c246fd6823bc78_B_6 = _SampleTexture2D_e60950bba990008885c246fd6823bc78_RGBA_0.b;
            float _SampleTexture2D_e60950bba990008885c246fd6823bc78_A_7 = _SampleTexture2D_e60950bba990008885c246fd6823bc78_RGBA_0.a;
            float4 _Lerp_eb1d105d1ff35a88b4a5066f2def8324_Out_3;
            Unity_Lerp_float4(_Property_c17c21327159a18ca8b8664a8b068016_Out_0, _Property_411901199da6388abd170ed7f1572505_Out_0, (_SampleTexture2D_e60950bba990008885c246fd6823bc78_R_4.xxxx), _Lerp_eb1d105d1ff35a88b4a5066f2def8324_Out_3);
            float4 _Lerp_4e0883bd978e1085b325579ae4bceddf_Out_3;
            Unity_Lerp_float4(_Multiply_098a1a5adc36458f9a9db8a89d29e5ee_Out_2, _Lerp_eb1d105d1ff35a88b4a5066f2def8324_Out_3, (_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_A_7.xxxx), _Lerp_4e0883bd978e1085b325579ae4bceddf_Out_3);
            surface.BaseColor = (_Lerp_4e0883bd978e1085b325579ae4bceddf_Out_3.xyz);
            return surface;
        }

            // --------------------------------------------------
            // Build Graph Inputs

            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);

            output.ObjectSpaceNormal =           input.normalOS;
            output.ObjectSpaceTangent =          input.tangentOS;
            output.ObjectSpacePosition =         input.positionOS;

            return output;
        }
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);





            output.uv0 =                         input.texCoord0;
            output.TimeParameters =              _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

            return output;
        }

            // --------------------------------------------------
            // Main

            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBR2DPass.hlsl"

            ENDHLSL
        }
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "UniversalMaterialType" = "Lit"
            "Queue"="Geometry"
        }
        Pass
        {
            Name "Universal Forward"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            // Render State
            Cull Back
        Blend One Zero
        ZTest LEqual
        ZWrite On

            // Debug
            // <None>

            // --------------------------------------------------
            // Pass

            HLSLPROGRAM

            // Pragmas
            #pragma target 2.0
        #pragma only_renderers gles gles3 glcore
        #pragma multi_compile_instancing
        #pragma multi_compile_fog
        #pragma vertex vert
        #pragma fragment frag

            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>

            // Keywords
            #pragma multi_compile _ _SCREEN_SPACE_OCCLUSION
        #pragma multi_compile _ LIGHTMAP_ON
        #pragma multi_compile _ DYNAMICLIGHTMAP_ON
        #pragma multi_compile _ DIRLIGHTMAP_COMBINED
        #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
        #pragma multi_compile _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS _ADDITIONAL_OFF
        #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
        #pragma multi_compile _ _SHADOWS_SOFT
        #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
        #pragma multi_compile _ SHADOWS_SHADOWMASK
            // GraphKeywords: <None>

            // Defines
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define ATTRIBUTES_NEED_TEXCOORD2
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_TANGENT_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_VIEWDIRECTION_WS
            #define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_FORWARD
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */

            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            // --------------------------------------------------
            // Structs and Packing

            struct Attributes
        {
            float3 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float4 uv0 : TEXCOORD0;
            float4 uv1 : TEXCOORD1;
            float4 uv2 : TEXCOORD2;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS;
            float3 normalWS;
            float4 tangentWS;
            float4 texCoord0;
            float3 viewDirectionWS;
            #if defined(LIGHTMAP_ON)
            float2 staticLightmapUV;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
            float2 dynamicLightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
            float3 sh;
            #endif
            float4 fogFactorAndVertexLight;
            float4 shadowCoord;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
            float3 TangentSpaceNormal;
            float4 uv0;
            float3 TimeParameters;
        };
        struct VertexDescriptionInputs
        {
            float3 ObjectSpaceNormal;
            float3 ObjectSpaceTangent;
            float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
            float4 positionCS : SV_POSITION;
            float3 interp0 : TEXCOORD0;
            float3 interp1 : TEXCOORD1;
            float4 interp2 : TEXCOORD2;
            float4 interp3 : TEXCOORD3;
            float3 interp4 : TEXCOORD4;
            #if defined(LIGHTMAP_ON)
            float2 interp5 : TEXCOORD5;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
            float2 interp5 : TEXCOORD5;
            #endif
            #if !defined(LIGHTMAP_ON)
            float3 interp6 : TEXCOORD6;
            #endif
            float4 interp7 : TEXCOORD7;
            float4 interp8 : TEXCOORD8;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };

            PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            output.positionCS = input.positionCS;
            output.interp0.xyz =  input.positionWS;
            output.interp1.xyz =  input.normalWS;
            output.interp2.xyzw =  input.tangentWS;
            output.interp3.xyzw =  input.texCoord0;
            output.interp4.xyz =  input.viewDirectionWS;
            #if defined(LIGHTMAP_ON)
            output.interp5.xy =  input.staticLightmapUV;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
            output.interp5.zw =  input.dynamicLightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
            output.interp6.xyz =  input.sh;
            #endif
            output.interp7.xyzw =  input.fogFactorAndVertexLight;
            output.interp8.xyzw =  input.shadowCoord;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.positionWS = input.interp0.xyz;
            output.normalWS = input.interp1.xyz;
            output.tangentWS = input.interp2.xyzw;
            output.texCoord0 = input.interp3.xyzw;
            output.viewDirectionWS = input.interp4.xyz;
            #if defined(LIGHTMAP_ON)
            output.staticLightmapUV = input.interp5.xy;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
            output.dynamicLightmapUV = input.interp5.zw;
            #endif
            #if !defined(LIGHTMAP_ON)
            output.sh = input.interp6.xyz;
            #endif
            output.fogFactorAndVertexLight = input.interp7.xyzw;
            output.shadowCoord = input.interp8.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }

            // --------------------------------------------------
            // Graph

            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
        float Vector1_2EE2CB80;
        float2 Vector2_C848BFFB;
        float4 Color_A2AAE5B5;
        float4 Color_23C9B10D;
        float4 Texture2D_C69EB180_TexelSize;
        float4 _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1_TexelSize;
        float4 _SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1_TexelSize;
        float4 _SampleTexture2D_aef34a082e463b8992e5f66de0345979_Texture_1_TexelSize;
        float4 _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_Texture_1_TexelSize;
        CBUFFER_END

        // Object and Global properties
        TEXTURE2D(Texture2D_C69EB180);
        SAMPLER(samplerTexture2D_C69EB180);
        TEXTURE2D(_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1);
        SAMPLER(sampler_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1);
        SAMPLER(SamplerState_Linear_Repeat);
        SAMPLER(SamplerState_Linear_Clamp);
        TEXTURE2D(_SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1);
        SAMPLER(sampler_SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1);
        TEXTURE2D(_SampleTexture2D_aef34a082e463b8992e5f66de0345979_Texture_1);
        SAMPLER(sampler_SampleTexture2D_aef34a082e463b8992e5f66de0345979_Texture_1);
        TEXTURE2D(_SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_Texture_1);
        SAMPLER(sampler_SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_Texture_1);

            // Graph Functions

        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }

        void Unity_Multiply_float(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }

        void Unity_Multiply_float(float A, float B, out float Out)
        {
            Out = A * B;
        }

        void Unity_Fraction_float(float In, out float Out)
        {
            Out = frac(In);
        }

        void Unity_Rotate_Degrees_float(float2 UV, float2 Center, float Rotation, out float2 Out)
        {
            //rotation matrix
            Rotation = Rotation * (3.1415926f/180.0f);
            UV -= Center;
            float s = sin(Rotation);
            float c = cos(Rotation);

            //center rotation matrix
            float2x2 rMatrix = float2x2(c, -s, s, c);
            rMatrix *= 0.5;
            rMatrix += 0.5;
            rMatrix = rMatrix*2 - 1;

            //multiply the UVs by the rotation matrix
            UV.xy = mul(UV.xy, rMatrix);
            UV += Center;

            Out = UV;
        }

        void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
        {
            Out = lerp(A, B, T);
        }

        void Unity_OneMinus_float(float In, out float Out)
        {
            Out = 1 - In;
        }

            // Graph Vertex
            struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };

        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }

            // Graph Pixel
            struct SurfaceDescription
        {
            float3 BaseColor;
            float3 NormalTS;
            float3 Emission;
            float Metallic;
            float Smoothness;
            float Occlusion;
        };

        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            float4 _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1).samplerstate, IN.uv0.xy);
            float _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_R_4 = _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0.r;
            float _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_G_5 = _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0.g;
            float _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_B_6 = _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0.b;
            float _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_A_7 = _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0.a;
            UnityTexture2D _Property_416a2417a5f16e86998a65bdfbfe105b_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_C69EB180);
            float2 _TilingAndOffset_5e3ed56d4eb5468ea3ee52c90f3fbe01_Out_3;
            Unity_TilingAndOffset_float(IN.uv0.xy, float2 (8, 32), float2 (-3.11, -6.86), _TilingAndOffset_5e3ed56d4eb5468ea3ee52c90f3fbe01_Out_3);
            float4 _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0 = SAMPLE_TEXTURE2D(_Property_416a2417a5f16e86998a65bdfbfe105b_Out_0.tex, UnityBuildSamplerStateStruct(SamplerState_Linear_Clamp).samplerstate, _TilingAndOffset_5e3ed56d4eb5468ea3ee52c90f3fbe01_Out_3);
            float _SampleTexture2D_53ef635328a52180babf003f88901af1_R_4 = _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0.r;
            float _SampleTexture2D_53ef635328a52180babf003f88901af1_G_5 = _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0.g;
            float _SampleTexture2D_53ef635328a52180babf003f88901af1_B_6 = _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0.b;
            float _SampleTexture2D_53ef635328a52180babf003f88901af1_A_7 = _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0.a;
            float4 _Multiply_098a1a5adc36458f9a9db8a89d29e5ee_Out_2;
            Unity_Multiply_float(_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0, _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0, _Multiply_098a1a5adc36458f9a9db8a89d29e5ee_Out_2);
            float4 _Property_c17c21327159a18ca8b8664a8b068016_Out_0 = Color_A2AAE5B5;
            float4 _Property_411901199da6388abd170ed7f1572505_Out_0 = Color_23C9B10D;
            float2 _Property_cdccdf9bd2b88d8ea1a0586c033ac4c8_Out_0 = Vector2_C848BFFB;
            float _Property_aeae242db5457e849304b95d8b1865e5_Out_0 = Vector1_2EE2CB80;
            float _Multiply_66801fb5ead25582bcac188fd69ff0d5_Out_2;
            Unity_Multiply_float(IN.TimeParameters.x, _Property_aeae242db5457e849304b95d8b1865e5_Out_0, _Multiply_66801fb5ead25582bcac188fd69ff0d5_Out_2);
            float _Fraction_f1b6b975af985484be20b419784bde3c_Out_1;
            Unity_Fraction_float(_Multiply_66801fb5ead25582bcac188fd69ff0d5_Out_2, _Fraction_f1b6b975af985484be20b419784bde3c_Out_1);
            float _Multiply_6598b1de072a41869510b57b1b24cfb5_Out_2;
            Unity_Multiply_float(_Fraction_f1b6b975af985484be20b419784bde3c_Out_1, 360, _Multiply_6598b1de072a41869510b57b1b24cfb5_Out_2);
            float2 _Rotate_6f00503a0a9f8e8ca3540eadb1831c6b_Out_3;
            Unity_Rotate_Degrees_float(IN.uv0.xy, _Property_cdccdf9bd2b88d8ea1a0586c033ac4c8_Out_0, _Multiply_6598b1de072a41869510b57b1b24cfb5_Out_2, _Rotate_6f00503a0a9f8e8ca3540eadb1831c6b_Out_3);
            float2 _TilingAndOffset_53b5cba0214a2c8fa262c98e44a577eb_Out_3;
            Unity_TilingAndOffset_float(_Rotate_6f00503a0a9f8e8ca3540eadb1831c6b_Out_3, float2 (4, 4), float2 (0, 0), _TilingAndOffset_53b5cba0214a2c8fa262c98e44a577eb_Out_3);
            float4 _SampleTexture2D_e60950bba990008885c246fd6823bc78_RGBA_0 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1).samplerstate, _TilingAndOffset_53b5cba0214a2c8fa262c98e44a577eb_Out_3);
            float _SampleTexture2D_e60950bba990008885c246fd6823bc78_R_4 = _SampleTexture2D_e60950bba990008885c246fd6823bc78_RGBA_0.r;
            float _SampleTexture2D_e60950bba990008885c246fd6823bc78_G_5 = _SampleTexture2D_e60950bba990008885c246fd6823bc78_RGBA_0.g;
            float _SampleTexture2D_e60950bba990008885c246fd6823bc78_B_6 = _SampleTexture2D_e60950bba990008885c246fd6823bc78_RGBA_0.b;
            float _SampleTexture2D_e60950bba990008885c246fd6823bc78_A_7 = _SampleTexture2D_e60950bba990008885c246fd6823bc78_RGBA_0.a;
            float4 _Lerp_eb1d105d1ff35a88b4a5066f2def8324_Out_3;
            Unity_Lerp_float4(_Property_c17c21327159a18ca8b8664a8b068016_Out_0, _Property_411901199da6388abd170ed7f1572505_Out_0, (_SampleTexture2D_e60950bba990008885c246fd6823bc78_R_4.xxxx), _Lerp_eb1d105d1ff35a88b4a5066f2def8324_Out_3);
            float4 _Lerp_4e0883bd978e1085b325579ae4bceddf_Out_3;
            Unity_Lerp_float4(_Multiply_098a1a5adc36458f9a9db8a89d29e5ee_Out_2, _Lerp_eb1d105d1ff35a88b4a5066f2def8324_Out_3, (_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_A_7.xxxx), _Lerp_4e0883bd978e1085b325579ae4bceddf_Out_3);
            float4 _SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_SampleTexture2D_aef34a082e463b8992e5f66de0345979_Texture_1).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2D_aef34a082e463b8992e5f66de0345979_Texture_1).samplerstate, IN.uv0.xy);
            _SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0.rgb = UnpackNormal(_SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0);
            float _SampleTexture2D_aef34a082e463b8992e5f66de0345979_R_4 = _SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0.r;
            float _SampleTexture2D_aef34a082e463b8992e5f66de0345979_G_5 = _SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0.g;
            float _SampleTexture2D_aef34a082e463b8992e5f66de0345979_B_6 = _SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0.b;
            float _SampleTexture2D_aef34a082e463b8992e5f66de0345979_A_7 = _SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0.a;
            float4 _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_RGBA_0 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_Texture_1).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_Texture_1).samplerstate, IN.uv0.xy);
            float _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_R_4 = _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_RGBA_0.r;
            float _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_G_5 = _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_RGBA_0.g;
            float _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_B_6 = _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_RGBA_0.b;
            float _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_A_7 = _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_RGBA_0.a;
            float _OneMinus_b677fa8cf63d2d8792ac7886710be407_Out_1;
            Unity_OneMinus_float(_SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_A_7, _OneMinus_b677fa8cf63d2d8792ac7886710be407_Out_1);
            surface.BaseColor = (_Lerp_4e0883bd978e1085b325579ae4bceddf_Out_3.xyz);
            surface.NormalTS = (_SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0.xyz);
            surface.Emission = float3(0, 0, 0);
            surface.Metallic = _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_R_4;
            surface.Smoothness = _OneMinus_b677fa8cf63d2d8792ac7886710be407_Out_1;
            surface.Occlusion = _SampleTexture2D_ae061cbc8301d686a4cbfc6826975296_G_5;
            return surface;
        }

            // --------------------------------------------------
            // Build Graph Inputs

            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);

            output.ObjectSpaceNormal =           input.normalOS;
            output.ObjectSpaceTangent =          input.tangentOS;
            output.ObjectSpacePosition =         input.positionOS;

            return output;
        }
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);



            output.TangentSpaceNormal =          float3(0.0f, 0.0f, 1.0f);


            output.uv0 =                         input.texCoord0;
            output.TimeParameters =              _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

            return output;
        }

            // --------------------------------------------------
            // Main

            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRForwardPass.hlsl"

            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            // Render State
            Cull Back
        Blend One Zero
        ZTest LEqual
        ZWrite On
        ColorMask 0

            // Debug
            // <None>

            // --------------------------------------------------
            // Pass

            HLSLPROGRAM

            // Pragmas
            #pragma target 2.0
        #pragma only_renderers gles gles3 glcore
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag

            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>

            // Keywords
            #pragma multi_compile _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            // GraphKeywords: <None>

            // Defines
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_SHADOWCASTER
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */

            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            // --------------------------------------------------
            // Structs and Packing

            struct Attributes
        {
            float3 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
        };
        struct VertexDescriptionInputs
        {
            float3 ObjectSpaceNormal;
            float3 ObjectSpaceTangent;
            float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
            float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };

            PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            output.positionCS = input.positionCS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }

            // --------------------------------------------------
            // Graph

            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
        float Vector1_2EE2CB80;
        float2 Vector2_C848BFFB;
        float4 Color_A2AAE5B5;
        float4 Color_23C9B10D;
        float4 Texture2D_C69EB180_TexelSize;
        CBUFFER_END

        // Object and Global properties
        TEXTURE2D(Texture2D_C69EB180);
        SAMPLER(samplerTexture2D_C69EB180);

            // Graph Functions
            // GraphFunctions: <None>

            // Graph Vertex
            struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };

        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }

            // Graph Pixel
            struct SurfaceDescription
        {
        };

        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            return surface;
        }

            // --------------------------------------------------
            // Build Graph Inputs

            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);

            output.ObjectSpaceNormal =           input.normalOS;
            output.ObjectSpaceTangent =          input.tangentOS;
            output.ObjectSpacePosition =         input.positionOS;

            return output;
        }
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);





        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

            return output;
        }

            // --------------------------------------------------
            // Main

            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl"

            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            // Render State
            Cull Back
        Blend One Zero
        ZTest LEqual
        ZWrite On
        ColorMask 0

            // Debug
            // <None>

            // --------------------------------------------------
            // Pass

            HLSLPROGRAM

            // Pragmas
            #pragma target 2.0
        #pragma only_renderers gles gles3 glcore
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag

            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>

            // Keywords
            // PassKeywords: <None>
            // GraphKeywords: <None>

            // Defines
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_DEPTHONLY
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */

            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            // --------------------------------------------------
            // Structs and Packing

            struct Attributes
        {
            float3 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
        };
        struct VertexDescriptionInputs
        {
            float3 ObjectSpaceNormal;
            float3 ObjectSpaceTangent;
            float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
            float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };

            PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            output.positionCS = input.positionCS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }

            // --------------------------------------------------
            // Graph

            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
        float Vector1_2EE2CB80;
        float2 Vector2_C848BFFB;
        float4 Color_A2AAE5B5;
        float4 Color_23C9B10D;
        float4 Texture2D_C69EB180_TexelSize;
        CBUFFER_END

        // Object and Global properties
        TEXTURE2D(Texture2D_C69EB180);
        SAMPLER(samplerTexture2D_C69EB180);

            // Graph Functions
            // GraphFunctions: <None>

            // Graph Vertex
            struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };

        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }

            // Graph Pixel
            struct SurfaceDescription
        {
        };

        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            return surface;
        }

            // --------------------------------------------------
            // Build Graph Inputs

            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);

            output.ObjectSpaceNormal =           input.normalOS;
            output.ObjectSpaceTangent =          input.tangentOS;
            output.ObjectSpacePosition =         input.positionOS;

            return output;
        }
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);





        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

            return output;
        }

            // --------------------------------------------------
            // Main

            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl"

            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }

            // Render State
            Cull Back
        Blend One Zero
        ZTest LEqual
        ZWrite On

            // Debug
            // <None>

            // --------------------------------------------------
            // Pass

            HLSLPROGRAM

            // Pragmas
            #pragma target 2.0
        #pragma only_renderers gles gles3 glcore
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag

            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>

            // Keywords
            // PassKeywords: <None>
            // GraphKeywords: <None>

            // Defines
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_TANGENT_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_DEPTHNORMALSONLY
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */

            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            // --------------------------------------------------
            // Structs and Packing

            struct Attributes
        {
            float3 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float4 uv0 : TEXCOORD0;
            float4 uv1 : TEXCOORD1;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 normalWS;
            float4 tangentWS;
            float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
            float3 TangentSpaceNormal;
            float4 uv0;
        };
        struct VertexDescriptionInputs
        {
            float3 ObjectSpaceNormal;
            float3 ObjectSpaceTangent;
            float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
            float4 positionCS : SV_POSITION;
            float3 interp0 : TEXCOORD0;
            float4 interp1 : TEXCOORD1;
            float4 interp2 : TEXCOORD2;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };

            PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            output.positionCS = input.positionCS;
            output.interp0.xyz =  input.normalWS;
            output.interp1.xyzw =  input.tangentWS;
            output.interp2.xyzw =  input.texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.normalWS = input.interp0.xyz;
            output.tangentWS = input.interp1.xyzw;
            output.texCoord0 = input.interp2.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }

            // --------------------------------------------------
            // Graph

            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
        float Vector1_2EE2CB80;
        float2 Vector2_C848BFFB;
        float4 Color_A2AAE5B5;
        float4 Color_23C9B10D;
        float4 Texture2D_C69EB180_TexelSize;
        float4 _SampleTexture2D_aef34a082e463b8992e5f66de0345979_Texture_1_TexelSize;
        CBUFFER_END

        // Object and Global properties
        TEXTURE2D(Texture2D_C69EB180);
        SAMPLER(samplerTexture2D_C69EB180);
        TEXTURE2D(_SampleTexture2D_aef34a082e463b8992e5f66de0345979_Texture_1);
        SAMPLER(sampler_SampleTexture2D_aef34a082e463b8992e5f66de0345979_Texture_1);
        SAMPLER(SamplerState_Linear_Repeat);

            // Graph Functions
            // GraphFunctions: <None>

            // Graph Vertex
            struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };

        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }

            // Graph Pixel
            struct SurfaceDescription
        {
            float3 NormalTS;
        };

        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            float4 _SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_SampleTexture2D_aef34a082e463b8992e5f66de0345979_Texture_1).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2D_aef34a082e463b8992e5f66de0345979_Texture_1).samplerstate, IN.uv0.xy);
            _SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0.rgb = UnpackNormal(_SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0);
            float _SampleTexture2D_aef34a082e463b8992e5f66de0345979_R_4 = _SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0.r;
            float _SampleTexture2D_aef34a082e463b8992e5f66de0345979_G_5 = _SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0.g;
            float _SampleTexture2D_aef34a082e463b8992e5f66de0345979_B_6 = _SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0.b;
            float _SampleTexture2D_aef34a082e463b8992e5f66de0345979_A_7 = _SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0.a;
            surface.NormalTS = (_SampleTexture2D_aef34a082e463b8992e5f66de0345979_RGBA_0.xyz);
            return surface;
        }

            // --------------------------------------------------
            // Build Graph Inputs

            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);

            output.ObjectSpaceNormal =           input.normalOS;
            output.ObjectSpaceTangent =          input.tangentOS;
            output.ObjectSpacePosition =         input.positionOS;

            return output;
        }
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);



            output.TangentSpaceNormal =          float3(0.0f, 0.0f, 1.0f);


            output.uv0 =                         input.texCoord0;
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

            return output;
        }

            // --------------------------------------------------
            // Main

            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthNormalsOnlyPass.hlsl"

            ENDHLSL
        }
        Pass
        {
            Name "Meta"
            Tags
            {
                "LightMode" = "Meta"
            }

            // Render State
            Cull Off

            // Debug
            // <None>

            // --------------------------------------------------
            // Pass

            HLSLPROGRAM

            // Pragmas
            #pragma target 2.0
        #pragma only_renderers gles gles3 glcore
        #pragma vertex vert
        #pragma fragment frag

            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>

            // Keywords
            #pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            // GraphKeywords: <None>

            // Defines
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define ATTRIBUTES_NEED_TEXCOORD2
            #define VARYINGS_NEED_TEXCOORD0
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_META
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */

            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

            // --------------------------------------------------
            // Structs and Packing

            struct Attributes
        {
            float3 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float4 uv0 : TEXCOORD0;
            float4 uv1 : TEXCOORD1;
            float4 uv2 : TEXCOORD2;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
            float4 uv0;
            float3 TimeParameters;
        };
        struct VertexDescriptionInputs
        {
            float3 ObjectSpaceNormal;
            float3 ObjectSpaceTangent;
            float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
            float4 positionCS : SV_POSITION;
            float4 interp0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };

            PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            output.positionCS = input.positionCS;
            output.interp0.xyzw =  input.texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.interp0.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }

            // --------------------------------------------------
            // Graph

            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
        float Vector1_2EE2CB80;
        float2 Vector2_C848BFFB;
        float4 Color_A2AAE5B5;
        float4 Color_23C9B10D;
        float4 Texture2D_C69EB180_TexelSize;
        float4 _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1_TexelSize;
        float4 _SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1_TexelSize;
        CBUFFER_END

        // Object and Global properties
        TEXTURE2D(Texture2D_C69EB180);
        SAMPLER(samplerTexture2D_C69EB180);
        TEXTURE2D(_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1);
        SAMPLER(sampler_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1);
        SAMPLER(SamplerState_Linear_Repeat);
        SAMPLER(SamplerState_Linear_Clamp);
        TEXTURE2D(_SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1);
        SAMPLER(sampler_SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1);

            // Graph Functions

        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }

        void Unity_Multiply_float(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }

        void Unity_Multiply_float(float A, float B, out float Out)
        {
            Out = A * B;
        }

        void Unity_Fraction_float(float In, out float Out)
        {
            Out = frac(In);
        }

        void Unity_Rotate_Degrees_float(float2 UV, float2 Center, float Rotation, out float2 Out)
        {
            //rotation matrix
            Rotation = Rotation * (3.1415926f/180.0f);
            UV -= Center;
            float s = sin(Rotation);
            float c = cos(Rotation);

            //center rotation matrix
            float2x2 rMatrix = float2x2(c, -s, s, c);
            rMatrix *= 0.5;
            rMatrix += 0.5;
            rMatrix = rMatrix*2 - 1;

            //multiply the UVs by the rotation matrix
            UV.xy = mul(UV.xy, rMatrix);
            UV += Center;

            Out = UV;
        }

        void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
        {
            Out = lerp(A, B, T);
        }

            // Graph Vertex
            struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };

        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }

            // Graph Pixel
            struct SurfaceDescription
        {
            float3 BaseColor;
            float3 Emission;
        };

        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            float4 _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1).samplerstate, IN.uv0.xy);
            float _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_R_4 = _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0.r;
            float _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_G_5 = _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0.g;
            float _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_B_6 = _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0.b;
            float _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_A_7 = _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0.a;
            UnityTexture2D _Property_416a2417a5f16e86998a65bdfbfe105b_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_C69EB180);
            float2 _TilingAndOffset_5e3ed56d4eb5468ea3ee52c90f3fbe01_Out_3;
            Unity_TilingAndOffset_float(IN.uv0.xy, float2 (8, 32), float2 (-3.11, -6.86), _TilingAndOffset_5e3ed56d4eb5468ea3ee52c90f3fbe01_Out_3);
            float4 _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0 = SAMPLE_TEXTURE2D(_Property_416a2417a5f16e86998a65bdfbfe105b_Out_0.tex, UnityBuildSamplerStateStruct(SamplerState_Linear_Clamp).samplerstate, _TilingAndOffset_5e3ed56d4eb5468ea3ee52c90f3fbe01_Out_3);
            float _SampleTexture2D_53ef635328a52180babf003f88901af1_R_4 = _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0.r;
            float _SampleTexture2D_53ef635328a52180babf003f88901af1_G_5 = _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0.g;
            float _SampleTexture2D_53ef635328a52180babf003f88901af1_B_6 = _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0.b;
            float _SampleTexture2D_53ef635328a52180babf003f88901af1_A_7 = _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0.a;
            float4 _Multiply_098a1a5adc36458f9a9db8a89d29e5ee_Out_2;
            Unity_Multiply_float(_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0, _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0, _Multiply_098a1a5adc36458f9a9db8a89d29e5ee_Out_2);
            float4 _Property_c17c21327159a18ca8b8664a8b068016_Out_0 = Color_A2AAE5B5;
            float4 _Property_411901199da6388abd170ed7f1572505_Out_0 = Color_23C9B10D;
            float2 _Property_cdccdf9bd2b88d8ea1a0586c033ac4c8_Out_0 = Vector2_C848BFFB;
            float _Property_aeae242db5457e849304b95d8b1865e5_Out_0 = Vector1_2EE2CB80;
            float _Multiply_66801fb5ead25582bcac188fd69ff0d5_Out_2;
            Unity_Multiply_float(IN.TimeParameters.x, _Property_aeae242db5457e849304b95d8b1865e5_Out_0, _Multiply_66801fb5ead25582bcac188fd69ff0d5_Out_2);
            float _Fraction_f1b6b975af985484be20b419784bde3c_Out_1;
            Unity_Fraction_float(_Multiply_66801fb5ead25582bcac188fd69ff0d5_Out_2, _Fraction_f1b6b975af985484be20b419784bde3c_Out_1);
            float _Multiply_6598b1de072a41869510b57b1b24cfb5_Out_2;
            Unity_Multiply_float(_Fraction_f1b6b975af985484be20b419784bde3c_Out_1, 360, _Multiply_6598b1de072a41869510b57b1b24cfb5_Out_2);
            float2 _Rotate_6f00503a0a9f8e8ca3540eadb1831c6b_Out_3;
            Unity_Rotate_Degrees_float(IN.uv0.xy, _Property_cdccdf9bd2b88d8ea1a0586c033ac4c8_Out_0, _Multiply_6598b1de072a41869510b57b1b24cfb5_Out_2, _Rotate_6f00503a0a9f8e8ca3540eadb1831c6b_Out_3);
            float2 _TilingAndOffset_53b5cba0214a2c8fa262c98e44a577eb_Out_3;
            Unity_TilingAndOffset_float(_Rotate_6f00503a0a9f8e8ca3540eadb1831c6b_Out_3, float2 (4, 4), float2 (0, 0), _TilingAndOffset_53b5cba0214a2c8fa262c98e44a577eb_Out_3);
            float4 _SampleTexture2D_e60950bba990008885c246fd6823bc78_RGBA_0 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1).samplerstate, _TilingAndOffset_53b5cba0214a2c8fa262c98e44a577eb_Out_3);
            float _SampleTexture2D_e60950bba990008885c246fd6823bc78_R_4 = _SampleTexture2D_e60950bba990008885c246fd6823bc78_RGBA_0.r;
            float _SampleTexture2D_e60950bba990008885c246fd6823bc78_G_5 = _SampleTexture2D_e60950bba990008885c246fd6823bc78_RGBA_0.g;
            float _SampleTexture2D_e60950bba990008885c246fd6823bc78_B_6 = _SampleTexture2D_e60950bba990008885c246fd6823bc78_RGBA_0.b;
            float _SampleTexture2D_e60950bba990008885c246fd6823bc78_A_7 = _SampleTexture2D_e60950bba990008885c246fd6823bc78_RGBA_0.a;
            float4 _Lerp_eb1d105d1ff35a88b4a5066f2def8324_Out_3;
            Unity_Lerp_float4(_Property_c17c21327159a18ca8b8664a8b068016_Out_0, _Property_411901199da6388abd170ed7f1572505_Out_0, (_SampleTexture2D_e60950bba990008885c246fd6823bc78_R_4.xxxx), _Lerp_eb1d105d1ff35a88b4a5066f2def8324_Out_3);
            float4 _Lerp_4e0883bd978e1085b325579ae4bceddf_Out_3;
            Unity_Lerp_float4(_Multiply_098a1a5adc36458f9a9db8a89d29e5ee_Out_2, _Lerp_eb1d105d1ff35a88b4a5066f2def8324_Out_3, (_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_A_7.xxxx), _Lerp_4e0883bd978e1085b325579ae4bceddf_Out_3);
            surface.BaseColor = (_Lerp_4e0883bd978e1085b325579ae4bceddf_Out_3.xyz);
            surface.Emission = float3(0, 0, 0);
            return surface;
        }

            // --------------------------------------------------
            // Build Graph Inputs

            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);

            output.ObjectSpaceNormal =           input.normalOS;
            output.ObjectSpaceTangent =          input.tangentOS;
            output.ObjectSpacePosition =         input.positionOS;

            return output;
        }
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);





            output.uv0 =                         input.texCoord0;
            output.TimeParameters =              _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

            return output;
        }

            // --------------------------------------------------
            // Main

            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/LightingMetaPass.hlsl"

            ENDHLSL
        }
        Pass
        {
            // Name: <None>
            Tags
            {
                "LightMode" = "Universal2D"
            }

            // Render State
            Cull Back
        Blend One Zero
        ZTest LEqual
        ZWrite On

            // Debug
            // <None>

            // --------------------------------------------------
            // Pass

            HLSLPROGRAM

            // Pragmas
            #pragma target 2.0
        #pragma only_renderers gles gles3 glcore
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag

            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>

            // Keywords
            // PassKeywords: <None>
            // GraphKeywords: <None>

            // Defines
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define VARYINGS_NEED_TEXCOORD0
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_2D
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */

            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            // --------------------------------------------------
            // Structs and Packing

            struct Attributes
        {
            float3 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float4 uv0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
            float4 uv0;
            float3 TimeParameters;
        };
        struct VertexDescriptionInputs
        {
            float3 ObjectSpaceNormal;
            float3 ObjectSpaceTangent;
            float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
            float4 positionCS : SV_POSITION;
            float4 interp0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };

            PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            output.positionCS = input.positionCS;
            output.interp0.xyzw =  input.texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.interp0.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }

            // --------------------------------------------------
            // Graph

            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
        float Vector1_2EE2CB80;
        float2 Vector2_C848BFFB;
        float4 Color_A2AAE5B5;
        float4 Color_23C9B10D;
        float4 Texture2D_C69EB180_TexelSize;
        float4 _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1_TexelSize;
        float4 _SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1_TexelSize;
        CBUFFER_END

        // Object and Global properties
        TEXTURE2D(Texture2D_C69EB180);
        SAMPLER(samplerTexture2D_C69EB180);
        TEXTURE2D(_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1);
        SAMPLER(sampler_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1);
        SAMPLER(SamplerState_Linear_Repeat);
        SAMPLER(SamplerState_Linear_Clamp);
        TEXTURE2D(_SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1);
        SAMPLER(sampler_SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1);

            // Graph Functions

        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }

        void Unity_Multiply_float(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }

        void Unity_Multiply_float(float A, float B, out float Out)
        {
            Out = A * B;
        }

        void Unity_Fraction_float(float In, out float Out)
        {
            Out = frac(In);
        }

        void Unity_Rotate_Degrees_float(float2 UV, float2 Center, float Rotation, out float2 Out)
        {
            //rotation matrix
            Rotation = Rotation * (3.1415926f/180.0f);
            UV -= Center;
            float s = sin(Rotation);
            float c = cos(Rotation);

            //center rotation matrix
            float2x2 rMatrix = float2x2(c, -s, s, c);
            rMatrix *= 0.5;
            rMatrix += 0.5;
            rMatrix = rMatrix*2 - 1;

            //multiply the UVs by the rotation matrix
            UV.xy = mul(UV.xy, rMatrix);
            UV += Center;

            Out = UV;
        }

        void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
        {
            Out = lerp(A, B, T);
        }

            // Graph Vertex
            struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };

        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }

            // Graph Pixel
            struct SurfaceDescription
        {
            float3 BaseColor;
        };

        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            float4 _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_Texture_1).samplerstate, IN.uv0.xy);
            float _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_R_4 = _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0.r;
            float _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_G_5 = _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0.g;
            float _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_B_6 = _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0.b;
            float _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_A_7 = _SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0.a;
            UnityTexture2D _Property_416a2417a5f16e86998a65bdfbfe105b_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_C69EB180);
            float2 _TilingAndOffset_5e3ed56d4eb5468ea3ee52c90f3fbe01_Out_3;
            Unity_TilingAndOffset_float(IN.uv0.xy, float2 (8, 32), float2 (-3.11, -6.86), _TilingAndOffset_5e3ed56d4eb5468ea3ee52c90f3fbe01_Out_3);
            float4 _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0 = SAMPLE_TEXTURE2D(_Property_416a2417a5f16e86998a65bdfbfe105b_Out_0.tex, UnityBuildSamplerStateStruct(SamplerState_Linear_Clamp).samplerstate, _TilingAndOffset_5e3ed56d4eb5468ea3ee52c90f3fbe01_Out_3);
            float _SampleTexture2D_53ef635328a52180babf003f88901af1_R_4 = _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0.r;
            float _SampleTexture2D_53ef635328a52180babf003f88901af1_G_5 = _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0.g;
            float _SampleTexture2D_53ef635328a52180babf003f88901af1_B_6 = _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0.b;
            float _SampleTexture2D_53ef635328a52180babf003f88901af1_A_7 = _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0.a;
            float4 _Multiply_098a1a5adc36458f9a9db8a89d29e5ee_Out_2;
            Unity_Multiply_float(_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_RGBA_0, _SampleTexture2D_53ef635328a52180babf003f88901af1_RGBA_0, _Multiply_098a1a5adc36458f9a9db8a89d29e5ee_Out_2);
            float4 _Property_c17c21327159a18ca8b8664a8b068016_Out_0 = Color_A2AAE5B5;
            float4 _Property_411901199da6388abd170ed7f1572505_Out_0 = Color_23C9B10D;
            float2 _Property_cdccdf9bd2b88d8ea1a0586c033ac4c8_Out_0 = Vector2_C848BFFB;
            float _Property_aeae242db5457e849304b95d8b1865e5_Out_0 = Vector1_2EE2CB80;
            float _Multiply_66801fb5ead25582bcac188fd69ff0d5_Out_2;
            Unity_Multiply_float(IN.TimeParameters.x, _Property_aeae242db5457e849304b95d8b1865e5_Out_0, _Multiply_66801fb5ead25582bcac188fd69ff0d5_Out_2);
            float _Fraction_f1b6b975af985484be20b419784bde3c_Out_1;
            Unity_Fraction_float(_Multiply_66801fb5ead25582bcac188fd69ff0d5_Out_2, _Fraction_f1b6b975af985484be20b419784bde3c_Out_1);
            float _Multiply_6598b1de072a41869510b57b1b24cfb5_Out_2;
            Unity_Multiply_float(_Fraction_f1b6b975af985484be20b419784bde3c_Out_1, 360, _Multiply_6598b1de072a41869510b57b1b24cfb5_Out_2);
            float2 _Rotate_6f00503a0a9f8e8ca3540eadb1831c6b_Out_3;
            Unity_Rotate_Degrees_float(IN.uv0.xy, _Property_cdccdf9bd2b88d8ea1a0586c033ac4c8_Out_0, _Multiply_6598b1de072a41869510b57b1b24cfb5_Out_2, _Rotate_6f00503a0a9f8e8ca3540eadb1831c6b_Out_3);
            float2 _TilingAndOffset_53b5cba0214a2c8fa262c98e44a577eb_Out_3;
            Unity_TilingAndOffset_float(_Rotate_6f00503a0a9f8e8ca3540eadb1831c6b_Out_3, float2 (4, 4), float2 (0, 0), _TilingAndOffset_53b5cba0214a2c8fa262c98e44a577eb_Out_3);
            float4 _SampleTexture2D_e60950bba990008885c246fd6823bc78_RGBA_0 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1).tex, UnityBuildTexture2DStructNoScale(_SampleTexture2D_e60950bba990008885c246fd6823bc78_Texture_1).samplerstate, _TilingAndOffset_53b5cba0214a2c8fa262c98e44a577eb_Out_3);
            float _SampleTexture2D_e60950bba990008885c246fd6823bc78_R_4 = _SampleTexture2D_e60950bba990008885c246fd6823bc78_RGBA_0.r;
            float _SampleTexture2D_e60950bba990008885c246fd6823bc78_G_5 = _SampleTexture2D_e60950bba990008885c246fd6823bc78_RGBA_0.g;
            float _SampleTexture2D_e60950bba990008885c246fd6823bc78_B_6 = _SampleTexture2D_e60950bba990008885c246fd6823bc78_RGBA_0.b;
            float _SampleTexture2D_e60950bba990008885c246fd6823bc78_A_7 = _SampleTexture2D_e60950bba990008885c246fd6823bc78_RGBA_0.a;
            float4 _Lerp_eb1d105d1ff35a88b4a5066f2def8324_Out_3;
            Unity_Lerp_float4(_Property_c17c21327159a18ca8b8664a8b068016_Out_0, _Property_411901199da6388abd170ed7f1572505_Out_0, (_SampleTexture2D_e60950bba990008885c246fd6823bc78_R_4.xxxx), _Lerp_eb1d105d1ff35a88b4a5066f2def8324_Out_3);
            float4 _Lerp_4e0883bd978e1085b325579ae4bceddf_Out_3;
            Unity_Lerp_float4(_Multiply_098a1a5adc36458f9a9db8a89d29e5ee_Out_2, _Lerp_eb1d105d1ff35a88b4a5066f2def8324_Out_3, (_SampleTexture2D_3ec74a86d8c1488d8ce597f8156fb7ab_A_7.xxxx), _Lerp_4e0883bd978e1085b325579ae4bceddf_Out_3);
            surface.BaseColor = (_Lerp_4e0883bd978e1085b325579ae4bceddf_Out_3.xyz);
            return surface;
        }

            // --------------------------------------------------
            // Build Graph Inputs

            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);

            output.ObjectSpaceNormal =           input.normalOS;
            output.ObjectSpaceTangent =          input.tangentOS;
            output.ObjectSpacePosition =         input.positionOS;

            return output;
        }
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);





            output.uv0 =                         input.texCoord0;
            output.TimeParameters =              _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

            return output;
        }

            // --------------------------------------------------
            // Main

            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBR2DPass.hlsl"

            ENDHLSL
        }
    }
    CustomEditor "ShaderGraph.PBRMasterGUI"
    FallBack "Hidden/Shader Graph/FallbackError"
}
