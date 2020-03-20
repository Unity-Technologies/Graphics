//-----------------------------------------------------------------------------
// Cookie sampling functions
// ----------------------------------------------------------------------------

#define COOKIE_ATLAS_SIZE _CookieAtlasData.x
#define COOKIE_ATLAS_RCP_PADDING _CookieAtlasData.y
#define COOKIE_ATLAS_LAST_VALID_MIP _CookieAtlasData.z

// Adjust UVs using the LOD padding size
float2 RemapUVWithPadding(float2 coord, float2 size, float rcpPaddingWidth, float lod)
{
    float2 scale  = rcp(size + rcpPaddingWidth) * size;
    float2 offset = 0.5 * (1.0 - scale);

    // Avoid edge bleeding for texture when sampling with lod by clamping uvs:
    float2 mipClamp = pow(2, lod) / (size * COOKIE_ATLAS_SIZE * 2);
    return clamp(coord * scale + offset, mipClamp, 1 - mipClamp);
}

// Used by directional, spots and area lights.
float3 SampleCookie2D(float2 coord, float4 scaleOffset, float lod = 0) // TODO: mip maps for cookies
{
    float2 scale    = scaleOffset.xy;
    float2 offset   = scaleOffset.zw;

    // Remap the uv to take in account the padding
    coord = RemapUVWithPadding(coord, scale, COOKIE_ATLAS_RCP_PADDING, max(0, lod - COOKIE_ATLAS_LAST_VALID_MIP));

    // Apply atlas scale and offset
    float2 atlasCoords = coord * scale + offset;

    float3 color = SAMPLE_TEXTURE2D_LOD(_CookieAtlas, s_trilinear_clamp_sampler, atlasCoords, lod).rgb;

    // Mip visualization (0 -> red, 10 -> blue)
    // color = saturate(1 - abs(3 * lod / 10 - float4(0, 1, 2, 3))).rgb;

    return color;
}

// Used by point lights.
float3 SampleCookieCube(float3 coord, int index)
{
    // TODO: add MIP maps to combat aliasing?
    return SAMPLE_TEXTURECUBE_ARRAY_LOD_ABSTRACT(_CookieCubeTextures, s_linear_clamp_sampler, coord, index, 0).rgb;
}

// From normalized vector
real2 CartesianToSpherical(float3 v)
{
    return real2(FastATan2(v.z, sqrt(saturate(dot(v.xy, v.xy)))), FastATan2(v.x, v.y));
}

// From normalized vector
real2 CartesianToSphericalUV(float3 v)
{
    float2 thetaPhi = CartesianToSpherical(v);

    return float2(saturate(thetaPhi.x * INV_HALF_PI), saturate(0.5f * thetaPhi.y * INV_PI + 0.5f));
}

float2 DirectionToSphericalTexCoordinate(float3 dir_in) // use this for the lookup
{
    float3 dir = normalize(dir_in);
    // coordinate frame is (-Z,X) meaning negative Z is primary axis and X is secondary axis.
    float recipPi = 1.0 / 3.1415926535897932384626433832795;
    return float2(1.0 - 0.5 * recipPi * atan2(dir.x, -dir.z), asin(dir.y) * recipPi + 0.5);
}

// Used by point lights.
float3 SamplePointCookie(float3 dir, float4 scaleOffset, float lod = 0)
{
    //float2 scale  = scaleOffset.xy;
    //float2 offset = scaleOffset.zw;
    //
    ////float TransformGLtoDX(CartesianToSpherical(dir))
    //float coord = CartesianToSphericalUV(dir);
    //
    //// Remap the uv to take in account the padding
    //coord = RemapUVWithPadding(coord, scale, COOKIE_ATLAS_RCP_PADDING, max(0, lod - COOKIE_ATLAS_LAST_VALID_MIP));
    //
    //// Apply atlas scale and offset
    //float2 atlasCoords = coord * scale + offset;
    //
    //float3 color = SAMPLE_TEXTURE2D_LOD(_CookieAtlas, s_trilinear_clamp_sampler, atlasCoords, lod).rgb;
    //
    //// Mip visualization (0 -> red, 10 -> blue)
    //// color = saturate(1 - abs(3 * lod / 10 - float4(0, 1, 2, 3))).rgb;
    //
    //return color;
    //float2 coord = CartesianToSphericalUV(TransformGLtoDX(dir));
    float2 coord = DirectionToSphericalTexCoordinate(dir);
    return SampleCookie2D(coord, scaleOffset, lod);
}
