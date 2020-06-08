#include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/ExposureCommon.hlsl"

#define HISTOGRAM_BINS 128

#define _HistogramRangeScale     _HistogramExposureParams.x
#define _HistogramRangeBias      _HistogramExposureParams.y
#define _HistogramMinPercentile  _HistogramExposureParams.z
#define _HistogramMaxPercentile  _HistogramExposureParams.w

#ifdef GEN_PASS
RWStructuredBuffer<uint> _HistogramBuffer;
#else
StructuredBuffer<uint> _HistogramBuffer;
#endif

#ifdef OUTPUT_DEBUG_DATA
RW_TEXTURE2D(float2, _ExposureDebugTexture);
#else
TEXTURE2D(_ExposureDebugTexture);
#endif

float UnpackWeight(uint val)
{
    return val * rcp(2048.0f);
}

float GetFractionWithinHistogram(float value)
{
    return ComputeEV100FromAvgLuminance(value, MeterCalibrationConstant) * _HistogramRangeScale + _HistogramRangeBias;
}

uint GetHistogramBinLocation(float value)
{
    return uint(saturate(GetFractionWithinHistogram(value)) * (HISTOGRAM_BINS - 1));
}

uint EVToBinLocation(float ev)
{
    return uint((ev * _HistogramRangeScale + _HistogramRangeBias) * (HISTOGRAM_BINS - 1));
}

float BinLocationToEV(uint binIdx)
{
    return (binIdx * rcp(float(HISTOGRAM_BINS - 1)) - _HistogramRangeBias) / _HistogramRangeScale;
}
