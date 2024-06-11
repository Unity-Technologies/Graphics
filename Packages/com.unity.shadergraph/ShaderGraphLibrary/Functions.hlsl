// UNITY_SHADER_NO_UPGRADE
#ifndef UNITY_GRAPHFUNCTIONS_INCLUDED
#define UNITY_GRAPHFUNCTIONS_INCLUDED

// ----------------------------------------------------------------------------
// Included in generated graph shaders
// ----------------------------------------------------------------------------

#ifndef BUILTIN_TARGET_API
bool IsGammaSpace()
{
    #ifdef UNITY_COLORSPACE_GAMMA
        return true;
    #else
        return false;
    #endif
}
#endif

float4 ComputeScreenPos (float4 pos, float projectionSign)
{
  float4 o = pos * 0.5f;
  o.xy = float2(o.x, o.y * projectionSign) + o.w;
  o.zw = pos.zw;
  return o;
}

struct Gradient
{
    int type;
    int colorsLength;
    int alphasLength;
    float4 colors[8];
    float2 alphas[8];
};

Gradient NewGradient(int type, int colorsLength, int alphasLength,
    float4 colors0, float4 colors1, float4 colors2, float4 colors3, float4 colors4, float4 colors5, float4 colors6, float4 colors7,
    float2 alphas0, float2 alphas1, float2 alphas2, float2 alphas3, float2 alphas4, float2 alphas5, float2 alphas6, float2 alphas7)
{
    Gradient output =
    {
        type, colorsLength, alphasLength,
        {colors0, colors1, colors2, colors3, colors4, colors5, colors6, colors7},
        {alphas0, alphas1, alphas2, alphas3, alphas4, alphas5, alphas6, alphas7}
    };
    return output;
}

// https://bottosson.github.io/posts/oklab/ for perceptual blend mode in gradients
float3 LinearToOklab(float3 rgb)
{
    float l = 0.4122214708f * rgb.r + 0.5363325363f * rgb.g + 0.0514459929f * rgb.b;
    float m = 0.2119034982f * rgb.r + 0.6806995451f * rgb.g + 0.1073969566f * rgb.b;
    float s = 0.0883024619f * rgb.r + 0.2817188376f * rgb.g + 0.6299787005f * rgb.b;

    float l_ = pow(l, 0.333333f);
    float m_ = pow(m, 0.333333f);
    float s_ = pow(s, 0.333333f);

    return float3(
        0.2104542553f * l_ + 0.7936177850f * m_ - 0.0040720468f * s_,
        1.9779984951f * l_ - 2.4285922050f * m_ + 0.4505937099f * s_,
        0.0259040371f * l_ + 0.7827717662f * m_ - 0.8086757660f * s_);
}

float3 OklabToLinear(float3 lab)
{
    float l_ = lab.r + 0.3963377774f * lab.g + 0.2158037573f * lab.b;
    float m_ = lab.r - 0.1055613458f * lab.g - 0.0638541728f * lab.b;
    float s_ = lab.r - 0.0894841775f * lab.g - 1.2914855480f * lab.b;

    float l = l_ * l_ * l_;
    float m = m_ * m_ * m_;
    float s = s_ * s_ * s_;

    return float3(
         4.0767416621f * l - 3.3077115913f * m + 0.2309699292f * s,
		-1.2684380046f * l + 2.6097574011f * m - 0.3413193965f * s,
		-0.0041960863f * l - 0.7034186147f * m + 1.7076147010f * s);
}

#ifndef SHADERGRAPH_SAMPLE_SCENE_DEPTH
    #define SHADERGRAPH_SAMPLE_SCENE_DEPTH(uv) shadergraph_SampleSceneDepth(uv)
#endif

#ifndef SHADERGRAPH_SAMPLE_SCENE_COLOR
    #define SHADERGRAPH_SAMPLE_SCENE_COLOR(uv) shadergraph_SampleSceneColor(uv)
#endif

#ifndef SHADERGRAPH_SAMPLE_SCENE_NORMAL
    #define SHADERGRAPH_SAMPLE_SCENE_NORMAL(uv) shadergraph_SampleSceneNormal(uv)
#endif

#ifndef SHADERGRAPH_BAKED_GI
    #define SHADERGRAPH_BAKED_GI(positionWS, normalWS, positionSS, uvStaticLightmap, uvDynamicLightmap, applyScaling) shadergraph_BakedGI(positionWS, normalWS, positionSS, uvStaticLightmap, uvDynamicLightmap, applyScaling)
#endif

#ifndef SHADERGRAPH_REFLECTION_PROBE
    #define SHADERGRAPH_REFLECTION_PROBE(viewDir, normalOS, lod) shadergraph_ReflectionProbe(viewDir, normalOS, lod)
#endif

#ifndef SHADERGRAPH_FOG
    #define SHADERGRAPH_FOG(position, color, density) shadergraph_Fog(position, color, density)
#endif

#ifndef SHADERGRAPH_AMBIENT_SKY
    #define SHADERGRAPH_AMBIENT_SKY float3(0,0,0)
#endif

#ifndef SHADERGRAPH_AMBIENT_EQUATOR
    #define SHADERGRAPH_AMBIENT_EQUATOR float3(0,0,0)
#endif

#ifndef SHADERGRAPH_AMBIENT_GROUND
    #define SHADERGRAPH_AMBIENT_GROUND float3(0,0,0)
#endif

#ifndef SHADERGRAPH_OBJECT_POSITION
    #define SHADERGRAPH_OBJECT_POSITION GetAbsolutePositionWS(UNITY_MATRIX_M._m03_m13_m23)
#endif

#ifndef SHADERGRAPH_RENDERER_BOUNDS_MIN
    #define SHADERGRAPH_RENDERER_BOUNDS_MIN float3(0, 0, 0)
#endif
#ifndef SHADERGRAPH_RENDERER_BOUNDS_MAX
    #define SHADERGRAPH_RENDERER_BOUNDS_MAX float3(0, 0, 0)
#endif

float shadergraph_SampleSceneDepth(float2 uv)
{
    return 1;
}

float3 shadergraph_SampleSceneColor(float2 uv)
{
    return 0;
}

float3 shadergraph_SampleSceneNormal(float2 uv)
{
    return 0;
}

float3 shadergraph_BakedGI(float3 positionWS, float3 normalWS, uint2 positionSS, float2 uvStaticLightmap, float2 uvDynamicLightmap, bool applyScaling)
{
    return 0;
}

float3 shadergraph_ReflectionProbe(float3 viewDir, float3 normalOS, float lod)
{
    return 0;
}

void shadergraph_Fog(float3 position, out float4 color, out float density)
{
    color = 0;
    density = 0;
}

#endif // UNITY_GRAPHFUNCTIONS_INCLUDED
