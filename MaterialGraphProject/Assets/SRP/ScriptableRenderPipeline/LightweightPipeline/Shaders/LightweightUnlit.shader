Shader "ScriptableRenderPipeline/LightweightPipeline/Unlit"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1, 1, 1, 1)
        _Cutoff("AlphaCutout", Range(0.0, 1.0)) = 0.5

        // BlendMode
        [HideInInspector] _Mode("Mode", Float) = 0.0
        [HideInInspector] _SrcBlend("Src", Float) = 1.0
        [HideInInspector] _DstBlend("Dst", Float) = 0.0
        [HideInInspector] _ZWrite("ZWrite", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "IgnoreProjectors" = "True" "RenderPipeline" = "LightweightPipe" "Lightmode" = "LightweightForward" }
        LOD 100

        Blend [_SrcBlend][_DstBlend]
        ZWrite [_ZWrite]

        Pass
        {
            CGPROGRAM
            
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            #pragma multi_compile_fog
            #pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON

            #include "UnityCG.cginc"
			#include "CGIncludes/LightweightUnlit.cginc"

			#pragma vertex LightweightVertexUnlit
            #pragma fragment LightweightFragmentUnlit

			void DefineSurface(LightweightVertexOutputUnlit i, inout SurfaceUnlit s)
			{
				// Albedo
				float4 c = tex2D(_MainTex, i.meshUV0);
				s.Color = c.rgb * _Color.rgb;
				// Alpha
				s.Alpha = c.a * _Color.a;
#ifdef _ALPHATEST_ON
				clip(s.Alpha - _Cutoff);
#endif
			}
            
            ENDCG
        }
    }
    CustomEditor "LightweightUnlitGUI"
}
