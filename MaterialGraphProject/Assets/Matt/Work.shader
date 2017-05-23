Shader "Work" {
	Properties 
	{
		// Shading model
		[Enum(Unlit, Standard, Subsurface, Skin, Foliage, Clearcoat, Cloth, Eye, Hair)] _ShadingModel("Shading model", Float) = 0

		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo", 2D) = "white" {}

		_Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
		
		[Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
		_MetallicGlossMap("Metallic", 2D) = "white" {}

		_BumpMap("Normal Map", 2D) = "bump" {}

		_OcclusionMap("Occlusion", 2D) = "white" {}

		_EmissionColor("Color", Color) = (0,0,0)
		_EmissionMap("Emission", 2D) = "white" {}

		// Anisotropy
		_Anisotropy("Anisotropy", Range(0,1)) = 0.0
		_AnisotropyMap("Anisotropy", 2D) = "white" {}
		_TangentMap("Tangent", 2D) = "white" {}

		// Subsurface Scattering
		_TranslucencyMap("Translucency Map", 2D) = "white" {}
		_TranslucentColor("Color", Color) = (0,0,0)
		_TDistortion("Transmission Distortion", Range(-10, 10)) = 2.0
		_TScale("Transmission Scale", Range(0, 10)) = 1.5
		_TAmbient("Transmission Ambient", Range(0, 10)) = 1.7
		_TPower("Transmission Power", Range(1, 10)) = 3.5
		_TAttenuation("Transmission Fake Attenuation", Range(0, 10)) = 1.0
		_TransmissionOverallStrength("Transmission Overall Strength", Range(0, 1)) = 1.0

		// Cloth
		_FuzzTex("Fuzz Map", 2D) = "white" {}
		_FuzzColor("Fuzz", Color) = (0,0,0)
		_Cloth("Cloth Factor", Range(0, 1)) = 0

		//Eye
		_IrisNormal("Iris Normal (RGB)", 2D) = "black" {}
		_IrisMask("Iris Mask (R)", 2D) = "black" {}
		_IrisDistance("Iris Distance", Range(0, 1)) = 0.5
	}
	SubShader 
	{
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM

		//#define UNITY_BRDF_PBS BRDF_Unity_Anisotropic
		#include "UnityCG.cginc"
		#include "AdvancedBRDF.cginc"
		#include "AdvancedShading.cginc"
		#include "AdvancedLighting.cginc"
		#pragma target 5.0
		
		
		#pragma surface SurfaceAdvanced Advanced vertex:vert fullforwardshadows

		sampler2D	_MainTex;

		struct Input 
		{
			float2 uv_MainTex;
			float3 normal;
			float3 viewDir;
			float3 normalDir;
			float3 tangentDir;
			float3 bitangentDir;
		};

		//Vertex shader
		void vert(inout appdata_full v, out Input o)
		{
			UNITY_INITIALIZE_OUTPUT(Input, o);
			//Normal 2 World
			o.normalDir = normalize(UnityObjectToWorldNormal(v.normal));
			//Tangent 2 World
			float3 tangentMul = normalize(mul(unity_ObjectToWorld, v.tangent.xyz));
			o.tangentDir = float4(tangentMul, v.tangent.w);
			// Bitangent
			o.bitangentDir = cross(o.normalDir, o.tangentDir);
		}

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		UNITY_INSTANCING_CBUFFER_START(Props)
		UNITY_INSTANCING_CBUFFER_END

		void SurfaceAdvanced(Input IN, inout SurfaceOutputAdvanced o)
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
			o.Anisotropy = tex2D(_AnisotropyMap, IN.uv_MainTex) * _Anisotropy;

			float4 custom;
			if (_ShadingModel == 2 /*Subsurface*/)
			{
				custom = float4(Translucency(IN.uv_MainTex),0);
			}
			else if (_ShadingModel == 6 /*Cloth*/)
			{
				custom = float4(Fuzz(IN.uv_MainTex), Cloth());
			}
			else if (_ShadingModel == 7 /*Eye*/)
			{
				custom = Iris(IN.uv_MainTex);
			}
			o.CustomData = custom;

			float3 tangentTS = tex2D(_TangentMap, IN.uv_MainTex);
			float3 fTangent;
			if (tangentTS.z < 1) // TODO - Clean this
			{
				float3x3 worldToTangent;
				worldToTangent[0] = float3(1, 0, 0);
				worldToTangent[1] = float3(0, 1, 0);
				worldToTangent[2] = float3(0, 0, 1);
				float3 tangentTWS = mul(tangentTS, worldToTangent);
				fTangent = tangentTWS;
			}
			else
				fTangent = IN.tangentDir;
			o.WorldVectors = float3x3(fTangent, IN.bitangentDir, IN.normalDir);
			//o.ShadingModel = _ShadingModel;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
