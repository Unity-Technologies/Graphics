
#define HISTOGRAM_BINS 128          // IMPORTANT: If this number is changed, the code needs adapting, I tried to add relevant comments to indicate where.

#define _HistogramRangeScale     _HistogramExposureParams.x
#define _HistogramRangeBias      _HistogramExposureParams.y
#define _HistogramMinPercentage  _HistogramExposureParams.z
#define _HistogramMaxPercentage  _HistogramExposureParams.w

#ifdef GEN_PASS
RWStructuredBuffer<uint> _HistogramBuffer;
#else
StructuredBuffer<uint> _HistogramBuffer;
#endif

float UnpackWeight(uint val)
{
    return val * rcp(2048.0f);
}

uint GetHistogramBinLocation(float value)
{
    float scaledLogLuma = ComputeEV100FromAvgLuminance(value) * _HistogramRangeScale + _HistogramRangeBias;
    return uint(saturate(scaledLogLuma) * (HISTOGRAM_BINS - 1));
}

float BinLocationToEV(uint binIdx)
{
    return (binIdx * rcp(float(HISTOGRAM_BINS - 1)) - _HistogramRangeBias) / _HistogramRangeScale;
}
