TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

half4 _Color;

CBUFFER_START(UnityPerDrawSprite)
#ifndef UNITY_INSTANCING_ENABLED
half4 _RendererColor;
half2 _Flip;
#endif
float _EnableExternalAlpha;
CBUFFER_END

//#if ETC1_EXTERNAL_ALPHA
//bool  _EnableExternalAlpha;
//TEXTURE2D(_AlphaTex)
//SAMPLER(sampler_AlphaTex)
//#endif

half4 SampleSpriteTexture(float2 uv)
{
    half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

    //#if ETC1_EXTERNAL_ALPHA
    //    half4 alpha = SAMPLE_TEXTURE2D(_AlphaTex, sampler_AlphaTex, uv);
    //    color.a = lerp(color.a, alpha.r, _EnableExternalAlpha);
    //#endif

    return color;
}


float4 UnityPixelSnap(float4 pos)
{
    float2 hpc = _ScreenParams.xy * 0.5f;
#if  SHADER_API_PSSL
    // An old sdk used to implement round() as floor(x+0.5) current sdks use the round to even method so we manually use the old method here for compatabilty.
    float2 temp = ((pos.xy / pos.w) * hpc) + float2(0.5f, 0.5f);
    float2 pixelPos = float2(floor(temp.x), floor(temp.y));
#else
    float2 pixelPos = round((pos.xy / pos.w) * hpc);
#endif
    pos.xy = pixelPos / hpc * pos.w;
    return pos;
}



