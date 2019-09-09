Shader "Hidden/ShadowGroup2D"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _ShadowStencilGroup("__ShadowStencilGroup", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Cull Off
        BlendOp Add
        Blend One One
        ZWrite Off

        Pass
        {
            Stencil
            {
                Ref [_ShadowStencilGroup]
                Comp NotEqual
                Pass Replace
                Fail Keep
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 vertex : POSITION;
                float4 tangent: TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            uniform float3 _LightPos;
            uniform float  _LightRadius;

            Varyings vert (Attributes v)
            {
                Varyings o;
                float3 vertexWS = TransformObjectToWorld(v.vertex);  // This should be in world space
                float3 lightDir = _LightPos - vertexWS;
                lightDir.z = 0;

                float3 lightDirection = normalize(lightDir);  

                float3 endpoint = vertexWS + (_LightRadius * -lightDirection);

                float3 worldTangent = TransformObjectToWorldDir(v.tangent.xyz);

                float sharedShadowTest = saturate(ceil(dot(lightDirection, worldTangent)));
                float3 sharedShadowOffset = sharedShadowTest * _LightRadius * -lightDirection;

                float3 position;
                position = vertexWS + sharedShadowOffset;

                o.vertex = TransformWorldToHClip(position);

                // RGB - R is shadow value (to support soft shadows), G is Self Shadow Mask, B is No Shadow Mask
                o.color = 1; // v.color;
                o.color.g = 0.5;
                o.color.b = 0;

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }

            float4 frag (Varyings i) : SV_Target
            {
                float4 main = tex2D(_MainTex, i.uv);
                float4 col = i.color;
                col.g = main.a * col.g;
                return col;
            }
            ENDHLSL
        }
        Pass
        {
            Stencil
            {
                Ref [_ShadowStencilGroup]
                Comp NotEqual
                Pass Replace
                Fail Keep
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.vertex = TransformObjectToHClip(v.vertex);

                // RGB - R is shadow value (to support soft shadows), G is Self Shadow Mask, B is No Shadow Mask
                o.color = 1; 
                o.color.g = 0.5;
                o.color.b = 1;

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 main = tex2D(_MainTex, i.uv);
                half4 color = i.color;
                color.b = 1 - main.a;

                return color;
            }
            ENDHLSL
        }
    }
}
