Shader "hidden/preview/TangentToWorld_C47CCB6C"
{
	Properties
	{
	}
	CGINCLUDE
	#include "UnityCG.cginc"
			void Unity_TangentToWorld_float(float3 inVector, out float3 result, float3 tangent, float3 biTangent, float3 normal)
			{
			    float3x3 tangentToWorld = transpose(float3x3(tangent, biTangent, normal));
			    result= saturate(mul(tangentToWorld, normalize(inVector)));
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
				float3 WorldSpaceNormal;
				float3 WorldSpaceTangent;
				float3 WorldSpaceBiTangent;
			};
			struct SurfaceDescription{
				float3 TangentToWorld_C47CCB6C_result;
			};
			void ScaleSurfaceDescription(inout SurfaceDescription surface, float scale){
				surface.TangentToWorld_C47CCB6C_result = scale * surface.TangentToWorld_C47CCB6C_result;
			};
			void AddSurfaceDescription(inout SurfaceDescription base, in SurfaceDescription add){
				base.TangentToWorld_C47CCB6C_result = base.TangentToWorld_C47CCB6C_result + add.TangentToWorld_C47CCB6C_result;
			};
			float4 TangentToWorld_C47CCB6C_inVector;
			float4 TangentToWorld_C47CCB6C_tangent;
			float4 TangentToWorld_C47CCB6C_biTangent;
			float4 TangentToWorld_C47CCB6C_normal;
			GraphVertexInput PopulateVertexData(GraphVertexInput v){
				return v;
			}
			SurfaceDescription PopulateSurfaceData(SurfaceInputs IN) {
				float3 WorldSpaceNormal = IN.WorldSpaceNormal;
				float3 WorldSpaceTangent = IN.WorldSpaceTangent;
				float3 WorldSpaceBiTangent = IN.WorldSpaceBiTangent;
				float3 TangentToWorld_C47CCB6C_result;
				Unity_TangentToWorld_float(TangentToWorld_C47CCB6C_inVector, TangentToWorld_C47CCB6C_result, WorldSpaceTangent, WorldSpaceBiTangent, WorldSpaceNormal);
				SurfaceDescription surface = (SurfaceDescription)0;
				surface.TangentToWorld_C47CCB6C_result = TangentToWorld_C47CCB6C_result;
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
	            float3 WorldSpaceNormal : TEXCOORD0;
	float3 WorldSpaceTangent : TEXCOORD1;
	float3 WorldSpaceBiTangent : TEXCOORD2;
	        };
	        GraphVertexOutput vert (GraphVertexInput v)
	        {
	            v = PopulateVertexData(v);
	            GraphVertexOutput o;
	            o.position = UnityObjectToClipPos(v.vertex);
	            o.WorldSpaceNormal = mul(v.normal,(float3x3)unity_WorldToObject);
	o.WorldSpaceTangent = mul((float3x3)unity_ObjectToWorld,v.tangent);
	o.WorldSpaceBiTangent = normalize(cross(o.WorldSpaceNormal, o.WorldSpaceTangent.xyz) * v.tangent.w);
	            return o;
	        }
	        fixed4 frag (GraphVertexOutput IN) : SV_Target
	        {
	            float3 WorldSpaceNormal = normalize(IN.WorldSpaceNormal);
	float3 WorldSpaceTangent = IN.WorldSpaceTangent;
	float3 WorldSpaceBiTangent = IN.WorldSpaceBiTangent;
	            SurfaceInputs surfaceInput = (SurfaceInputs)0;;
	            surfaceInput.WorldSpaceNormal = WorldSpaceNormal;
	surfaceInput.WorldSpaceTangent = WorldSpaceTangent;
	surfaceInput.WorldSpaceBiTangent = WorldSpaceBiTangent;
	            SurfaceDescription surf = PopulateSurfaceData(surfaceInput);
	            return half4(surf.TangentToWorld_C47CCB6C_result.x, surf.TangentToWorld_C47CCB6C_result.y, surf.TangentToWorld_C47CCB6C_result.z, 1.0);
	        }
	        ENDCG
	    }
	}
}
