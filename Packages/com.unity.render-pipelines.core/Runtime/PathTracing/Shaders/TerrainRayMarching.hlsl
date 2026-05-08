#ifndef _PATHTRACING_TERRAINRAYMARCHING_HLSL_
#define _PATHTRACING_TERRAINRAYMARCHING_HLSL_

// DDA heightmap ray marching for terrain procedural intersection.

//// The terrain is divided in tiles      A tile is divided in cells         A cell corresponds to 4 heightmap texels
// +---+---+---+---+---+---+              +----+----+----+----+              +----+ and is divided into 2 triangles
// |   |   |   |   |   |   |              |   /|   /|   /|   /|              |   /|
// |   |   |   |   |   |   |              |  / |  / |  / |  / |              |  / |
// +---+---+---+---+---+---+              | /  | /  | /  | /  |              | /  |
// |   |   |   |   |   |   |              |/   |/   |/   |/   |              |/   |
// |   |   |   |   |   |   |              +----+----+----+----+              +----+
// +---+---+---+---+---+---+              |   /|   /|   /|   /|
// |   |   |   |   |   |   |              |  / |  / |  / |  / |
// |   |   |   |   |   |   |              | /  | /  | /  | /  |
// +---+---+---+---+---+---+              |/   |/   |/   |/   |
// |   |   |   |   |   |   |              +----+----+----+----+
// |   |   |   |   |   |   |              |   /|   /|   /|   /|
// +---+---+---+---+---+---+              |  / |  / |  / |  / |
// |   |   |   |   |   |   |              | /  | /  | /  | /  |
// |   |   |   |   |   |   |              |/   |/   |/   |/   |
// +---+---+---+---+---+---+              +----+----+----+----+
// |   |   |   |   |   |   |                tileWidthInCells
// |   |   |   |   |   |   |              <------------------->
// +---+---+---+---+---+---+
//        tileCountX
// <----------------------->


float _terrainMax4(float4 v)
{
    return max(max(v.x, v.y), max(v.z, v.w));
}

float2 TerrainBboxIntersect(float3 rayOrigin, float3 rayInvDir, float3 boxMin, float3 boxMax, float tMin, float tMax)
{
    float3 f = (boxMax - rayOrigin) * rayInvDir;
    float3 n = (boxMin - rayOrigin) * rayInvDir;
    float3 tmax = max(f, n);
    float3 tmin = min(f, n);
    float maxT = min(min(min(tmax.x, tmax.y), tmax.z), tMax);
    float minT = max(max(max(tmin.x, tmin.y), tmin.z), tMin);

    return float2(minT, maxT);
}

// Ordering of a cell's corners as returned by textureGather:
//
//              0 +----+ 1
//                |   /|
//                |  / |
//                | /  |
//                |/   |
//              3 +----+ 2
//                ---> X axis

static const int kCellCorner01 = 0;
static const int kCellCorner11 = 1;
static const int kCellCorner10 = 2;
static const int kCellCorner00 = 3;

void GetCellCornerHeights(int2 coord, float invTerrainWidthInCells, int terrainIndex, out float4 cellHeights, out bool cellIsHole)
{
    float2 uv = (float2(coord) + 1.0) * invTerrainWidthInCells;
    cellHeights = _TerrainTexture.Gather(sampler_TerrainTexture, float3(uv, terrainIndex)) - 1.0f / 32767.f;
    // A hole cell has all 4 corners negated. A single negative corner can occur at height=0
    // when the +1 bias wasn't applied (no-hole terrain), so check all corners.
    cellIsHole = all(cellHeights < 0);
    cellHeights = abs(cellHeights);
}

void IntersectRayWithTrianglePlane(float C, float A, float B, float3 rayOrigin, float3 rayDirection, float2 currentCell, out float hitT, out float2 hitPos, out bool frontFaceHit)
{
    float denom = A * rayDirection.x + B * rayDirection.y - rayDirection.z;
    frontFaceHit = (denom > 0);
    hitT = (A * (currentCell.x - rayOrigin.x) + B * (currentCell.y - rayOrigin.y) - C + rayOrigin.z) * rcp(denom);
    hitPos = rayOrigin.xy - currentCell + hitT * rayDirection.xy;
}

bool RayMarchTerrainTile(uint instanceID, int tileIndex, float3 rayOrigin, float3 rayDirection, float rayTmax, out float hitT, out bool frontFaceHit, out float2 hitUv)
{
    UnifiedRT::InstanceData instanceData = UnifiedRT::GetInstance(instanceID);
    UnifiedRT::TerrainData terrainData = UnifiedRT::GetTerrain(instanceData.terrainIndex);

    // Transform world-space ray to terrain-local cell coordinates
    float3 terrainOrigin = mul(float4(0, 0, 0, 1), instanceData.localToWorld);
    rayOrigin = (rayOrigin - terrainOrigin) * terrainData.invTerrainScale;
    rayDirection *= terrainData.invTerrainScale;
    rayDirection.yz = rayDirection.zy;
    rayOrigin.yz = rayOrigin.zy;

    // Avoid division by zero in DDA setup
    if (rayDirection.x == 0.0)
        rayDirection.x = 1.e-37;
    if (rayDirection.y == 0.0)
        rayDirection.y = 1.e-37;

    // Tile bounding box in cell coordinates
    int2 tileCoords = int2(tileIndex & terrainData.pow2ModuloTileCountX, tileIndex >> terrainData.pow2DivideTileCountX);
    int2 tileBboxMin = tileCoords * terrainData.tileWidthInCells;
    int2 tileBboxMax = (tileCoords + 1) * terrainData.tileWidthInCells;
    float3 boxMin = float3(tileBboxMin.x, tileBboxMin.y, 0.0);
    float3 boxMax = float3(tileBboxMax.x, tileBboxMax.y, 1.0);

    // Intersect ray with tile AABB
    float2 tRange = TerrainBboxIntersect(rayOrigin, 1.0 / rayDirection, boxMin, boxMax, 0.0f, rayTmax);
    float tTileEntry = tRange.x;
    float tTileExit = tRange.y;

    hitT = 0;
    hitUv = 0;
    frontFaceHit = false;

    // No intersection with tile AABB
    if (tTileEntry > tTileExit)
        return false;

    // DDA grid traversal setup
    float3 startPos = rayOrigin + tTileEntry * rayDirection;
    int2 startCell = clamp(int2(startPos.xy), tileBboxMin, tileBboxMax - 1);
    int2 step = int2(sign(rayDirection.xy));
    float2 nextCellEdge = float2(startCell) + float2(step.x > 0 ? 1.0f : 0.0f, step.y > 0 ? 1.0f : 0.0f);
    float2 tMax = (nextCellEdge - rayOrigin.xy) / rayDirection.xy;
    float2 tDelta = abs(1.0 / rayDirection.xy);

    float tCellEntry = tTileEntry;
    int2 iterCell = startCell;

    for (int i = 0; i < 2 * terrainData.tileWidthInCells; i++)
    {
        if (iterCell.x < tileBboxMin.x || iterCell.y < tileBboxMin.y ||
            iterCell.x >= tileBboxMax.x || iterCell.y >= tileBboxMax.y ||
            tCellEntry > tTileExit)
            break;

        float4 cellHeights;
        bool cellIsHole;
        GetCellCornerHeights(iterCell, terrainData.invHeightmapWidthInTexels, instanceData.terrainIndex, cellHeights, cellIsHole);

        float tCellExit = min(tMax.x, tMax.y);
        float hRayAtExit = rayOrigin.z + tCellExit * rayDirection.z;
        float hRayAtEntry = rayOrigin.z + tCellEntry * rayDirection.z;

        // Check if ray's height range overlaps the cell's height range
        if (min(hRayAtExit, hRayAtEntry) <= _terrainMax4(cellHeights) && !cellIsHole)
        {
            bool hit = false;

            // Triangle 1: C00, C10, C11 (upper-right triangle)
            float t1;
            float2 p1;
            bool frontFaceHit1;
            IntersectRayWithTrianglePlane(cellHeights[kCellCorner00], cellHeights[kCellCorner10] - cellHeights[kCellCorner00], cellHeights[kCellCorner11] - cellHeights[kCellCorner10], rayOrigin, rayDirection, iterCell, t1, p1, frontFaceHit1);
            if (saturate(p1.x) == p1.x && saturate(p1.y) == p1.y && p1.x >= p1.y && t1 >= tCellEntry)
            {
                hitT = t1;
                hitUv = (iterCell + p1) * terrainData.invTerrainWidthInCells;
                frontFaceHit = frontFaceHit1;
                hit = true;
            }

            // Triangle 2: C00, C11, C01 (lower-left triangle)
            float t2;
            float2 p2;
            bool frontFaceHit2;
            IntersectRayWithTrianglePlane(cellHeights[kCellCorner00], cellHeights[kCellCorner11] - cellHeights[kCellCorner01], cellHeights[kCellCorner01] - cellHeights[kCellCorner00], rayOrigin, rayDirection, iterCell, t2, p2, frontFaceHit2);
            if (saturate(p2.x) == p2.x && saturate(p2.y) == p2.y && p2.x <= p2.y && t2 >= tCellEntry && (!hit || t2 < t1))
            {
                hitT = t2;
                hitUv = (iterCell + p2) * terrainData.invTerrainWidthInCells;
                frontFaceHit = frontFaceHit2;
                hit = true;
            }

            if (hit)
                return true;
        }

        // Advance to next cell
        tCellEntry = tCellExit;
        if (tMax.x < tMax.y)
        {
            tMax.x += tDelta.x;
            iterCell.x += step.x;
        }
        else
        {
            tMax.y += tDelta.y;
            iterCell.y += step.y;
        }
    }

    return false;
}

// Compute terrain local-space position and normal from cell coordinates.
// Shared between the ray hit path (GetTerrainHitGeomInfo) and the GBuffer path (GetExpandedSampleTerrain).
// Position uses triangle-matched interpolation (matching DDA ray marching exactly).
// Normal uses central differences for smooth shading (matching vertex-interpolated normals).
void ComputeTerrainLocalPosAndNormal(UnifiedRT::TerrainData terrainData, int terrainIndex, float2 heightmapUV, out float3 localPos, out float3 localNormal)
{
    float3 heightmapScale = terrainData.terrainScale;
    float3 invHeightmapScale = terrainData.invTerrainScale;
    float numCells = terrainData.heightmapWidthInTexels - 1.0;

    int2 cellCoord = clamp(int2(heightmapUV), 0, int(numCells) - 1);
    float2 cellFrac = heightmapUV - float2(cellCoord);

    float4 cellHeights;
    bool cellIsHole;
    GetCellCornerHeights(cellCoord, terrainData.invHeightmapWidthInTexels, terrainIndex, cellHeights, cellIsHole);

    // Interpolate height using the correct triangle (matching DDA split).
    float h;
    if (cellFrac.x >= cellFrac.y)
        h = cellHeights[kCellCorner00] + (cellHeights[kCellCorner10] - cellHeights[kCellCorner00]) * cellFrac.x + (cellHeights[kCellCorner11] - cellHeights[kCellCorner10]) * cellFrac.y;
    else
        h = cellHeights[kCellCorner00] + (cellHeights[kCellCorner11] - cellHeights[kCellCorner01]) * cellFrac.x + (cellHeights[kCellCorner01] - cellHeights[kCellCorner00]) * cellFrac.y;

    localPos = float3(heightmapUV.x * heightmapScale.x, h * heightmapScale.y, heightmapUV.y * heightmapScale.z);

    // Sobel 3x3 normal filter matching TerrainToMesh.CalculateTerrainNormal.
    // Kernel weights: [-1,-2,-1, 0,0,0, 1,2,1] for each axis.
    float2 sampleUV = (heightmapUV + 0.5) * terrainData.invHeightmapWidthInTexels;
    float eps = terrainData.invHeightmapWidthInTexels;

    float hTL = abs(_TerrainTexture.SampleLevel(sampler_TerrainTexture, float3(sampleUV + float2(-eps, -eps), terrainIndex), 0)) - 1.0 / 32767.0;
    float hML = abs(_TerrainTexture.SampleLevel(sampler_TerrainTexture, float3(sampleUV + float2(-eps,    0), terrainIndex), 0)) - 1.0 / 32767.0;
    float hBL = abs(_TerrainTexture.SampleLevel(sampler_TerrainTexture, float3(sampleUV + float2(-eps,  eps), terrainIndex), 0)) - 1.0 / 32767.0;
    float hTR = abs(_TerrainTexture.SampleLevel(sampler_TerrainTexture, float3(sampleUV + float2( eps, -eps), terrainIndex), 0)) - 1.0 / 32767.0;
    float hMR = abs(_TerrainTexture.SampleLevel(sampler_TerrainTexture, float3(sampleUV + float2( eps,    0), terrainIndex), 0)) - 1.0 / 32767.0;
    float hBR = abs(_TerrainTexture.SampleLevel(sampler_TerrainTexture, float3(sampleUV + float2( eps,  eps), terrainIndex), 0)) - 1.0 / 32767.0;
    float hTM = abs(_TerrainTexture.SampleLevel(sampler_TerrainTexture, float3(sampleUV + float2(   0, -eps), terrainIndex), 0)) - 1.0 / 32767.0;
    float hBM = abs(_TerrainTexture.SampleLevel(sampler_TerrainTexture, float3(sampleUV + float2(   0,  eps), terrainIndex), 0)) - 1.0 / 32767.0;

    float dX = (-hTL - 2.0 * hML - hBL + hTR + 2.0 * hMR + hBR) * heightmapScale.y * invHeightmapScale.x;
    float dZ = (-hTL - 2.0 * hTM - hTR + hBL + 2.0 * hBM + hBR) * heightmapScale.y * invHeightmapScale.z;
    localNormal = normalize(float3(-dX, 8.0, -dZ));
}

#endif // _PATHTRACING_TERRAINRAYMARCHING_HLSL_
