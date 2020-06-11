﻿#if ETC1_EXTERNAL_ALPHA
    TEXTURE2D(_AlphaTex); SAMPLER(sampler_AlphaTex);
    float _EnableAlphaTexture;
#endif

PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    output = BuildVaryings(input);
    PackedVaryings packedOutput = PackVaryings(output);
    return packedOutput;
}

half4 frag(PackedVaryings packedInput) : SV_TARGET 
{    
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);

    SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(unpacked);
    SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);

#if ETC1_EXTERNAL_ALPHA
    float4 alpha = SAMPLE_TEXTURE2D(_AlphaTex, sampler_AlphaTex, unpacked.texCoord0.xy);
    surfaceDescription.Alpha = lerp (surfaceDescription.Alpha, alpha.r, _EnableAlphaTexture);
#endif

#ifdef UNIVERSAL_USELEGACYSPRITEBLOCKS
    half4 color = surfaceDescription.SpriteColor;
#else
    half4 color = half4(surfaceDescription.BaseColor, surfaceDescription.Alpha);
#endif

    color *= unpacked.color;
    return color;
}
