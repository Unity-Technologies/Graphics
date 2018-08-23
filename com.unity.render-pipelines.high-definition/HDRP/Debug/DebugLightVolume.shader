Shader "Hidden/HDRenderPipeline/DebugLightVolume"
{
    Properties
    {
        _Color ("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Range("Range", Vector) = (1.0, 1.0, 1.0, 1.0)
        _Offset("Offset", Vector) = (1.0, 1.0, 1.0, 1.0)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" }
        Tags {"Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "CoreRP/ShaderLibrary/Common.hlsl"
            #include "HDRP/ShaderVariables.hlsl"

            struct AttributesDefault
            {
                float4 positionOS : POSITION;
            };

            struct VaryingsDefault
            {
                float4 positionCS : SV_POSITION;
            };

            float3 _Range;
            float3 _Offset;
            float4 _Color;

            VaryingsDefault vert(AttributesDefault att) 
            {
                VaryingsDefault output;

                float3 positionRWS = TransformObjectToWorld(att.positionOS.xyz * _Range + _Offset);
                output.positionCS = TransformWorldToHClip(positionRWS);
                return output;
            }

            float4 frag(VaryingsDefault varying) : SV_Target
            {
                return _Color;
            }

            ENDHLSL
        }
    }
}