#ifndef UNITY_LIGHT_TRANSMITTANCE_UTILS_INCLUDED
#define UNITY_LIGHT_TRANSMITTANCE_UTILS_INCLUDED

#define ENABLE_LIGHT_TRANSMITTANCE

TEXTURE3D_HALF(_VShadowMapBuffer);

void EvaluateLightTransmittance(float3 positionWS, float3 directionWS, float randomOffset, float length, uint step, inout float attenuation)
{
    float stepLength = length / (float)step;
    float sampleOffset = randomOffset * stepLength;
    
    float3  positionVSM = ( positionWS / _VShadowMapMag) + 0.5;
    float3 directionVSM = (directionWS / _VShadowMapMag);
    
    float transmittance = 1.0f;
    
    for (uint i = 0; i < step; i++)
    {
        // todo : extinction should have rgb channel?
        //        so absorption is needed to be 3-channels for that!
        
        float t = sampleOffset + stepLength * i;
        float3 uvw = positionVSM + t * directionVSM;
        float extinction = stepLength * SAMPLE_TEXTURE3D_LOD(_VShadowMapBuffer, s_linear_clamp_sampler, uvw, 0).x;
        //if (any(uvw > 1.0)) extinction = 0.0;
        //if (any(uvw < 0.0)) extinction = 0.0;
        
        transmittance *= exp(-extinction);
    }
    
    attenuation *= transmittance;
}

#endif // UNITY_LIGHT_TRANSMITTANCE_UTILS_INCLUDED
