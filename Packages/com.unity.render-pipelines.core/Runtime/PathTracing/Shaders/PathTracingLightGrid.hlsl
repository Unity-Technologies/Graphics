#ifndef _PATHTRACING_LIGHTGRID_HLSL_
#define _PATHTRACING_LIGHTGRID_HLSL_

// Grid reservoirs per cell
uint    g_NumReservoirs;

// Grid dimentions
uint    g_GridDimX;
uint    g_GridDimY;
uint    g_GridDimZ;

// Grid Bounds
float4 g_GridMin;
float4 g_GridSize;

// Cell dimentions
float4 g_CellSize;
float4 g_InvCellSize;

struct ThinReservoir
{
    int lightIndex;
    float weight;
};

float GetCellSize()
{
    return g_CellSize.w;
}

float3 GetCellPosition(uint3 cellIndex)
{
    return g_GridMin.xyz + g_CellSize.xyz * (cellIndex + float3(0.5f, 0.5f, 0.5f));
}

uint GetCellIndex(uint3 cellCoord)
{
    uint slice = (g_GridDimX * g_GridDimY) * cellCoord.z;
    return slice + cellCoord.y * g_GridDimX + cellCoord.x;
}

uint GetCellIndexFromPosition(float3 pos)
{
    uint3 cellCoord = pos * g_InvCellSize.xyz - (g_GridMin.xyz * g_InvCellSize.xyz);
    cellCoord = clamp(cellCoord, uint3(0, 0, 0), uint3(g_GridDimX, g_GridDimY, g_GridDimZ) - uint3(1, 1, 1));
    return GetCellIndex(cellCoord);
}

#endif
