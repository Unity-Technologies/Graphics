#ifndef TERRAIN_TOOL_INCLUDED
#define TERRAIN_TOOL_INCLUDED


// function to convert paint context UV to brush uv
float4 _PCUVToBrushUVScales;
float2 _PCUVToBrushUVOffset;
float2 PaintContextUVToBrushUV(float2 pcUV)
{
    return _PCUVToBrushUVScales.xy * pcUV.x +
           _PCUVToBrushUVScales.zw * pcUV.y +
           _PCUVToBrushUVOffset;
}


float2 PaintContextUVToHeightmapUV(float2 pcUV)
{
    return pcUV;
}


#endif
