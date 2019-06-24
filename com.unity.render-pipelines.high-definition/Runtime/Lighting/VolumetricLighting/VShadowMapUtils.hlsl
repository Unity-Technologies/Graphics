#ifndef UNITY_VOLUMETRIC_SHADOWMAP_UTILS_INCLUDED
#define UNITY_VOLUMETRIC_SHADOWMAP_UTILS_INCLUDED

TEXTURE3D(_VShadowMapBuffer);

void EvaluateVolumetricAttenuation_Directional(float3 positionWS, float3 directionWS, uint step, out float vAttenuation)
{
    float length = 12.0;
    
    float3  positionVSM = ( positionWS / _VShadowMapMag) + 0.5;
    float3 directionVSM = (directionWS / _VShadowMapMag);
    
    float extinction = 0.0f;
    
    for (uint i = 0; i < step; i++)
    {
        // todo : extinction should have rgb channel?
        //        so absorption is needed to be 3channel for that!
        
        float3 uvw = positionVSM + length * ((float)i * directionVSM);
        float  vsm = SAMPLE_TEXTURE3D_LOD(_VShadowMapBuffer, s_linear_clamp_sampler, uvw, 0).x;
        
        extinction += exp(-vsm);
    }
    
    vAttenuation = extinction / (float)step;
}

#endif // UNITY_VOLUMETRIC_SHADOWMAP_UTILS_INCLUDED
