Shader "IMP/ImposterBaker"
{
	Properties
	{
	}
	SubShader
	{
		ZTest LEqual
		ZWrite on
		Cull off

		// pixels only pass (used for min max frame computation)
		Pass
		{
			Blend one one
			HLSLPROGRAM
			#pragma target 4.0

			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct Attributes
			{
				float3 positionOS 	: POSITION;
			};

			struct Varyings
			{
				float4 positionCS 	: SV_POSITION;
			};

			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;

				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
				output.positionCS = vertexInput.positionCS;

				return output;
			}

			float4 frag(Varyings input) : SV_Target
			{
				return 1;
			}
			ENDHLSL
		}
		
		// alpha copy
		Pass
		{
			Blend one one
			HLSLPROGRAM
			#pragma target 4.0

			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			TEXTURE2D(_AlphaMap);
			SAMPLER(SamplerState_Point_Clamp);

			float4 _Channels;
			
			struct Attributes
			{
				float3 positionOS 	: POSITION;
				float2 uv 			: TEXCOORD0;
			};

			struct Varyings
			{
				float2 uv 			: TEXCOORD0;
				float4 positionCS 	: SV_POSITION;
			};

			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;

				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
				output.positionCS = vertexInput.positionCS;
				output.uv = input.uv;

				return output;
			}

			float4 frag(Varyings input) : SV_Target
			{
				float alpha = SAMPLE_TEXTURE2D_LOD(_AlphaMap, SamplerState_Point_Clamp, input.uv, 0).a;
				return alpha * _Channels;
			}
			ENDHLSL
		}

		// depth copy
		Pass
		{
			Blend one one
			HLSLPROGRAM
			#pragma target 4.0

			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			TEXTURE2D(_DepthMap);
			SAMPLER(SamplerState_Point_Clamp);

			float4 _Channels;
			
			struct Attributes
			{
				float3 positionOS 	: POSITION;
				float2 uv 			: TEXCOORD0;
			};

			struct Varyings
			{
				float2 uv 			: TEXCOORD0;
				float4 positionCS 	: SV_POSITION;
			};

			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;

				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
				output.positionCS = vertexInput.positionCS;
				output.uv = input.uv;

				return output;
			}

			float4 frag(Varyings input) : SV_Target
			{
				float depth = SAMPLE_TEXTURE2D_LOD(_DepthMap, SamplerState_Point_Clamp, input.uv, 0).r;
				return depth * _Channels;
			}
			ENDHLSL
		}

		// merge normals + depth
		Pass
		{
			Blend one zero
			HLSLPROGRAM
			#pragma target 4.0

			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"

			SAMPLER(SamplerState_Point_Clamp);
			TEXTURE2D(_NormalMap);
			float4 _NormalMap_ST;
			float4 _NormalMap_TexelSize;

			TEXTURE2D(_DepthMap);
			float4 _DepthMap_ST;
			float4 _DepthMap_TexelSize;

			struct Attributes
			{
				float3 positionOS 	: POSITION;
				float2 uv 			: TEXCOORD0;
			};

			struct Varyings
			{
				float2 uv 			: TEXCOORD0;
				float4 positionCS 	: SV_POSITION;
			};

			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;

				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
				output.positionCS = vertexInput.positionCS;
				output.uv = TRANSFORM_TEX(input.uv, _NormalMap);

				return output;
			}

			float4 frag(Varyings input) : SV_Target
			{
				float3 normalSample = SAMPLE_TEXTURE2D_LOD(_NormalMap, SamplerState_Point_Clamp, input.uv, 0).rgb;
				float depthSample = SAMPLE_TEXTURE2D_LOD(_DepthMap, SamplerState_Point_Clamp, input.uv, 0).r;

				float3 unpackedNormal = UnpackNormal(normalSample) * 0.5 + 0.5;

				return float4(unpackedNormal, depthSample);
			}
			ENDHLSL
		}

		// dilate pass (super slow - might explode computer)
		Pass
		{
			Blend one one
			HLSLPROGRAM
			#pragma target 4.0

			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			SAMPLER(SamplerState_Point_Clamp);
			TEXTURE2D(_MainTex);
			float4 _MainTex_ST;
			float4 _MainTex_TexelSize;

			TEXTURE2D(_DilateMask);
			float4 _DilateMask_ST;
			float4 _DilateMask_TexelSize;

			float4 _Channels;

			struct Attributes
			{
				float3 positionOS 	: POSITION;
				float2 uv 			: TEXCOORD0;
			};

			struct Varyings
			{
				float2 uv 			: TEXCOORD0;
				float4 positionCS 	: SV_POSITION;
			};

			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;

				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
				output.positionCS = vertexInput.positionCS;
				output.uv = TRANSFORM_TEX(input.uv, _MainTex);

				return output;
			}

			float4 frag(Varyings input) : SV_Target
			{
				// Pixel colour
				float4 outColor = SAMPLE_TEXTURE2D_LOD(_MainTex, SamplerState_Point_Clamp, input.uv, 0);
				float mask = SAMPLE_TEXTURE2D_LOD(_DilateMask, SamplerState_Point_Clamp, input.uv, 0).r;

				if (mask > 0) return outColor;

				float minDistance = sqrt(_MainTex_TexelSize.z * _MainTex_TexelSize.z + _MainTex_TexelSize.w * _MainTex_TexelSize.w);
				float4 closestColor = outColor;
				float2 uv = input.uv;

				UNITY_LOOP
				for (int i = 0; i < _MainTex_TexelSize.z; ++i) 
				{
					UNITY_LOOP
					for (int j = 0; j < _MainTex_TexelSize.z; ++j) 
					{
						float2 sampleUV = float2(i, j) * _MainTex_TexelSize.xy;

						if (sampleUV.x == uv.x && sampleUV.y == uv.y) continue;

						float texelDistance = distance(sampleUV, input.uv);
						
						float4 sample = SAMPLE_TEXTURE2D_LOD(_MainTex, SamplerState_Point_Clamp, sampleUV, 0);
						float sampleMask = SAMPLE_TEXTURE2D_LOD(_DilateMask, SamplerState_Point_Clamp, sampleUV, 0).r;
						if (sampleMask > 0 && texelDistance < minDistance)
						{
							minDistance = texelDistance;
							closestColor = sample;
						}
					}
				}

				outColor = lerp(outColor, closestColor, _Channels);
				return outColor;
			}
			ENDHLSL
		}
	}
}
