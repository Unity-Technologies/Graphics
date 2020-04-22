// Shader outputs a pipeline environment color for testing reasons.
Shader "Universal Render Pipeline/Custom/DebugEnvironment"
{
    Properties
    {
        _Index ("Index in the array of colors [Sky, Equator, Ground, RealtimeShadow, Fog]", Int) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalRenderPipeline"}

        Pass
        {
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
            int _Index;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 color[5] = {unity_AmbientSky, unity_AmbientEquator, unity_AmbientGround, _SubtractiveShadowColor, unity_FogColor}; 
                return color[_Index];
            }
            ENDHLSL
        }
    }
}
