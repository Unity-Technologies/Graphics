Shader "Hidden/Shadow2D"
{
    Properties
    {
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
		Cull Off
        //Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
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

                float2 endpoint = vertexWS.xy + (_LightRadius * -lightDirection.xy);

                float3 worldTangent = TransformObjectToWorldDir(v.tangent.xyz);

                float sharedShadowTest = saturate(ceil(dot(lightDirection.xy, worldTangent.xy)));
                float3 sharedShadowOffset = sharedShadowTest * _LightRadius * -lightDirection;  // Calculates the hard shadow. The soft shadow will be offset from that

				float3 position;
                position = vertexWS + sharedShadowOffset;

                o.vertex = TransformWorldToHClip(position);
				o.color = v.color;

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
