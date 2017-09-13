#ifndef LIGHTWEIGHT_PIPELINE_CORE_INCLUDED
#define LIGHTWEIGHT_PIPELINE_CORE_INCLUDED

// -------------------------------------

#include "LightweightPipelineInput.cginc"
#include "LightweightPipelineLighting.cginc"
#include "LightweightPipelineBRDF.cginc"

#if defined(_HARD_SHADOWS) || defined(_SOFT_SHADOWS) || defined(_HARD_SHADOWS_CASCADES) || defined(_SOFT_SHADOWS_CASCADES)
#define _SHADOWS
#endif

#if defined(_HARD_SHADOWS_CASCADES) || defined(_SOFT_SHADOWS_CASCADES)
#define _SHADOW_CASCADES
#endif

#ifdef _SHADOWS
#include "LightweightPipelineShadows.cginc"
#endif

#if defined(_SPECGLOSSMAP_BASE_ALPHA) || defined(_SPECGLOSSMAP) || defined(_SPECULAR_COLOR)
#define LIGHTWEIGHT_SPECULAR_HIGHLIGHTS
#endif

#define _DieletricSpec half4(0.04, 0.04, 0.04, 1.0 - 0.04) // standard dielectric reflectivity coef at incident angle (= 4%)

half SpecularReflectivity(half3 specular)
{
#if (SHADER_TARGET < 30)
    // SM2.0: instruction count limitation
    // SM2.0: simplified SpecularStrength
    return specular.r; // Red channel - because most metals are either monocrhome or with redish/yellowish tint
#else
    return max(max(specular.r, specular.g), specular.b);
#endif
}

half3 MetallicSetup(float2 uv, Surface s, /*half3 albedo, half albedoAlpha, */out half3 specular, out half smoothness, out half oneMinusReflectivity)
{
    //half2 metallicGloss = MetallicSpecGloss(uv, albedoAlpha).ra;
    //half metallic = metallicGloss.r;
	smoothness = s.Smoothness;// metallicGloss.g;

    // We'll need oneMinusReflectivity, so
    //   1-reflectivity = 1-lerp(dielectricSpec, 1, metallic) = lerp(1-dielectricSpec, 0, metallic)
    // store (1-dielectricSpec) in unity_ColorSpaceDielectricSpec.a, then
    //   1-reflectivity = lerp(alpha, 0, metallic) = alpha + metallic*(0 - alpha) =
    //                  = alpha - metallic * alpha
    half oneMinusDielectricSpec = _DieletricSpec.a;
    oneMinusReflectivity = oneMinusDielectricSpec - s.Metallic * oneMinusDielectricSpec;
    specular = lerp(_DieletricSpec.rgb, s.Albedo, s.Metallic);

    return s.Albedo * oneMinusReflectivity;
}

half3 SpecularSetup(float2 uv, Surface s, /*half3 albedo, half albedoAlpha, */out half3 specular, out half smoothness, out half oneMinusReflectivity)
{
	half4 specGloss = float4(s.Specular, s.Smoothness);// MetallicSpecGloss(uv, albedoAlpha);

#if defined(UNITY_COLORSPACE_GAMMA) && defined(LIGHTWEIGHT_LINEAR)
	specGloss.rgb = LIGHTWEIGHT_GAMMA_TO_LINEAR(specGloss.rgb);
#endif

    specular = specGloss.rgb;
    smoothness = specGloss.a;
    oneMinusReflectivity = 1.0h - SpecularReflectivity(specular);
    return s.Albedo * (half3(1, 1, 1) - specular);
}

half4 OutputColor(half3 color, half alpha)
{
#if defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
    return LIGHTWEIGHT_LINEAR_TO_GAMMA(half4(color, alpha));
#else
    return half4(LIGHTWEIGHT_LINEAR_TO_GAMMA(color), 1);
#endif
}

LightweightVertexOutput LightweightVertex(LightweightVertexInput v)
            {
                LightweightVertexOutput o = (LightweightVertexOutput)0;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.uv01.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
#ifdef LIGHTMAP_ON
                o.uv01.zw = v.lightmapUV * unity_LightmapST.xy + unity_LightmapST.zw;
#endif
                o.hpos = UnityObjectToClipPos(v.vertex);

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.posWS.xyz = worldPos;

                o.viewDir.xyz = normalize(_WorldSpaceCameraPos - worldPos);
                half3 normal = normalize(UnityObjectToWorldNormal(v.normal));

#if _NORMALMAP
                half sign = v.tangent.w * unity_WorldTransformParams.w;
                half3 tangent = normalize(UnityObjectToWorldDir(v.tangent));
                half3 binormal = cross(normal, tangent) * v.tangent.w;

                // Initialize tangetToWorld in column-major to benefit from better glsl matrix multiplication code
                o.tangentToWorld0 = half3(tangent.x, binormal.x, normal.x);
                o.tangentToWorld1 = half3(tangent.y, binormal.y, normal.y);
                o.tangentToWorld2 = half3(tangent.z, binormal.z, normal.z);
#else
                o.normal = normal;
#endif

                // TODO: change to only support point lights per vertex. This will greatly simplify shader ALU
#if defined(_VERTEX_LIGHTS) && defined(_MULTIPLE_LIGHTS)
                half3 diffuse = half3(1.0, 1.0, 1.0);
                // pixel lights shaded = min(pixelLights, perObjectLights)
                // vertex lights shaded = min(vertexLights, perObjectLights) - pixel lights shaded
                // Therefore vertexStartIndex = pixelLightCount;  vertexEndIndex = min(vertexLights, perObjectLights)
                int vertexLightStart = min(globalLightCount.x, unity_LightIndicesOffsetAndCount.y);
                int vertexLightEnd = min(globalLightCount.y, unity_LightIndicesOffsetAndCount.y);
                for (int lightIter = vertexLightStart; lightIter < vertexLightEnd; ++lightIter)
                {
                    int lightIndex = unity_4LightIndices0[lightIter];
                    LightInput lightInput;
                    INITIALIZE_LIGHT(lightInput, lightIndex);

                    half3 lightDirection;
                    half atten = ComputeLightAttenuationVertex(lightInput, normal, worldPos, lightDirection);
                    o.fogCoord.yzw += LightingLambert(diffuse, lightDirection, normal, atten);
                }
#endif

#if defined(_LIGHT_PROBES_ON) && !defined(LIGHTMAP_ON)
                o.fogCoord.yzw += max(half3(0, 0, 0), ShadeSH9(half4(normal, 1)));
#endif

                UNITY_TRANSFER_FOG(o, o.hpos);
                return o;
            }

			Surface InitializeSurface()
			{
				Surface s;
				s.Albedo = float3(1, 1, 1);
				s.Specular = float3(0, 0, 0);
				s.Metallic = 0;
				s.Normal = float3(.5, .5, 1);
				s.Emission = 0;
				s.Smoothness = 0;
				s.Occlusion = 1;
				s.Alpha = 1;
				return s;
			}		

			void DefineSurface(LightweightVertexOutput i, inout Surface s);

            half4 LightweightFragmentPBR(LightweightVertexOutput i) : SV_Target
            {
				Surface s = InitializeSurface();
				DefineSurface(i, s);

                float2 uv = i.uv01.xy;
                float2 lightmapUV = i.uv01.zw;

                half3 specColor;
                half smoothness;
                half oneMinusReflectivity;
#ifdef _METALLIC_SETUP
                half3 diffColor = MetallicSetup(uv, s, specColor, smoothness, oneMinusReflectivity);
#else
                half3 diffColor = SpecularSetup(uv, s, specColor, smoothness, oneMinusReflectivity);
#endif

                diffColor = PreMultiplyAlpha(diffColor, s.Alpha, oneMinusReflectivity, /*out*/ s.Alpha);

                // Roughness is (1.0 - smoothness)²
                half perceptualRoughness = 1.0h - smoothness;

				// TODO - Actually handle normal
                half3 normal;
                CalculateNormal(s.Normal, i, normal);

                // TODO: shader keyword for occlusion
                // TODO: Reflection Probe blend support.
                half3 reflectVec = reflect(-i.viewDir.xyz, normal);

                UnityIndirect indirectLight = LightweightGI(lightmapUV, i.fogCoord.yzw, reflectVec, s.Occlusion, perceptualRoughness);

                // PBS
                // grazingTerm = F90
                half grazingTerm = saturate(smoothness + (1 - oneMinusReflectivity));
                half fresnelTerm = Pow4(1.0 - saturate(dot(normal, i.viewDir.xyz)));
                half3 color = LightweightBRDFIndirect(diffColor, specColor, indirectLight, perceptualRoughness * perceptualRoughness, grazingTerm, fresnelTerm);
                half3 lightDirection;

#ifndef _MULTIPLE_LIGHTS
                LightInput light;
                INITIALIZE_MAIN_LIGHT(light);
                half lightAtten = ComputeLightAttenuation(light, normal, i.posWS.xyz, lightDirection);

#ifdef _SHADOWS
                lightAtten *= ComputeShadowAttenuation(i, _ShadowLightDirection.xyz);
#endif

                half NdotL = saturate(dot(normal, lightDirection));
                half3 radiance = light.color * (lightAtten * NdotL);
                color += LightweightBDRF(diffColor, specColor, oneMinusReflectivity, perceptualRoughness, normal, lightDirection, i.viewDir.xyz) * radiance;
#else

#ifdef _SHADOWS
                half shadowAttenuation = ComputeShadowAttenuation(i, _ShadowLightDirection.xyz);
#endif
                int pixelLightCount = min(globalLightCount.x, unity_LightIndicesOffsetAndCount.y);
                for (int lightIter = 0; lightIter < pixelLightCount; ++lightIter)
                {
                    LightInput light;
                    int lightIndex = unity_4LightIndices0[lightIter];
                    INITIALIZE_LIGHT(light, lightIndex);
                    half lightAtten = ComputeLightAttenuation(light, normal, i.posWS.xyz, lightDirection);
#ifdef _SHADOWS
                    lightAtten *= max(shadowAttenuation, half(lightIndex != _ShadowData.x));
#endif
                    half NdotL = saturate(dot(normal, lightDirection));
                    half3 radiance = light.color * (lightAtten * NdotL);

                    color += LightweightBDRF(diffColor, specColor, oneMinusReflectivity, perceptualRoughness, normal, lightDirection, i.viewDir.xyz) * radiance;
                }
#endif

				color += s.Emission;
                UNITY_APPLY_FOG(i.fogCoord, color);
                return OutputColor(color, s.Alpha);
            }

			half4 LightweightFragmentFastBlinn(LightweightVertexOutput i) : SV_Target
            {
                half4 diffuseAlpha = tex2D(_MainTex, i.uv01.xy);
                half3 diffuse = LIGHTWEIGHT_GAMMA_TO_LINEAR(diffuseAlpha.rgb) * _Color.rgb;
                half alpha = diffuseAlpha.a * _Color.a;

                // Keep for compatibility reasons. Shader Inpector throws a warning when using cutoff
                // due overdraw performance impact.
#ifdef _ALPHATEST_ON
                clip(alpha - _Cutoff);
#endif

                half3 normal;
				half3 normalMap = UnpackNormal(tex2D(_BumpMap, i.uv01.xy));
                CalculateNormal(normalMap, i, normal);

                half4 specularGloss;
                SpecularGloss(i.uv01.xy, alpha, specularGloss);

                half3 viewDir = i.viewDir.xyz;
                float3 worldPos = i.posWS.xyz;

                half3 lightDirection;
                
#ifndef _MULTIPLE_LIGHTS
                LightInput lightInput;
                INITIALIZE_MAIN_LIGHT(lightInput);
                half lightAtten = ComputeLightAttenuation(lightInput, normal, worldPos, lightDirection);
#ifdef _SHADOWS
                lightAtten *= ComputeShadowAttenuation(i, _ShadowLightDirection.xyz);
#endif

#ifdef LIGHTWEIGHT_SPECULAR_HIGHLIGHTS
                half3 color = LightingBlinnPhong(diffuse, specularGloss, lightDirection, normal, viewDir, lightAtten) * lightInput.color;
#else
                half3 color = LightingLambert(diffuse, lightDirection, normal, lightAtten) * lightInput.color;
#endif
    
#else
                half3 color = half3(0, 0, 0);

#ifdef _SHADOWS
                half shadowAttenuation = ComputeShadowAttenuation(i, _ShadowLightDirection.xyz);
#endif
                int pixelLightCount = min(globalLightCount.x, unity_LightIndicesOffsetAndCount.y);
                for (int lightIter = 0; lightIter < pixelLightCount; ++lightIter)
                {
                    LightInput lightData;
                    int lightIndex = unity_4LightIndices0[lightIter];
                    INITIALIZE_LIGHT(lightData, lightIndex);
                    half lightAtten = ComputeLightAttenuation(lightData, normal, worldPos, lightDirection);
#ifdef _SHADOWS
                    lightAtten *= max(shadowAttenuation, half(lightIndex != _ShadowData.x));
#endif

#ifdef LIGHTWEIGHT_SPECULAR_HIGHLIGHTS
                    color += LightingBlinnPhong(diffuse, specularGloss, lightDirection, normal, viewDir, lightAtten) * lightData.color;
#else
                    color += LightingLambert(diffuse, lightDirection, normal, lightAtten) * lightData.color;
#endif
                }

#endif // _MULTIPLE_LIGHTS

#ifdef _EMISSION
                color += LIGHTWEIGHT_GAMMA_TO_LINEAR(tex2D(_EmissionMap, i.uv01.xy).rgb) * _EmissionColor;
#else
                color += _EmissionColor;
#endif

#if defined(LIGHTMAP_ON)
                color += (DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.uv01.zw)) + i.fogCoord.yzw) * diffuse;
#elif defined(_VERTEX_LIGHTS) || defined(_LIGHT_PROBES_ON)
                color += i.fogCoord.yzw * diffuse;
#endif

#if _REFLECTION_CUBEMAP
                // TODO: we can use reflect vec to compute specular instead of half when computing cubemap reflection
                half3 reflectVec = reflect(-i.viewDir.xyz, normal);
                color += texCUBE(_Cube, reflectVec).rgb * specularGloss.rgb;
#elif defined(_REFLECTION_PROBE)
                half3 reflectVec = reflect(-i.viewDir.xyz, normal);
                half4 reflectionProbe = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, reflectVec);
                color += reflectionProbe.rgb * (reflectionProbe.a * unity_SpecCube0_HDR.x) * specularGloss.rgb;
#endif

                UNITY_APPLY_FOG(i.fogCoord, color);

                return OutputColor(color, alpha);
            };

#endif
