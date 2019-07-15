Shader "Unlit/ShadowTest"
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
			uniform float _ShadowLength;

            v2f vert (appdata v)
            {
				v2f o;
				float softRadius = 0.2;  // This would be equal to the length of the shadow * sin(angle)

                float3 vertexWS = TransformObjectToWorld(v.vertex);  // This should be in world space
                float3 lightDirection = normalize(_LightPos - vertexWS); // 

				// There is something here that needs to be done for when we are not dealing with the xy plane...
				float passesXY = saturate(ceil(-dot(lightDirection.xy, -v.tangent.xy)));
				float passesZW = saturate(ceil(-dot(lightDirection.xy, -v.tangent.zw)));

                float isSoftShadow = saturate(ceil(abs(v.tangent.z) + abs(v.tangent.w)));
                float isSoftShadowCorner = isSoftShadow * abs(passesXY - passesZW);

                float2 endpoint = vertexWS.xy + isSoftShadowCorner * (_ShadowLength * -lightDirection.xy);

                float2 softShadowTangentDir = normalize(isSoftShadow * passesZW * v.tangent.zw + passesXY * v.tangent.xy);
                float3 cross1 = cross(float3(softShadowTangentDir,0), -lightDirection);
                float3 maxAngle = normalize(cross(cross1, -lightDirection));

                float angle = dot(softShadowTangentDir, lightDirection.xy);
                float t = 1 - abs(2 * angle * angle - 1);
                float3 offset = clamp(t * maxAngle, -1, 1);

                float sharedShadowTest = saturate(ceil(dot(lightDirection.xy, v.tangent.xy)));
                float3 softShadowOffset = isSoftShadowCorner * offset;
                float3 sharedShadowOffset = sharedShadowTest * _ShadowLength * -lightDirection;  // Calculates the hard shadow. The soft shadow will be offset from that

				float3 position;
                position = vertexWS + sharedShadowOffset + softShadowOffset;

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
