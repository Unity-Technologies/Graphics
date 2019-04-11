#ifndef HDRP_SPEEDTREE8_PASSES_INCLUDED
#define HDRP_SPEEDTREE8_PASSES_INCLUDED

#ifdef CUSTOM_UNPACK

FragInputs UnpackVaryingsMeshToFragInputs(PackedVaryingsMeshToPS input)
{
    FragInputs output;
    ZERO_INITIALIZE(FragInputs, output);

    UNITY_SETUP_INSTANCE_ID(input);

    output.worldToTangent = k_identity3x3;

    output.positionSS = input.positionCS;   // input.positionCS is SV_Position
    output.positionRWS.xyz = input.interpolators0.xyz;

    // uvHueVariation.xy
    output.texCoord0.xy = input.interpolators3.xy;

#ifdef VARYINGS_NEED_TANGENT_TO_WORLD
#ifdef EFFECT_BUMP
    output.worldToTangent = BuildWorldToTangent(input.interpolators2, input.interpolators1);
#else
    output.worldToTangent = BuildWorldToTangent(input.interpolators2, input.interpolators1);
#endif
    output.worldToTangent[0] = GetOddNegativeScale() * cross(output.worldToTangent[2], output.worldToTangent[1]);
#endif

    // Vertex Color
    output.color.rgba = input.interpolators5.rgba;

#if (SHADERPASS != SHADERPASS_SHADOWS) && (SHADERPASS != SHADERPASS_DEPTH_ONLY)
    // Z component of uvHueVariation
#ifdef EFFECT_HUE_VARIATION
    output.texCoord0.z = input.interpolators3.z;
#endif
	output.texCoord0.w = input.interpolators3.w;

#endif

    return output;
}

#endif // CUSTOM_UNPACK


void InitializeData(inout SpeedTreeVertexInput input, float lodValue, inout float geometryType)
{
    // smooth LOD
#if defined(LOD_FADE_PERCENTAGE) && !defined(EFFECT_BILLBOARD)
    input.vertex.xyz = lerp(input.vertex.xyz, input.texcoord2.xyz, lodValue);
#endif

    // wind
#if defined(ENABLE_WIND) && !defined(_WINDQUALITY_NONE)
    if (_WindEnabled > 0)
    {
		float3 rotatedWindVector = normalize(mul(_ST_WindVector.xyz, (float3x3)UNITY_MATRIX_M));
        //float3 rotatedWindVector = mul(_ST_WindVector.xyz, (float3x3)unity_ObjectToWorld);
        float windLength = length(rotatedWindVector);
        if (windLength < 1e-5)
        {
            // sanity check that wind data is available
            return;
        }
        rotatedWindVector /= windLength;

        //float3 treePos = float3(unity_ObjectToWorld[0].w, unity_ObjectToWorld[1].w, unity_ObjectToWorld[2].w);
		float3 treePos = float3(UNITY_MATRIX_M[0].w, UNITY_MATRIX_M[1].w, UNITY_MATRIX_M[2].w);
        float3 windyPosition = input.vertex.xyz;

#ifndef EFFECT_BILLBOARD
        // geometry type
        geometryType = (int)(input.texcoord3.w + 0.25);
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
				float4x4 mtx_ITMV = transpose(mul(UNITY_MATRIX_I_M, unity_MatrixInvV));
                //windyPosition = mul(windyPosition.xyz, (float3x3)UNITY_MATRIX_IT_MV); // inv(MV) * windyPosition
				windyPosition = mul(mtx_ITMV, float4(windyPosition.xyz, 0)).xyz;
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
            windyPosition = LeafWind(bBestWind, leafTwo, windyPosition, input.normal, input.texcoord3.x, float3(0, 0, 0), input.texcoord3.y, input.texcoord3.z, leafWindTrigOffset, rotatedWindVector);
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
#endif	// defined(ENABLE_WIND) && !defined(_WINDQUALITY_NONE)

#if defined(EFFECT_BILLBOARD)
    float3 treePos = float3(UNITY_MATRIX_M[0].w, UNITY_MATRIX_M[1].w, UNITY_MATRIX_M[2].w);
    // crossfade faces
    bool topDown = (input.texcoord.z > 0.5);
	float4x4 mtx_ITMV = transpose(mul(UNITY_MATRIX_I_M, unity_MatrixInvV));
    float3 viewDir = mtx_ITMV[2].xyz;
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

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

PackedVaryingsType SpeedTree8Vert(SpeedTreeVertexInput input)
{
    PackedVaryingsType output = (PackedVaryingsType)0;
	float geomType = 0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output.vmesh);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output.vmesh);

    // handle speedtree wind and lod
    InitializeData(input, unity_LODFade.x, geomType);

    float3 positionWS = TransformObjectToWorld(input.vertex.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normal);
    float3 viewDirWS = _WorldSpaceCameraPos - positionWS;

    float4 positionCS = TransformWorldToHClip(positionWS);

#if (SHADERPASS == SHADERPASS_DEPTH_ONLY) || (SHADERPASS == SHADERPASS_SHADOWS)
    positionCS.z -= _ZBias;
#endif

#ifdef EFFECT_BUMP
    output.vmesh.interpolators1 = normalWS;
    output.vmesh.interpolators2.xyz = TransformObjectToWorldDir(input.tangent.xyz);
#else
    output.vmesh.interpolators1 = normalWS;
    output.vmesh.interpolators2.xyz = viewDirWS;
#endif
    output.vmesh.interpolators2.w = -1.0;

    // uvHueVariation.xy as well as diffuseUV
    output.vmesh.interpolators3.xy = input.texcoord.xy;
    output.vmesh.interpolators5.rgb = _Color.rgb;
    output.vmesh.interpolators5.a = input.color.r;      // ambient occlusion factor

#if (SHADERPASS != SHADERPASS_SHADOWS) && (SHADERPASS != SHADERPASS_DEPTH_ONLY)
                                                        // Z component of uvHueVariation
#ifdef EFFECT_HUE_VARIATION
    float4x4 objToWorld = GetRawUnityObjectToWorld();
    float hueVariationAmount = frac(objToWorld[0].w + objToWorld[1].w + objToWorld[2].w);
    hueVariationAmount += frac(input.vertex.x + input.normal.y + input.normal.x) * 0.5 - 0.3;
    output.vmesh.interpolators3.z = saturate(hueVariationAmount * _HueVariationColor.a);
#endif
	// Pass down the geometry type so we can use it in the fragment data prep
	output.vmesh.interpolators3.w = geomType;

#endif

    output.vmesh.interpolators0.xyz = positionWS;
    output.vmesh.positionCS = positionCS;

    return output;
}

#endif
