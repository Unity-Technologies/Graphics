Shader "Hidden/HardShadow2D"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        [PerRendererData][HideInInspector] _ShadowStencilGroup("__ShadowStencilGroup", Float) = 1.0
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

            struct appdata
            {
                float3 vertex : POSITION;
				float4 tangent: TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
				float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

			uniform float3 _LightPos;
			uniform float  _LightRadius;
            uniform float  _LightSize;


            float3 ProjectUnclippedShadow(float3 vertex, float3 circleOrigin, float radius, float3 vertTan1, float3 vertTan2, float shadowDist, float2 uv)
            {
                vertex.z = 0; // This is just in case...
                circleOrigin.z = 0; //
                vertTan1.z = 0;
                vertTan2.z = 0;


                float3 calcCenter = 0.5f * (vertex + circleOrigin);
                float3 a = circleOrigin;
                float3 b = calcCenter;
                float dist = distance(calcCenter, circleOrigin);

                float nx = (b.x - a.x) / dist;
                float ny = (b.y - a.y) / dist;

                float distSq = dist * dist;
                float x = (radius * radius - distSq + distSq) / (2 * dist);
                float y = sqrt(radius * radius - x * x);

                float3 lightTan1;
                lightTan1.x = a.x + x * nx - y * ny;
                lightTan1.y = a.y + x * ny + y * nx;
                lightTan1.z = 0;

                // Should I relocate the normalize?
                float3 dir1 = normalize(lightTan1 - vertex);
                float dot1 = dot(dir1, vertTan1);
                float dot2 = dot(dir1, vertTan2);

                float3 lightTan2;
                lightTan2.x = a.x + x * nx + y * ny;
                lightTan2.y = a.y + x * ny - y * nx;
                lightTan2.z = 0;

                // Should I relocate the normalize?
                float3 dir2 = normalize(lightTan2 - vertex);
                float dot3 = dot(dir2, vertTan1);
                float dot4 = dot(dir2, vertTan2);


                if ((sign(dot1) != sign(dot2)) || (sign(dot3) != sign(dot4)))
                {
                    float3 shadowPos1 = shadowDist * -dir1;
                    float3 shadowPos2 = shadowDist * -dir2;

                    //float3 shadowPos1 = float3(-4, 4, 0);
                    //float3 shadowPos2 = float3(4, 4, 0);

                    return uv.x * shadowPos1 + uv.y * shadowPos2 + vertex;
                }

                // Don't do anything
                return vertex;
            }


            v2f vert (appdata v)
            {
				v2f o;
                float3 vertexWS = TransformObjectToWorld(v.vertex);  // This should be in world space

                float3 tangent0WS = TransformObjectToWorldDir(float3(v.tangent.xy,0));
                float3 tangent1WS = TransformObjectToWorldDir(float3(v.tangent.zw,0));

                float3 position = ProjectUnclippedShadow(vertexWS, _LightPos, 2.0, tangent0WS, tangent1WS, _LightRadius, v.uv);
                o.vertex = TransformWorldToHClip(position);

                // RGB - R is shadow value (to support soft shadows), G is Self Shadow Mask, B is No Shadow Mask
                o.color = 1; // v.color;
                o.color.g = 0.5;
                o.color.b = 0;

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 main = tex2D(_MainTex, i.uv);
				float4 col = i.color;
                col.g = main.a * col.g;

                return col;
            }
            ENDHLSL
        }
        // This pass is for rendering self shadow
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

            struct appdata
            {
                float3 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
				v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);

                // RGB - R is shadow value (to support soft shadows), G is Self Shadow Mask, B is No Shadow Mask
                o.color = 1; 
                o.color.g = 0.5;
                o.color.b = 0;

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 main = tex2D(_MainTex, i.uv);
				float4 col = i.color;
                col.r = 1;
                col.g = 0.5 * main.a;
                return col;
            }
            ENDHLSL
        }

    }
}
