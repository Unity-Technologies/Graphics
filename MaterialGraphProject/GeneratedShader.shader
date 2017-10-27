Shader "hidden/preview/Smoothstep_F2C698A4"
{
	Properties
	{
				Float_BAE7EB71("Strength", Float) = 1
				Float_21781ECA("Ref", Float) = 0
				[HideInInspector] [NonModifiableTextureData] [NoScaleOffset] Texture_930E6693("9a61064b-169a-41ef-b682-771a60220772", 2D) = "white" {}
	}
	CGINCLUDE
	#include "UnityCG.cginc"
			void Unity_Smoothstep_float(float A, float B, float T, out float Out)
			{
			    Out = smoothstep(A, B, T);
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
			};
			struct SurfaceDescription{
				float _Smoothstep_F2C698A4_Out;
			};
			void ScaleSurfaceDescription(inout SurfaceDescription surface, float scale){
				surface._Smoothstep_F2C698A4_Out = scale * surface._Smoothstep_F2C698A4_Out;
			};
			void AddSurfaceDescription(inout SurfaceDescription base, in SurfaceDescription add){
				base._Smoothstep_F2C698A4_Out = base._Smoothstep_F2C698A4_Out + add._Smoothstep_F2C698A4_Out;
			};
			float Float_BAE7EB71;
			float Float_21781ECA;
			UNITY_DECLARE_TEX2D(Texture_930E6693);
			float _Smoothstep_F2C698A4_A;
			float _Smoothstep_F2C698A4_B;
			float _Smoothstep_F2C698A4_T;
			GraphVertexInput PopulateVertexData(GraphVertexInput v){
				return v;
			}
			SurfaceDescription PopulateSurfaceData(SurfaceInputs IN) {
				float _Smoothstep_F2C698A4_Out;
				Unity_Smoothstep_float(_Smoothstep_F2C698A4_A, _Smoothstep_F2C698A4_B, _Smoothstep_F2C698A4_T, _Smoothstep_F2C698A4_Out);
				SurfaceDescription surface = (SurfaceDescription)0;
				surface._Smoothstep_F2C698A4_Out = _Smoothstep_F2C698A4_Out;
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
	            return half4(surf._Smoothstep_F2C698A4_Out, surf._Smoothstep_F2C698A4_Out, surf._Smoothstep_F2C698A4_Out, 1.0);
	        }
	        ENDCG
	    }
	}
}
