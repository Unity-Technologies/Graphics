Shader "BoatAttack/WaterFXShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		// Blend mode values
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend mode", Float) = 0.0
		// Blend mode values
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend mode", Float) = 0.0
		// Will set "_INVERT_ON" shader keyword when set
		[Toggle] _Invert ("Invert?", Float) = 0
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline" = "UniversalPipeline" }
		ZWrite Off
		Blend[_SrcBlend][_DstBlend]
		LOD 100

		Pass
		{
			Name "WaterFX"
			Tags{"LightMode" = "WaterFX"}
			HLSLPROGRAM
			#pragma vertex WaterFXVertex
			#pragma fragment WaterFXFragment
			#pragma shader_feature _INVERT_ON
			
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct Attributes
			{
				float3 positionOS : POSITION;
				float3 normalOS : NORMAL;
    			float4 tangentOS : TANGENT;
				half4 color : COLOR;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float2 uv : TEXCOORD0;
				half4 normal : TEXCOORD1;    // xyz: normal, w: viewDir.x
    			half4 tangent : TEXCOORD2;    // xyz: tangent, w: viewDir.y
    			half4 bitangent : TEXCOORD3;    // xyz: binormal, w: viewDir.z
				half4 color : TEXCOORD4;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			
			Varyings WaterFXVertex (Attributes input)
			{
				Varyings output = (Varyings)0;
				
				VertexPositionInputs vertexPosition = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs vertexTBN = GetVertexNormalInputs(input.normalOS, input.tangentOS);
				
				output.vertex = vertexPosition.positionCS;
				
				output.uv = input.uv;

				output.color = input.color;

				half3 viewDir = GetCameraPositionWS() - vertexPosition.positionWS;

                output.normal = half4(vertexTBN.normalWS, viewDir.x);
                output.tangent = half4(vertexTBN.tangentWS, viewDir.y);
                output.bitangent = half4(vertexTBN.bitangentWS, viewDir.z);

				return output;
			}
			
			half4 WaterFXFragment (Varyings input) : SV_Target
			{
				half4 col = tex2D(_MainTex, input.uv);

				half foamMask = col.r * input.color.r;
				half disp = col.a * 2 - 1;

				disp *= input.color.a;

				half3 tNorm = half3(col.b, col.g, 1) * 2 - 1;

				half3 viewDir = half3(input.normal.w, input.tangent.w, input.bitangent.w);
    			half3 normalWS = TransformTangentToWorld(tNorm, half3x3(input.tangent.xyz, input.bitangent.xyz, input.normal.xyz));

				normalWS = lerp(half3(0, 1, 0), normalWS, input.color.g);
				half4 comp = half4(foamMask, normalWS.xz, disp);

				#ifdef _INVERT_ON
				comp *= -1;
				#endif

				return comp;
			}
			ENDHLSL
		}
	}
}
