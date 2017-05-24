Shader "Graph/UnityEngine.MaterialGraph.MetallicMasterNode0dd415f6-2593-4617-b37c-c9d9dd315a4c" 
{
	Properties 
	{
		Vector1_20d0d38d_52d0_4444_8ef4_da01a311590e_Uniform("", Float) = 0.1

	}	
	
SubShader 
{
		Tags
		{
			"RenderType"="Opaque"
			"Queue"="Geometry"
		}

		Blend One Zero

		Cull Back

		ZTest LEqual

		ZWrite On


	LOD 200
	
	CGPROGRAM
	#pragma target 3.0
	#pragma surface surf Standard vertex:vert
	#pragma glsl
	#pragma debug

		inline float unity_posterize_float (float input, float stepsize)
		{
			return floor(input / stepsize) * stepsize;
		}

		float Vector1_20d0d38d_52d0_4444_8ef4_da01a311590e_Uniform;


	struct Input 
	{
			float4 color : COLOR;
			float3 worldNormal;

	};

	void vert (inout appdata_full v, out Input o)
	{
		UNITY_INITIALIZE_OUTPUT(Input,o);

	}
  
	void surf (Input IN, inout SurfaceOutputStandard o) 
	{
			float3 worldSpaceNormal = normalize(IN.worldNormal);
			float3 ReflectionProbe_4f8cb744_19c5_4a6e_9bc1_172200f2fa25 = ShadeSH9(float4(worldSpaceNormal.xyz , 1));
			float4 Split_f630c862_c9a4_4765_b085_f9b01dd46061 = float4(ReflectionProbe_4f8cb744_19c5_4a6e_9bc1_172200f2fa25, 1.0);
			float Posterize_f232fbec_5ce8_4ba5_8491_a7a66291d45f_Output = unity_posterize_float (Split_f630c862_c9a4_4765_b085_f9b01dd46061.r, Vector1_20d0d38d_52d0_4444_8ef4_da01a311590e_Uniform);
			float Posterize_3187d243_6c53_430c_9d5e_fa7a1561872c_Output = unity_posterize_float (Split_f630c862_c9a4_4765_b085_f9b01dd46061.g, Vector1_20d0d38d_52d0_4444_8ef4_da01a311590e_Uniform);
			float Posterize_c83cc006_d2b4_4a06_8f45_52c85f6e2a73_Output = unity_posterize_float (Split_f630c862_c9a4_4765_b085_f9b01dd46061.b, Vector1_20d0d38d_52d0_4444_8ef4_da01a311590e_Uniform);
			float4 Combine_2e6d43a4_be04_453e_a0f2_a0520f41fea5_Output = float4(Posterize_f232fbec_5ce8_4ba5_8491_a7a66291d45f_Output,Posterize_3187d243_6c53_430c_9d5e_fa7a1561872c_Output,Posterize_c83cc006_d2b4_4a06_8f45_52c85f6e2a73_Output,0.0);
			o.Emission = Combine_2e6d43a4_be04_453e_a0f2_a0520f41fea5_Output;

	}
	ENDCG
}


	FallBack "Diffuse"
	CustomEditor "LegacyIlluminShaderGUI"
}
