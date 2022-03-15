Shader "2D/Clear"
{
	SubShader {
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
		LOD 100

		Pass
        {
            Name "2D/Clear"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"

            half4 _Color0;
            half4 _Color1;
            half4 _Color2;
            half4 _Color3;
            half4 _NormalColor;

            struct Attributes
            {
                float4 positionHCS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };


            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = float4(input.positionHCS.xyz, 1.0);
                return output;
            }

            FragmentOutput Fragment(Varyings input)
            {
                FragmentOutput output;
                output.GLightBuffer0 = _Color0;
                output.GLightBuffer1 = _Color1;
                output.GLightBuffer2 = _Color2;
                output.GLightBuffer3 = _Color3;
                output.NormalBuffer = _NormalColor;
                return output;
            }

            ENDHLSL
        }
	}
}
