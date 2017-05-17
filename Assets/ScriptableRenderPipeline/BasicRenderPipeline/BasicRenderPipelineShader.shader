// Example shader for a scriptable render loop that calculates multiple lights
// in a single forward-rendered shading pass. Uses same PBR shading model as the
// Standard shader.
//
// The parameters and inspector of the shader are the same as Standard shader,
// for easier experimentation.
Shader "BasicRenderPipeline/Standard"
{
    // Properties is just a copy of Standard.shader. Our example shader does not use all of them,
    // but the inspector UI expects all these to exist.
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo", 2D) = "white" {}
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
        _GlossMapScale("Smoothness Scale", Range(0.0, 1.0)) = 1.0
        [Enum(Metallic Alpha,0,Albedo Alpha,1)] _SmoothnessTextureChannel ("Smoothness texture channel", Float) = 0
        [Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _MetallicGlossMap("Metallic", 2D) = "white" {}
        [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [ToggleOff] _GlossyReflections("Glossy Reflections", Float) = 1.0
        _BumpScale("Scale", Float) = 1.0
        _BumpMap("Normal Map", 2D) = "bump" {}
        _Parallax ("Height Scale", Range (0.005, 0.08)) = 0.02
        _ParallaxMap ("Height Map", 2D) = "black" {}
        _OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
        _OcclusionMap("Occlusion", 2D) = "white" {}
        _EmissionColor("Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}
        _DetailMask("Detail Mask", 2D) = "white" {}
        _DetailAlbedoMap("Detail Albedo x2", 2D) = "grey" {}
        _DetailNormalMapScale("Scale", Float) = 1.0
        _DetailNormalMap("Normal Map", 2D) = "bump" {}
        [Enum(UV0,0,UV1,1)] _UVSec("UV Set for secondary textures", Float) = 0
        [HideInInspector] _Mode("__mode", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "PerformanceChecks" = "False" }
        LOD 300

        // Include forward (base + additive) pass from regular Standard shader.
        // They are not used by the scriptable render loop; only here so that
        // if we turn off our example loop, then regular forward rendering kicks in
        // and objects look just like with a Standard shader.
        UsePass "Standard/FORWARD"
        UsePass "Standard/FORWARD_DELTA"


        // Multiple lights at once pass, for our example Basic render loop.
        Pass
        {
            Tags { "LightMode" = "BasicPass" }

            // Use same blending / depth states as Standard shader
            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]

CGPROGRAM
#pragma target 3.0
#pragma vertex vert
#pragma fragment frag
#pragma shader_feature _METALLICGLOSSMAP
#include "UnityCG.cginc"
#include "UnityStandardBRDF.cginc"
#include "UnityStandardUtils.cginc"


// Global lighting data (setup from C# code once per frame).
CBUFFER_START(GlobalLightData)
    // The variables are very similar to built-in unity_LightColor, unity_LightPosition,
    // unity_LightAtten, unity_SpotDirection as used by the VertexLit shaders, except here
    // we use world space positions instead of view space.
    half4 globalLightColor[8];
    float4 globalLightPos[8];
    float4 globalLightSpotDir[8];
    float4 globalLightAtten[8];
    int4  globalLightCount;
    // Global ambient/SH probe, similar to unity_SH* built-in variables.
    float4 globalSH[7];
CBUFFER_END


// Surface inputs for evaluating Standard BRDF
struct SurfaceInputData
{
    half3 diffColor, specColor;
    half oneMinusReflectivity, smoothness;
};


// Compute attenuation & illumination from one light
half3 EvaluateOneLight(int idx, float3 positionWS, half3 normalWS, half3 eyeVec, SurfaceInputData s)
{
    // direction to light
    float3 dirToLight = globalLightPos[idx].xyz;
    dirToLight -= positionWS * globalLightPos[idx].w;
    // distance attenuation
    float att = 1.0;
    float distSqr = dot(dirToLight, dirToLight);
    att /= (1.0 + globalLightAtten[idx].z * distSqr);
    if (globalLightPos[idx].w != 0 && distSqr > globalLightAtten[idx].w) att = 0.0; // set to 0 if outside of range
    distSqr = max(distSqr, 0.000001); // don't produce NaNs if some vertex position overlaps with the light
    dirToLight *= rsqrt(distSqr);
    // spotlight angular attenuation
    float rho = max(dot(dirToLight, globalLightSpotDir[idx].xyz), 0.0);
    float spotAtt = (rho - globalLightAtten[idx].x) * globalLightAtten[idx].y;
    att *= saturate(spotAtt);

    // Super simple diffuse lighting instead of PBR would be this:
    //half ndotl = max(dot(normalWS, dirToLight), 0.0);
    //half3 color = ndotl * s.diffColor * globalLightColor[idx].rgb;
    //return color * att;

    // Fill in light & indirect structures, and evaluate Standard BRDF
    UnityLight light;
    light.color = globalLightColor[idx].rgb * att;
    light.dir = dirToLight;
    UnityIndirect indirect;
    indirect.diffuse = 0;
    indirect.specular = 0;
    half4 c = BRDF1_Unity_PBS(s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, normalWS, -eyeVec, light, indirect);
    return c.rgb;
}


// Evaluate 2nd order spherical harmonics, given normalized world space direction.
// Similar to ShadeSH9 in UnityCG.cginc
half3 EvaluateSH(half3 n)
{
    half3 res;
    half4 normal = half4(n, 1);
    // Linear (L1) + constant (L0) polynomial terms
    res.r = dot(globalSH[0], normal);
    res.g = dot(globalSH[1], normal);
    res.b = dot(globalSH[2], normal);
    // 4 of the quadratic (L2) polynomials
    half4 vB = normal.xyzz * normal.yzzx;
    res.r += dot(globalSH[3], vB);
    res.g += dot(globalSH[4], vB);
    res.b += dot(globalSH[5], vB);
    // Final (5th) quadratic (L2) polynomial
    half vC = normal.x*normal.x - normal.y*normal.y;
    res += globalSH[6].rgb * vC;
    return res;
}


// Vertex shader
struct v2f
{
    float2 uv : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    float3 normalWS : TEXCOORD2;
    float4 hpos : SV_POSITION;
};

float4 _MainTex_ST;

v2f vert(appdata_base v)
{
    v2f o;
    o.uv = TRANSFORM_TEX(v.texcoord,_MainTex);
    o.hpos = UnityObjectToClipPos(v.vertex);
    o.positionWS = mul(unity_ObjectToWorld, v.vertex).xyz;
    o.normalWS = UnityObjectToWorldNormal(v.normal);
    return o;
}



sampler2D _MainTex;
sampler2D _MetallicGlossMap;
float _Metallic;
float _Glossiness;


// Fragment shader
half4 frag(v2f i) : SV_Target
{
    i.normalWS = normalize(i.normalWS);
    half3 eyeVec = normalize(i.positionWS - _WorldSpaceCameraPos);

    // Sample textures
    half4 diffuseAlbedo = tex2D(_MainTex, i.uv);
    half2 metalSmooth;
    #ifdef _METALLICGLOSSMAP
    metalSmooth = tex2D(_MetallicGlossMap, i.uv).ra;
    #else
    metalSmooth.r = _Metallic;
    metalSmooth.g = _Glossiness;
    #endif

    // Fill in surface input structure
    SurfaceInputData s;
    s.diffColor = DiffuseAndSpecularFromMetallic(diffuseAlbedo.rgb, metalSmooth.x, s.specColor, s.oneMinusReflectivity);
    s.smoothness = metalSmooth.y;

    // Ambient lighting
    half4 color = half4(0,0,0, diffuseAlbedo.a);
    UnityLight light;
    light.color = 0;
    light.dir = 0;
    UnityIndirect indirect;
    indirect.diffuse = EvaluateSH(i.normalWS);
    indirect.specular = 0;
    color.rgb += BRDF1_Unity_PBS(s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, i.normalWS, -eyeVec, light, indirect);

    // Add illumination from all lights
    for (int il = 0; il < globalLightCount.x; ++il)
    {
        color.rgb += EvaluateOneLight(il, i.positionWS, i.normalWS, eyeVec, s);
    }
    return color;
}

ENDCG
        }
    }

    CustomEditor "StandardShaderGUI"
}
