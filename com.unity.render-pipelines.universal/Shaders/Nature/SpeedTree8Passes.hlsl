#ifndef UNIVERSAL_SPEEDTREE8_PASSES_INCLUDED
#define UNIVERSAL_SPEEDTREE8_PASSES_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
    float4 clipPos                  : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

struct SpeedTreeVertexDepthOutput
{
    half2 uv                        : TEXCOORD0;
    half4 color                     : TEXCOORD1;
    float4 clipPos                  : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

struct SpeedTreeFragmentInput
{
    SpeedTreeVertexOutput interpolated;
#ifdef EFFECT_BACKSIDE_NORMALS
    half facing : VFACE;
#endif
};

void InitializeData(inout SpeedTreeVertexInput input, float lodValue)
{
    // smooth LOD
    #if defined(LOD_FADE_PERCENTAGE) && !defined(EFFECT_BILLBOARD)
        input.vertex.xyz = lerp(input.vertex.xyz, input.texcoord2.xyz, lodValue);
    #endif

    // wind
    #if defined(ENABLE_WIND) && !defined(_WINDQUALITY_NONE)
        if (_WindEnabled > 0)
        {
            float3 rotatedWindVector = mul(_ST_WindVector.xyz, (float3x3)unity_ObjectToWorld);
            float windLength = length(rotatedWindVector);
            if (windLength < 1e-5)
            {
                // sanity check that wind data is available
                return;
            }
            rotatedWindVector /= windLength;

            float3 treePos = float3(unity_ObjectToWorld[0].w, unity_ObjectToWorld[1].w, unity_ObjectToWorld[2].w);
            float3 windyPosition = input.vertex.xyz;

            #ifndef EFFECT_BILLBOARD
                // geometry type
                float geometryType = (int)(input.texcoord3.w + 0.25);
                bool leafTwo = false;
                if (geometryType > GEOM_TYPE_FACINGLEAF)
                {
                    geometryType -= 2;
                    leafTwo = true;
                }

                // leaves
                if (geometryType > GEOM_TYPE_FROND)
                {
                    // remove anchor position
                    float3 anchor = float3(input.texcoord1.zw, input.texcoord2.w);
                    windyPosition -= anchor;

                    if (geometryType == GEOM_TYPE_FACINGLEAF)
                    {
                        // face camera-facing leaf to camera
                        float offsetLen = length(windyPosition);
                        windyPosition = mul(windyPosition.xyz, (float3x3)UNITY_MATRIX_IT_MV); // inv(MV) * windyPosition
                        windyPosition = normalize(windyPosition) * offsetLen; // make sure the offset vector is still scaled
                    }

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
                    float3 rotatedBranchAnchor = normalize(mul(_ST_WindBranchAnchor.xyz, (float3x3)unity_ObjectToWorld)) * _ST_WindBranchAnchor.w;
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
    half fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

    half3 viewDirWS = GetCameraPositionWS() - vertexInput.positionWS;

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

#ifdef SHADOW_CASTER
    half3 normalWS = TransformObjectToWorldNormal(input.normal);
    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(vertexInput.positionWS, normalWS, _LightDirection));
    output.clipPos = positionCS;
#else
    output.clipPos = vertexInput.positionCS;
#endif

    return output;
}

void InitializeInputData(SpeedTreeFragmentInput input, half3 normalTS, out InputData inputData)
{
    inputData.positionWS = input.interpolated.positionWS.xyz;

#ifdef EFFECT_BUMP
    inputData.normalWS = TransformTangentToWorld(normalTS, half3x3(input.interpolated.tangentWS.xyz, input.interpolated.bitangentWS.xyz, input.interpolated.normalWS.xyz));
    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    inputData.viewDirectionWS = half3(input.interpolated.normalWS.w, input.interpolated.tangentWS.w, input.interpolated.bitangentWS.w);
#else
    inputData.normalWS = NormalizeNormalPerPixel(input.interpolated.normalWS);
    inputData.viewDirectionWS = input.interpolated.viewDirWS;
#endif

#if SHADER_HINT_NICE_QUALITY
    inputData.viewDirectionWS = SafeNormalize(inputData.viewDirectionWS);
#endif

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        inputData.shadowCoord = input.interpolated.shadowCoord;
    #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
        inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
    #else
        inputData.shadowCoord = float4(0, 0, 0, 0);
    #endif

    inputData.fogCoord = input.interpolated.fogFactorAndVertexLight.x;
    inputData.vertexLighting = input.interpolated.fogFactorAndVertexLight.yzw;
    inputData.bakedGI = half3(0, 0, 0); // No GI currently.
}

half4 SpeedTree8Frag(SpeedTreeFragmentInput input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input.interpolated);

#if !defined(SHADER_QUALITY_LOW)
    #ifdef LOD_FADE_CROSSFADE // enable dithering LOD transition if user select CrossFade transition in LOD group
        #ifdef EFFECT_BILLBOARD
            LODDitheringTransition(input.interpolated.clipPos.xy, unity_LODFade.x);
        #endif
    #endif
#endif

    half2 uv = input.interpolated.uv;
    half4 diffuse = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_MainTex, sampler_MainTex)) * _Color;

    half alpha = diffuse.a * input.interpolated.color.a;
    AlphaDiscard(alpha - 0.3333, 0.0);

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
        if (input.facing < 0.5)
        {
            normalTs.z = -normalTs.z;
        }
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

    // subsurface (hijack emissive)
    #ifdef EFFECT_SUBSURFACE
        emission = tex2D(_SubsurfaceTex, uv).rgb * _SubsurfaceColor.rgb;
    #endif

    InputData inputData;
    InitializeInputData(input, normalTs, inputData);

    half4 color = UniversalFragmentPBR(inputData, albedo, metallic, specular, smoothness, occlusion, emission, alpha);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    color.a = OutputAlpha(color.a);

    return color;
}

half4 SpeedTree8FragDepth(SpeedTreeVertexDepthOutput input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);

#if !defined(SHADER_QUALITY_LOW)
    #ifdef LOD_FADE_CROSSFADE // enable dithering LOD transition if user select CrossFade transition in LOD group
        #ifdef EFFECT_BILLBOARD
            LODDitheringTransition(input.clipPos.xy, unity_LODFade.x);
        #endif
    #endif
#endif

    half2 uv = input.uv;
    half4 diffuse = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_MainTex, sampler_MainTex)) * _Color;

    half alpha = diffuse.a * input.color.a;
    AlphaDiscard(alpha - 0.3333, 0.0);

    #if defined(SCENESELECTIONPASS)
        // We use depth prepass for scene selection in the editor, this code allow to output the outline correctly
        return half4(_ObjectId, _PassValue, 1.0, 1.0);
    #else
        return half4(0, 0, 0, 0);
    #endif
}

#endif
