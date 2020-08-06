// alpha below which a mask should discard a pixel, thereby preventing the stencil buffer from being marked with the Mask's presence
half  _Cutoff;

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

struct Attributes
{
    float4 positionOS : POSITION;
    half2  texcoord : TEXCOORD0;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    half2  uv : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};


Varyings vert(Attributes input)
{
    v2f_masking OUT;

    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

    OUT.positionCS = TransformObjectToHClip(attributes.positionOS);
    OUT.uv = v.texcoord;

    return OUT;
}

half4 frag(Varyings input) : SV_Target
{
    half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
    // for masks: discard pixel if alpha falls below MaskingCutoff
    clip(c.a - _Cutoff);
    return 0;
}

