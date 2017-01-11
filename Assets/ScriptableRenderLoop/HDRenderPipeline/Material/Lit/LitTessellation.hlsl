/*
float _Tess;
float _TessNear;
float _TessFar;
float _UseDisplacementfalloff;
float _DisplacementfalloffNear;
float _DisplacementfalloffFar;
*/

float4 TesselationEdge(float3 p0, float3 p1, float3 p2, float3 n0, float3 n1, float3 n2)
{
    if (_TessellationFactorFixed >= 0.0f)
    {
        return  _TessellationFactorFixed.xxxx;
    }
 
    return  _TessellationFactorFixed.xxxx;

 //   return UnityDistanceBasedTess(input0.positionOS, input1.positionOS, input2.positionOS, minDist, maxDist, 0.5 /* _Tess */, unity_ObjectToWorld, _WorldSpaceCameraPos);
}

void Displacement(inout Attributes v)
{
    /*
    float LengthLerp = length(ObjSpaceViewDir(v.vertex));
    LengthLerp -= _DisplacementfalloffNear;
    LengthLerp /= _DisplacementfalloffFar - _DisplacementfalloffNear;
    LengthLerp = 1 - (saturate(LengthLerp));

    float d = ((tex2Dlod(_DispTex, float4(v.texcoord.xy * _Tiling, 0, 0)).r) - _DisplacementCenter) * (_Displacement * LengthLerp);
    d /= max(0.0001, _Tiling);
    */

#ifdef _HEIGHTMAP
    float height = (SAMPLE_TEXTURE2D_LOD(ADD_ZERO_IDX(_HeightMap), ADD_ZERO_IDX(sampler_HeightMap), v.uv0, 0).r - ADD_ZERO_IDX(_HeightCenter)) * ADD_IDX(_HeightAmplitude);
#else
    float height = 0.0;
#endif

#if (SHADERPASS != SHADERPASS_VELOCITY) && (SHADERPASS != SHADERPASS_DISTORTION)
    v.positionOS.xyz += height * v.normalOS;
#endif
}
