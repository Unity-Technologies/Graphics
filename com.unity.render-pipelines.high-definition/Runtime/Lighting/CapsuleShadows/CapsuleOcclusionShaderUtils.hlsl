// This file is more a scratch pad for now with various functions we might use but don't have a home yet (or never will :) )

// http://iquilezles.org/www/articles/sphereao/sphereao.htm
float IQSphereAO(float3 positionWS, float3 normalWS, float3 sphereCenter, float sphereRadius)
{
    float3 posToSphere = sphereCenter - positionWS; // Or L in some var names for brevity
    float posToSphereLen = length(posToSphere);
    float3 posToSphereNorm = (posToSphere / posToSphereLen);

    float NdotPosToSphere = dot(normalWS, posToSphereNorm);

    float h = posToSphereLen / sphereRadius;
    float h2 = h * h;
    float NdotPosToSphere2 = NdotPosToSphere * NdotPosToSphere;
    float k2 = 1.0f - h2 * NdotPosToSphere2;

    float result = max(0.0f, NdotPosToSphere) / h2;

    if (k2 > 0.0f)
    {
#if 1
        // TODO: Test with fast acos. 
        result = NdotPosToSphere * acos(-NdotPosToSphere * sqrt((h2 - 1.0) / (1.0 - NdotPosToSphere2))) - sqrt(k2*(h2 - 1.0));
        result = result / h2 + atan(sqrt(k2 / (h2 - 1.0)));
        result /= PI;
#else
        // cheap approximation: Quilez
        result = PositivePow(saturate(0.5f * (NdotPosToSphere * h + 1.0f) / h2), 1.5f);
#endif
    }

    return result;
}
