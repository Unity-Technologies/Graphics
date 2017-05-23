//-------------------------------------------------------------------------------------
// Lighting Helpers

// Glossy Environment
half3 Unity_AnisotropicGlossyEnvironment(UNITY_ARGS_TEXCUBE(tex), half4 hdr, Unity_GlossyEnvironmentData glossIn, half anisotropy) //Reference IBL from HD Pipe (Add half3 L input and replace R)
{
	half perceptualRoughness = glossIn.roughness /* perceptualRoughness */;

	// TODO: CAUTION: remap from Morten may work only with offline convolution, see impact with runtime convolution!
	// For now disabled
#if 0
	float m = PerceptualRoughnessToRoughness(perceptualRoughness); // m is the real roughness parameter
	const float fEps = 1.192092896e-07F;        // smallest such that 1.0+FLT_EPSILON != 1.0  (+1e-4h is NOT good here. is visibly very wrong)
	float n = (2.0 / max(fEps, m*m)) - 2.0;        // remap to spec power. See eq. 21 in --> https://dl.dropboxusercontent.com/u/55891920/papers/mm_brdf.pdf

	n /= 4;                                     // remap from n_dot_h formulatino to n_dot_r. See section "Pre-convolved Cube Maps vs Path Tracers" --> https://s3.amazonaws.com/docs.knaldtech.com/knald/1.0.0/lys_power_drops.html

	perceptualRoughness = pow(2 / (n + 2), 0.25);      // remap back to square root of real roughness (0.25 include both the sqrt root of the conversion and sqrt for going from roughness to perceptualRoughness)
#else
	// MM: came up with a surprisingly close approximation to what the #if 0'ed out code above does.
	perceptualRoughness = perceptualRoughness*(1.7 - 0.7*perceptualRoughness);
#endif


	half mip = perceptualRoughnessToMipmapLevel(perceptualRoughness);
	half3 R = glossIn.reflUVW;// -half3(anisotropy, 0, 0);
	half4 rgbm = UNITY_SAMPLE_TEXCUBE_LOD(tex, R, mip);

	return DecodeHDR(rgbm, hdr);
}

// Indirect Specular
inline half3 UnityGI_AnisotropicIndirectSpecular(UnityGIInput data, half occlusion, Unity_GlossyEnvironmentData glossIn, half anisotropy, half3x3 worldVectors)
{
	half3 specular;
	float3 tangentX = worldVectors[0];
	float3 tangentY = worldVectors[1];
	float3 N = worldVectors[2];
	float3 V = data.worldViewDir;
	float3 iblNormalWS = GetAnisotropicModifiedNormal(tangentY, N, V, anisotropy);
	float3 iblR = reflect(-V, iblNormalWS);

#ifdef UNITY_SPECCUBE_BOX_PROJECTION
	// we will tweak reflUVW in glossIn directly (as we pass it to Unity_GlossyEnvironment twice for probe0 and probe1), so keep original to pass into BoxProjectedCubemapDirection

	half3 originalReflUVW = glossIn.reflUVW;
	glossIn.reflUVW = BoxProjectedCubemapDirection(iblR, data.worldPos, data.probePosition[0], data.boxMin[0], data.boxMax[0]);
#endif

#ifdef _GLOSSYREFLECTIONS_OFF
	specular = unity_IndirectSpecColor.rgb;
#else
	half3 env0 = Unity_AnisotropicGlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), data.probeHDR[0], glossIn, anisotropy);
	//half3 env0 = Unity_AnisotropicGlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), data.probeHDR[0], glossIn, anisotropy, L); //Reference IBL from HD Pipe
#ifdef UNITY_SPECCUBE_BLENDING
	const float kBlendFactor = 0.99999;
	float blendLerp = data.boxMin[0].w;
	UNITY_BRANCH
		if (blendLerp < kBlendFactor)
		{
#ifdef UNITY_SPECCUBE_BOX_PROJECTION
			glossIn.reflUVW = BoxProjectedCubemapDirection(iblR, data.worldPos, data.probePosition[1], data.boxMin[1], data.boxMax[1]);
#endif
			half3 env1 = Unity_AnisotropicGlossyEnvironment(UNITY_PASS_TEXCUBE_SAMPLER(unity_SpecCube1, unity_SpecCube0), data.probeHDR[1], glossIn, anisotropy);
			//half3 env1 = Unity_AnisotropicGlossyEnvironment(UNITY_PASS_TEXCUBE_SAMPLER(unity_SpecCube1, unity_SpecCube0), data.probeHDR[1], glossIn, anisotropy, L); //Reference IBL from HD Pipe
			specular = lerp(env1, env0, blendLerp);
		}
		else
		{
			specular = env0;
		}
#else
	specular = env0;
#endif
#endif

	return specular * occlusion;// *weightOverPdf; //Reference IBL from HD Pipe
								//return specular * occlusion * weightOverPdf; //Reference IBL from HD Pipe
}

// Global Illumination
inline UnityGI UnityAnisotropicGlobalIllumination(UnityGIInput data, half occlusion, half3 normalWorld, Unity_GlossyEnvironmentData glossIn, half anisotropy, half3x3 worldVectors)
{
	UnityGI o_gi = UnityGI_Base(data, occlusion, normalWorld);
	o_gi.indirect.specular = UnityGI_AnisotropicIndirectSpecular(data, occlusion, glossIn, anisotropy, worldVectors);
	return o_gi;
}

//-------------------------------------------------------------------------------------
// Lighting Functions

//Surface Description
struct SurfaceOutputAdvanced
{
	fixed3 Albedo;		// base (diffuse or specular) color
	fixed3 Normal;		// tangent space normal, if written
	half3 Emission;
	half Metallic;		// 0=non-metal, 1=metal
						// Smoothness is the user facing name, it should be perceptual smoothness but user should not have to deal with it.
						// Everywhere in the code you meet smoothness it is perceptual smoothness
	half Smoothness;	// 0=rough, 1=smooth
	half Occlusion;		// occlusion (default 1)
	fixed Alpha;		// alpha for transparencies
	half Anisotropy;
	half4 CustomData;
	float3x3 WorldVectors;
	//half ShadingModel;
};

inline half4 LightingAdvanced(SurfaceOutputAdvanced s, half3 viewDir, UnityGI gi)
{
	s.Normal = normalize(s.Normal);

	half oneMinusReflectivity;
	half3 specColor;
	s.Albedo = DiffuseAndSpecularFromMetallic(s.Albedo, s.Metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);

	// shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)
	// this is necessary to handle transparency in physically correct way - only diffuse component gets affected by alpha
	half outputAlpha;
	s.Albedo = PreMultiplyAlpha(s.Albedo, s.Alpha, oneMinusReflectivity, /*out*/ outputAlpha);

	half4 c = SurfaceShading(s.Albedo, specColor, oneMinusReflectivity, s.Smoothness, s.Normal, s.WorldVectors, s.Anisotropy, s.CustomData, s.Metallic, viewDir, gi.light, gi.indirect);
	c.rgb += SubsurfaceShading(s.Albedo, specColor, s.Normal, s.Smoothness, viewDir, s.CustomData, gi.light);

	//c.rgb += UNITY_BRDF_GI(s.Albedo, specColor, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, s.Occlusion, gi);
	c.a = outputAlpha;
	return c;
}

//This is pointless as always forward?
inline half4 LightingAdvanced_Deferred(SurfaceOutputAdvanced s, half3 viewDir, UnityGI gi, out half4 outGBuffer0, out half4 outGBuffer1, out half4 outGBuffer2)
{
	half oneMinusReflectivity;
	half3 specColor;
	s.Albedo = DiffuseAndSpecularFromMetallic(s.Albedo, s.Metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);

	half4 c = SurfaceShading(s.Albedo, specColor, oneMinusReflectivity, s.Smoothness, s.Normal, s.WorldVectors, s.Anisotropy, s.CustomData, s.Metallic, viewDir, gi.light, gi.indirect);
	c.rgb += SubsurfaceShading(s.Albedo, specColor, s.Normal, s.Smoothness, viewDir, s.CustomData, gi.light);

	UnityStandardData data;
	data.diffuseColor = s.Albedo;
	data.occlusion = s.Occlusion;
	data.specularColor = specColor;
	data.smoothness = s.Smoothness;
	data.normalWorld = s.Normal;

	UnityStandardDataToGbuffer(data, outGBuffer0, outGBuffer1, outGBuffer2);

	half4 emission = half4(s.Emission + c.rgb, 1);
	return emission;
}

inline void LightingAdvanced_GI(SurfaceOutputAdvanced s, UnityGIInput data, inout UnityGI gi)
{
#if defined(UNITY_PASS_DEFERRED) && UNITY_ENABLE_REFLECTION_BUFFERS
	gi = UnityGlobalIllumination(data, s.Occlusion, s.Normal);
#else
	Unity_GlossyEnvironmentData g = UnityGlossyEnvironmentSetup(s.Smoothness, data.worldViewDir, s.Normal, lerp(unity_ColorSpaceDielectricSpec.rgb, s.Albedo, s.Metallic));
	gi = UnityAnisotropicGlobalIllumination(data, s.Occlusion, s.Normal, g, s.Anisotropy, s.WorldVectors);
#endif
}