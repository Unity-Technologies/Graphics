// Final compositing pass, just does gamma correction for now.
Shader "Hidden/Unity/FinalPass" 
{
	Properties { _MainTex ("Texture", any) = "" {} }
	SubShader { 
		Pass {
 			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma target 5.0

			#include "../../../ShaderLibrary/Color.hlsl"
			#include "../ShaderVariables.hlsl"

			sampler2D _MainTex;
			uniform float4 _MainTex_ST;

			struct Attributes {
				float3 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct Varyings {
				float4 vertex : SV_POSITION;
				float2 texcoord : TEXCOORD0;
			};

			Varyings Vert(Attributes input)
			{
				Varyings output;
				output.vertex = TransformWorldToHClip(input.vertex);
				output.texcoord = input.texcoord.xy;
				return output;
			}

			fixed4 Frag(Varyings input) : SV_Target
			{
				float4 c = tex2D(_MainTex, input.texcoord);
				// Gamma correction
				return LinearToSRGB(c);
			}
			ENDCG 

		}
	}
	Fallback Off 
}
