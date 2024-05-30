void PseudoSubsurface_half (half3 WorldPosition, half3 WorldNormal, half SSRadius, half ShadowResponse, out half3 ssAmount)
{

#ifdef SHADERGRAPH_PREVIEW 
	half3 color = half3(0,0,0);
	half atten = 1;
	half3 dir = half3 (0.707, 0, 0.707);
	
#else
	#if defined(UNIVERSAL_PIPELINE_CORE_INCLUDED)
		half4 shadowCoord = TransformWorldToShadowCoord(WorldPosition);
		Light mainLight = GetMainLight(shadowCoord);
		half3 color = mainLight.color;
		half atten = mainLight.shadowAttenuation;
		half3 dir = mainLight.direction;
	#else
		half3 color = half3(0, 0, 0);
		half atten = 1;
		half3 dir = half3 (0.707, 0, 0.707);
	#endif
	
#endif

    half NdotL = dot(WorldNormal, -1 * dir);
    half alpha = SSRadius;
    half theta_m = acos(-alpha); // boundary of the lighting function

    half theta = max(0, NdotL + alpha) - alpha;
    half normalizer = (2 + alpha) / (2 * (1 + alpha));
    half wrapped  = (pow(abs((theta + alpha) / (1 + alpha)), 1 + alpha)) * normalizer;
	half shadow = lerp (1, atten, ShadowResponse);
    ssAmount = abs(color * shadow  * wrapped);

}