// Area light textures
TEXTURE2D_ARRAY(_LtcData); // We pack all Ltc data inside one texture array to limit the number of resource used

#define LTC_GGX_MATRIX_INDEX 0 // RGBA
#define LTC_DISNEY_DIFFUSE_MATRIX_INDEX 1 // RGBA

#define LTC_LUT_SIZE   64
#define LTC_LUT_SCALE  ((LTC_LUT_SIZE - 1) * rcp(LTC_LUT_SIZE))
#define LTC_LUT_OFFSET (0.5 * rcp(LTC_LUT_SIZE))


// BMAYAUX (18/07/09) Additional BRDFs, tables are stored with new encoding (i.e. U=Perceptual roughness, V=sqrt( 1 - N.V ))
#define LTC_MATRIX_INDEX_GGX            2
#define LTC_MATRIX_INDEX_DISNEY         3
#define LTC_MATRIX_INDEX_COOK_TORRANCE  4
#define LTC_MATRIX_INDEX_CHARLIE        5
#define LTC_MATRIX_INDEX_WARD           6
#define LTC_MATRIX_INDEX_OREN_NAYAR     7



// BMAYAUX (18/07/04) Added sampling helpers + their new version

////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Former tables
// U = Perceptual roughness
// V = acos(N.V) / (PI/2)   <== Expensive!
//
// Texture contains XYZW = { m00, m20, m11, m02 } coefficients of the M^-1 matrix. All other coefficients except m22=1 are assumed to be 0
// Note we load the matrix transposed (to avoid having to transpose it in shader) (so use it as mul( point, invM )
//

// Expects NdotV clamped in [0,1]
float2  LTCGetSamplingUV(float NdotV, float perceptualRoughness)
{
// Original code
//    return LTC_LUT_OFFSET + LTC_LUT_SCALE * float2( perceptualRoughness, theta * INV_HALF_PI );

    float2  xy;
    xy.x = perceptualRoughness;
    xy.y = FastACosPos(NdotV) * INV_HALF_PI;

    xy *= (LTC_LUT_SIZE-1);     // 0 is pixel 0, 1 = last pixel in the table
    xy += 0.5;                  // Perfect pixel sampling starts at the center
    return xy / LTC_LUT_SIZE;   // Finally, return UVs in [0,1]
}

// Fetches the transposed M^-1 matrix need for runtime LTC estimate
float3x3    LTCSampleMatrix(float2 UV, uint BRDFIndex)
{
    float3x3    invM = 0.0;
                invM._m22 = 1.0;
                invM._m00_m02_m11_m20 = SAMPLE_TEXTURE2D_ARRAY_LOD( _LtcData, s_linear_clamp_sampler, UV, BRDFIndex, 0 );

    return invM;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////
// New tables
// U = Perceptual roughness
// V = sqrt( 1 - N.V )      <== Cheaper
//
// Texture contains XYZW = { m00, m20, m02, m22 } coefficients of the M^-1 matrix. All other coefficients except m11=1 are assumed to be 0
// Note we load the matrix transposed (to avoid having to transpose it in shader) (so use it as mul( point, invM )
//

// Expects NdotV clamped in [0,1]
float2  LTCGetSamplingUV_New(float NdotV, float perceptualRoughness)
{
    float2  xy;
    xy.x = perceptualRoughness;
    xy.y = sqrt( 1 - NdotV );                   // Now, we use V = sqrt( 1 - cos(theta) ) which is kind of linear and only requires a single sqrt() instead of an expensive acos()

    xy *= (LTC_LUT_SIZE-1);     // 0 is pixel 0, 1 = last pixel in the table
    xy += 0.5;                  // Perfect pixel sampling starts at the center
    return xy / LTC_LUT_SIZE;   // Finally, return UVs in [0,1]
}

// Fetches the transposed M^-1 matrix need for runtime LTC estimate
// Note we load the matrix transposed (to avoid having to transpose it in shader)
float3x3    LTCSampleMatrix_New(float2 UV, uint BRDFIndex)
{
    float3x3    invM = 0.0;
                invM._m11 = 1.0;
                invM._m00_m02_m20_m22 = SAMPLE_TEXTURE2D_ARRAY_LOD( _LtcData, s_linear_clamp_sampler, UV, BRDFIndex, 0 );

    return invM;
}
