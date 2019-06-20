Shader "Unlit/CameraOpaque"
{
	Properties
	{

	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent"}
		LOD 100

		Pass
		{
			HLSLPROGRAM
			#pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float4 screenUV : TEXCOORD1;
			};

			TEXTURE2D(_CameraOpaqueTexture);
			SAMPLER(sampler_CameraOpaqueTexture_linear_clamp);
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = TransformObjectToHClip(v.vertex);
				o.uv = v.uv;
				o.screenUV = ComputeScreenPos(o.vertex);
				return o;
			}
			
			half4 frag (v2f i) : SV_Target
			{
				// sample the texture
				half2 screenUV = i.screenUV.xy / i.screenUV.w;
				half v = 0.05;
				screenUV += (frac(screenUV * 80) * v) - v * 0.5;
				half4 col = half4(-0.05, 0, -0.05, 1);
				col.r += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture_linear_clamp, ((screenUV - 0.5) * 1.1) + 0.5).r;
				col.g += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture_linear_clamp, screenUV).g;
				col.b += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture_linear_clamp, ((screenUV - 0.5) * 0.9) + 0.5).b;
				return col;
			}
			ENDHLSL
		}
	}
}
