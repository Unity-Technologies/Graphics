#ifndef TERRAIN_PREVIEW_INCLUDED
#define TERRAIN_PREVIEW_INCLUDED


// function to convert paint context pixels to heightmap uv
sampler2D _Heightmap;
float2 _HeightmapUV_PCPixelsX;
float2 _HeightmapUV_PCPixelsY;
float2 _HeightmapUV_Offset;
float2 PaintContextPixelsToHeightmapUV(float2 pcPixels)
{
    return _HeightmapUV_PCPixelsX * pcPixels.x +
        _HeightmapUV_PCPixelsY * pcPixels.y +
        _HeightmapUV_Offset;
}

// function to convert paint context pixels to object position (terrain position)
float3 _ObjectPos_PCPixelsX;
float3 _ObjectPos_PCPixelsY;
float3 _ObjectPos_HeightMapSample;
float3 _ObjectPos_Offset;
float3 PaintContextPixelsToObjectPosition(float2 pcPixels, float heightmapSample)
{
    // note: we could assume no object space rotation and make this dramatically simpler
    return _ObjectPos_PCPixelsX * pcPixels.x +
        _ObjectPos_PCPixelsY * pcPixels.y +
        _ObjectPos_HeightMapSample * heightmapSample +
        _ObjectPos_Offset;
}

// function to convert paint context pixels to brush uv
float2 _BrushUV_PCPixelsX;
float2 _BrushUV_PCPixelsY;
float2 _BrushUV_Offset;
float2 PaintContextPixelsToBrushUV(float2 pcPixels)
{
    return _BrushUV_PCPixelsX * pcPixels.x +
        _BrushUV_PCPixelsY * pcPixels.y +
        _BrushUV_Offset;
}

// function to convert terrain object position to world position
// We would normally use the ObjectToWorld / ObjectToClip calls to do this, but DrawProcedural does not set them
// 'luckily' terrains cannot be rotated or scaled, so this transform is very simple
float3 _TerrainObjectToWorldOffset;
float3 TerrainObjectToWorldPosition(float3 objectPosition)
{
    return objectPosition + _TerrainObjectToWorldOffset;
}

// function to build a procedural quad mesh
// based on the quad resolution defined by _QuadRez
// returns integer positions, starting with (0, 0), and ending with (_QuadRez.xy - 1)
float4 _QuadRez;    // quads X, quads Y, vertexCount, vertSkip
float2 BuildProceduralQuadMeshVertex(uint vertexID)
{
    int quadIndex = vertexID / 6;                       // quad index, each quad is made of 6 vertices
    int vertIndex = vertexID - quadIndex * 6;           // vertex index within the quad [0..5]
    int qY = floor((quadIndex + 0.5f) / _QuadRez.x);    // quad coords for current quad (Y)
    int qX = round(quadIndex - qY * _QuadRez.x);        // quad coords for current quad (X)

    // each quad is defined by 6 vertices (two triangles), offset from (qX,qY) as follows:
    // vX = 0, 0, 1, 1, 1, 0
    // vY = 0, 1, 1, 1, 0, 0
    float sequence[6] = { 0.0f, 0.0f, 1.0f, 1.0f, 1.0f, 0.0f };
    float vX = sequence[vertIndex];
    float vY = sequence[5 - vertIndex];     // vY is just vX reversed
    float2 coord = float2(qX + vX, qY + vY);
    return coord * _QuadRez.w;
}


float Stripe(in float x, in float stripeX, in float pixelWidth)
{
    // compute derivatives to get ddx / pixel
    float2 derivatives = float2(ddx(x), ddy(x));
    float derivLen = length(derivatives);
    float sharpen = 1.0f / max(derivLen, 0.00001f);
    return saturate(0.5f + 0.5f * (0.5f * pixelWidth - sharpen * abs(x - stripeX)));
}

#endif
