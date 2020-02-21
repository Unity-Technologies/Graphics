#ifndef UNIVERSAL_AO_INCLUDED
#define UNIVERSAL_AO_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"

#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    TEXTURE2D_ARRAY_FLOAT(_CameraDepthTexture);
#else
    TEXTURE2D_FLOAT(_CameraDepthTexture);
#endif

SAMPLER(sampler_CameraDepthTexture);
TEXTURE2D(_NoiseTex);
SAMPLER(sampler_NoiseTex);

#define SAMPLE_DEPTH_AO(uv) LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r, _ZBufferParams);
//#define SAMPLE_DEPTH_AO(uv) SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;

float4 _NoiseTex_TexelSize;
float4x4 ProjectionMatrix;

//Common Settings
half _AO_Intensity;
half _AO_Radius;

// SSAO Settings
int _SSAO_Samples;
float _SSAO_Area;

float3 RandomVector(float2 coords)
{
    return float3(GenerateHashedRandomFloat(coords * 1.12358), GenerateHashedRandomFloat(coords + 3.58), GenerateHashedRandomFloat(coords - 1.321)) * 2 - 1;
}

float3 reconstructPosition(in float2 uv, in float z, in float4x4 InvVP)
{
  float x = uv.x * 2.0f - 1.0f;
  float y = (1.0 - uv.y) * 2.0f - 1.0f;
  float4 position_s = float4(x, y, z, 1.0f);
  float4 position_v = mul(InvVP, position_s);
  return position_v.xyz / position_v.w;
}

float3 NormalFromDepth(float depth, float2 texcoords)
{
    const float2 offset = float2(0.001,0.0);

    float depthX = SAMPLE_DEPTH_AO(texcoords + offset.xy);
    float depthY = SAMPLE_DEPTH_AO(texcoords + offset.yx);

    float3 pX = float3(offset.xy, depthX - depth);
    float3 pY = float3(offset.yx, depthY - depth);

    float3 normal = cross(pY, pX);
    normal.z = -normal.z;

    return normalize(normal);
}

float3 ReconstructNormals(float2 coords)
{
    float3 normals = 0;

    float2 coodsX = coords - float2(_ScreenParams.z - 1, 0.0);
    float2 coodsY = coords + float2(0.0, _ScreenParams.w - 1);

    float d = SAMPLE_DEPTH_AO(coords);
    float dX = SAMPLE_DEPTH_AO(coodsX);
    float dY = SAMPLE_DEPTH_AO(coodsY);

    float3 base = float3(coords, d);
    float3 x = float3(coodsX, dX);
    float3 y = float3(coodsY, dY);

    normals = normalize(cross(x - base, y - base));
    normals.z = -normals.z;

    return normals;
}

float4 SSAO_V2(float2 coords, float3 vpos)
{
    const float3 sample_sphere[16] = {
        float3( 0.5381, 0.1856, 0.4319), float3( 0.1379, 0.2486, 0.4430),
        float3( 0.3371, 0.5679, 0.0057), float3(-0.6999,-0.0451, 0.0019),
        float3( 0.0689,-0.1598, 0.8547), float3( 0.0560, 0.0069, 0.1843),
        float3(-0.0146, 0.1402, 0.0762), float3( 0.0100,-0.1924, 0.0344),
        float3(-0.3577,-0.5301, 0.4358), float3(-0.3169, 0.1063, 0.0158),
        float3( 0.0103,-0.5869, 0.0046), float3(-0.0897,-0.4940, 0.3287),
        float3( 0.7119,-0.0154, 0.0918), float3(-0.0533, 0.0596, 0.5411),
        float3( 0.0352,-0.0631, 0.5460), float3(-0.4776, 0.2847, 0.0271)
    };

//new
    float bias = _SSAO_Area;
    float3 debug = 0;
    float depth = SAMPLE_DEPTH_AO(coords.xy);

    float occlusion = 0.0;

    float3 fragPos   = float3(coords, depth);
    float3 normal    = ReconstructNormals(coords);
    //Random Noise vector
    float2 noiseCoords = coords.xy * ( _ScreenParams.xy / _NoiseTex_TexelSize.zw);
    float3 randomVec = normalize((SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex , noiseCoords)) * 2 - 1) * half3(1, 1, 0);

    //matrix
    float3 tangent = normalize(randomVec - normal * dot(randomVec, normal));
    float3 bitangent = cross(normal, tangent);
    float3x3 TBN = float3x3(tangent, bitangent, normal);

    //float3 noiseNormal = mul(TBN, randomVec);

    for(int i = 0; i < _SSAO_Samples; ++i)
    {
        // get sample position
        float3 randRot = randomVec;// RandomVector(coords.xy * _ScreenParams.xy * i);
        float3 sample = mul(TBN, sample_sphere[i]); //randRot;//reflect(randRot, randomVec);

        //sample *= dot(randRot, normal);

        // shift sample closer
        half scale = (float)i / _SSAO_Samples;
        scale  = lerp(0.1f, 1.0f, scale * scale);
        sample = sample * _AO_Radius * scale;

        float3 offset      =  vpos + sample;
        offset = TransformWViewToHClip(offset);

        sample.z += depth;

        //offset.xyz /= offset.w;               // perspective divide
        //offset.xyz  = offset.xyz * 0.5 + 0.5; // transform to range 0.0 - 1.0

        float sampleDepth = SAMPLE_DEPTH_AO(offset);

        float rangeCheck = smoothstep(0.0, 1.0, _AO_Radius / abs(depth - sampleDepth));
        occlusion += ((sampleDepth >= sample.z ? 1.0 : lerp(1.0, 0.0, _AO_Intensity)) / _SSAO_Samples);
        debug = lerp(half3(0,0,1), half3(1,0,0), sample.x);// half3(offset.xy, 0);
    }

    //return saturate(occlusion.xxxx);
    return float4(debug, 1);
}

float4 SSAO_V4(float2 coords, float3 vpos)
{
    const float3 sample_sphere[16] = {
        float3( 0.5381, 0.1856,-0.4319), float3( 0.1379, 0.2486, 0.4430),
        float3( 0.3371, 0.5679,-0.0057), float3(-0.6999,-0.0451,-0.0019),
        float3( 0.0689,-0.1598,-0.8547), float3( 0.0560, 0.0069,-0.1843),
        float3(-0.0146, 0.1402, 0.0762), float3( 0.0100,-0.1924,-0.0344),
        float3(-0.3577,-0.5301,-0.4358), float3(-0.3169, 0.1063, 0.0158),
        float3( 0.0103,-0.5869, 0.0046), float3(-0.0897,-0.4940, 0.3287),
        float3( 0.7119,-0.0154,-0.0918), float3(-0.0533, 0.0596,-0.5411),
        float3( 0.0352,-0.0631, 0.5460), float3(-0.4776, 0.2847,-0.0271)
    };

//new
    float bias = _SSAO_Area;
    float3 debug = 0;
    float depth = SAMPLE_DEPTH_AO(coords.xy);

    float occlusion = 0.0;

    float3 fragPos   = float3(coords, depth);
    float3 normal    = ReconstructNormals(coords);
    //Random Noise vector
    float2 noiseCoords = coords.xy * ( _ScreenParams.xy / _NoiseTex_TexelSize.zw);
    float3 randomVec = normalize((SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex , noiseCoords) * 2 - 1));


    for(int i = 0; i < _SSAO_Samples; ++i)
    {
        // get sample position
        float3 randRot = RandomVector(frac(coords.xy + normal.xy + _WorldSpaceCameraPos.xy) * _ScreenParams.xy * i) * randomVec;
        //randRot = dot(normal, randRot) >= 0 ? randRot : -randRot;
        float3 sample = randRot;//reflect(randRot, randomVec);
        sample *= _AO_Radius;
        sample.z += depth;
        float2 offset      =  fragPos.xy + sample.xy;
        //offset.xyz /= offset.w;               // perspective divide
        //offset.xyz  = offset.xyz * 0.5 + 0.5; // transform to range 0.0 - 1.0

        float sampleDepth = SAMPLE_DEPTH_AO(offset);

        float rangeCheck = smoothstep(0.0, 1.0, _AO_Radius / abs(depth - sampleDepth));
        occlusion += ((sampleDepth >= sample.z ? 2.0 : 0.0) / _SSAO_Samples);
        debug = rangeCheck.xxx;
    }

    //return float4(debug, 1);
    return saturate(pow(occlusion.xxxx, _AO_Intensity * 4));
    //return float4(debug, 1);
}

float4 SSAO_V3(float2 coords, float3 vpos)
{
    const float3 sample_sphere[16] = {
        float3( 0.5381, 0.1856, 0.4319), float3( 0.1379, 0.2486, 0.4430),
        float3( 0.3371, 0.5679, 0.0057), float3(-0.6999,-0.0451, 0.0019),
        float3( 0.0689,-0.1598, 0.8547), float3( 0.0560, 0.0069, 0.1843),
        float3(-0.0146, 0.1402, 0.0762), float3( 0.0100,-0.1924, 0.0344),
        float3(-0.3577,-0.5301, 0.4358), float3(-0.3169, 0.1063, 0.0158),
        float3( 0.0103,-0.5869, 0.0046), float3(-0.0897,-0.4940, 0.3287),
        float3( 0.7119,-0.0154, 0.0918), float3(-0.0533, 0.0596, 0.5411),
        float3( 0.0352,-0.0631, 0.5460), float3(-0.4776, 0.2847, 0.0271)
    };

    //new
    float bias = -0.1;
    float3 debug = 0;
    float depth = SAMPLE_DEPTH_AO(coords.xy);

    float occlusion = 0.0;

    float3 fragPos   = float3(coords, depth);
    float3 normal    = ReconstructNormals(coords);
    //Random Noise vector
    float2 noiseCoords = coords.xy * ( _ScreenParams.xy / _NoiseTex_TexelSize.zw);
    float3 randomVec = (SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex , noiseCoords) * 2 - 1) * float3(1, 1, 0);
    float3 tangent   = normalize(randomVec - normal * dot(randomVec, normal));
    float3 bitangent = cross(normal, tangent);
    float3x3 TBN       = float3x3(tangent, bitangent, normal);

    for(int i = 0; i < _SSAO_Samples; ++i)
    {
        // get sample position
        float3 sample = mul(sample_sphere[i], TBN); // From tangent to view-space
        sample = fragPos + sample * _AO_Radius;
        float4 offset = float4(sample, 1.0);
        //offset      = ComputeClipSpacePosition(fragPos, ProjectionMatrix); //offset);    // from view to clip-space
        //offset.xyz /= offset.w;               // perspective divide
        //offset.xyz  = offset.xyz * 0.5 + 0.5; // transform to range 0.0 - 1.0

        float sampleDepth = SAMPLE_DEPTH_AO(offset.xy);
        debug = frac(offset.xyz);

        occlusion += (sampleDepth >= sample.z + bias ? 1.0 : 0.0);
    }

    return float4(occlusion.xxx, 1);
}

float4 SSAO(float2 coords, float3 vpos)
{
    float area = _SSAO_Area;
    const float falloff = 0.05;
    float radius = _AO_Radius;

    const int samples = _SSAO_Samples;
    const float3 sample_sphere[16] = {
        float3( 0.5381, 0.1856, 0.4319), float3( 0.1379, 0.2486, 0.4430),
        float3( 0.3371, 0.5679, 0.0057), float3(-0.6999,-0.0451, 0.0019),
        float3( 0.0689,-0.1598, 0.8547), float3( 0.0560, 0.0069, 0.1843),
        float3(-0.0146, 0.1402, 0.0762), float3( 0.0100,-0.1924, 0.0344),
        float3(-0.3577,-0.5301, 0.4358), float3(-0.3169, 0.1063, 0.0158),
        float3( 0.0103,-0.5869, 0.0046), float3(-0.0897,-0.4940, 0.3287),
        float3( 0.7119,-0.0154, 0.0918), float3(-0.0533, 0.0596, 0.5411),
        float3( 0.0352,-0.0631, 0.5460), float3(-0.4776, 0.2847, 0.0271)
    };

    //Random Noise vector
    float2 noiseCoords = coords.xy * ( _ScreenParams.xy / _NoiseTex_TexelSize.zw);
    float3 random = normalize(SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex , noiseCoords) * 2 - 1);
    //Depth texture
    float depth = SAMPLE_DEPTH_AO(coords.xy);

    float3 wsPos = vpos - _WorldSpaceCameraPos.xyz;
    //float3 normal = normalize(cross(ddx(wsPos), ddy(wsPos)));

    float3 position = float3(coords.xy, depth);
    // Reconstruct normals
    float3 normal = ReconstructNormals(coords);// NormalFromDepth(depth, coords.xy);

//    vec3 tangent   = normalize(randomVec - normal * dot(randomVec, normal));
//    vec3 bitangent = cross(normal, tangent);
//    mat3 TBN       = mat3(tangent, bitangent, normal);
    float3 tangent = float3(0, 0, 1);
    float3 bitangent = cross(normal, tangent);
    float3x3 TBN = float3x3(tangent, bitangent, normal);

    float radius_depth = radius;///depth;
    float occlusion = 0.0;
    half samplesDiv = 1.0 / samples;

float3 debug = 0;

    for(int i=0; i < samples; i++)
    {
        /*half scale   = i / samples;
        scale   = lerp(0.1f, 1.0f, scale * scale);
        float3 randomVec = RandomVector(coords + i * 0.2) * scale;
        float3 ray = radius_depth * reflect(randomVec, random);
        */
        //random = normalize(SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseCoords + sample_sphere[i].xz) * 2 - 1);

        float3 ray = radius_depth * reflect(sample_sphere[i], random);

        //ray = lerp(ray, ray2, floor(coords.x - 0.5));

        float3 hemi_ray = sign(dot(ray,normal)) * ray;

        float occ_depth = SAMPLE_DEPTH_AO(hemi_ray.xy + position.xy);

        //occ_depth =
        //float difference = depth - occ_depth;

debug = hemi_ray;
        ////float rangeCheck = smoothstep(0.0, 1.0, radius_depth / abs(difference));
        //occlusion += step(falloff, difference) * (1.0-smoothstep(falloff, area, difference));
        occlusion += (depth >= occ_depth ? 0.0 : 1.0) * samplesDiv;// * rangeCheck;
    }
    float ao = 1.0 - _AO_Intensity * occlusion;

    return float4(occlusion.xxx, 0);
}

#endif //UNIVERSAL_AO_INCLUDED
