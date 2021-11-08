#ifndef UNITY_META_PASS_INCLUDED
#define UNITY_META_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

CBUFFER_START(UnityMetaPass)
    // x = use uv1 as raster position
    // y = use uv2 as raster position
    bool4 unity_MetaVertexControl;

    // x = return albedo
    // y = return normal
    bool4 unity_MetaFragmentControl;

    // Control which VisualizationMode we will
    // display in the editor
    int unity_VisualizationMode;
CBUFFER_END

struct UnityMetaInput
{
    half3 Albedo;
    half3 Emission;
#ifdef EDITOR_VISUALIZATION
    float2 VizUV;
    float4 LightCoord;
#endif
};

#ifdef EDITOR_VISUALIZATION
// Visualization defines
// Should be kept in sync with the EditorVisualizationMode enum in EditorCameraDrawing.cpp
// First two are unused.
#define EDITORVIZ_PBR_VALIDATION_ALBEDO         0
#define EDITORVIZ_PBR_VALIDATION_METALSPECULAR  1
#define EDITORVIZ_TEXTURE                       2
#define EDITORVIZ_SHOWLIGHTMASK                 3

uniform sampler2D unity_EditorViz_Texture;
uniform half4 unity_EditorViz_Texture_ST;
uniform int unity_EditorViz_UVIndex;
uniform half4 unity_EditorViz_Decode_HDR;
uniform bool unity_EditorViz_ConvertToLinearSpace;
uniform half4 unity_EditorViz_ColorMul;
uniform half4 unity_EditorViz_ColorAdd;
uniform half unity_EditorViz_Exposure;
uniform sampler2D unity_EditorViz_LightTexture;
uniform sampler2D unity_EditorViz_LightTextureB;
#define unity_EditorViz_ChannelSelect unity_EditorViz_ColorMul
#define unity_EditorViz_Color         unity_EditorViz_ColorAdd
#define unity_EditorViz_LightType     unity_EditorViz_UVIndex
uniform float4x4 unity_EditorViz_WorldToLight;
#endif // EDITOR_VISUALIZATION

float2 UnityMetaVizUV(int uvIndex, float2 uv0, float2 uv1, float2 uv2, float4 st)
{
    if (uvIndex == 0)
        return uv0 * st.xy + st.zw;
    else if (uvIndex == 1)
        return uv1 * st.xy + st.zw;
    else
        return uv2 * st.xy + st.zw;
}

void UnityEditorVizData(float3 positionOS, float2 uv0, float2 uv1, float2 uv2, float4 st, out float2 VizUV, out float4 LightCoord)
{
#ifdef EDITOR_VISUALIZATION
    if (unity_VisualizationMode == EDITORVIZ_TEXTURE)
        VizUV = UnityMetaVizUV(unity_EditorViz_UVIndex, uv0, uv1, uv2, unity_EditorViz_Texture_ST);
    else if (unity_VisualizationMode == EDITORVIZ_SHOWLIGHTMASK)
    {
        VizUV = uv1 * st.xy + st.zw;
        LightCoord = mul(unity_EditorViz_WorldToLight, float4(TransformObjectToWorld(positionOS), 1));
    }
#endif
}

void UnityEditorVizData(float3 positionOS, float2 uv0, float2 uv1, float2 uv2, out float2 VizUV, out float4 LightCoord)
{
    float4 st = unity_LightmapST;
    if (unity_MetaVertexControl.y)
        st = unity_DynamicLightmapST;
    UnityEditorVizData(positionOS, uv0, uv1, uv2, st, VizUV, LightCoord);
}

float4 UnityMetaVertexPosition(float3 vertex, float2 uv1, float2 uv2, float4 lightmapST, float4 dynlightmapST)
{
#ifndef EDITOR_VISUALIZATION
    if (unity_MetaVertexControl.x)
    {
        vertex.xy = uv1 * lightmapST.xy + lightmapST.zw;
        // OpenGL right now needs to actually use incoming vertex position,
        // so use it in a very dummy way
        vertex.z = vertex.z > 0 ? REAL_MIN : 0.0f;
    }
    if (unity_MetaVertexControl.y)
    {
        vertex.xy = uv2 * dynlightmapST.xy + dynlightmapST.zw;
        // OpenGL right now needs to actually use incoming vertex position,
        // so use it in a very dummy way
        vertex.z = vertex.z > 0 ? REAL_MIN : 0.0f;
    }
    return TransformWorldToHClip(vertex);
#else
    return TransformObjectToHClip(vertex);
#endif
}

float4 UnityMetaVertexPosition(float3 vertex, float2 uv1, float2 uv2)
{
    return UnityMetaVertexPosition(vertex, uv1, uv2, unity_LightmapST, unity_DynamicLightmapST);
}

float unity_OneOverOutputBoost;
float unity_MaxOutputValue;
float unity_UseLinearSpace;

half4 UnityMetaFragment (UnityMetaInput IN)
{
    half4 res = 0;
#ifndef EDITOR_VISUALIZATION
    if (unity_MetaFragmentControl.x)
    {
        res = half4(IN.Albedo,1);

        // Apply Albedo Boost from LightmapSettings.
        res.rgb = clamp(pow(abs(res.rgb), saturate(unity_OneOverOutputBoost)), 0, unity_MaxOutputValue);
    }
    if (unity_MetaFragmentControl.y)
    {
        half3 emission;
        if (unity_UseLinearSpace)
            emission = IN.Emission;
        else
            emission = Gamma20ToLinear(IN.Emission);

        res = half4(emission, 1.0);
    }
#else
    // SRPs don't support EDITORVIZ_PBR_VALIDATION_ALBEDO or EDITORVIZ_PBR_VALIDATION_METALSPECULAR
    if (unity_VisualizationMode == EDITORVIZ_TEXTURE)
    {
        res = tex2D(unity_EditorViz_Texture, IN.VizUV);

        if (unity_EditorViz_Decode_HDR.x > 0)
            res = half4(DecodeHDREnvironment(res, unity_EditorViz_Decode_HDR), 1);

        if (unity_EditorViz_ConvertToLinearSpace)
            res.rgb = LinearToGamma20(res.rgb);

        res *= unity_EditorViz_ColorMul;
        res += unity_EditorViz_ColorAdd;
        res *= exp2(unity_EditorViz_Exposure);
    }
    else if (unity_VisualizationMode == EDITORVIZ_SHOWLIGHTMASK)
    {
        float result = dot(unity_EditorViz_ChannelSelect, tex2D(unity_EditorViz_Texture, IN.VizUV).rgba);
        if (result == 0)
            discard;

        float atten = 1;
        if (unity_EditorViz_LightType == 0)
        {
            // directional:  no attenuation
        }
        else if (unity_EditorViz_LightType == 1)
        {
            // point
            atten = tex2D(unity_EditorViz_LightTexture, dot(IN.LightCoord.xyz, IN.LightCoord.xyz).xx).r;
        }
        else if (unity_EditorViz_LightType == 2)
        {
            // spot
            atten = tex2D(unity_EditorViz_LightTexture, dot(IN.LightCoord.xyz, IN.LightCoord.xyz).xx).r;
            float cookie = tex2D(unity_EditorViz_LightTextureB, IN.LightCoord.xy / max(IN.LightCoord.w, 0.0001) + 0.5).w;
            atten *= (IN.LightCoord.z > 0) * cookie;
        }
        clip(atten - 0.001f);

        res = float4(unity_EditorViz_Color.xyz * result, unity_EditorViz_Color.w);
    }
#endif // EDITOR_VISUALIZATION
    return res;
}


#endif // UNITY_META_PASS_INCLUDED
