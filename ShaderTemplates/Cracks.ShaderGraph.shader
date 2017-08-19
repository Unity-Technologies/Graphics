Shader "Cracks" 
{
	Properties 
	{
		[NonModifiableTextureData] Texture2D_Texture2D_CF0DB0B2_Uniform("Texture2D", 2D) = "white" {}
		[NonModifiableTextureData] Texture2D_Texture2D_D8FD66BB_Uniform("Texture2D", 2D) = "white" {}
		Color_Color_B59542C3_Uniform("Color", Color) = (0,0.201217,0.9411765,0)
		Vector1_Vector1_2852A57B_Uniform("Vector1", Float) = 2.75
		[NonModifiableTextureData] Texture2D_Texture2D_DF8FC6BF_Uniform("Texture2D", 2D) = "white" {}

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

		UNITY_DECLARE_TEX2D(Texture2D_Texture2D_CF0DB0B2_Uniform);
		UNITY_DECLARE_TEX2D(Texture2D_Texture2D_D8FD66BB_Uniform);
		float4 Color_Color_B59542C3_Uniform;
		float Vector1_Vector1_2852A57B_Uniform;
		UNITY_DECLARE_TEX2D(Texture2D_Texture2D_DF8FC6BF_Uniform);

		void Unity_UnpackNormal_float(float4 packedNormal, out float3 normal)
		{
		    normal = UnpackNormal(packedNormal);
		}
		void Unity_DotProduct_float(float3 first, float3 second, out float result)
		{
		    result = dot(first, second);
		}
		void Unity_Subtract_float(float first, float second, out float result)
		{
		    result = first - second;
		}
		void Unity_Sin_float(float argument, out float result)
		{
		    result = sin(argument);
		}
		void Unity_Add_float(float first, float second, out float result)
		{
		    result = first + second;
		}
		void Unity_Multiply_float(float first, float second, out float result)
		{
		    result = first * second;
		}
		void Unity_Pow_float(float first, float second, out float result)
		{
		    result = pow(first, second);
		}
		void Unity_Multiply_float(float4 first, float4 second, out float4 result)
		{
		    result = first * second;
		}
		void Unity_Pow_float(float4 first, float4 second, out float4 result)
		{
		    result = pow(first, second);
		}
		void Unity_Add_float(float4 first, float4 second, out float4 result)
		{
		    result = first + second;
		}
		void Unity_Subtract_float(float4 first, float4 second, out float4 result)
		{
		    result = first - second;
		}



	struct Input 
	{
			float4 color : COLOR;
			half4 meshUV0;
			float3 worldNormal;
			INTERNAL_DATA

	};

	void vert (inout appdata_full v, out Input o)
	{
		UNITY_INITIALIZE_OUTPUT(Input,o);
			o.meshUV0 = v.texcoord;

	}
  
	void surf (Input IN, inout SurfaceOutputStandard o) 
	{
			half4 uv0 = IN.meshUV0;
			float3 worldSpaceNormal = normalize(IN.worldNormal);
			float4 Sample2DTexture_CBB2AC98_rgba = UNITY_SAMPLE_TEX2D(Texture2D_Texture2D_CF0DB0B2_Uniform,uv0.xy);
			float4 Sample2DTexture_E4154D7A_rgba = UNITY_SAMPLE_TEX2D(Texture2D_Texture2D_D8FD66BB_Uniform,uv0.xy);
			float3 UnpackNormal_6C531D77_normal;
			Unity_UnpackNormal_float(Sample2DTexture_E4154D7A_rgba, UnpackNormal_6C531D77_normal);
			float DotProduct_EE0C09EE_result;
			Unity_DotProduct_float((UNITY_MATRIX_IT_MV [2].xyz.xyz), worldSpaceNormal, DotProduct_EE0C09EE_result);
			float Subtract_E13D23D8_result;
			Unity_Subtract_float(1, DotProduct_EE0C09EE_result, Subtract_E13D23D8_result);
			float Sin_727041A3_result;
			Unity_Sin_float(_Time.z, Sin_727041A3_result);
			float Add_4994C002_result;
			Unity_Add_float(Sin_727041A3_result, 2, Add_4994C002_result);
			float Multiply_A835CC27_result;
			Unity_Multiply_float(Vector1_Vector1_2852A57B_Uniform, Add_4994C002_result, Multiply_A835CC27_result);
			float Power_B6C405D6_result;
			Unity_Pow_float(Subtract_E13D23D8_result, Multiply_A835CC27_result, Power_B6C405D6_result);
			float4 Multiply_FC556C52_result;
			Unity_Multiply_float(Color_Color_B59542C3_Uniform, (Power_B6C405D6_result.xxxx), Multiply_FC556C52_result);
			float4 Sample2DTexture_9E83468E_rgba = UNITY_SAMPLE_TEX2D(Texture2D_Texture2D_DF8FC6BF_Uniform,uv0.xy);
			float4 Power_91C00DF5_result;
			Unity_Pow_float(Sample2DTexture_9E83468E_rgba, float4 (30,30,30,30), Power_91C00DF5_result);
			float4 Multiply_B90514AD_result;
			Unity_Multiply_float(Power_91C00DF5_result, _SinTime, Multiply_B90514AD_result);
			float4 Add_27E0B602_result;
			Unity_Add_float(Multiply_FC556C52_result, Multiply_B90514AD_result, Add_27E0B602_result);
			float4 Subtract_D747760F_result;
			Unity_Subtract_float(float4 (1,1,1,1), Sample2DTexture_9E83468E_rgba, Subtract_D747760F_result);
			o.Albedo = Sample2DTexture_CBB2AC98_rgba;
			o.Normal = UnpackNormal_6C531D77_normal;
			o.Normal += 1e-6;
			o.Emission = Add_27E0B602_result;
			o.Metallic = Sample2DTexture_9E83468E_rgba;
			o.Smoothness = Subtract_D747760F_result;

	}
	ENDCG
}


	FallBack "Diffuse"
	CustomEditor "LegacyIlluminShaderGUI"
}
