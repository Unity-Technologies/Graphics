Shader "hidden/preview/_E2A7472C"
{
	Properties
	{
	}
	CGINCLUDE
	#include "UnityCG.cginc"
			void Unity_AACheckerboard_float(float2 uv, float4 colorA, float4 colorB, float3 aaTweak, float2 frequency, out float4 result)
			{
			    float4 derivatives = float4(ddx(uv), ddy(uv));
			    float2 duv_length = sqrt(float2(dot(derivatives.xz, derivatives.xz), dot(derivatives.yw, derivatives.yw)));
			    float width = 0.5f;
			    float2 distance3 = 2.0f * abs(frac(uv.xy * frequency) - 0.5f) - width;
			    float2 scale = aaTweak.x / duv_length.xy;
			    float2 blend_out = saturate((scale - aaTweak.zz) / (aaTweak.yy - aaTweak.zz));
			    float2 vector_alpha = clamp(distance3 * scale.xy * blend_out.xy, -1.0f, 1.0f);
			    float alpha = saturate(0.5f + 0.5f * vector_alpha.x * vector_alpha.y);
			    result= lerp(colorA, colorB, alpha.xxxx);
			}
	struct GraphVertexInput
	{
	     float4 vertex : POSITION;
	     float3 normal : NORMAL;
	     float4 tangent : TANGENT;
	     float4 texcoord0 : TEXCOORD0;
	     float4 lightmapUV : TEXCOORD1;
	     UNITY_VERTEX_INPUT_INSTANCE_ID
	};
			struct SurfaceInputs{
				half4 uv0;
			};
			struct SurfaceDescription{
				float4 _E2A7472C_result;
			};
			float4 _E2A7472C_uv;
			float4 _E2A7472C_colorA;
			float4 _E2A7472C_colorB;
			float4 _E2A7472C_aaTweak;
			float4 _E2A7472C_frequency;
			GraphVertexInput PopulateVertexData(GraphVertexInput v){
				return v;
			}
			SurfaceDescription PopulateSurfaceData(SurfaceInputs IN) {
				half4 uv0 = IN.uv0;
				float4 _E2A7472C_result;
				Unity_AACheckerboard_float(uv0, _E2A7472C_colorA, _E2A7472C_colorB, _E2A7472C_aaTweak, _E2A7472C_frequency, _E2A7472C_result);
				SurfaceDescription surface = (SurfaceDescription)0;
				surface._E2A7472C_result = _E2A7472C_result;
				return surface;
			}
	ENDCG
	SubShader
	{
	    Tags { "RenderType"="Opaque" }
	    LOD 100
	    Pass
	    {
	        CGPROGRAM
	        #pragma vertex vert
	        #pragma fragment frag
	        #include "UnityCG.cginc"
	        struct GraphVertexOutput
	        {
	            float4 position : POSITION;
	            half4 uv0 : TEXCOORD;
	        };
	        GraphVertexOutput vert (GraphVertexInput v)
	        {
	            v = PopulateVertexData(v);
	            GraphVertexOutput o;
	            o.position = UnityObjectToClipPos(v.vertex);
	            o.uv0 = v.texcoord0;
	            return o;
	        }
	        fixed4 frag (GraphVertexOutput IN) : SV_Target
	        {
	            float4 uv0  = IN.uv0;
	            SurfaceInputs surfaceInput = (SurfaceInputs)0;;
	            surfaceInput.uv0  =uv0;
	            SurfaceDescription surf = PopulateSurfaceData(surfaceInput);
	            return half4(surf._E2A7472C_result.x, surf._E2A7472C_result.y, surf._E2A7472C_result.z, 1.0);
	        }
	        ENDCG
	    }
	}
}
