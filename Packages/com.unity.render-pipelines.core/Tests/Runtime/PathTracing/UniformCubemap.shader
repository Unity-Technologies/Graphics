Shader "PathIteratorTesting/UniformCubemap" {
    Properties {
        _Radiance ("Radiance", Vector) = (.5, .5, .5, 1.0)
    }

    SubShader {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            half4 _Radiance;

            struct appdata_t {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex.xyz);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(_Radiance.xyz, 1.0);
            }
            ENDCG
        }
    }

    Fallback Off
}
