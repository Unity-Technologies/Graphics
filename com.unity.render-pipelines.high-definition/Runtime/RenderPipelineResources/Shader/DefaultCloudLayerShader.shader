Shader "Hidden/DefaultCloudLayer"
{
	Properties
	{
		[NoScaleOffset]
		_Tex("Cloud Texture", 2D) = "white" {}

		Opacity_R("Cumulus Opacity", Range(0,1)) = 1
		Opacity_G("Stratus Opacity", Range(0,1)) = 0.25
		Opacity_B("Cirrus Opacity", Range(0,1)) = 1
		Opacity_A("Wispy Opacity", Range(0,1)) = 0.1
	}

	SubShader
    {
        Lighting Off
        Blend One Zero

        Pass
        {
            CGPROGRAM

            #include "UnityCustomRenderTexture.cginc"
            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 3.0

            sampler2D _Tex;
            float Opacity_R;
            float Opacity_G;
            float Opacity_B;
            float Opacity_A;

            float4 frag(v2f_customrendertexture IN) : COLOR
            {               
                float2 UV = float2 (IN.localTexcoord.x, IN.localTexcoord.y);
                float4 opacity = float4(Opacity_R, Opacity_G, Opacity_B, Opacity_A);
                float4 clouds = tex2D(_Tex, UV) * opacity;

                return max(max(clouds.r, clouds.g), max(clouds.b, clouds.a));
            }

            ENDCG
        }
    }
    Fallback Off
}
