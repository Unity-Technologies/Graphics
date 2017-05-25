Shader "Generated.ExportTextureMasterNode3bf8de92-956b-458c-bbd4-470b91d6bd57" 
{
	Properties 
	{
				[NonModifiableTextureData] TextureAsset_a266f89a_2f3e_47a6_a8c0_5f05a62b6679_Uniform("TextureAsset", 2D) = "white" {}

	}	
	
	SubShader 
	{
		Tags { "Queue"="Geometry" "IgnoreProjector"="True" "RenderType"="Opaque" }

		ZWrite Off
		Blend One Zero
		
		Pass 
		{
			CGPROGRAM
			#include "UnityCustomRenderTexture.cginc"
		 	#pragma vertex CustomRenderTextureVertexShader_Preview
			#pragma fragment frag
			#pragma target 4.0
			
			v2f_customrendertexture CustomRenderTextureVertexShader_Preview(appdata_base IN)
			{
				v2f_customrendertexture OUT;
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.primitiveID = 0;//TODO
				OUT.localTexcoord = IN.texcoord;
				OUT.globalTexcoord = IN.texcoord;
				OUT.direction = CustomRenderTextureComputeCubeDirection(OUT.globalTexcoord.xy);
				return OUT;
			}

			sampler2D TextureAsset_a266f89a_2f3e_47a6_a8c0_5f05a62b6679_Uniform;
			float4 TextureAsset_a266f89a_2f3e_47a6_a8c0_5f05a62b6679_Uniform_TexelSize;
			float4 convolutionFilter0_da4b8ee3_365e_467c_8166_39db9bd7c370;
			float4 convolutionFilter1_da4b8ee3_365e_467c_8166_39db9bd7c370;
			float4 convolutionFilter2_da4b8ee3_365e_467c_8166_39db9bd7c370;
			float4 convolutionFilter3_da4b8ee3_365e_467c_8166_39db9bd7c370;
			float4 convolutionFilter4_da4b8ee3_365e_467c_8166_39db9bd7c370;
			float4 convolutionFilter5_da4b8ee3_365e_467c_8166_39db9bd7c370;
			float4 convolutionFilter6_da4b8ee3_365e_467c_8166_39db9bd7c370;


			inline float4 unity_convolution_float (sampler2D textSampler, float2 baseUv, float4 weights0,float4 weights1,float4 weights2,float4 weights3,float4 weights4,float4 weights5,float4 weights6, float2 texelSize)
			{
				fixed4 fetches = fixed4(0,0,0,0);
				fixed weight = 1;
				weight = weights0.x;
				fetches += weight * tex2D(textSampler, baseUv + texelSize * fixed2(-2,-2));
				weight = weights0.y;
				fetches += weight * tex2D(textSampler, baseUv + texelSize * fixed2(-1,-2));
				weight = weights0.z;
				fetches += weight * tex2D(textSampler, baseUv + texelSize * fixed2(0,-2));
				weight = weights0.w;
				fetches += weight * tex2D(textSampler, baseUv + texelSize * fixed2(1,-2));
				weight = weights1.x;
				fetches += weight * tex2D(textSampler, baseUv + texelSize * fixed2(2,-2));
				weight = weights1.y;
				fetches += weight * tex2D(textSampler, baseUv + texelSize * fixed2(-2,-1));
				weight = weights1.z;
				fetches += weight * tex2D(textSampler, baseUv + texelSize * fixed2(-1,-1));
				weight = weights1.w;
				fetches += weight * tex2D(textSampler, baseUv + texelSize * fixed2(0,-1));
				weight = weights2.x;
				fetches += weight * tex2D(textSampler, baseUv + texelSize * fixed2(1,-1));
				weight = weights2.y;
				fetches += weight * tex2D(textSampler, baseUv + texelSize * fixed2(2,-1));
				weight = weights2.z;
				fetches += weight * tex2D(textSampler, baseUv + texelSize * fixed2(-2,0));
				weight = weights2.w;
				fetches += weight * tex2D(textSampler, baseUv + texelSize * fixed2(-1,0));
				weight = weights3.x;
				fetches += weight * tex2D(textSampler, baseUv + texelSize * fixed2(0,0));
				weight = weights3.y;
				fetches += weight * tex2D(textSampler, baseUv + texelSize * fixed2(1,0));
				weight = weights3.z;
				fetches += weight * tex2D(textSampler, baseUv + texelSize * fixed2(2,0));
				weight = weights3.w;
				fetches += weight * tex2D(textSampler, baseUv + texelSize * fixed2(-2,1));
				weight = weights4.x;
				fetches += weight * tex2D(textSampler, baseUv + texelSize * fixed2(-1,1));
				weight = weights4.y;
				fetches += weight * tex2D(textSampler, baseUv + texelSize * fixed2(0,1));
				weight = weights4.z;
				fetches += weight * tex2D(textSampler, baseUv + texelSize * fixed2(1,1));
				weight = weights4.w;
				fetches += weight * tex2D(textSampler, baseUv + texelSize * fixed2(2,1));
				weight = weights5.x;
				fetches += weight * tex2D(textSampler, baseUv + texelSize * fixed2(-2,2));
				weight = weights5.y;
				fetches += weight * tex2D(textSampler, baseUv + texelSize * fixed2(-1,2));
				weight = weights5.z;
				fetches += weight * tex2D(textSampler, baseUv + texelSize * fixed2(0,2));
				weight = weights5.w;
				fetches += weight * tex2D(textSampler, baseUv + texelSize * fixed2(1,2));
				weight = weights6.x;
				fetches += weight * tex2D(textSampler, baseUv + texelSize * fixed2(2,2));
				fetches /= weights6.w;
				return fetches;
			}


			float4 frag(v2f_customrendertexture IN) : COLOR
			{
				half4 uv0 = float4(IN.localTexcoord.xyz,1.0);
				float4 Convolution_da4b8ee3_365e_467c_8166_39db9bd7c370_Output = unity_convolution_float (TextureAsset_a266f89a_2f3e_47a6_a8c0_5f05a62b6679_Uniform, uv0.xy, convolutionFilter0_da4b8ee3_365e_467c_8166_39db9bd7c370, convolutionFilter1_da4b8ee3_365e_467c_8166_39db9bd7c370, convolutionFilter2_da4b8ee3_365e_467c_8166_39db9bd7c370, convolutionFilter3_da4b8ee3_365e_467c_8166_39db9bd7c370, convolutionFilter4_da4b8ee3_365e_467c_8166_39db9bd7c370, convolutionFilter5_da4b8ee3_365e_467c_8166_39db9bd7c370, convolutionFilter6_da4b8ee3_365e_467c_8166_39db9bd7c370, TextureAsset_a266f89a_2f3e_47a6_a8c0_5f05a62b6679_Uniform_TexelSize.xy);
				return Convolution_da4b8ee3_365e_467c_8166_39db9bd7c370_Output;

			}
			ENDCG
		}
	}
	Fallback Off
}