using UnityEngine.Profiling;
using Unity.Collections;

namespace UnityEngine.Rendering.Universal
{
    internal class DeferredTiler
    {
        // Precomputed light data
        public struct PrePointLight
        {
            // view-space position.
            public Vector3 vsPos;
            // Radius in world unit.
            public float radius;
            // Projected position of the sphere centre on the screen (near plane).
            public Vector2 screenPos;
            // Index into renderingData.lightData.visibleLights native array.
            public ushort visLightIndex;
        }

        enum ClipResult
        {
            Unknown,
            In,
            Out,
        }

        int m_TilePixelWidth = 0;
        int m_TilePixelHeight = 0;
        int m_TileXCount = 0;
        int m_TileYCount = 0;
        int m_TileSize = 32;
        int m_TileHeader = 5; // ushort lightCount, half minDepth, half maxDepth, uint bitmask

        // Adjusted frustum planes to account for tile size.
        FrustumPlanes m_FrustumPlanes;

        // Store all visible light indices for all tiles.
        NativeArray<ushort> m_Tiles;
        // Precompute tile data.
        NativeArray<PreTile> m_PreTiles;

        public DeferredTiler(int tilePixelWidth, int tilePixelHeight)
        {
            m_TilePixelWidth = tilePixelWidth;
            m_TilePixelHeight = tilePixelHeight;
        }

        public int GetTileXCount()
        {
            return m_TileXCount;
        }

        public int GetTileYCount()
        {
            return m_TileYCount;
        }

        public int GetTileXStride()
        {
            return m_TileSize;
        }

        public int GetTileYStride()
        {
            return m_TileSize * m_TileXCount;
        }

        public int GetMaxLightPerTile()
        {
            return m_TileSize * m_TileHeader;
        }

        public int GetTileHeader()
        {
            return m_TileHeader;
        }

        public ref NativeArray<ushort> GetTiles()
        {
            return ref m_Tiles;
        }

        public void Setup()
        {
            m_Tiles = new NativeArray<ushort>(m_TileXCount * m_TileYCount * m_TileSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        }

        public void FrameCleanup()
        {
            if (m_Tiles.IsCreated)
                m_Tiles.Dispose();
        }

        public void PrecomputeTiles(Matrix4x4 proj, bool isOrthographic, int renderWidth, int renderHeight)
        {
            m_TileXCount = (renderWidth + m_TilePixelWidth - 1) / m_TilePixelWidth;
            m_TileYCount = (renderHeight + m_TilePixelHeight - 1) / m_TilePixelHeight;

            m_PreTiles = DeferredShaderData.instance.GetPreTiles(m_TileXCount * m_TileYCount);

            // Adjust render width and height to account for tile size expanding over the screen (tiles have a fixed pixel size).
            int adjustedRenderWidth = Align(renderWidth, DeferredConfig.kTilePixelWidth);
            int adjustedRenderHeight = Align(renderHeight, DeferredConfig.kTilePixelHeight);

            // Now adjust the right and bottom clipping planes.
            m_FrustumPlanes = proj.decomposeProjection;
            m_FrustumPlanes.right = m_FrustumPlanes.left + (m_FrustumPlanes.right - m_FrustumPlanes.left) * (adjustedRenderWidth / (float)renderWidth);
            m_FrustumPlanes.bottom = m_FrustumPlanes.top + (m_FrustumPlanes.bottom - m_FrustumPlanes.top) * (adjustedRenderHeight / (float)renderHeight);

            // Tile size in world units.
            float tileWsWidth = (m_FrustumPlanes.right - m_FrustumPlanes.left) / m_TileXCount;
            float tileWsHeight = (m_FrustumPlanes.top - m_FrustumPlanes.bottom) / m_TileYCount;

            if (!isOrthographic) // perspective
            {
                for (int j = 0; j < m_TileYCount; ++j)
                {
                    float tileTop = m_FrustumPlanes.top - tileWsHeight * j;
                    float tileBottom = tileTop - tileWsHeight;

                    for (int i = 0; i < m_TileXCount; ++i)
                    {
                        float tileLeft = m_FrustumPlanes.left + tileWsWidth * i;
                        float tileRight = tileLeft + tileWsWidth;

                        // Camera view space is always OpenGL RH coordinates system.
                        // In view space with perspective projection, all planes pass by (0,0,0).
                        PreTile preTile;
                        preTile.planeLeft = MakePlane(new Vector3(tileLeft, tileBottom, -m_FrustumPlanes.zNear), new Vector3(tileLeft, tileTop, -m_FrustumPlanes.zNear));
                        preTile.planeRight = MakePlane(new Vector3(tileRight, tileTop, -m_FrustumPlanes.zNear), new Vector3(tileRight, tileBottom, -m_FrustumPlanes.zNear));
                        preTile.planeBottom = MakePlane(new Vector3(tileRight, tileBottom, -m_FrustumPlanes.zNear), new Vector3(tileLeft, tileBottom, -m_FrustumPlanes.zNear));
                        preTile.planeTop = MakePlane(new Vector3(tileLeft, tileTop, -m_FrustumPlanes.zNear), new Vector3(tileRight, tileTop, -m_FrustumPlanes.zNear));

                        m_PreTiles[i + j * m_TileXCount] = preTile;
                    }
                }
            }
            else
            {
                for (int j = 0; j < m_TileYCount; ++j)
                {
                    float tileTop = m_FrustumPlanes.top - tileWsHeight * j;
                    float tileBottom = tileTop - tileWsHeight;

                    for (int i = 0; i < m_TileXCount; ++i)
                    {
                        float tileLeft = m_FrustumPlanes.left + tileWsWidth * i;
                        float tileRight = tileLeft + tileWsWidth;

                        // Camera view space is always OpenGL RH coordinates system.
                        PreTile preTile;
                        preTile.planeLeft = MakePlane(new Vector3(tileLeft, tileBottom, -m_FrustumPlanes.zNear), new Vector3(tileLeft, tileBottom, -m_FrustumPlanes.zNear - 1.0f), new Vector3(tileLeft, tileTop, -m_FrustumPlanes.zNear));
                        preTile.planeRight = MakePlane(new Vector3(tileRight, tileTop, -m_FrustumPlanes.zNear), new Vector3(tileRight, tileTop, -m_FrustumPlanes.zNear - 1.0f), new Vector3(tileRight, tileBottom, -m_FrustumPlanes.zNear));
                        preTile.planeBottom = MakePlane(new Vector3(tileRight, tileBottom, -m_FrustumPlanes.zNear), new Vector3(tileRight, tileBottom, -m_FrustumPlanes.zNear - 1.0f), new Vector3(tileLeft, tileBottom, -m_FrustumPlanes.zNear));
                        preTile.planeTop = MakePlane(new Vector3(tileLeft, tileTop, -m_FrustumPlanes.zNear), new Vector3(tileLeft, tileTop, -m_FrustumPlanes.zNear - 1.0f), new Vector3(tileRight, tileTop, -m_FrustumPlanes.zNear));

                        m_PreTiles[i + j * m_TileXCount] = preTile;
                    }
                }
            }
        }

        public void CullLightsWithMinMaxDepth(NativeArray<PrePointLight> visPointLights, bool isOrthographic)
        {
            Profiler.BeginSample("CullLightsWithMinMaxDepth");

            Assertions.Assert.IsTrue(m_TileHeader >= 5, "not enough space to store min&max depth information for light list ");

            int tileXStride = m_TileSize;
            int tileYStride = m_TileSize * m_TileXCount;
            int maxLightPerTile = m_TileSize - m_TileHeader;
            int tileHeader = m_TileHeader;

            Vector2 tileSize = new Vector2((m_FrustumPlanes.right - m_FrustumPlanes.left) / m_TileXCount, (m_FrustumPlanes.top - m_FrustumPlanes.bottom) / m_TileYCount);
            Vector2 tileExtents = tileSize * 0.5f;
            Vector2 tileExtentsInv = new Vector2(1.0f / tileExtents.x, 1.0f / tileExtents.y);

            // Store min&max depth range for each light in a tile.
            Vector2[] minMax = new Vector2[maxLightPerTile];

            for (int j = 0; j < m_TileYCount; ++j)
            {
                float tileYCentre = m_FrustumPlanes.top - (tileExtents.y + j * tileSize.y);

                for (int i = 0; i < m_TileXCount; ++i)
                {
                    float tileXCentre = m_FrustumPlanes.left + tileExtents.x + i * tileSize.x;

                    PreTile preTile = m_PreTiles[i + j * m_TileXCount];
                    int tileOffset = i * tileXStride + j * tileYStride;
                    ushort tileLightCount = 0;

                    // For the current tile's light list, min&max depth range (absolute values).
                    float listMinDepth = float.MaxValue;
                    float listMaxDepth = -float.MaxValue;

                    for (ushort visLightIndex = 0; visLightIndex < visPointLights.Length; ++visLightIndex)
                    {
                        PrePointLight ppl = visPointLights[visLightIndex];

                        // Offset tileCentre toward the light to calculate a more conservative minMax depth bound,
                        // but it must remains inside the tile and must not pass further than the light centre.
                        Vector2 tileCentre = new Vector3(tileXCentre, tileYCentre);
                        Vector2 dir = ppl.screenPos - tileCentre;
                        Vector2 d = Abs(dir * tileExtentsInv);
                        float s = Max(d.x, d.y, 1.0f);
                        Vector3 tileOffCentre;
                        Vector3 tileOrigin;

                        if (isOrthographic)
                        {
                            tileOrigin = new Vector3(tileCentre.x + dir.x / s, tileCentre.y + dir.y / s, 0.0f);
                            tileOffCentre = new Vector3(0, 0, -m_FrustumPlanes.zNear);
                        }
                        else
                        {
                            tileOrigin = Vector3.zero;
                            tileOffCentre = new Vector3(tileCentre.x + dir.x / s, tileCentre.y + dir.y / s, -m_FrustumPlanes.zNear);
                        }

                        float t0, t1;
                        // This is more expensive than Clip() but allow to compute min&max depth range for the part of the light inside the tile.
                        if (!IntersectionLineSphere(ppl.vsPos, ppl.radius, tileOrigin, tileOffCentre, out t0, out t1))
                            continue;

                        if (tileLightCount == maxLightPerTile)
                        {
                            // TODO log error: tile is full
                            break;
                        }

                        // Looking towards -z axin in view space, we want absolute depth values.
                        float minDepth = m_FrustumPlanes.zNear * t0;
                        float maxDepth = m_FrustumPlanes.zNear * t1;
                        listMinDepth = listMinDepth < minDepth ? listMinDepth : minDepth;
                        listMaxDepth = listMaxDepth > maxDepth ? listMaxDepth : maxDepth;
                        minMax[tileLightCount].x = minDepth;
                        minMax[tileLightCount].y = maxDepth;

                        m_Tiles[tileOffset + tileHeader + tileLightCount] = ppl.visLightIndex;
                        ++tileLightCount;
                    }

                    // Clamp our light list depth range.
                    listMinDepth = Mathf.Max(listMinDepth, m_FrustumPlanes.zNear);
                    listMaxDepth = Mathf.Min(listMaxDepth, m_FrustumPlanes.zFar);

                    // Calculate bitmask for 2.5D culling.
                    uint bitMask = 0;
                    float depthRangeInv = 1.0f / (listMaxDepth - listMinDepth);
                    for (int tileLightIndex = 0; tileLightIndex < tileLightCount; ++tileLightIndex)
                    {
                        int firstBit = (int)((minMax[tileLightIndex].x - listMinDepth) * 32.0f * depthRangeInv);
                        int lastBit = (int)((minMax[tileLightIndex].y - listMinDepth) * 32.0f * depthRangeInv);
                        int bitCount = lastBit - firstBit + 1;
                        bitCount = (bitCount > 32 ? 32 : bitCount);
                        bitMask |= (uint)(((1ul << bitCount) - 1) << firstBit);
                    }

                    m_Tiles[tileOffset] = tileLightCount;
                    m_Tiles[tileOffset + 1] = Mathf.FloatToHalf(listMinDepth);
                    m_Tiles[tileOffset + 2] = Mathf.FloatToHalf(listMaxDepth);
                    m_Tiles[tileOffset + 3] = (ushort)(bitMask & 0xFFFF);
                    m_Tiles[tileOffset + 4] = (ushort)((bitMask >> 16) & 0xFFFF);
                }
            }

            Profiler.EndSample();
        }

        public void CullLights(NativeArray<PrePointLight> visPointLights)
        {
            Profiler.BeginSample("CullLights");

            int tileXStride = m_TileSize;
            int tileYStride = m_TileSize * m_TileXCount;
            int maxLightPerTile = m_TileSize - m_TileHeader;
            int tileHeader = m_TileHeader;

            for (int j = 0; j < m_TileYCount; ++j)
            {
                for (int i = 0; i < m_TileXCount; ++i)
                {
                    PreTile preTile = m_PreTiles[i + j * m_TileXCount];
                    int tileOffset = i * tileXStride + j * tileYStride;
                    ushort tileLightCount = 0;

                    for (ushort visLightIndex = 0; visLightIndex < visPointLights.Length; ++visLightIndex)
                    {
                        PrePointLight ppl = visPointLights[visLightIndex];

                        // This is faster than IntersectionLineSphere().
                        if (!Clip(ref preTile, ppl.vsPos, ppl.radius))
                            continue;

                        if (tileLightCount == maxLightPerTile)
                        {
                            // TODO log error: tile is full
                            break;
                        }

                        m_Tiles[tileOffset + tileHeader + tileLightCount] = ppl.visLightIndex;
                        ++tileLightCount;
                    }

                    m_Tiles[tileOffset] = tileLightCount;
                }
            }

            Profiler.EndSample();
        }

        // Return parametric intersection between a sphere and a line.
        // The intersections points P0 and P1 are:
        // P0 = raySource + rayDirection * t0.
        // P1 = raySource + rayDirection * t1.
        static bool IntersectionLineSphere(Vector3 centre, float radius, Vector3 raySource, Vector3 rayDirection, out float t0, out float t1)
        {
            float A = Vector3.Dot(rayDirection, rayDirection); // always >= 0
            float B = Vector3.Dot(raySource - centre, rayDirection);
            float C = Vector3.Dot(raySource, raySource)
                    + Vector3.Dot(centre, centre)
                    - (radius * radius)
                    - 2 * Vector3.Dot(raySource, centre);
            float discriminant = (B*B) - A * C;
            if (discriminant > 0)
            {
                float sqrt_discriminant = Mathf.Sqrt(discriminant);
                float A_inv = 1.0f / A;
                t0 = (-B - sqrt_discriminant) * A_inv;
                t1 = (-B + sqrt_discriminant) * A_inv;
                return true;
            }
            else
            {
                t0 = 0.0f; // invalid
                t1 = 0.0f; // invalid
                return false;
            }
        }

        // Clip a sphere against a 2D tile. Near and far planes are ignored (already tested).
        static bool Clip(ref PreTile tile, Vector3 vsPos, float radius)
        {
            // Simplified clipping code, only deals with 4 clipping planes.
            // zNear and zFar clipping planes are ignored as presumably the light is already visible to the camera frustum.
            
            float radiusSq = radius * radius;
            int insideCount = 0;
            ClipResult res;

            res = ClipPartial(tile.planeLeft, tile.planeBottom, tile.planeTop, vsPos, radius, radiusSq, ref insideCount);
            if (res != ClipResult.Unknown)
                return res == ClipResult.In;

            res = ClipPartial(tile.planeRight, tile.planeBottom, tile.planeTop, vsPos, radius, radiusSq, ref insideCount);
            if (res != ClipResult.Unknown)
                return res == ClipResult.In;

            res = ClipPartial(tile.planeTop, tile.planeLeft, tile.planeRight, vsPos, radius, radiusSq, ref insideCount);
            if (res != ClipResult.Unknown)
                return res == ClipResult.In;

            res = ClipPartial(tile.planeBottom, tile.planeLeft, tile.planeRight, vsPos, radius, radiusSq, ref insideCount);
            if (res != ClipResult.Unknown)
                return res == ClipResult.In;

            return insideCount == 4;
        }

        // Internal function to clip against 1 plane of a cube, with additional 2 side planes for false-positive detection (normally 4 planes, but near and far planes are ignored).
        static ClipResult ClipPartial(Vector4 plane, Vector4 sidePlaneA, Vector4 sidePlaneB, Vector3 vsPos, float radius, float radiusSq, ref int insideCount)
        {
            float d = DistanceToPlane(plane, vsPos);
            if (d + radius <= 0.0f) // completely outside
                return ClipResult.Out;
            else if (d < 0.0f) // intersection: further check: only need to consider case where more than half the sphere is outside
            {
                Vector3 p = vsPos - (Vector3)plane * d;
                float rSq = radiusSq - d * d;
                if (SignedSq(DistanceToPlane(sidePlaneA, p)) >= -rSq
                 && SignedSq(DistanceToPlane(sidePlaneB, p)) >= -rSq)
                    return ClipResult.In;
            }
            else // consider as good as completely inside
                ++insideCount;

            return ClipResult.Unknown;
        }

        static Vector4 MakePlane(Vector3 pb, Vector3 pc)
        {
            Vector3 v0 = pb;
            Vector3 v1 = pc;
            Vector3 n = Vector3.Cross(v0, v1);
            n = Vector3.Normalize(n);

            // The planes pass all by the origin.
            return new Vector4(n.x, n.y, n.z, 0.0f);
        }

        static Vector4 MakePlane(Vector3 pa, Vector3 pb, Vector3 pc)
        {
            Vector3 v0 = pb - pa;
            Vector3 v1 = pc - pa;
            Vector3 n = Vector3.Cross(v0, v1);
            n = Vector3.Normalize(n);

            return new Vector4(n.x, n.y, n.z, -Vector3.Dot(n, pa));
        }

        static float DistanceToPlane(Vector4 plane, Vector3 p)
        {
            return plane.x * p.x + plane.y * p.y + plane.z * p.z + plane.w;
        }

        static float SignedSq(float f)
        {
            // slower!
            //return Mathf.Sign(f) * (f * f);
            return (f < 0.0f ? -1.0f : 1.0f) * (f * f);
        }

        static float Max(float a, float b, float c)
        {
            return a > b ? (a > c ? a : c) : (b > c ? b : c);
        }

        static Vector2 Abs(Vector2 v)
        {
            return new Vector2(v.x < 0.0f ? -v.x : v.x, v.y < 0.0f ? -v.y : v.y);
        }

        static int Align(int s, int alignment)
        {
            return ((s + alignment - 1) / alignment) * alignment;
        }
    }
}
