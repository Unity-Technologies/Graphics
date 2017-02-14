// Ref: https://gist.github.com/TheRealMJP/c83b8c0f46b63f3a88a5986f4fa982b1 from MJP
// Samples a texture with Catmull-Rom filtering, using 9 texture fetches instead of 16.
// See http://vec3.ca/bicubic-filtering-in-fewer-taps/ for more details
float4 SampleTextureCatmullRom(TEXTURE2D_ARGS(tex, linearSampler), float2 uv, float2 texSize)
{
    // We're going to sample a a 4x4 grid of texels surrounding the target UV coordinate. We'll do this by rounding
    // down the sample location to get the exact center of our "starting" texel. The starting texel will be at
    // location [1, 1] in the grid, where [0, 0] is the top left corner.
    float2 samplePos = uv * texSize;
    float2 texPos1 = floor(samplePos - 0.5f) + 0.5f;

    // Compute the fractional offset from our starting texel to our original sample location, which we'll
    // feed into the Catmull-Rom spline function to get our filter weights.
    float2 f = samplePos - texPos1;
    float2 f2 = f * f;
    float2 f3 = f2 * f;

    // Compute the Catmull-Rom weights using the fractional offset that we calculated earlier.
    // These equations are pre-expanded based on our knowledge of where the texels will be located,
    // which lets us avoid having to evaluate a piece-wise function.
    float2 w0 = (1.0f / 6.0) * (-3.0 * f3 + 6.0 * f2 - 3.0 * f);
    float2 w1 = (1.0f / 6.0) * (9.0 * f3 - 15.0 * f2 + 6.0);
    float2 w2 = (1.0f / 6.0) * (-9.0 * f3 + 12.0 * f2 + 3.0 * f);
    float2 w3 = (1.0f / 6.0) * (3.0 * f3 - 3.0 * f2);

    // Otim by Vlad, to test
    // float2 w0 = (1.0 / 2.0) * f * (-1.0 + f * (2.0 - f));
    // float2 w1 = (1.0 / 6.0) * f2 * (-15.0 + 9.0 * f)) + 1.0;
    // float2 w2 = (1.0 / 6.0) * f * (3.0 + f * (12.0 - f * 9.0));
    // float2 w3 = (1.0 / 2.0) * f2 * (f - 1.0);

    // Work out weighting factors and sampling offsets that will let us use bilinear filtering to
    // simultaneously evaluate the middle 2 samples from the 4x4 grid.
    float2 w12 = w1 + w2;
    float2 offset12 = w2 / (w1 + w2);

    // Compute the final UV coordinates we'll use for sampling the texture
    float2 texPos0 = texPos1 - 1;
    float2 texPos3 = texPos1 + 2;
    float2 texPos12 = texPos1 + offset12;

    texPos0 /= texSize;
    texPos3 /= texSize;
    texPos12 /= texSize;

    float4 result = 0.0;
    result += TEXTURE2D_SAMPLE_LOD(tex, linearSampler, float2(texPos0.x, texPos0.y), 0.0) * w0.x * w0.y;
    result += TEXTURE2D_SAMPLE_LOD(tex, linearSampler, float2(texPos12.x, texPos0.y), 0.0) * w12.x * w0.y;
    result += TEXTURE2D_SAMPLE_LOD(tex, linearSampler, float2(texPos3.x, texPos0.y), 0.0) * w3.x * w0.y;

    result += TEXTURE2D_SAMPLE_LOD(tex, linearSampler, float2(texPos0.x, texPos12.y), 0.0) * w0.x * w12.y;
    result += TEXTURE2D_SAMPLE_LOD(tex, linearSampler, float2(texPos12.x, texPos12.y), 0.0) * w12.x * w12.y;
    result += TEXTURE2D_SAMPLE_LOD(tex, linearSampler, float2(texPos3.x, texPos12.y), 0.0) * w3.x * w12.y;

    result += TEXTURE2D_SAMPLE_LOD(tex, linearSampler, float2(texPos0.x, texPos3.y), 0.0) * w0.x * w3.y;
    result += TEXTURE2D_SAMPLE_LOD(tex, linearSampler, float2(texPos12.x, texPos3.y), 0.0) * w12.x * w3.y;
    result += TEXTURE2D_SAMPLE_LOD(tex, linearSampler, float2(texPos3.x, texPos3.y), 0.0) * w3.x * w3.y;

    return result;
}

/*

// manual tri-linearly interpolated texture fetch
// not really needed: used hard-wired texture interpolation
vec4 manualTexture3D( sampler3D samp, vec3 p ){
vec3 qa = p*uvMapSize + vec3(0.5);
vec3 qi = floor(qa);
qa -= qi;
qi -= vec3(0.5);
return
mix( mix( mix( texture3D( samp, (qi+vec3(0.0,0.0,0.0))*oneOverUvMapSize ),
texture3D( samp, (qi+vec3(1.0,0.0,0.0))*oneOverUvMapSize ), qa.x ),
mix( texture3D( samp, (qi+vec3(0.0,1.0,0.0))*oneOverUvMapSize ),
texture3D( samp, (qi+vec3(1.0,1.0,0.0))*oneOverUvMapSize ), qa.x ), qa.y ),
mix( mix( texture3D( samp, (qi+vec3(0.0,0.0,1.0))*oneOverUvMapSize ),
texture3D( samp, (qi+vec3(1.0,0.0,1.0))*oneOverUvMapSize ), qa.x ),
mix( texture3D( samp, (qi+vec3(0.0,1.0,1.0))*oneOverUvMapSize ),
texture3D( samp, (qi+vec3(1.0,1.0,1.0))*oneOverUvMapSize ), qa.x ), qa.y ), qa.z );

}
*/
