Shader "Custom/LightprobeNodeShader"
{
	Properties 
	{

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


		float3 ReflectionProbe_4f8cb744_19c5_4a6e_9bc1_172200f2fa25_normalDir;


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
			o.Emission = ReflectionProbe_4f8cb744_19c5_4a6e_9bc1_172200f2fa25;

	}
	ENDCG
}


	FallBack "Diffuse"
	CustomEditor "LegacyIlluminShaderGUI"
}
