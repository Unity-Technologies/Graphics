Shader "Hidden/DistordFromTex"
{
	Properties
	{
        _Tex("InputTex", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
            #include "UnityCustomRenderTexture.cginc"
            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 3.0

            sampler2D   _Tex;

            float4 frag(v2f_customrendertexture IN) : COLOR
            {
                float4 c = float4(0,0,0,0);

                c.rgb = tex2D(_Tex, IN.localTexcoord.xy).rgb;
                c.rg = c.rg * 2 - 1;
				
                return c;
            }
			ENDCG
		}
	}
}
