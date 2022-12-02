#ifndef WATER_GENERATION_UTILITIES_H
#define WATER_GENERATION_UTILITIES_H

// The set of generators that should be applied this frame
StructuredBuffer<WaterGeneratorData> _WaterGeneratorData;
Texture2D<float2> _WaterGeneratorTextureAtlas;

// This array allows us to convert vertex ID to local position
static const float2 generatorCorners[6] = {float2(-1, -1), float2(1, -1), float2(1, 1), float2(-1, -1), float2(1, 1), float2(-1, 1)};

#endif // WATER_GENERATION_UTILITIES_H
