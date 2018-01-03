Shader "Hidden/HDRP/HDTest/GBufferOutput"
{
    HLSLINCLUDE

        #include "../../../../../../PostProcessing/PostProcessing/Shaders/StdLib.hlsl"

        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);

        float _BufferIndex;

        sampler2D _GBufferTexture0;
        sampler2D _GBufferTexture1;
        sampler2D _GBufferTexture2;
        sampler2D _GBufferTexture3;
        sampler2D _GBufferTexture4;
        sampler2D _GBufferTexture5;
        sampler2D _GBufferTexture6;
        sampler2D _GBufferTexture7;

        TEXTURE2D_SAMPLER2D(_CameraMotionVectorsTexture, sampler_CameraMotionVectorsTexture);

        float4 Frag(VaryingsDefault i) : SV_Target
        {
            float4 color = float4(0,0,0,0);

            /*
            if (_BufferIndex == 0) tex2D(_GBufferTexture0, i.texcoord);
            if (_BufferIndex == 1) tex2D(_GBufferTexture1, i.texcoord);
            if (_BufferIndex == 2) tex2D(_GBufferTexture2, i.texcoord);
            if (_BufferIndex == 3) tex2D(_GBufferTexture3, i.texcoord);
            if (_BufferIndex == 4) tex2D(_GBufferTexture4, i.texcoord);
            if (_BufferIndex == 5) tex2D(_GBufferTexture5, i.texcoord);
            if (_BufferIndex == 6) tex2D(_GBufferTexture6, i.texcoord);
            if (_BufferIndex == 7) tex2D(_GBufferTexture7, i.texcoord);
            */

            color = SAMPLE_TEXTURE2D(_CameraMotionVectorsTexture, sampler_CameraMotionVectorsTexture, i.texcoord);

            float luminance = dot(color.rgb, float3(0.2126729, 0.7151522, 0.0721750));
            //color.rgb = lerp(color.rgb, luminance.xxx, _Blend.xxx);
            color.rgb = luminance;
            return color;
        }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment Frag

            ENDHLSL
        }
    }
}
