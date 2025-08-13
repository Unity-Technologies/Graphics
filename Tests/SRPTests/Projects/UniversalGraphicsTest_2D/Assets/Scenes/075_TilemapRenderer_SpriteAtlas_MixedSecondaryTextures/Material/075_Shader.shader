Shader "Custom/075_Material"
{

            Properties
            { 
                _MaskTex("Mask Tex", 2D) = "white" {}
                _MainTex("MainTex", 2D) = "white" {}
                _WindDir("_WindDir", Vector) =(1,1,0,0)
                _WindOffset("_WindOffset", Vector) =(1,1,0,0)
            }
            HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            ENDHLSL
            SubShader
            {
                Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
                Blend SrcAlpha OneMinusSrcAlpha
                Cull Off
                ZWrite Off
                Pass
                {
                Tags { "LightMode" = "Universal2D" }
                HLSLPROGRAM
                               
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
                #pragma enable_d3d11_debug_symbols
                #pragma vertex CombinedShapeLightVertex
                #pragma fragment CombinedShapeLightFragment
                #pragma multi_compile USE_SHAPE_LIGHT_TYPE_0 __

                struct Attributes
                {
                    float3 positionOS   : POSITION;
                    float4 color        : COLOR;
                    float2  uv           : TEXCOORD0;
                    UNITY_SKINNED_VERTEX_INPUTS
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct Varyings
                {
                    float4  positionCS  : SV_POSITION;
                    float4  color       : COLOR;
                    float2	uv          : TEXCOORD0;
                    float2	lightingUV  : TEXCOORD1;
                    float2 maskUV  : TEXCOORD2;
                    UNITY_VERTEX_OUTPUT_STEREO
                };
                #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"
                CBUFFER_START(UnityPerMaterial)
                half4 _WindDir;
                half4 _WindOffset;
                half4 _MainTex_ST;
                half4 _MaskTex_ST;
                CBUFFER_END

                TEXTURE2D(_MainTex);
                SAMPLER(sampler_MainTex);
                TEXTURE2D(_MaskTex);
                SAMPLER(sampler_MaskTex);

                Varyings CombinedShapeLightVertex(Attributes v)
                {
                    Varyings o = (Varyings)0;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                    UNITY_SKINNED_VERTEX_COMPUTE(v);

                    v.positionOS = UnityFlipSprite(v.positionOS, unity_SpriteProps.xy);
                    o.positionCS = TransformObjectToHClip(v.positionOS);

                    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                    o.maskUV = TRANSFORM_TEX(v.uv, _MaskTex);
                    o.lightingUV = half2(ComputeScreenPos(o.positionCS / o.positionCS.w).xy);

                    o.color = v.color  * unity_SpriteColor;
                    return o;
             
                }
          
                half4 CombinedShapeLightFragment(Varyings i) : SV_Target
                {
                    half4 main = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv );
                    half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, i.uv);
                    //we are doing this to show that the mask texture which is assigned as "MaskTex" 
                    //in the sprite atlas is able to pass the mask texture fine to both tilemaps in scene mode
                    //in play mode however it is only able to do so for one "
                    //the other thing to note in the tilemap where it does not work which you can tell by the white instead of yellow)
                    //it does work when changed to chunk mode specifically 
                    //i believe this has to do with the sprite atlas having some sprites with and without masks in the same atlas
                    //and so this difference may be ignored when doing batching, 
                    //this only breaks when theres multiple different datatypes in the atlas, if you make an atlas
                    //with just one that has the mask texture it works as expected

                    main = mask;
                    SurfaceData2D surfaceData;
                    InputData2D inputData;

                 
                    InitializeSurfaceData(main.rgb, main.a, mask, surfaceData);
                    InitializeInputData(i.uv, i.lightingUV, inputData);

                    return CombinedShapeLightShared(surfaceData, inputData);
                }
                ENDHLSL
            }
        }
     Fallback "Sprites/Default"
}
