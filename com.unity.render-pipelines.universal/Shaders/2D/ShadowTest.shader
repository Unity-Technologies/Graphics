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

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

			

            struct appdata
            {
                float4 vertex : POSITION;
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

                float4 vertexWS = v.vertex;  // This should be in world space
                float3 lightDirection = normalize(_LightPos - vertexWS.xyz); // 

				// There is something here that needs to be done for when we are not dealing with the xy plane...
				float passesXY = saturate(ceil(-dot(lightDirection.xy, -v.tangent.xy)));
				float passesZW = saturate(ceil(-dot(lightDirection.xy, -v.tangent.zw)));

                float isSoftShadow = saturate(ceil(abs(v.tangent.z) + abs(v.tangent.w)));
                float isSoftShadowCorner = isSoftShadow * abs(passesXY - passesZW);

				float stretchHardShadow = (1 - isSoftShadow) * saturate(ceil(dot(lightDirection.xy, v.tangent.xy)));
				float2 softShadowTangentDir = passesZW * v.tangent.zw + passesXY * v.tangent.xy;


                float3 cross1 = cross(float3(softShadowTangentDir,0), -lightDirection);
                float3 maxAngle = normalize(cross(cross1, -lightDirection));
                float angle = dot(softShadowTangentDir, lightDirection);
                float t = 1 - abs(2 * angle * angle - 1);
                float3 offset = isSoftShadowCorner * t * maxAngle;

                float2 hardShadowOffset = stretchHardShadow * _ShadowLength * -lightDirection.xy;
                float2 softShadowOffset = isSoftShadowCorner * (_ShadowLength * -lightDirection.xy + offset);


				float4 position;
                position.xy = v.vertex.xy + hardShadowOffset;
				position.z = 0;
				position.w = v.vertex.w;
                o.vertex = UnityObjectToClipPos(position);
				o.color = v.color;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				fixed4 col = i.color;
                return col;
            }
            ENDCG
        }
    }
}
