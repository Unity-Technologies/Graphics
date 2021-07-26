#ifndef UNITY_AXF_PATH_TRACING_CAR_PAINT_INCLUDED
#define UNITY_AXF_PATH_TRACING_CAR_PAINT_INCLUDED

// By defining this, we replace specular GGX values with the original Cook-Torrance ones used in the raster version
#define AXF_PATH_TRACING_CAR_PAINT_USE_RASTER_SPECULAR

#ifdef AXF_PATH_TRACING_CAR_PAINT_USE_RASTER_SPECULAR
void OverrideSpecularValue(MaterialData mtlData, float3 sampleDir, out float3 specularValue)
{
    float3 H = normalize(mtlData.V + sampleDir);
    float NdotL = dot(GetSpecularNormal(mtlData), sampleDir);
    float NdotV = dot(GetSpecularNormal(mtlData), mtlData.V);
    float NdotH = dot(GetSpecularNormal(mtlData), H);
    float VdotH = dot(mtlData.V, H);
    specularValue = MultiLobesCookTorrance(mtlData.bsdfData, NdotL, NdotV, NdotH, VdotH) * NdotL;
}
#endif

bool SampleSpecular(MaterialData mtlData, float3 inputSample, out float3 sampleDir, out float3 value, out float pdf)
{
    uint i;

    float lobeWeight[CARPAINT2_LOBE_COUNT], CoeffNormalization = rcp(GetSpecularCoeffSum(mtlData)); // We know the sum is > 0.0

    UNITY_UNROLL
    for (i = 0; i < CARPAINT2_LOBE_COUNT; i++)
        lobeWeight[i] = _CarPaint2_CTCoeffs[i] * CoeffNormalization;

    uint lobeIndex[CARPAINT2_LOBE_COUNT];

    UNITY_UNROLL
    for (i = 0; i < CARPAINT2_LOBE_COUNT; i++)
    {
        if (inputSample.z < lobeWeight[i])
            break;
        inputSample.z -= lobeWeight[i];
    }

    // Should always be the case, but let's avoid any precision issue
    i = min(i, CARPAINT2_LOBE_COUNT - 1);

    UNITY_UNROLL
    for (uint j = 0; j < CARPAINT2_LOBE_COUNT; j++)
        lobeIndex[j] = (i + j) % CARPAINT2_LOBE_COUNT;

    if (!BRDF::SampleGGX(mtlData, GetSpecularNormal(mtlData), mtlData.bsdfData.roughness[lobeIndex[0]], _CarPaint2_CTF0s[lobeIndex[0]], inputSample, sampleDir, value, pdf))
        return false;

    value *= _CarPaint2_CTCoeffs[lobeIndex[0]];
    pdf *= lobeWeight[lobeIndex[0]];

    float3 lobeValue;
    float lobePdf;

    UNITY_UNROLL
    for (i = 1; i < CARPAINT2_LOBE_COUNT; i++)
    {
        BRDF::EvaluateGGX(mtlData, GetSpecularNormal(mtlData), mtlData.bsdfData.roughness[lobeIndex[i]], _CarPaint2_CTF0s[lobeIndex[i]], sampleDir, lobeValue, lobePdf);
        value += lobeValue * _CarPaint2_CTCoeffs[lobeIndex[i]];
        pdf += lobePdf * lobeWeight[lobeIndex[i]];
    }

#ifdef AXF_PATH_TRACING_CAR_PAINT_USE_RASTER_SPECULAR
    OverrideSpecularValue(mtlData, sampleDir, value);
#else
    value *= 4.0 / PI; // Matches an error in MultiLobesCookTorrance(...) in AxF.hlsl
#endif

    return true;
}

void EvaluateSpecular(MaterialData mtlData, float3 sampleDir, out float3 value, out float pdf)
{
    value = 0.0;
    pdf = 0.0;

    float3 lobeValue;
    float lobePdf, CoeffNormalization = rcp(GetSpecularCoeffSum(mtlData)); // We know the sum is > 0.0

    UNITY_UNROLL
    for (uint i = 0; i < CARPAINT2_LOBE_COUNT; i++)
    {
        BRDF::EvaluateGGX(mtlData, GetSpecularNormal(mtlData), mtlData.bsdfData.roughness[i], _CarPaint2_CTF0s[i], sampleDir, lobeValue, lobePdf);
        value += lobeValue * _CarPaint2_CTCoeffs[i];
        pdf += lobePdf * _CarPaint2_CTCoeffs[i] * CoeffNormalization;
    }

#ifdef AXF_PATH_TRACING_CAR_PAINT_USE_RASTER_SPECULAR
    OverrideSpecularValue(mtlData, sampleDir, value);
#else
    value *= 4.0 / PI; // Matches an error in MultiLobesCookTorrance(...) in AxF.hlsl
#endif
}

void GetAnglesFromDirections(float3 N, float3 V, float3 L, out float thetaH, out float thetaD)
{
    float3 H = normalize(V + L);
    float NdotH = saturate(dot(N, H));
    float LdotH = saturate(dot(L, H));

    thetaH = FastACosPos(NdotH);
    thetaD = FastACosPos(LdotH);
}

float3 GetFlakesValue(MaterialData mtlData, float3 sampleDir, float thetaH, float thetaD)
{
    float NdotL = dot(GetSpecularNormal(mtlData), sampleDir);
    return CarPaint_BTF(thetaH, thetaD, (SurfaceData)0, mtlData.bsdfData, true) * NdotL;
}

bool SampleMaterial(MaterialData mtlData, float3 inputSample, out float3 sampleDir, out MaterialResult result)
{
    Init(result);

    if (IsAbove(mtlData))
    {
        float3 value, brdfColor;
        float  pdf, thetaH, thetaD;
        float  f0ClearCoat = IorToFresnel0(mtlData.bsdfData.clearcoatIOR);
        float3 fresnelClearCoat, coatingTransmission = 1.0;

        if (inputSample.z < mtlData.bsdfWeight[0]) // Diffuse BRDF
        {
            if (!BRDF::SampleLambert(mtlData, GetDiffuseNormal(mtlData), inputSample, sampleDir, result.diffValue, result.diffPdf))
                return false;

            GetAnglesFromDirections(GetDiffuseNormal(mtlData), mtlData.V, sampleDir, thetaH, thetaD);
            brdfColor = GetBRDFColor(thetaH, thetaD);

            result.diffPdf *= mtlData.bsdfWeight[0];

            if (mtlData.bsdfWeight[1] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateGGX(mtlData, GetCoatNormal(mtlData), CLEAR_COAT_ROUGHNESS, f0ClearCoat, sampleDir, value, pdf, fresnelClearCoat);
                coatingTransmission = 1.0 - fresnelClearCoat;
                result.specValue += value * mtlData.bsdfData.clearcoatColor;
                result.specPdf += mtlData.bsdfWeight[1] * pdf;
            }

            result.diffValue *= brdfColor * mtlData.bsdfData.ambientOcclusion * coatingTransmission;

            if (mtlData.bsdfWeight[2] > BSDF_WEIGHT_EPSILON)
            {
                EvaluateSpecular(mtlData, sampleDir, value, pdf);
                result.specValue += value * brdfColor * coatingTransmission;
                result.specPdf += mtlData.bsdfWeight[2] * pdf;
            }
        }
        else if (inputSample.z < mtlData.bsdfWeight[0] + mtlData.bsdfWeight[1]) // Clear coat BRDF
        {
            if (!BRDF::SampleGGX(mtlData, GetCoatNormal(mtlData), CLEAR_COAT_ROUGHNESS, f0ClearCoat, inputSample, sampleDir, result.specValue, result.specPdf, fresnelClearCoat))
                return false;

            GetAnglesFromDirections(GetDiffuseNormal(mtlData), mtlData.V, sampleDir, thetaH, thetaD);
            brdfColor = GetBRDFColor(thetaH, thetaD);

            coatingTransmission = 1.0 - fresnelClearCoat;
            result.specValue *= mtlData.bsdfData.clearcoatColor;
            result.specPdf *= mtlData.bsdfWeight[1];

            if (mtlData.bsdfWeight[0] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateLambert(mtlData, GetDiffuseNormal(mtlData), sampleDir, result.diffValue, result.diffPdf);
                result.diffValue *= brdfColor * mtlData.bsdfData.ambientOcclusion * coatingTransmission;
                result.diffPdf *= mtlData.bsdfWeight[0];
            }

            if (mtlData.bsdfWeight[2] > BSDF_WEIGHT_EPSILON)
            {
                EvaluateSpecular(mtlData, sampleDir, value, pdf);
                result.specValue += value * brdfColor * coatingTransmission;
                result.specPdf += mtlData.bsdfWeight[2] * pdf;
            }
        }
        else // Specular BRDF
        {
            // Renormalize inputSample.z
            inputSample.z = (inputSample.z - (mtlData.bsdfWeight[0] + mtlData.bsdfWeight[1])) / mtlData.bsdfWeight[3];

            if (!SampleSpecular(mtlData, inputSample, sampleDir, result.specValue, result.specPdf))
                return false;

            GetAnglesFromDirections(GetDiffuseNormal(mtlData), mtlData.V, sampleDir, thetaH, thetaD);
            brdfColor = GetBRDFColor(thetaH, thetaD);

            result.specValue *= brdfColor;
            result.specPdf *= mtlData.bsdfWeight[2];

            if (mtlData.bsdfWeight[1] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateGGX(mtlData, GetCoatNormal(mtlData), CLEAR_COAT_ROUGHNESS, f0ClearCoat, sampleDir, value, pdf, fresnelClearCoat);
                coatingTransmission = 1.0 - fresnelClearCoat;
                result.specValue = result.specValue * coatingTransmission + value * mtlData.bsdfData.clearcoatColor;
                result.specPdf += mtlData.bsdfWeight[1] * pdf;
            }

            if (mtlData.bsdfWeight[0] > BSDF_WEIGHT_EPSILON)
            {
                BRDF::EvaluateLambert(mtlData, GetDiffuseNormal(mtlData), sampleDir, result.diffValue, result.diffPdf);
                result.diffValue *= brdfColor * mtlData.bsdfData.ambientOcclusion * coatingTransmission;
                result.diffPdf *= mtlData.bsdfWeight[0];
            }
        }

        // Add flakes to the specular component
        result.specValue += GetFlakesValue(mtlData, sampleDir, thetaH, thetaD) * coatingTransmission;
    }

    return result.diffPdf + result.specPdf > 0.0;
}

void EvaluateMaterial(MaterialData mtlData, float3 sampleDir, out MaterialResult result)
{
    Init(result);

    if (IsAbove(mtlData))
    {
        float3 value, brdfColor;
        float  pdf, thetaH, thetaD;
        float3 fresnelClearCoat, coatingTransmission = 1.0;

        GetAnglesFromDirections(GetDiffuseNormal(mtlData), mtlData.V, sampleDir, thetaH, thetaD);
        brdfColor = GetBRDFColor(thetaH, thetaD);

        if (mtlData.bsdfWeight[1] > BSDF_WEIGHT_EPSILON)
        {
            BRDF::EvaluateGGX(mtlData, GetCoatNormal(mtlData), CLEAR_COAT_ROUGHNESS, IorToFresnel0(mtlData.bsdfData.clearcoatIOR), sampleDir, result.specValue, result.specPdf, fresnelClearCoat);
            coatingTransmission = 1.0 - fresnelClearCoat;
            result.specValue *= mtlData.bsdfData.clearcoatColor;
            result.specPdf *= mtlData.bsdfWeight[1];
        }

        if (mtlData.bsdfWeight[0] > BSDF_WEIGHT_EPSILON)
        {
            BRDF::EvaluateLambert(mtlData, GetDiffuseNormal(mtlData), sampleDir, result.diffValue, result.diffPdf);
            result.diffValue *= brdfColor * coatingTransmission; // AO purposedly ignored here
            result.diffPdf *= mtlData.bsdfWeight[0];
        }

        if (mtlData.bsdfWeight[2] > BSDF_WEIGHT_EPSILON)
        {
            EvaluateSpecular(mtlData, sampleDir, value, pdf);
            result.specValue += value * brdfColor * coatingTransmission;
            result.specPdf += mtlData.bsdfWeight[2] * pdf;
        }

        // Add flakes to the specular component
        result.specValue += GetFlakesValue(mtlData, sampleDir, thetaH, thetaD) * coatingTransmission;
    }
}

#endif // UNITY_AXF_PATH_TRACING_CAR_PAINT_INCLUDED
