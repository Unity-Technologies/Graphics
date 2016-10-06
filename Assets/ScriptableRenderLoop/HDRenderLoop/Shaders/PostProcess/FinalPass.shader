// Final compositing pass, just does gamma correction for now.
Shader "Hidden/Unity/FinalPass" 
{
	Properties { _MainTex ("Texture", any) = "" {} }
	SubShader { 
		Pass {
 			ZTest Always Cull Off ZWrite Off

			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma target 5.0

			#include "../../../ShaderLibrary/Common.hlsl"
			#include "../../../ShaderLibrary/Color.hlsl"
			#include "../ShaderVariables.hlsl"

			sampler2D _MainTex;

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

			float4 Frag(Varyings input) : SV_Target
			{
				float4 c = tex2D(_MainTex, input.texcoord);
				// Gamma correction

				// TODO: Currenlt in the editor there a an additional pass were the result is copyed in a render target RGBA8_sRGB.
				// So we must not correct the sRGB here else it will be done two time.
				// To fix!

				// return LinearToSRGB(c);
				return c;

				
			}
			ENDHLSL

		}
	}
	Fallback Off 
}
