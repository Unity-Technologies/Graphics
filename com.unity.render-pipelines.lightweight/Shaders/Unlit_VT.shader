Shader "Lightweight Render Pipeline/Unlit VT"
{
    Properties
    {
        [Stack(_TextureStack)] _BaseMap("Texture", 2D) = "white" {}
        _TextureStack ("_TextureStack", Stack ) = { _BaseMap }

        _BaseColor("Color", Color) = (1, 1, 1, 1)
        _Cutoff("AlphaCutout", Range(0.0, 1.0)) = 0.5

        // BlendMode
        [HideInInspector] _Surface("__surface", Float) = 0.0
        [HideInInspector] _Blend("__blend", Float) = 0.0
        [HideInInspector] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _SrcBlend("Src", Float) = 1.0
        [HideInInspector] _DstBlend("Dst", Float) = 0.0
        [HideInInspector] _ZWrite("ZWrite", Float) = 1.0
        [HideInInspector] _Cull("__cull", Float) = 2.0
        
        // Editmode props
        [HideInInspector] _QueueOffset("Queue offset", Float) = 0.0
        
        // ObsoleteProperties
        [HideInInspector] _MainTex("BaseMap", 2D) = "white" {}
        [HideInInspector] _Color("Base Color", Color) = (0.5, 0.5, 0.5, 1)
        [HideInInspector] _SampleGI("SampleGI", float) = 0.0 // needed from bakedlit
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "IgnoreProjector" = "True" "RenderPipeline" = "LightweightPipeline" }
        LOD 100

        Blend [_SrcBlend][_DstBlend]
        ZWrite [_ZWrite]
        Cull [_Cull]

        Pass
        {
            Name "Unlit"
            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x

            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _ALPHATEST_ON
            #pragma shader_feature _ALPHAPREMULTIPLY_ON

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "UnlitInput.hlsl"

            struct Attributes
            {
                float4 positionOS       : POSITION;
                float2 uv               : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv        : TEXCOORD0;
                float fogCoord  : TEXCOORD1;
                float4 vertex : SV_POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4x4 _TextureStack_spaceparams[2];
            float4   _TextureStack_atlasparams[2];
            TEXTURE2D(_TextureStack_transtab);          SAMPLER(sampler_TextureStack_transtab);
            TEXTURE2D(_TextureStack_cache0);            SAMPLER(sampler_TextureStack_cache0);

			#define GRA_HLSL_5 1
			#define GRA_ROW_MAJOR 1
			#define GRA_TEXTURE_ARRAY_SUPPORT 0
			#include "GraniteShaderLib3.cginc"

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.vertex = vertexInput.positionCS;
                
				output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogCoord = ComputeFogFactor(vertexInput.positionCS.z);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half2 uv = input.uv;

               // half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);

				GraniteStreamingTextureConstantBuffer textureParamBlock;
				textureParamBlock.data[0] = _TextureStack_atlasparams[0];
				textureParamBlock.data[1] = _TextureStack_atlasparams[1];

				GraniteTilesetConstantBuffer graniteParamBlock;
				graniteParamBlock.data[0] = _TextureStack_spaceparams[0];
				graniteParamBlock.data[1] = _TextureStack_spaceparams[1];

				GraniteConstantBuffers grCB;
				grCB.tilesetBuffer = graniteParamBlock;
				grCB.streamingTextureBuffer = textureParamBlock;

				GraniteTranslationTexture translationTable;
				translationTable.Texture = _TextureStack_transtab;
				translationTable.Sampler = sampler_TextureStack_transtab;

				GraniteCacheTexture cache;
				cache.Texture = _TextureStack_cache0;
				cache.Sampler = sampler_TextureStack_cache0;

				GraniteLookupData graniteLookupData;
				float4 resolve;
				Granite_Lookup_Anisotropic(grCB, translationTable, uv, graniteLookupData, resolve);

				half4 texColor;
				Granite_Sample_HQ(grCB, graniteLookupData, cache, 0, texColor);

                half3 color = texColor.rgb * _BaseColor.rgb;
                half alpha = texColor.a * _BaseColor.a;
                AlphaDiscard(alpha, _Cutoff);

#ifdef _ALPHAPREMULTIPLY_ON
                color *= alpha;
#endif

                color = MixFog(color, input.fogCoord);

                return half4(color, alpha);
            }
            ENDHLSL
        }

        Pass
        {
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature _ALPHATEST_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #include "UnlitInput.hlsl"
            #include "DepthOnlyPass.hlsl"
            ENDHLSL
        }

        // This pass it not used during regular rendering, only for lightmap baking.
        Pass
        {
            Name "Meta"
            Tags{"LightMode" = "Meta"}

            Cull Off

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma vertex LightweightVertexMeta
            #pragma fragment LightweightFragmentMetaUnlit

            #include "UnlitInput.hlsl"
            #include "UnlitMetaPass.hlsl"

            ENDHLSL
        }


		// This pass it not used during regular rendering, only for lightmap baking.
        Pass
        {
            Name "VTFeedback"
            Tags{"LightMode" = "VTFeedback"}

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x

            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_instancing

            #include "UnlitInput.hlsl"

			float4x4 _TextureStack_spaceparams[2];
            float4   _TextureStack_atlasparams[2];
         
			float4 VT_ResolveConstantPatch;

			#define GRA_HLSL_5 1
			#define GRA_ROW_MAJOR 1
			#define GRA_TEXTURE_ARRAY_SUPPORT 0
			#include "GraniteShaderLib3.cginc"

            struct Attributes
            {
                float4 positionOS       : POSITION;
                float2 uv               : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv        : TEXCOORD0;
                float4 vertex : SV_POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            }; 

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.vertex = vertexInput.positionCS;

				GraniteStreamingTextureConstantBuffer textureParamBlock;
				textureParamBlock.data[0] = _TextureStack_atlasparams[0];
				textureParamBlock.data[1] = _TextureStack_atlasparams[1];

				output.uv = Granite_Transform(textureParamBlock, TRANSFORM_TEX(input.uv, _BaseMap));

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

				GraniteStreamingTextureConstantBuffer textureParamBlock;
				textureParamBlock.data[0] = _TextureStack_atlasparams[0];
				textureParamBlock.data[1] = _TextureStack_atlasparams[1];

                //TODO(ddebaets) this should be part of the GraniteShaderLib
#if GRA_ROW_MAJOR == 1
	#define gra_CalcMiplevelDeltaScaleX 	_TextureStack_spaceparams[0][2][0]
	#define gra_CalcMiplevelDeltaScaleY 	_TextureStack_spaceparams[0][3][0]
#else
	#define gra_CalcMiplevelDeltaScaleX 	_TextureStack_spaceparams[0][0][2]
	#define gra_CalcMiplevelDeltaScaleY 	_TextureStack_spaceparams[0][0][3]
#endif

				gra_CalcMiplevelDeltaScaleX *= VT_ResolveConstantPatch.x;
				gra_CalcMiplevelDeltaScaleY *= VT_ResolveConstantPatch.y;

				GraniteTilesetConstantBuffer graniteParamBlock;
				graniteParamBlock.data[0] = _TextureStack_spaceparams[0];
				graniteParamBlock.data[1] = _TextureStack_spaceparams[1];

				GraniteConstantBuffers grCB;
				grCB.tilesetBuffer = graniteParamBlock;
				grCB.streamingTextureBuffer = textureParamBlock;

				return Granite_ResolverPixel_PreTransformed_Anisotropic(grCB, input.uv);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/InternalErrorShader"
    CustomEditor "UnityEditor.Rendering.LWRP.ShaderGUI.UnlitShader"
}
