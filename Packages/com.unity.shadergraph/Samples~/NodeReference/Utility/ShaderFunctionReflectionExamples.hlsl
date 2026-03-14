#include "ShaderApiReflectionSupport.hlsl"

namespace ShaderFunctionReflectionExamples {
    ///<funchints>
    ///    <sg:ProviderKey>SG.NodeReference.Antialias.Example</sg:ProviderKey>
    ///    <sg:DisplayName>Antialias</sg:DisplayName>
    ///    <sg:SearchCategory>Reference/Shader Functions</sg:SearchCategory>
    ///    <sg:SearchName>Antialias</sg:SearchName>
    ///    <sg:SearchTerms>Antialias</sg:SearchTerms>
    ///</funchints>
    ///<paramhints name = "cutoff">
    ///     <Default>0.5</Default>
    ///</paramhints>
    UNITY_EXPORT_REFLECTION
    float Antialias(float gradient, float cutoff)
    {
        float c = cutoff - gradient;
        float d = length(float2(ddx(c), ddy(c)));
        return saturate(0.5 - (c / d));
    }

    ///<funchints>
    ///    <sg:ProviderKey>SG.NodeReference.Grid.Example</sg:ProviderKey>
    ///    <sg:DisplayName>Grid</sg:DisplayName>
    ///    <sg:SearchCategory>Reference/Shader Functions</sg:SearchCategory>
    ///    <sg:SearchName>Grid</sg:SearchName>
    ///    <sg:SearchTerms>Grid</sg:SearchTerms>
    ///</funchints>
    ///<paramhints name = "uv">
    ///     <UV/>
    ///     <Default>UV0</Default>
    ///</paramhints>
    ///<paramhints name = "spacing">
    ///     <sg:Default>5,5</sg:Default>
    ///</paramhints>
    ///<paramhints name = "thickness">
    ///     <sg:Default>0.01</sg:Default>
    ///</paramhints>
    ///<paramhints name = "lineColor">
    ///     <sg:Color />
    ///     <sg:Default>0.2,0.7,0.8</sg:Default>
    ///</paramhints>
    ///<paramhints name = "antialiased">
    ///     <sg:Static />
    ///</paramhints>
    UNITY_EXPORT_REFLECTION
    float3 Grid(
    float2 uv,
    float2 spacing,
    float thickness,
    float3 lineColor,
    bool antialiased,
    out float mask)
    {
        float2 g = uv * spacing;

        float2 gridDist = abs(frac(g - 0.5) - 0.5) / fwidth(g);

        float l = min(gridDist.x, gridDist.y);
        mask = 1.0 - saturate(l - thickness);

        if (antialiased)
            mask = Antialias(mask, 0.5);
    
        return lineColor * mask;
    }

    ///<funchints>
    ///    <sg:ProviderKey>SG.NodeReference.UV_Swirl.Example</sg:ProviderKey>
    ///    <sg:DisplayName>UV Swirl</sg:DisplayName>
    ///    <sg:SearchCategory>Reference/Shader Functions</sg:SearchCategory>
    ///    <sg:SearchName>UV Swirl</sg:SearchName>
    ///    <sg:SearchTerms>UV, Distorion, Swirl</sg:SearchTerms>
    ///</funchints>
    ///<paramhints name = "uv">
    ///     <UV/>
    ///     <Default>UV0</Default>
    ///</paramhints>
    ///<paramhints name = "center">
    ///     <sg:Default>0.5,0.5</sg:Default>
    ///</paramhints>
    ///<paramhints name = "animated">
    ///     <sg:Static />
    ///</paramhints>
    UNITY_EXPORT_REFLECTION
    void UV_Swirl(
    inout float2 uv,
    bool animated,
    float time,
    float2 center,
    float strength)
    {
        float2 d = uv - center;
        float r = length(d);
        float a = atan2(d.y, d.x);

        a += strength * (1 - r);

    [branch]
        if (animated)
            a += time;

        uv = center + float2(cos(a), sin(a)) * r;
    }
}
