// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Skybox/CubemapFoged" {
Properties {
    _Tint ("Tint Color", Color) = (.5, .5, .5, .5)
    [Gamma] _Exposure ("Exposure", Range(0, 8)) = 1.0
    _Rotation ("Rotation", Range(0, 360)) = 0
    [NoScaleOffset] _Tex ("Cubemap   (HDR)", Cube) = "grey" {}
    _Value ("_Value", FLOAT) = 1
}

SubShader {
    Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
    Cull Off ZWrite Off

    Pass {

        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma target 2.0
        #pragma multi_compile_fog
        #pragma multi_compile _ FOGMAP
        #pragma multi_compile _ PHYSICAL_SKY

        #include "UnityCG.cginc"
        
        CBUFFER_START(_PerCamera)
        float3 _FogColor;
        float4 _FogParams;
        float4 _FogMap_HDR;
        CBUFFER_END
        
        #ifdef FOGMAP
        samplerCUBE _FogMap;
        #endif

        samplerCUBE _Tex;
        half4 _Tex_HDR;
        half4 _Tint;
        half _Exposure;
        float _Rotation;
        float _Value;
        
        float3 RotateAroundYInDegrees (float3 vertex, float degrees)
        {
            float alpha = degrees * UNITY_PI / 180.0;
            float sina, cosa;
            sincos(alpha, sina, cosa);
            float2x2 m = float2x2(cosa, -sina, sina, cosa);
            return float3(mul(m, vertex.xz), vertex.y).xzy;
        }
      
        half2 ComputeSkyFogFactor(half3 dir)
        {
            #if defined(FOG_EXP)
                half height = (pow(saturate((_WorldSpaceCameraPos.y - _FogParams.z) * 0.01), _FogParams.x*100));
                return half2(min(height,saturate(1-dot(dir,float3(0,-500,0)))), height);
            #else
                return 0.0h;
            #endif
        }

        float Linear01Depth(float depth, float4 zBufferParam)
        {
            return 1.0 / (zBufferParam.x * depth + zBufferParam.y);
        }

        half3 MipFog(half3 viewDirection, float z, half2 heightFactor)
        {
        #ifdef FOGMAP
            half depth = lerp(1,1-heightFactor.y,heightFactor.x)*_Value;
            //viewDirection.z = -viewDirection.z; // flip to match skybox
            half3 color = texCUBElod(_FogMap, float4(viewDirection.r,viewDirection.g,viewDirection.b,depth));
        #if !defined(UNITY_USE_NATIVE_HDR)
            color = DecodeHDR(half4(color, 1), _FogMap_HDR);
        #endif
        #else
            half3 color = half3(1, 0, 0);
        #endif
            return color;
        }

        half3 MixSkyFogColor(half3 fragColor, half2 fogFactor, half3 viewDirection)
        {
            #if defined(FOG_EXP)
                #ifdef FOGMAP
                    half3 fogColor = MipFog(viewDirection, viewDirection, fogFactor);
                #else
                    half3 fogColor = _FogColor;
                #endif
                return lerp(fogColor,fragColor,saturate(ceil(_FogParams.x) * fogFactor.x));
            #else
                return fragColor;
            #endif
        }
        
        struct appdata_t {
            float4 vertex : POSITION;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct v2f {
            float4 vertex : SV_POSITION;
            float3 texcoord : TEXCOORD0;
            float2 fogFactor : TEXCOORD1;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        v2f vert (appdata_t v)
        {
            v2f o;
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            float3 rotated = RotateAroundYInDegrees(v.vertex, _Rotation);
            o.vertex = UnityObjectToClipPos(rotated);
            o.texcoord = v.vertex.xyz;
            o.fogFactor = ComputeSkyFogFactor(v.vertex.xyz);
            return o;
        }

        fixed4 frag (v2f i) : SV_Target
        {
            half4 tex = texCUBE (_Tex, i.texcoord);
            half3 c = DecodeHDR (tex, _Tex_HDR);
            c = c * _Tint.rgb * unity_ColorSpaceDouble.rgb;
            c *= _Exposure;
            c = MixSkyFogColor(c,i.fogFactor, i.texcoord);
            
            return half4(c, 1);
        }
        ENDCG
    }
}


Fallback Off

}
