#ifndef UNIVERSAL_SPEEDTREE8_PASSES_INCLUDED
#define UNIVERSAL_SPEEDTREE8_PASSES_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferOutput.hlsl"
#include "Packages/com.unity.shadergraph/ShaderGraphLibrary/Nature/SpeedTreeCommon.hlsl"
#include "SpeedTreeUtility.hlsl"
#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

struct SpeedTreeVertexInput
{
    float4 vertex       : POSITION;
    float3 normal       : NORMAL;
    float4 tangent      : TANGENT;
    float4 texcoord     : TEXCOORD0;
    float4 texcoord1    : TEXCOORD1;
    float4 texcoord2    : TEXCOORD2;
    float4 texcoord3    : TEXCOORD3;
    float4 color        : COLOR;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct SpeedTreeVertexOutput
{
    half2 uv                        : TEXCOORD0;
    half4 color                     : TEXCOORD1;

    half4 fogFactorAndVertexLight   : TEXCOORD2;    // x: fogFactor, yzw: vertex light

    #ifdef EFFECT_BUMP
        half4 normalWS              : TEXCOORD3;    // xyz: normal, w: viewDir.x
        half4 tangentWS             : TEXCOORD4;    // xyz: tangent, w: viewDir.y
        half4 bitangentWS           : TEXCOORD5;    // xyz: bitangent, w: viewDir.z
    #else
        half3 normalWS              : TEXCOORD3;
        half3 viewDirWS             : TEXCOORD4;
    #endif

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        float4 shadowCoord          : TEXCOORD6;
    #endif

    float3 positionWS               : TEXCOORD7;
    DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 8);
    float4 clipPos                  : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

struct SpeedTreeVertexDepthOutput
{
    half2 uv                        : TEXCOORD0;
    half4 color                     : TEXCOORD1;
    half3 viewDirWS                 : TEXCOORD2;
    float4 clipPos                  : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

struct SpeedTreeVertexDepthNormalOutput
{
    half2 uv                        : TEXCOORD0;
    half4 color                     : TEXCOORD1;

    #ifdef EFFECT_BUMP
        half4 normalWS              : TEXCOORD2;    // xyz: normal, w: viewDir.x
        half4 tangentWS             : TEXCOORD3;    // xyz: tangent, w: viewDir.y
        half4 bitangentWS           : TEXCOORD4;    // xyz: bitangent, w: viewDir.z
    #else
        half3 normalWS              : TEXCOORD2;
        half3 viewDirWS             : TEXCOORD3;
    #endif

    float4 clipPos                  : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

struct SpeedTreeDepthNormalFragmentInput
{
    SpeedTreeVertexDepthNormalOutput interpolated;
#ifdef EFFECT_BACKSIDE_NORMALS
    FRONT_FACE_TYPE facing : FRONT_FACE_SEMANTIC;
#endif
};

struct SpeedTreeFragmentInput
{
    SpeedTreeVertexOutput interpolated;
#ifdef EFFECT_BACKSIDE_NORMALS
    FRONT_FACE_TYPE facing : FRONT_FACE_SEMANTIC;
#endif
};

void InitializeData(inout SpeedTreeVertexInput input, float lodValue)
{
#if !defined(EFFECT_BILLBOARD)
    #if defined(LOD_FADE_PERCENTAGE)
    UNITY_BRANCH if (unity_LODFade.w <= 1.0)
        input.vertex.xyz = lerp(input.vertex.xyz, input.texcoord2.xyz, lodValue);
    #endif

    // geometry type
    float geometryType = (int) (input.texcoord3.w + 0.25);
    bool leafTwo = false;
    if (geometryType > GEOM_TYPE_FACINGLEAF)
    {
        geometryType -= 2;
        leafTwo = true;
    }

    // leaf facing
    if (geometryType == GEOM_TYPE_FACINGLEAF)
    {
        float3 anchor = float3(input.texcoord1.zw, input.texcoord2.w);
        input.vertex.xyz = DoLeafFacing(input.vertex.xyz, anchor);
    }
#endif

    // wind
    #if defined(ENABLE_WIND) && !defined(_WINDQUALITY_NONE)
        float windEnabled = dot(_ST_WindVector.xyz, _ST_WindVector.xyz) > 0.0f ? 1.0f : 0.0f;
        if (windEnabled > 0)
        {
            float3 rotatedWindVector = mul(_ST_WindVector.xyz, (float3x3)UNITY_MATRIX_M);
            float windLength = length(rotatedWindVector);
            if (windLength < 1e-5)
            {
                // sanity check that wind data is available
                return;
            }
            rotatedWindVector /= windLength;

            float3 treePos = float3(UNITY_MATRIX_M[0].w, UNITY_MATRIX_M[1].w, UNITY_MATRIX_M[2].w);
            float3 windyPosition = input.vertex.xyz;

            #ifndef EFFECT_BILLBOARD

                // leaves
                if (geometryType > GEOM_TYPE_FROND)
                {
                    // remove anchor position
                    float3 anchor = float3(input.texcoord1.zw, input.texcoord2.w);
                    windyPosition -= anchor;

                    // leaf wind
                    #if defined(_WINDQUALITY_FAST) || defined(_WINDQUALITY_BETTER) || defined(_WINDQUALITY_BEST)
                        #ifdef _WINDQUALITY_BEST
                            bool bBestWind = true;
                        #else
                            bool bBestWind = false;
                        #endif
                        float leafWindTrigOffset = anchor.x + anchor.y;
                        windyPosition = LeafWind(bBestWind, leafTwo, windyPosition, input.normal, input.texcoord3.x, float3(0,0,0), input.texcoord3.y, input.texcoord3.z, leafWindTrigOffset, rotatedWindVector);
                    #endif

                    // move back out to anchor
                    windyPosition += anchor;
                }

                // frond wind
                bool bPalmWind = false;
                #ifdef _WINDQUALITY_PALM
                    bPalmWind = true;
                    if (geometryType == GEOM_TYPE_FROND)
                    {
                        windyPosition = RippleFrond(windyPosition, input.normal, input.texcoord.x, input.texcoord.y, input.texcoord3.x, input.texcoord3.y, input.texcoord3.z);
                    }
                #endif

                // branch wind (applies to all 3D geometry)
                #if defined(_WINDQUALITY_BETTER) || defined(_WINDQUALITY_BEST) || defined(_WINDQUALITY_PALM)
                    float3 rotatedBranchAnchor = normalize(mul(_ST_WindBranchAnchor.xyz, (float3x3)UNITY_MATRIX_M)) * _ST_WindBranchAnchor.w;
                    windyPosition = BranchWind(bPalmWind, windyPosition, treePos, float4(input.texcoord.zw, 0, 0), rotatedWindVector, rotatedBranchAnchor);
                #endif

            #endif // !EFFECT_BILLBOARD

            // global wind
            float globalWindTime = _ST_WindGlobal.x;
            #if defined(EFFECT_BILLBOARD) && defined(UNITY_INSTANCING_ENABLED)
                globalWindTime += UNITY_ACCESS_INSTANCED_PROP(STWind, _GlobalWindTime);
            #endif

            windyPosition = GlobalWind(windyPosition, treePos, true, rotatedWindVector, globalWindTime);
            input.vertex.xyz = windyPosition;
        }
    #endif

    #if defined(EFFECT_BILLBOARD)
        float3 treePos = float3(UNITY_MATRIX_M[0].w, UNITY_MATRIX_M[1].w, UNITY_MATRIX_M[2].w);
        // crossfade faces
        bool topDown = (input.texcoord.z > 0.5);
        float3 viewDir = UNITY_MATRIX_IT_MV[2].xyz;
        float3 cameraDir = normalize(mul((float3x3)UNITY_MATRIX_M, _WorldSpaceCameraPos - treePos));
        float viewDot = max(dot(viewDir, input.normal), dot(cameraDir, input.normal));
        viewDot *= viewDot;
        viewDot *= viewDot;
        viewDot += topDown ? 0.38 : 0.18; // different scales for horz and vert billboards to fix transition zone

        // if invisible, avoid overdraw
        if (viewDot < 0.3333)
        {
            input.vertex.xyz = float3(0, 0, 0);
        }

        input.color = float4(1, 1, 1, clamp(viewDot, 0, 1));

        // adjust lighting on billboards to prevent seams between the different faces
        if (topDown)
        {
            input.normal += cameraDir;
        }
        else
        {
            half3 binormal = cross(input.normal, input.tangent.xyz) * input.tangent.w;
            float3 right = cross(cameraDir, binormal);
            input.normal = cross(binormal, right);
        }
        input.normal = normalize(input.normal);
    #endif
}

SpeedTreeVertexOutput SpeedTree8Vert(SpeedTreeVertexInput input)
{
    SpeedTreeVertexOutput output = (SpeedTreeVertexOutput)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    // handle speedtree wind and lod
    InitializeData(input, unity_LODFade.x);

    output.uv = input.texcoord.xy;
    output.color = input.color;

    // color already contains (ao, ao, ao, blend)
    // put hue variation amount in there
    #ifdef EFFECT_HUE_VARIATION
        float3 treePos = float3(UNITY_MATRIX_M[0].w, UNITY_MATRIX_M[1].w, UNITY_MATRIX_M[2].w);
        float hueVariationAmount = frac(treePos.x + treePos.y + treePos.z);
        output.color.g = saturate(hueVariationAmount * _HueVariationColor.a);
    #endif

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.vertex.xyz);
    half3 normalWS = TransformObjectToWorldNormal(input.normal);

    half3 vertexLight = VertexLighting(vertexInput.positionWS, normalWS);
    half fogFactor = 0.0;
    #if !defined(_FOG_FRAGMENT)
    fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
    #endif
    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);

    #ifdef EFFECT_BUMP
        real sign = input.tangent.w * GetOddNegativeScale();
        output.normalWS.xyz = normalWS;
        output.tangentWS.xyz = TransformObjectToWorldDir(input.tangent.xyz);
        output.bitangentWS.xyz = cross(output.normalWS.xyz, output.tangentWS.xyz) * sign;

        // View dir packed in w.
        output.normalWS.w = viewDirWS.x;
        output.tangentWS.w = viewDirWS.y;
        output.bitangentWS.w = viewDirWS.z;
    #else
        output.normalWS = normalWS;
        output.viewDirWS = viewDirWS;
    #endif

    output.positionWS = vertexInput.positionWS;

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        output.shadowCoord = GetShadowCoord(vertexInput);
    #endif

    output.clipPos = vertexInput.positionCS;

    OUTPUT_SH4(vertexInput.positionWS, output.normalWS.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS), output.vertexSH, output.probeOcclusion);

    return output;
}

SpeedTreeVertexDepthOutput SpeedTree8VertDepth(SpeedTreeVertexInput input)
{
    SpeedTreeVertexDepthOutput output = (SpeedTreeVertexDepthOutput)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    // handle speedtree wind and lod
    InitializeData(input, unity_LODFade.x);
    output.uv = input.texcoord.xy;
    output.color = input.color;

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.vertex.xyz);

    output.viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);

#ifdef SHADOW_CASTER
    half3 normalWS = TransformObjectToWorldNormal(input.normal);

#if _CASTING_PUNCTUAL_LIGHT_SHADOW
    float3 lightDirectionWS = normalize(_LightPosition - vertexInput.positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif

    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(vertexInput.positionWS, normalWS, lightDirectionWS));
    output.clipPos = positionCS;
#else
    output.clipPos = vertexInput.positionCS;
#endif

    return output;
}

void InitializeInputData(SpeedTreeFragmentInput input, half3 normalTS, out InputData inputData)
{
    inputData = (InputData)0;

    inputData.positionWS = input.interpolated.positionWS.xyz;
    inputData.positionCS = input.interpolated.clipPos;

#ifdef EFFECT_BUMP
    inputData.normalWS = TransformTangentToWorld(normalTS, half3x3(input.interpolated.tangentWS.xyz, input.interpolated.bitangentWS.xyz, input.interpolated.normalWS.xyz));
    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    inputData.viewDirectionWS = half3(input.interpolated.normalWS.w, input.interpolated.tangentWS.w, input.interpolated.bitangentWS.w);
#else
    inputData.normalWS = NormalizeNormalPerPixel(input.interpolated.normalWS);
    inputData.viewDirectionWS = input.interpolated.viewDirWS;
#endif

    inputData.viewDirectionWS = SafeNormalize(inputData.viewDirectionWS);

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        inputData.shadowCoord = input.interpolated.shadowCoord;
    #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
        inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
    #else
        inputData.shadowCoord = float4(0, 0, 0, 0);
    #endif

    inputData.fogCoord = InitializeInputDataFog(float4(input.interpolated.positionWS, 1.0), input.interpolated.fogFactorAndVertexLight.x);
    inputData.vertexLighting = input.interpolated.fogFactorAndVertexLight.yzw;
#if !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
    inputData.bakedGI = SAMPLE_GI(input.interpolated.vertexSH,
        GetAbsolutePositionWS(inputData.positionWS),
        inputData.normalWS,
        inputData.viewDirectionWS,
        inputData.positionCS.xy,
        input.probeOcclusion,
        inputData.shadowMask);
#else
    inputData.bakedGI = SAMPLE_GI(NOT_USED, input.interpolated.vertexSH, inputData.normalWS);
#endif

    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.interpolated.clipPos);
    inputData.shadowMask = half4(1, 1, 1, 1); // No GI currently.

    #if defined(DEBUG_DISPLAY) && !defined(LIGHTMAP_ON)
    inputData.vertexSH = input.interpolated.vertexSH;
    #endif

    #if defined(_NORMALMAP)
    inputData.tangentToWorld = half3x3(input.interpolated.tangentWS.xyz, input.interpolated.bitangentWS.xyz, input.interpolated.normalWS.xyz);
    #endif
}

#ifdef GBUFFER
GBufferFragOutput SpeedTree8Frag(SpeedTreeFragmentInput input)
#else
half4 SpeedTree8Frag(SpeedTreeFragmentInput input) : SV_Target
#endif
{
    UNITY_SETUP_INSTANCE_ID(input.interpolated);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input.interpolated);

    half2 uv = input.interpolated.uv;
    half4 diffuse = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_MainTex, sampler_MainTex)) * _Color;

    half alpha = diffuse.a * input.interpolated.color.a;
    alpha = AlphaDiscard(alpha, 0.3333);

    #ifdef LOD_FADE_CROSSFADE
        LODFadeCrossFade(input.interpolated.clipPos);
    #endif

    half3 albedo = diffuse.rgb;
    half3 emission = 0;
    half metallic = 0;
    half smoothness = 0;
    half occlusion = 0;
    half3 specular = 0;

    // hue variation
    #ifdef EFFECT_HUE_VARIATION
        half3 shiftedColor = lerp(albedo, _HueVariationColor.rgb, input.interpolated.color.g);

        // preserve vibrance
        half maxBase = max(albedo.r, max(albedo.g, albedo.b));
        half newMaxBase = max(shiftedColor.r, max(shiftedColor.g, shiftedColor.b));
        maxBase /= newMaxBase;
        maxBase = maxBase * 0.5f + 0.5f;
        shiftedColor.rgb *= maxBase;

        albedo = saturate(shiftedColor);
    #endif

    // normal
    #ifdef EFFECT_BUMP
        half3 normalTs = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap));
    #else
        half3 normalTs = half3(0, 0, 1);
    #endif

    // flip normal on backsides
    #ifdef EFFECT_BACKSIDE_NORMALS
        normalTs.z = IS_FRONT_VFACE(input.facing, normalTs.z, -normalTs.z);
    #endif

    // adjust billboard normals to improve GI and matching
    #ifdef EFFECT_BILLBOARD
        normalTs.z *= 0.5;
        normalTs = normalize(normalTs);
    #endif

    // extra
    #ifdef EFFECT_EXTRA_TEX
        half4 extra = tex2D(_ExtraTex, uv);
        smoothness = extra.r;
        metallic = extra.g;
        occlusion = extra.b * input.interpolated.color.r;
    #else
        smoothness = _Glossiness;
        metallic = _Metallic;
        occlusion = input.interpolated.color.r;
    #endif

    InputData inputData;
    InitializeInputData(input, normalTs, inputData);
    SETUP_DEBUG_TEXTURE_DATA(inputData, input.interpolated.uv);

#if defined(GBUFFER) || defined(EFFECT_SUBSURFACE)
    Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, inputData.shadowMask);
#endif

    // subsurface (hijack emissive)
    #ifdef EFFECT_SUBSURFACE
    half fSubsurfaceRough = 0.7 - smoothness * 0.5;
    half fSubsurface = D_GGX(clamp(-dot(mainLight.direction.xyz, inputData.viewDirectionWS.xyz), 0, 1), fSubsurfaceRough);

    float4 shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
    half realtimeShadow = MainLightRealtimeShadow(shadowCoord);
    float3 tintedSubsurface = tex2D(_SubsurfaceTex, uv).rgb * _SubsurfaceColor.rgb;
        float3 directSubsurface = tintedSubsurface.rgb * mainLight.color.rgb * fSubsurface * realtimeShadow;
    float3 indirectSubsurface = tintedSubsurface.rgb * inputData.bakedGI.rgb * _SubsurfaceIndirect;
    emission = directSubsurface + indirectSubsurface;
    #endif

#ifdef GBUFFER
    // in LitForwardPass GlobalIllumination (and temporarily LightingPhysicallyBased) are called inside UniversalFragmentPBR
    // in Deferred rendering we store the sum of these values (and of emission as well) in the GBuffer
    BRDFData brdfData;
    InitializeBRDFData(albedo, metallic, specular, smoothness, alpha, brdfData);

    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI, inputData.shadowMask);
    half3 color = GlobalIllumination(brdfData, (BRDFData)0, 0, inputData.bakedGI, occlusion, inputData.positionWS,
                                     inputData.normalWS, inputData.viewDirectionWS, inputData.normalizedScreenSpaceUV);

    return PackGBuffersBRDFData(brdfData, inputData, smoothness, emission + color, occlusion);

#else
    SurfaceData surfaceData;

    surfaceData.albedo = albedo;
    surfaceData.specular = specular;
    surfaceData.metallic = metallic;
    surfaceData.smoothness = smoothness;
    surfaceData.normalTS = normalTs;
    surfaceData.emission = emission;
    surfaceData.occlusion = occlusion;
    surfaceData.alpha = alpha;
    surfaceData.clearCoatMask = 0;
    surfaceData.clearCoatSmoothness = 1;

#if defined(DEBUG_DISPLAY)
    inputData.uv = uv;
#endif

    half4 color = UniversalFragmentPBR(inputData, surfaceData);

    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    color.a = OutputAlpha(color.a, _Surface);

    return color;

#endif
}

half4 SpeedTree8FragDepth(SpeedTreeVertexDepthOutput input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    half2 uv = input.uv;
    half4 diffuse = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_MainTex, sampler_MainTex)) * _Color;

    half alpha = diffuse.a * input.color.a;
    AlphaDiscard(alpha, 0.3333);

    #ifdef LOD_FADE_CROSSFADE
        LODFadeCrossFade(input.clipPos);
    #endif

    #if defined(SCENESELECTIONPASS)
        // We use depth prepass for scene selection in the editor, this code allow to output the outline correctly
        return half4(_ObjectId, _PassValue, 1.0, 1.0);
    #else
        return half4(input.clipPos.z, 0, 0, 0);
    #endif
}

SpeedTreeVertexDepthNormalOutput SpeedTree8VertDepthNormal(SpeedTreeVertexInput input)
{
    SpeedTreeVertexDepthNormalOutput output = (SpeedTreeVertexDepthNormalOutput)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    // handle speedtree wind and lod
    InitializeData(input, unity_LODFade.x);
    output.uv = input.texcoord.xy;
    output.color = input.color;

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.vertex.xyz);
    half3 normalWS = TransformObjectToWorldNormal(input.normal);
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
    #ifdef EFFECT_BUMP
        real sign = input.tangent.w * GetOddNegativeScale();
        output.normalWS.xyz = normalWS;
        output.tangentWS.xyz = TransformObjectToWorldDir(input.tangent.xyz);
        output.bitangentWS.xyz = cross(output.normalWS.xyz, output.tangentWS.xyz) * sign;

        // View dir packed in w.
        output.normalWS.w = viewDirWS.x;
        output.tangentWS.w = viewDirWS.y;
        output.bitangentWS.w = viewDirWS.z;
    #else
        output.normalWS = normalWS;
        output.viewDirWS = viewDirWS;
    #endif

    output.clipPos = vertexInput.positionCS;
    return output;
}

half4 SpeedTree8FragDepthNormal(SpeedTreeDepthNormalFragmentInput input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input.interpolated);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input.interpolated);

    half2 uv = input.interpolated.uv;
    half4 diffuse = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_MainTex, sampler_MainTex)) * _Color;

    half alpha = diffuse.a * input.interpolated.color.a;
    AlphaDiscard(alpha, 0.3333);

    #ifdef LOD_FADE_CROSSFADE
        LODFadeCrossFade(input.interpolated.clipPos);
    #endif

    // normal
    #if defined(EFFECT_BUMP)
        half3 normalTs = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap));
    #else
        half3 normalTs = half3(0, 0, 1);
    #endif

    // flip normal on backsides
    #ifdef EFFECT_BACKSIDE_NORMALS
        if (input.facing < 0.5)
        {
            normalTs.z = -normalTs.z;
        }
    #endif

    // adjust billboard normals to improve GI and matching
    #if defined(EFFECT_BILLBOARD)
        normalTs.z *= 0.5;
        normalTs = normalize(normalTs);
    #endif

    #if defined(EFFECT_BUMP)
        float3 normalWS = TransformTangentToWorld(normalTs, half3x3(input.interpolated.tangentWS.xyz, input.interpolated.bitangentWS.xyz, input.interpolated.normalWS.xyz));
        return half4(NormalizeNormalPerPixel(normalWS), 0.0h);
    #else
        return half4(NormalizeNormalPerPixel(input.interpolated.normalWS), 0.0h);
    #endif
}

#endif
