#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/PhysicalCamera.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"


TEXTURE2D(_ExposureWeightMask);
TEXTURE2D_X(_SourceTexture);
TEXTURE2D(_PreviousExposureTexture);
RW_TEXTURE2D(float2, _OutputTexture);

CBUFFER_START(cb)
float4 _ExposureParams;
float4 _HistogramExposureParams;
float4 _AdaptationParams;
uint4 _Variants;
CBUFFER_END

#define ParamEV100                  _ExposureParams.y
#define ParamExposureCompensation   _ExposureParams.x
#define ParamAperture               _ExposureParams.y
#define ParamShutterSpeed           _ExposureParams.z
#define ParamISO                    _ExposureParams.w
#define ParamSpeedLightToDark       _AdaptationParams.x
#define ParamSpeedDarkToLight       _AdaptationParams.y
#define ParamExposureLimitMin       _ExposureParams.y
#define ParamExposureLimitMax       _ExposureParams.z
#define ParamCurveMin               _ExposureParams.y
#define ParamCurveMax               _ExposureParams.z
#define ParamSourceBuffer           _Variants.x
#define ParamMeteringMode           _Variants.y
#define ParamAdaptationMode         _Variants.z
#define ParamEvaluateMode           _Variants.w

// TODO_FCC: IMPORTANT! This function uses hard coded values for the texture that is output by the prepass.
// Need to make the analytical metering texture size independent.
// When that is done, these defines should be moved back to Exposure.compute


float GetPreviousExposureEV100()
{
    return _PreviousExposureTexture[uint2(0u, 0u)].y;
}

float WeightSample(uint2 pixel, float2 sourceSize)
{
    UNITY_BRANCH
        switch (ParamMeteringMode)
        {
        case 1u:
        {
            // Spot metering
            float screenDiagonal = 0.5f * (sourceSize.x + sourceSize.y);
            const float kRadius = 0.075 * screenDiagonal;
            const float2 kCenter = sourceSize * 0.5f;
            float d = length(kCenter - pixel) - kRadius;
            return 1.0 - saturate(d);
        }
        case 2u:
        {
            // Center-weighted
            float screenDiagonal = 0.5f * (sourceSize.x + sourceSize.y);
            const float2 kCenter = sourceSize * 0.5f;
            return 1.0 - saturate(pow(length(kCenter - pixel) / screenDiagonal, 1.0));
        }
        case 3u:
        {
            // Mask weigthing
            return SAMPLE_TEXTURE2D_LOD(_ExposureWeightMask, s_linear_clamp_sampler, pixel * rcp(sourceSize), 0.0).x;
        }

        default:
        {
            // Global average
            return 1.0;
        }
        }
}

float SampleLuminance(float2 uv)
{
    if (ParamSourceBuffer == 1)
    {
        // Color buffer
        float prevExposure = ConvertEV100ToExposure(GetPreviousExposureEV100());
        float3 color = SAMPLE_TEXTURE2D_X_LOD(_SourceTexture, s_linear_clamp_sampler, uv, 0.0).xyz;
        return Luminance(color / prevExposure);
    }
    else
    {
        return 1.0f;
    }
}

float AdaptExposure(float exposure)
{
    if (ParamAdaptationMode == 1)
    {
        return ComputeLuminanceAdaptation(GetPreviousExposureEV100(), exposure, ParamSpeedDarkToLight, ParamSpeedLightToDark, unity_DeltaTime.x);
    }
    else
    {
        return exposure;
    }
}
