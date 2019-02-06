#define USE_RAY_CONE_LOD
float computeTextureLOD(Texture2D targetTexture, float4 uvMask, float3 viewWS, float3 normalWS, RayCone rayCone, IntersectionVertice intersectionVertice)
{
    // First of all we need to grab the dimensions of the target texture
    uint texWidth, texHeight, numMips;
    targetTexture.GetDimensions(0, texWidth, texHeight, numMips);

    // Fetch the target area based on the mask
    float targetTexcoordArea = uvMask.x * intersectionVertice.texCoord0Area 
                        + uvMask.y * intersectionVertice.texCoord1Area
                        + uvMask.z * intersectionVertice.texCoord2Area
                        + uvMask.w * intersectionVertice.texCoord3Area;

    // Compute dot product between view and surface normal
    float lambda = 0.0; //0.5f * log2(targetTexcoordArea / intersectionVertice.triangleArea);
    lambda += log2(abs(rayCone.width));
    lambda += 0.5 * log2(texWidth * texHeight);
    lambda -= log2(abs(dot(viewWS, normalWS)));
    return lambda;
}