// Example Shader Interface dressed with hints for testing in Shader Graph.
ShaderInterface "Hidden/Testing/StylizedSurfaceInterface"
{
    Properties
    {
        [Uniform(half4, UnityPerMaterial)]
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        [Uniform(float, UnityPerMaterial)]
        _WindTime ("Wind Time", Float) = 0
    }
    SubShader
    {
        Public
        {
            /// <funchints>
            ///     <sg:ProviderKey>Stylized.WindSway</sg:ProviderKey>
            ///     <sg:DisplayName>Wind Sway</sg:DisplayName>
            ///     <sg:Tooltip>Bends the mesh in object space, like foliage in the wind.</sg:Tooltip>
            ///     <sg:Context>Vertex, 1</sg:Context>
            ///     <sg:ReturnDisplayName>Offset</sg:ReturnDisplayName>
            ///     <sg:ReturnTooltip>Object space offset added to the vertex position.</sg:ReturnTooltip>
            ///     <sg:HelpURL>https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/index.html</sg:HelpURL>
            /// </funchints>
            /// <paramhints name="normalOS">
            ///     <sg:DisplayName>Normal</sg:DisplayName>
            ///     <sg:Tooltip>Object space normal after bending.</sg:Tooltip>
            /// </paramhints>
            /// <paramhints name="strength">
            ///     <sg:DisplayName>Strength</sg:DisplayName>
            ///     <sg:Range>0, 1</sg:Range>
            ///     <sg:Default>0.25</sg:Default>
            /// </paramhints>
            /// <paramhints name="frequency">
            ///     <sg:DisplayName>Frequency</sg:DisplayName>
            ///     <sg:Default>1.5</sg:Default>
            /// </paramhints>
            /// <paramhints name="windMode">
            ///     <sg:DisplayName>Wind</sg:DisplayName>
            ///     <sg:Dropdown>Still, Breeze, Gusty, Storm</sg:Dropdown>
            ///     <sg:Default>1</sg:Default>
            ///     <sg:Static />
            /// </paramhints>
            UNITY_VIRTUAL float3 OnWindSway(in float3 positionOS, in float time, inout float3 normalOS, out float strength, out float frequency, out float windMode)
            {
                strength = 0;
                frequency = 1;
                windMode = 0;
                return 0;
            }

            /// <funchints>
            ///     <sg:ProviderKey>Stylized.BaseColor</sg:ProviderKey>
            ///     <sg:DisplayName>Base Color</sg:DisplayName>
            ///     <sg:Tooltip>The albedo and opacity of the surface.</sg:Tooltip>
            ///     <sg:Context>Surface, 2, 1</sg:Context>
            /// </funchints>
            /// <paramhints name="baseColor">
            ///     <sg:DisplayName>Base Color</sg:DisplayName>
            /// </paramhints>
            /// <paramhints name="tint">
            ///     <sg:DisplayName>Tint</sg:DisplayName>
            ///     <sg:Color />
            ///     <sg:Default>1, 0.92, 0.85</sg:Default>
            /// </paramhints>
            /// <paramhints name="opacity">
            ///     <sg:DisplayName>Opacity</sg:DisplayName>
            ///     <sg:Range>0, 1</sg:Range>
            ///     <sg:Default>1</sg:Default>
            /// </paramhints>
            /// <paramhints name="alphaClip">
            ///     <sg:DisplayName>Alpha Clip</sg:DisplayName>
            ///     <sg:Tooltip>Pixels with opacity below this value are discarded.</sg:Tooltip>
            ///     <sg:Range>0, 1</sg:Range>
            ///     <sg:Default>0.5</sg:Default>
            ///     <sg:Static />
            /// </paramhints>
            /// <paramhints name="detailUV">
            ///     <sg:DisplayName>Detail UV</sg:DisplayName>
            ///     <sg:Referable>UV</sg:Referable>
            ///     <sg:Default>UV1</sg:Default>
            /// </paramhints>
            UNITY_VIRTUAL void OnBaseColor(in float2 uv, inout float3 baseColor, out float3 tint, out float opacity, out float alphaClip, out float2 detailUV)
            {
                tint = 1;
                opacity = 1;
                alphaClip = 0;
                detailUV = uv;
            }

            /// <funchints>
            ///     <sg:ProviderKey>Stylized.SurfaceDetail</sg:ProviderKey>
            ///     <sg:DisplayName>Surface Detail</sg:DisplayName>
            ///     <sg:Tooltip>Normals and material response.</sg:Tooltip>
            ///     <sg:Context>Surface, 2, 2</sg:Context>
            /// </funchints>
            /// <paramhints name="normalTS">
            ///     <sg:DisplayName>Normal</sg:DisplayName>
            ///     <sg:Tooltip>Tangent space normal.</sg:Tooltip>
            /// </paramhints>
            /// <paramhints name="smoothness">
            ///     <sg:DisplayName>Smoothness</sg:DisplayName>
            ///     <sg:Range>0, 1</sg:Range>
            ///     <sg:Default>0.5</sg:Default>
            /// </paramhints>
            /// <paramhints name="metallic">
            ///     <sg:DisplayName>Metallic</sg:DisplayName>
            ///     <sg:Range>0, 1</sg:Range>
            /// </paramhints>
            /// <paramhints name="occlusion">
            ///     <sg:DisplayName>Occlusion</sg:DisplayName>
            ///     <sg:Range>0, 1</sg:Range>
            ///     <sg:Default>1</sg:Default>
            /// </paramhints>
            /// <paramhints name="bentNormalWS">
            ///     <sg:DisplayName>Bent Normal</sg:DisplayName>
            ///     <sg:Referable>Normal</sg:Referable>
            ///     <sg:Default>World</sg:Default>
            /// </paramhints>
            UNITY_VIRTUAL void OnSurfaceDetail(inout float3 normalTS, out float smoothness, out float metallic, out float occlusion, out float3 bentNormalWS)
            {
                smoothness = 0.5;
                metallic = 0;
                occlusion = 1;
                bentNormalWS = float3(0, 1, 0);
            }

            /// <funchints>
            ///     <sg:ProviderKey>Stylized.ToonShading</sg:ProviderKey>
            ///     <sg:DisplayName>Toon Shading</sg:DisplayName>
            ///     <sg:Tooltip>Banded diffuse lighting with tinted shadows.</sg:Tooltip>
            ///     <sg:Context>Lighting, 3, 1</sg:Context>
            ///     <sg:ReturnDisplayName>Diffuse</sg:ReturnDisplayName>
            ///     <sg:ReturnTooltip>Lit diffuse color for this light.</sg:ReturnTooltip>
            /// </funchints>
            /// <paramhints name="shadow">
            ///     <sg:DisplayName>Shadow</sg:DisplayName>
            ///     <sg:Tooltip>Shadow attenuation from the light.</sg:Tooltip>
            /// </paramhints>
            /// <paramhints name="bands">
            ///     <sg:DisplayName>Bands</sg:DisplayName>
            ///     <sg:Dropdown>Two, Three, Four, Five</sg:Dropdown>
            ///     <sg:Default>1</sg:Default>
            ///     <sg:Static />
            /// </paramhints>
            /// <paramhints name="softness">
            ///     <sg:DisplayName>Softness</sg:DisplayName>
            ///     <sg:Range>0, 0.5</sg:Range>
            ///     <sg:Default>0.05</sg:Default>
            /// </paramhints>
            /// <paramhints name="shadowColor">
            ///     <sg:DisplayName>Shadow Color</sg:DisplayName>
            ///     <sg:Color />
            ///     <sg:Default>0.32, 0.27, 0.45</sg:Default>
            /// </paramhints>
            /// <paramhints name="highlightColor">
            ///     <sg:DisplayName>Highlight Color</sg:DisplayName>
            ///     <sg:Color />
            ///     <sg:Default>1, 0.97, 0.9</sg:Default>
            /// </paramhints>
            UNITY_VIRTUAL float3 OnToonShading(in float3 normalWS, in float3 lightDirWS, in float3 lightColor, inout float shadow, out float bands, out float softness, out float3 shadowColor, out float3 highlightColor)
            {
                bands = 1;
                softness = 0.05;
                shadowColor = 0;
                highlightColor = 1;
                return lightColor * saturate(dot(normalWS, lightDirWS)) * shadow;
            }

            /// <funchints>
            ///     <sg:ProviderKey>Stylized.RimEmission</sg:ProviderKey>
            ///     <sg:DisplayName>Rim &amp; Emission</sg:DisplayName>
            ///     <sg:Tooltip>Fresnel rim light and self illumination.</sg:Tooltip>
            ///     <sg:Context>Lighting, 3, 2</sg:Context>
            /// </funchints>
            /// <paramhints name="emission">
            ///     <sg:DisplayName>Emission</sg:DisplayName>
            /// </paramhints>
            /// <paramhints name="rimColor">
            ///     <sg:DisplayName>Rim Color</sg:DisplayName>
            ///     <sg:Color />
            ///     <sg:Default>0.55, 0.85, 1</sg:Default>
            /// </paramhints>
            /// <paramhints name="rimPower">
            ///     <sg:DisplayName>Rim Power</sg:DisplayName>
            ///     <sg:Range>0.5, 8</sg:Range>
            ///     <sg:Default>3</sg:Default>
            /// </paramhints>
            /// <paramhints name="rimBlend">
            ///     <sg:DisplayName>Rim Blend</sg:DisplayName>
            ///     <sg:Dropdown>Off, Add, Multiply, Screen</sg:Dropdown>
            ///     <sg:Default>1</sg:Default>
            ///     <sg:Static />
            /// </paramhints>
            /// <paramhints name="intensity">
            ///     <sg:DisplayName>Intensity</sg:DisplayName>
            ///     <sg:Default>1</sg:Default>
            /// </paramhints>
            /// <paramhints name="pulse">
            ///     <sg:DisplayName>Pulse</sg:DisplayName>
            ///     <sg:Tooltip>Animates the emission over time.</sg:Tooltip>
            ///     <sg:Static />
            /// </paramhints>
            UNITY_VIRTUAL void OnRimEmission(in float3 normalWS, in float3 viewDirWS, inout float3 emission, out float3 rimColor, out float rimPower, out float rimBlend, out float intensity, out bool pulse)
            {
                rimColor = 0;
                rimPower = 3;
                rimBlend = 0;
                intensity = 1;
                pulse = false;
            }
        }

        Pass
        {
            Name "Forward"
            Private
            {
                #pragma vertex Vert
                #pragma fragment Frag

                struct Attributes
                {
                    float4 positionOS : POSITION0;
                    float3 normalOS : NORMAL0;
                    float2 uv : TEXCOORD0;
                };

                struct Varyings
                {
                    float4 positionCS : SV_POSITION0;
                    float3 normalWS : TEXCOORD0;
                    float2 uv : TEXCOORD1;
                };

                Varyings Vert(Attributes input)
                {
                    float strength, frequency, windMode;
                    float3 normalOS = input.normalOS;
                    float3 offset = OnWindSway(input.positionOS.xyz, _WindTime, normalOS, strength, frequency, windMode);

                    Varyings output;
                    output.positionCS = float4(input.positionOS.xyz + offset * strength, 1);
                    output.normalWS = normalOS;
                    output.uv = input.uv;
                    return output;
                }

                float4 Frag(Varyings input) : SV_TARGET0
                {
                    float3 baseColor = _BaseColor.rgb;
                    float3 tint;
                    float opacity, alphaClip;
                    float2 detailUV;
                    OnBaseColor(input.uv, baseColor, tint, opacity, alphaClip, detailUV);
                    clip(opacity - alphaClip);

                    float3 normalTS = float3(0, 0, 1);
                    float smoothness, metallic, occlusion;
                    float3 bentNormalWS;
                    OnSurfaceDetail(normalTS, smoothness, metallic, occlusion, bentNormalWS);

                    float3 normalWS = normalize(input.normalWS);
                    float shadow = 1;
                    float bands, softness;
                    float3 shadowColor, highlightColor;
                    float3 diffuse = OnToonShading(normalWS, float3(0, 1, 0), 1, shadow, bands, softness, shadowColor, highlightColor);

                    float3 emission = 0;
                    float3 rimColor;
                    float rimPower, rimBlend, intensity;
                    bool pulse;
                    OnRimEmission(normalWS, float3(0, 0, 1), emission, rimColor, rimPower, rimBlend, intensity, pulse);

                    float3 color = baseColor * tint * lerp(shadowColor, highlightColor, diffuse) * occlusion + emission * intensity;
                    return float4(color, opacity);
                }
            }
        }

        Pass
        {
            Name "ShadowCaster"
            Private
            {
                #pragma vertex Vert
                #pragma fragment Frag

                struct Attributes
                {
                    float4 positionOS : POSITION0;
                    float3 normalOS : NORMAL0;
                    float2 uv : TEXCOORD0;
                };

                struct Varyings
                {
                    float4 positionCS : SV_POSITION0;
                    float2 uv : TEXCOORD0;
                };

                Varyings Vert(Attributes input)
                {
                    float strength, frequency, windMode;
                    float3 normalOS = input.normalOS;
                    float3 offset = OnWindSway(input.positionOS.xyz, _WindTime, normalOS, strength, frequency, windMode);

                    Varyings output;
                    output.positionCS = float4(input.positionOS.xyz + offset * strength, 1);
                    output.uv = input.uv;
                    return output;
                }

                float4 Frag(Varyings input) : SV_TARGET0
                {
                    float3 baseColor = _BaseColor.rgb;
                    float3 tint;
                    float opacity, alphaClip;
                    float2 detailUV;
                    OnBaseColor(input.uv, baseColor, tint, opacity, alphaClip, detailUV);
                    clip(opacity - alphaClip);
                    return 0;
                }
            }
        }
    }
}
