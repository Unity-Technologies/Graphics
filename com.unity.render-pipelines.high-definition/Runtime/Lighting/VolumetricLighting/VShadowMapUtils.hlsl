#ifndef UNITY_VOLUMETRIC_SHADOWMAP_UTILS_INCLUDED
#define UNITY_VOLUMETRIC_SHADOWMAP_UTILS_INCLUDED

TEXTURE3D_FLOAT(_VShadowMapBuffer);

void EvaluateTransmittance_Directional(float3 positionWS, float3 directionWS, float length, uint step, inout float attenuation)
{
    float stepLength = length / (float)step;
    
    float3  positionVSM = ( positionWS / _VShadowMapMag) + 0.5;
    float3 directionVSM = (directionWS / _VShadowMapMag);
    
    float transmittance = 0.0f;
    
    for (uint i = 0; i < step; i++)
    {
        // todo : extinction should have rgb channel?
        //        so absorption is needed to be 3-channels for that!
        
        float  t = stepLength * i;
        float3 uvw = positionVSM + t * directionVSM;
        float  vsm = SAMPLE_TEXTURE3D_LOD(_VShadowMapBuffer, s_linear_clamp_sampler, uvw, 0).x;
        
        transmittance += exp(-vsm * stepLength);
    }
    
    attenuation *= (transmittance / (float)step);
}

#endif // UNITY_VOLUMETRIC_SHADOWMAP_UTILS_INCLUDED
