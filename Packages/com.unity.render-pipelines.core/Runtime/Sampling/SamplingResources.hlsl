#ifndef _SAMPLING_SAMPLINGRESOURCES_HLSL_
#define _SAMPLING_SAMPLINGRESOURCES_HLSL_

Texture2D<float>                _SobolScramblingTile;
Texture2D<float>                _SobolRankingTile;
Texture2D<float2>               _SobolOwenScrambledSequence;

StructuredBuffer<uint>          _SobolMatricesBuffer;

#endif

