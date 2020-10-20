Shader "Hidden/Shader/ChromaKeying"
{
    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone vulkan metal switch

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/FXAA.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/RTUpscale.hlsl"

    struct Attributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord   : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
        output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
        return output;
    }

    // List of properties to control your post process effect
    float3 _KeyColor;
	float4 _KeyParams;
    TEXTURE2D_X(_InputTexture);

	// RGB <-> YCgCo color space conversion
	float3 RGB2YCgCo(float3 rgb)
	{
		float3x3 m = {
			 0.25, 0.5,  0.25,
			-0.25, 0.5, -0.25,
			 0.50, 0.0, -0.50
		};
		return mul(m, rgb);
	}

	float3 YCgCo2RGB(float3 ycgco)
	{
		return float3(
			ycgco.x - ycgco.y + ycgco.z,
			ycgco.x + ycgco.y,
			ycgco.x - ycgco.y - ycgco.z
			);
	}

	// Adapted from https://github.com/keijiro/ProcAmp
	// Main difference is that we do the chroma keying in linear space (not gamma)
	float ChromaKeyAt(float3 keyColorYCoCg, float2 uv)
	{
		float3 rgb = LOAD_TEXTURE2D_X_LOD(_InputTexture, uv, 0).xyz;
		float3 inputColor = LinearToSRGB(rgb);

		float d = distance(RGB2YCgCo(inputColor).yz, keyColorYCoCg.yz) * 10 ;
		return smoothstep(_KeyParams.x, _KeyParams.x + _KeyParams.y, d);
	}

    float4 CustomPostProcess(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        uint2 positionSS = input.texcoord * _ScreenSize.xy;
        float3 outColor = LOAD_TEXTURE2D_X_LOD(_InputTexture, positionSS, 0).xyz;
		float3 keyColorYCoCg = RGB2YCgCo(_KeyColor);

		// Calculate keys for surrounding four points and get the minima of them.
		// This works like a blur and dilate filter.
		float4 duv = _ScreenSize.zwzw * float4(-0.5, -0.5, 0.5, 0.5);
		float alpha = ChromaKeyAt(keyColorYCoCg, positionSS + duv.xy);
		alpha = min(alpha, ChromaKeyAt(keyColorYCoCg, positionSS + duv.zy));
		alpha = min(alpha, ChromaKeyAt(keyColorYCoCg, positionSS + duv.xw));
		alpha = min(alpha, ChromaKeyAt(keyColorYCoCg, positionSS + duv.zw));

		if (_KeyParams.z > 0)
		{
			// Spill removal
			// What the following lines do is flattening the CgCo chroma values
			// so that dot(ycgco, _KeyCgCo) == 0.5. This shifts colors toward
			// the anticolor of the key color.
			outColor = RGB2YCgCo(LinearToSRGB(outColor));
			float sub = dot(keyColorYCoCg.yz, outColor.yz) / dot(keyColorYCoCg.yz, keyColorYCoCg.yz);
			outColor.yz -= keyColorYCoCg.yz * (sub + 0.5) * _KeyParams.z;
			outColor = SRGBToLinear(YCgCo2RGB(outColor));
		}

		return float4(outColor, alpha);
    }

    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "ChromaKeying"

            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment CustomPostProcess
                #pragma vertex Vert
            ENDHLSL
        }
    }
    Fallback Off
}
