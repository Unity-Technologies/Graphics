Shader "UnityEngine.MaterialGraph.ExportTextureMasterNode31754f39-6eb9-4773-ae21-b2fca7f2fcde"
{
	Properties
	{

	}

	SubShader
	{
		Tags{ "Queue" = "Geometry" "IgnoreProjector" = "True" "RenderType" = "Opaque" }

		ZWrite Off
		Blend One Zero

		Pass
	{
		CGPROGRAM
#include "UnityCustomRenderTexture.cginc"
#pragma vertex CustomRenderTextureVertexShader
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



	inline void unity_Gradient_float(float v, out float4 finalColor, out float finalR, out float finalG, out float finalB, out float finalA)
	{
		float3 color0 = float3(0.1176471,0.08960744,0.07439446);
		float colorp0 = 0;
		float3 color1 = float3(0,0,0);
		float colorp1 = 0.1147021;
		float3 color2 = float3(0.2132353,0.07766853,0);
		float colorp2 = 0.2500038;
		float3 color3 = float3(0.6364486,0.2844907,0);
		float colorp3 = 0.3764706;
		float3 color4 = float3(1,0.9310344,0);
		float colorp4 = 0.5058824;
		float3 color5 = float3(1,1,1);
		float colorp5 = 0.6529488;
		float3 color6 = float3(1,0.6413793,0);
		float colorp6 = 0.7823606;
		float3 color7 = float3(0.4779412,0.353851,0.02811421);
		float colorp7 = 1;
		float3 gradcolor = color0;
		float colorLerpPosition0 = smoothstep(colorp0,colorp1,v);
		gradcolor = lerp(gradcolor,color1,colorLerpPosition0);
		float colorLerpPosition1 = smoothstep(colorp1,colorp2,v);
		gradcolor = lerp(gradcolor,color2,colorLerpPosition1);
		float colorLerpPosition2 = smoothstep(colorp2,colorp3,v);
		gradcolor = lerp(gradcolor,color3,colorLerpPosition2);
		float colorLerpPosition3 = smoothstep(colorp3,colorp4,v);
		gradcolor = lerp(gradcolor,color4,colorLerpPosition3);
		float colorLerpPosition4 = smoothstep(colorp4,colorp5,v);
		gradcolor = lerp(gradcolor,color5,colorLerpPosition4);
		float colorLerpPosition5 = smoothstep(colorp5,colorp6,v);
		gradcolor = lerp(gradcolor,color6,colorLerpPosition5);
		float colorLerpPosition6 = smoothstep(colorp6,colorp7,v);
		gradcolor = lerp(gradcolor,color7,colorLerpPosition6);
		float alpha0 = 1;
		float alphap0 = 0;
		float alpha1 = 1;
		float alphap1 = 1;
		float gradalpha = alpha0;
		float alphaLerpPosition0 = smoothstep(alphap0,alphap1,v);
		gradalpha = lerp(gradalpha,alpha1,alphaLerpPosition0);
		finalColor = float4(gradcolor,gradalpha);
		finalR = finalColor.r;
		finalG = finalColor.g;
		finalB = finalColor.b;
		finalA = finalColor.a;
	}


	float4 frag(v2f_customrendertexture IN) : COLOR
	{
		half4 uv0 = float4(IN.localTexcoord.xyz,1.0);
		float4 UV_49a5272a_c49d_4f7e_94ba_3a2e27fcbed4_UV = uv0;
		float4 Gradient_9866089f_0763_409d_904a_c1f7836ea742_finalColor;
		float Gradient_9866089f_0763_409d_904a_c1f7836ea742_finalR;
		float Gradient_9866089f_0763_409d_904a_c1f7836ea742_finalG;
		float Gradient_9866089f_0763_409d_904a_c1f7836ea742_finalB;
		float Gradient_9866089f_0763_409d_904a_c1f7836ea742_finalA;
		unity_Gradient_float(UV_49a5272a_c49d_4f7e_94ba_3a2e27fcbed4_UV, Gradient_9866089f_0763_409d_904a_c1f7836ea742_finalColor, Gradient_9866089f_0763_409d_904a_c1f7836ea742_finalR, Gradient_9866089f_0763_409d_904a_c1f7836ea742_finalG, Gradient_9866089f_0763_409d_904a_c1f7836ea742_finalB, Gradient_9866089f_0763_409d_904a_c1f7836ea742_finalA);
		return Gradient_9866089f_0763_409d_904a_c1f7836ea742_finalColor;

	}
		ENDCG
	}
	}
		Fallback Off
}