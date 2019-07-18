Shader "Hidden/Shadow2D"
{
    Properties
    {
        [HideInInspector] _ShadowStencilGroup("__ShadowStencilGroup", Float) = 1.0
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
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float3 vertex : POSITION;
				float4 tangent: TANGENT;
				float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
				float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

			uniform float3 _LightPos;
			uniform float  _LightRadius;

            v2f vert (appdata v)
            {
				v2f o;
				float softRadius = 0.2;  // This would be equal to the length of the shadow * sin(angle)

                float3 vertexWS = TransformObjectToWorld(v.vertex);  // This should be in world space
                float3 lightDirection = normalize(_LightPos - vertexWS); // 

                float3 endpoint = vertexWS + (_LightRadius * -lightDirection);

                float3 worldTangent = TransformObjectToWorldDir(v.tangent.xyz);

                float sharedShadowTest = saturate(ceil(dot(lightDirection, worldTangent)));
                float3 sharedShadowOffset = sharedShadowTest * _LightRadius * -lightDirection;  // Calculates the hard shadow. The soft shadow will be offset from that

				float3 position;
                position = vertexWS + sharedShadowOffset;

                o.vertex = TransformWorldToHClip(position);

                // RGB - R is shadow value (to support soft shadows), G is Self Shadow Mask, B is No Shadow Mask
                o.color = 1; // v.color;
                o.color.g = 0.5;
                o.color.b = 0;

                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
				float4 col = i.color;
                return col;
            }
            ENDHLSL
        }
    }
}
