Shader "hidden/preview/Gradient_BB2657E7"
{
	Properties
	{
	}
	CGINCLUDE
	#include "UnityCG.cginc"
			void Unity_Gradient_BB2657E7_float(float value, out float4 result)
			{
				float3 color0=float3(1,1,1);
				float colorp0=0;
				float3 color1=float3(1,1,1);
				float colorp1=1;
				float3 gradcolor = color0;
				float colorLerpPosition0=smoothstep(colorp0,colorp1,value);
				gradcolor = lerp(gradcolor,color1,colorLerpPosition0);
				float alpha0=1;
				float alphap0=0;
				float alpha1=1;
				float alphap1=1;
				float gradalpha = alpha0;
				float alphaLerpPosition0=smoothstep(alphap0,alphap1,value);
				gradalpha = lerp(gradalpha,alpha1,alphaLerpPosition0);
				result = float4(gradcolor,gradalpha);
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
			};
			struct SurfaceDescription{
				float4 Gradient_BB2657E7_result;
			};
			float Gradient_BB2657E7_value;
			GraphVertexInput PopulateVertexData(GraphVertexInput v){
				return v;
			}
			SurfaceDescription PopulateSurfaceData(SurfaceInputs IN) {
				float4 Gradient_BB2657E7_result;
				Unity_Gradient_BB2657E7_float(Gradient_BB2657E7_value, Gradient_BB2657E7_result);
				SurfaceDescription surface = (SurfaceDescription)0;
				surface.Gradient_BB2657E7_result = Gradient_BB2657E7_result;
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
	            
	        };
	        GraphVertexOutput vert (GraphVertexInput v)
	        {
	            v = PopulateVertexData(v);
	            GraphVertexOutput o;
	            o.position = UnityObjectToClipPos(v.vertex);
	            
	            return o;
	        }
	        fixed4 frag (GraphVertexOutput IN) : SV_Target
	        {
	            
	            SurfaceInputs surfaceInput = (SurfaceInputs)0;;
	            
	            SurfaceDescription surf = PopulateSurfaceData(surfaceInput);
	            return half4(surf.Gradient_BB2657E7_result.x, surf.Gradient_BB2657E7_result.y, surf.Gradient_BB2657E7_result.z, 1.0);
	        }
	        ENDCG
	    }
	}
}
