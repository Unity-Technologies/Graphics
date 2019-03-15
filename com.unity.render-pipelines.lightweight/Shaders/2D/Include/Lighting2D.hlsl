#if USE_NORMAL_MAP
	#if LIGHT_QUALITY_FAST
		#define UNITY_2D_LIGHTING_COORDS(TEXCOORD) float4	lightDirection	: TEXCOORD;
		#define UNITY_2D_TRANSFER_LIGHTING(output, worldSpacePos)\
            output.lightDirection.xy = _LightPosition.xy - worldSpacePos.xy;\
			output.lightDirection.z = _LightZDistance;\
			output.lightDirection.w = 0;\
			output.lightDirection.xyz = normalize(output.lightDirection.xyz);
		#define UNITY_2D_APPLY_LIGHTING(lightColor)\
			half4 normal = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.screenUV);\
			float3 normalUnpacked = UnpackNormal(normal);\
			lightColor = lightColor * saturate(dot(input.lightDirection.xyz, normalUnpacked));
	#else
		#define UNITY_2D_LIGHTING_COORDS(TEXCOORD) float4	positionWS : TEXCOORD;
		#define UNITY_2D_TRANSFER_LIGHTING(output, worldSpacePos) output.positionWS = worldSpacePos;
		#define UNITY_2D_APPLY_LIGHTING(lightColor)\
			half4 normal = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.screenUV);\
			float3 normalUnpacked = UnpackNormal(normal);\
			float3 dirToLight;\
			dirToLight.xy = _LightPosition.xy - input.positionWS.xy;\
			dirToLight.z =  _LightZDistance;\
			dirToLight = normalize(dirToLight);\
			lightColor = lightColor * saturate(dot(dirToLight, normalUnpacked));
	#endif

	#define UNITY_2D_LIGHTING_VARIABLES			   float4	_LightPosition;
#else
	#define UNITY_2D_LIGHTING_COORDS(TEXCOORD)
	#define UNITY_2D_LIGHTING_VARIABLES
	#define UNITY_2D_TRANSFER_LIGHTING(output, worldSpacePos)
	#define UNITY_2D_APPLY_LIGHTING(lightColor)
#endif
