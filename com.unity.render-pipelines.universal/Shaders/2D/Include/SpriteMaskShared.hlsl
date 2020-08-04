// alpha below which a mask should discard a pixel, thereby preventing the stencil buffer from being marked with the Mask's presence
half  _Cutoff;
half4 _Color;
half4 _RendererColor;
half2 _Flip;

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

struct appdata_masking
{
    float4 vertex : POSITION;
    half2 texcoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f_masking
{
    float4 pos : SV_POSITION;
    half2 uv : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};


v2f_masking vert(appdata_masking IN)
{
    v2f_masking OUT;

    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

    OUT.pos = TransformObjectToHClip(IN.vertex);
    OUT.uv = IN.texcoord;

    return OUT;
}

half4 frag(v2f_masking IN) : SV_Target
{
    half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
    // for masks: discard pixel if alpha falls below MaskingCutoff
    clip(c.a - _Cutoff);
    return _Color;
}

