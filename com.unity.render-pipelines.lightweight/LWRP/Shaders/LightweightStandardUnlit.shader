Shader "LightweightPipeline/Standard Unlit"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1, 1, 1, 1)
        _Cutoff("AlphaCutout", Range(0.0, 1.0)) = 0.5
        [Toggle] _SampleGI("SampleGI", float) = 0.0
        _BumpMap("Normal Map", 2D) = "bump" {}

        // BlendMode
        [HideInInspector] _Surface("__surface", Float) = 0.0
        [HideInInspector] _Blend("__blend", Float) = 0.0
        [HideInInspector] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _SrcBlend("Src", Float) = 1.0
        [HideInInspector] _DstBlend("Dst", Float) = 0.0
        [HideInInspector] _ZWrite("ZWrite", Float) = 1.0
        [HideInInspector] _Cull("__cull", Float) = 2.0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "IgnoreProjectors" = "True" "RenderPipeline" = "LightweightPipeline" }
        LOD 100

        Blend [_SrcBlend][_DstBlend]
        ZWrite [_ZWrite]
        Cull [_Cull]

        Pass
        {
            Name "StandardUnlit"
            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x

            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _SAMPLE_GI
            #pragma shader_feature _ALPHATEST_ON
            #pragma shader_feature _ALPHAPREMULTIPLY_ON

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            // Lighting include is needed because of GI
            #include "LWRP/ShaderLibrary/Lighting.hlsl"
            #include "LWRP/ShaderLibrary/InputSurfaceUnlit.hlsl"

            struct VertexInput
            {
                float4 vertex       : POSITION;
                float2 uv           : TEXCOORD0;
                float2 lightmapUV   : TEXCOORD1;
                float3 normal       : NORMAL;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VertexOutput
            {
                float3 uv0AndFogCoord           : TEXCOORD0; // xy: uv0, z: fogCoord
#if _SAMPLE_GI
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);
                half3 normal                    : TEXCOORD2;
    #if _NORMALMAP
                half3 tangent                   : TEXCOORD3;
                half3 binormal                  : TEXCOORD4;
    #endif
#endif
                float4 vertex : SV_POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            VertexOutput vert(VertexInput v)
            {
                VertexOutput o = (VertexOutput)0;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv0AndFogCoord.xy = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv0AndFogCoord.z = ComputeFogFactor(o.vertex.z);

#if _SAMPLE_GI
                OUTPUT_NORMAL(v, o);
                OUTPUT_LIGHTMAP_UV(v.lightmapUV, unity_LightmapST, o.lightmapUV);
                OUTPUT_SH(o.normal, o.vertexSH);
#endif
                return o;
            }

            half4 frag(VertexOutput IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                half2 uv = IN.uv0AndFogCoord.xy;
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                half3 color = texColor.rgb * _Color.rgb;
                half alpha = texColor.a * _Color.a;
                AlphaDiscard(alpha, _Cutoff);

#ifdef _ALPHAPREMULTIPLY_ON
                color *= alpha;
#endif


#if _SAMPLE_GI
    #if _NORMALMAP
                half3 normalWS = TangentToWorldNormal(surfaceData.normalTS, IN.tangent, IN.binormal, IN.normal);
    #else
                half3 normalWS = normalize(IN.normal);
    #endif
                color += SAMPLE_GI(IN.lightmapUV, IN.vertexSH, normalWS);
#endif
                ApplyFog(color, IN.uv0AndFogCoord.z);

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

            #include "LWRP/ShaderLibrary/InputSurfaceUnlit.hlsl"
            #include "LWRP/ShaderLibrary/LightweightPassDepthOnly.hlsl"
            ENDHLSL
        }
    }
    FallBack "Hidden/InternalErrorShader"
    CustomEditor "LightweightUnlitGUI"
}
