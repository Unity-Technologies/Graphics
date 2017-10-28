Shader "hidden/preview/_D6313FDD"
{
	Properties
	{
				[HideInInspector] [NonModifiableTextureData] [NoScaleOffset] Texture_38F8F2CD("texture", 2D) = "white" {}
	}
	CGINCLUDE
	#include "UnityCG.cginc"
			void Unity_ScaleAndOffset_float(float2 uv, float2 scale, float2 scaleCenter, float2 offset, out float2 result)
			{
			    float4 xform = float4(scale, offset + scaleCenter - scaleCenter * scale);
			    result = uv * xform.xy + xform.zw;
			}
	struct GraphVertexInput
	{
	     float4 vertex : POSITION;
	     float3 normal : NORMAL;
	     float4 tangent : TANGENT;
	     float4 color : COLOR;
	     float4 texcoord0 : TEXCOORD0;
	     float4 lightmapUV : TEXCOORD1;
	     UNITY_VERTEX_INPUT_INSTANCE_ID
	};
			struct SurfaceInputs{
				half4 uv0;
			};
			struct SurfaceDescription{
				float2 __D6313FDD_result;
			};
			void ScaleSurfaceDescription(inout SurfaceDescription surface, float scale){
				surface.__D6313FDD_result = scale * surface.__D6313FDD_result;
			};
			void AddSurfaceDescription(inout SurfaceDescription base, in SurfaceDescription add){
				base.__D6313FDD_result = base.__D6313FDD_result + add.__D6313FDD_result;
			};
			UNITY_DECLARE_TEX2D(Texture_38F8F2CD);
			float4 __D6313FDD_uv;
			float4 __D6313FDD_scale;
			float4 __D6313FDD_scaleCenter;
			float4 __D6313FDD_offset;
			GraphVertexInput PopulateVertexData(GraphVertexInput v){
				return v;
			}
			SurfaceDescription PopulateSurfaceData(SurfaceInputs IN) {
				half4 uv0 = IN.uv0;
				float2 __D6313FDD_result;
				Unity_ScaleAndOffset_float(uv0, __D6313FDD_scale, __D6313FDD_scaleCenter, __D6313FDD_offset, __D6313FDD_result);
				SurfaceDescription surface = (SurfaceDescription)0;
				surface.__D6313FDD_result = __D6313FDD_result;
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
	            return half4(surf.__D6313FDD_result.x, surf.__D6313FDD_result.y, 0.0, 1.0);
	        }
	        ENDCG
	    }
	}
}
