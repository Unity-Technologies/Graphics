Shader "hidden/preview/PartialDerivative_CA98ACC1"
{
	Properties
	{
	}
	CGINCLUDE
	#include "UnityCG.cginc"
			void Unity_DDX_Coarse_float(float In, out float Out)
			{
			    Out = ddx_coarse(In);
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
				float _PartialDerivative_CA98ACC1_Out;
			};
			void ScaleSurfaceDescription(inout SurfaceDescription surface, float scale){
				surface._PartialDerivative_CA98ACC1_Out = scale * surface._PartialDerivative_CA98ACC1_Out;
			};
			void AddSurfaceDescription(inout SurfaceDescription base, in SurfaceDescription add){
				base._PartialDerivative_CA98ACC1_Out = base._PartialDerivative_CA98ACC1_Out + add._PartialDerivative_CA98ACC1_Out;
			};
			float Vector1_13AB6CC;
			GraphVertexInput PopulateVertexData(GraphVertexInput v){
				return v;
			}
			SurfaceDescription PopulateSurfaceData(SurfaceInputs IN) {
				float _PartialDerivative_CA98ACC1_Out;
				Unity_DDX_Coarse_float(Vector1_13AB6CC, _PartialDerivative_CA98ACC1_Out);
				SurfaceDescription surface = (SurfaceDescription)0;
				surface._PartialDerivative_CA98ACC1_Out = _PartialDerivative_CA98ACC1_Out;
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
	            return half4(surf._PartialDerivative_CA98ACC1_Out, surf._PartialDerivative_CA98ACC1_Out, surf._PartialDerivative_CA98ACC1_Out, 1.0);
	        }
	        ENDCG
	    }
	}
}
