Shader "Hidden/CubemapColorMap" {
    Properties {
        _ColorMap("Color", Cube) = "white" {}
        _MipIndex("Mip Index", Int) = 0
    }

    HLSLINCLUDE
    #pragma target 5.0

    #include "UnityCG.cginc"

    TextureCube _ColorMap;
    int _MipIndex;
    SamplerState s_bilinear_clamp;

    struct appdata {
        float3 positionOS       : POSITION;
        float3 normalOS         : NORMAL;
    };

    struct v2f {
        float4 positionCS       : SV_POSITION;
        float3 normalWS         : TEXCOORD0;
    };

    v2f Vert(appdata i) {
        v2f o;
        float4 positionCS = UnityObjectToClipPos(i.positionOS);
        float3 normalWS = normalize(mul(i.normalOS, (float3x3)unity_WorldToObject));
        o.positionCS = positionCS;
        o.normalWS = normalWS;
        return o;
    }

    float4 Frag(v2f i) : SV_Target {
        float3 col = _ColorMap.SampleLevel(s_bilinear_clamp, i.normalWS, _MipIndex).xyz;
        return float4(col, 0.5);
    }

    #pragma vertex Vert
    #pragma fragment Frag

    ENDHLSL

    SubShader {

        Pass {
            Name "ForwardUnlit"
            Tags{ "LightMode" = "ForwardOnly" }

            Blend One Zero
            ZWrite On
            Cull Back

            HLSLPROGRAM

            #define SHADERPASS SHADERPASS_FORWARD_UNLIT

            ENDHLSL
        }
    }
}
