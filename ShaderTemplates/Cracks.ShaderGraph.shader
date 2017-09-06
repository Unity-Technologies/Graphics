Shader "Cracks" 
{
	Properties 
	{
		[NonModifiableTextureData] Texture2D_Texture2D_CF0DB0B2_Uniform("Texture2D", 2D) = "white" {}
		[NonModifiableTextureData] Texture2D_Texture2D_D8FD66BB_Uniform("Texture2D", 2D) = "white" {}
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
			float4 Sample2DTexture_CBB2AC98_RGBA = UNITY_SAMPLE_TEX2D(Texture2D_Texture2D_CF0DB0B2_Uniform,uv0.xy);
			float Sample2DTexture_CBB2AC98_R = Sample2DTexture_CBB2AC98_RGBA.r;
			float Sample2DTexture_CBB2AC98_G = Sample2DTexture_CBB2AC98_RGBA.g;
			float Sample2DTexture_CBB2AC98_B = Sample2DTexture_CBB2AC98_RGBA.b;
			float Sample2DTexture_CBB2AC98_A = Sample2DTexture_CBB2AC98_RGBA.a;
			float4 Sample2DTexture_E4154D7A_RGBA = UNITY_SAMPLE_TEX2D(Texture2D_Texture2D_D8FD66BB_Uniform,uv0.xy);
			float Sample2DTexture_E4154D7A_R = Sample2DTexture_E4154D7A_RGBA.r;
			float Sample2DTexture_E4154D7A_G = Sample2DTexture_E4154D7A_RGBA.g;
			float Sample2DTexture_E4154D7A_B = Sample2DTexture_E4154D7A_RGBA.b;
			float Sample2DTexture_E4154D7A_A = Sample2DTexture_E4154D7A_RGBA.a;
			float3 UnpackNormal_6C531D77_normal;
			Unity_UnpackNormal_float(Sample2DTexture_E4154D7A_RGBA, UnpackNormal_6C531D77_normal);
			float4 Color_Color_B59542C3_Uniform = float4 (0.04574043, 0.6029412, 0, 0);
			// Subgraph for node SubGraph_36F1FBE3
			float4 SubGraph_36F1FBE3_Output1 = 0;
			{
				float4 SubGraphInputs_6ACBBD95_Input1 = (Vector1_Vector1_2852A57B_Uniform.xxxx);
				float DotProduct_10B07FF5_result;
				Unity_DotProduct_float((UNITY_MATRIX_IT_MV [2].xyz.xyz), worldSpaceNormal, DotProduct_10B07FF5_result);
				float Subtract_72198B00_result;
				Unity_Subtract_float(1, DotProduct_10B07FF5_result, Subtract_72198B00_result);
				float Sin_4FB6BE0D_result;
				Unity_Sin_float(_Time.z, Sin_4FB6BE0D_result);
				float Add_716D55BE_result;
				Unity_Add_float(Sin_4FB6BE0D_result, 2, Add_716D55BE_result);
				float4 Multiply_E986BCF2_result;
				Unity_Multiply_float(SubGraphInputs_6ACBBD95_Input1, (Add_716D55BE_result.xxxx), Multiply_E986BCF2_result);
				float4 Power_A9D71705_result;
				Unity_Pow_float((Subtract_72198B00_result.xxxx), Multiply_E986BCF2_result, Power_A9D71705_result);
				SubGraph_36F1FBE3_Output1 = Power_A9D71705_result;
			}
			// Subgraph ends
			float4 Multiply_FC556C52_result;
			Unity_Multiply_float(Color_Color_B59542C3_Uniform, SubGraph_36F1FBE3_Output1, Multiply_FC556C52_result);
			float4 Sample2DTexture_9E83468E_RGBA = UNITY_SAMPLE_TEX2D(Texture2D_Texture2D_DF8FC6BF_Uniform,uv0.xy);
			float Sample2DTexture_9E83468E_R = Sample2DTexture_9E83468E_RGBA.r;
			float Sample2DTexture_9E83468E_G = Sample2DTexture_9E83468E_RGBA.g;
			float Sample2DTexture_9E83468E_B = Sample2DTexture_9E83468E_RGBA.b;
			float Sample2DTexture_9E83468E_A = Sample2DTexture_9E83468E_RGBA.a;
			float4 Power_91C00DF5_result;
			Unity_Pow_float(Sample2DTexture_9E83468E_RGBA, float4 (30,30,30,30), Power_91C00DF5_result);
			float4 Multiply_B90514AD_result;
			Unity_Multiply_float(Power_91C00DF5_result, _SinTime, Multiply_B90514AD_result);
			float4 Add_27E0B602_result;
			Unity_Add_float(Multiply_FC556C52_result, Multiply_B90514AD_result, Add_27E0B602_result);
			float4 Subtract_D747760F_result;
			Unity_Subtract_float(float4 (1,1,1,1), Sample2DTexture_9E83468E_RGBA, Subtract_D747760F_result);
			o.Albedo = Sample2DTexture_CBB2AC98_RGBA;
			o.Normal = UnpackNormal_6C531D77_normal;
			o.Normal += 1e-6;
			o.Emission = Add_27E0B602_result;
			o.Metallic = Sample2DTexture_9E83468E_RGBA;
			o.Smoothness = Subtract_D747760F_result;

	}
	ENDCG
}


	FallBack "Diffuse"
	CustomEditor "LegacyIlluminShaderGUI"
}
