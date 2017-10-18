Shader "hidden/preview/Fractal_70AE3FBF"
{
	Properties
	{
	}
	CGINCLUDE
	#include "UnityCG.cginc"
			void Unity_Fractal_float(float2 uv, float2 pan, float zoom, float aspect, out float result)
			{
			    const int Iterations = 128;
			    float2 c = (uv - 0.5) * zoom * float2(1, aspect) - pan;
			    float2 v = 0;
			    for (int n = 0; n < Iterations && dot(v,v) < 4; n++)
			    {
			        v = float2(v.x * v.x - v.y * v.y, v.x * v.y * 2) + c;
			    }
			    result = (dot(v, v) > 4) ? (float)n / (float)Iterations : 0;
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
				float Fractal_70AE3FBF_result;
			};
			float4 Fractal_70AE3FBF_uv;
			float4 Fractal_70AE3FBF_pan;
			float Fractal_70AE3FBF_zoom;
			float Fractal_70AE3FBF_aspect;
			GraphVertexInput PopulateVertexData(GraphVertexInput v){
				return v;
			}
			SurfaceDescription PopulateSurfaceData(SurfaceInputs IN) {
				half4 uv0 = IN.uv0;
				float Fractal_70AE3FBF_result;
				Unity_Fractal_float(uv0, Fractal_70AE3FBF_pan, Fractal_70AE3FBF_zoom, Fractal_70AE3FBF_aspect, Fractal_70AE3FBF_result);
				SurfaceDescription surface = (SurfaceDescription)0;
				surface.Fractal_70AE3FBF_result = Fractal_70AE3FBF_result;
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
	            return half4(surf.Fractal_70AE3FBF_result, surf.Fractal_70AE3FBF_result, surf.Fractal_70AE3FBF_result, 1.0);
	        }
	        ENDCG
	    }
	}
}
