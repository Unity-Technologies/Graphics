using Unity.Collections;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace UnityEngine.Rendering.Universal.Internal
{
    internal class DeferredTiler
    {
        // Precomputed light data
        internal struct PrePointLight
        {
            // view-space position.
            public float3 vsPos;
            // Radius in world unit.
            public float radius;
            // Projected position of the sphere centre on the screen (near plane).
            public float2 screenPos;
            // Index into renderingData.lightData.visibleLights native array.
            public ushort visLightIndex;
        }

        enum ClipResult
        {
            Unknown,
            In,
            Out,
        }

        int m_TilePixelWidth;
        int m_TilePixelHeight;
        int m_TileXCount;
        int m_TileYCount;
        int m_TileHeader;
        int m_TileSize;
        int m_TilerLevel;

        // Adjusted frustum planes to account for tile size.
        FrustumPlanes m_FrustumPlanes;
        // Are we dealing with an orthographic projection.
        bool m_IsOrthographic;

        // Store all visible light indices for all tiles.
        // (currently) Contains sequential blocks of ushort values (light count, light indices and optionally additional per-tile "header" values), for each tile
        // For example for platforms using 16x16px tiles:
        // in a finest        tiler DeferredLights.m_Tilers[0] ( 16x16px  tiles), each tile will use a block of  1 *  1 * 32 (DeferredConfig.kMaxLightPerTile + 1) - 1 + 5 (fine m_TileHeader) =   36 ushort values
        // in an intermediate tiler DeferredLights.m_Tilers[1] ( 64x64px  tiles), each tile will use a block of  4 *  4 * 32 (DeferredConfig.kMaxLightPerTile + 1) - 1 + 1 (     m_TileHeader) =  512 ushort values
        // in a coarsest      tiler DeferredLights.m_Tilers[2] (256x256px tiles), each tile will use a block of 16 * 16 * 32 (DeferredConfig.kMaxLightPerTile + 1) - 1 + 1 (     m_TileHeader) = 8192 ushort values
        NativeArray<ushort> m_Tiles;    

        // Precompute tile data.
        NativeArray<PreTile> m_PreTiles;

        public DeferredTiler(int tilePixelWidth, int tilePixelHeight, int maxLightPerTile, int tilerLevel)
        {
            m_TilePixelWidth = tilePixelWidth;
            m_TilePixelHeight = tilePixelHeight;
            // Finest tiler (at index 0) computes extra tile data stored into the header, so it requires more space.
            // See CullFinalLights().
            m_TileHeader = tilerLevel == 0 ? 5 : 1;
            m_TileSize = maxLightPerTile + m_TileHeader;
            m_TilerLevel = tilerLevel;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetTileXCount()
        {
            return m_TileXCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetTileYCount()
        {
            return m_TileYCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetTilePixelWidth()
        {
            return m_TilePixelWidth;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetTilePixelHeight()
        {
            return m_TilePixelHeight;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetTileXStride()
        {
            return m_TileSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetTileYStride()
        {
            return m_TileSize * m_TileXCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetMaxLightPerTile()
        {
            return m_TileSize * m_TileHeader;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetTileHeader()
        {
            return m_TileHeader;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref NativeArray<ushort> GetTiles()
        {
            return ref m_Tiles;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetTileOffset(int i, int j)
        { return (i + j * m_TileXCount) * m_TileSize; }

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

            m_PreTiles = DeferredShaderData.instance.GetPreTiles(m_TilerLevel, m_TileXCount * m_TileYCount);

            // Adjust render width and height to account for tile size expanding over the screen (tiles have a fixed pixel size).
            int adjustedRenderWidth = Align(renderWidth, m_TilePixelWidth);
            int adjustedRenderHeight = Align(renderHeight, m_TilePixelHeight);

            // Now adjust the right and bottom clipping planes.
            m_FrustumPlanes = proj.decomposeProjection;
            m_FrustumPlanes.right = m_FrustumPlanes.left + (m_FrustumPlanes.right - m_FrustumPlanes.left) * (adjustedRenderWidth / (float)renderWidth);
            m_FrustumPlanes.bottom = m_FrustumPlanes.top + (m_FrustumPlanes.bottom - m_FrustumPlanes.top) * (adjustedRenderHeight / (float)renderHeight);
            m_IsOrthographic = isOrthographic;

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
                        preTile.planeLeft = MakePlane(new float3(tileLeft, tileBottom, -m_FrustumPlanes.zNear), new float3(tileLeft, tileTop, -m_FrustumPlanes.zNear));
                        preTile.planeRight = MakePlane(new float3(tileRight, tileTop, -m_FrustumPlanes.zNear), new float3(tileRight, tileBottom, -m_FrustumPlanes.zNear));
                        preTile.planeBottom = MakePlane(new float3(tileRight, tileBottom, -m_FrustumPlanes.zNear), new float3(tileLeft, tileBottom, -m_FrustumPlanes.zNear));
                        preTile.planeTop = MakePlane(new float3(tileLeft, tileTop, -m_FrustumPlanes.zNear), new float3(tileRight, tileTop, -m_FrustumPlanes.zNear));

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
                        preTile.planeLeft = MakePlane(new float3(tileLeft, tileBottom, -m_FrustumPlanes.zNear), new float3(tileLeft, tileBottom, -m_FrustumPlanes.zNear - 1.0f), new float3(tileLeft, tileTop, -m_FrustumPlanes.zNear));
                        preTile.planeRight = MakePlane(new float3(tileRight, tileTop, -m_FrustumPlanes.zNear), new float3(tileRight, tileTop, -m_FrustumPlanes.zNear - 1.0f), new float3(tileRight, tileBottom, -m_FrustumPlanes.zNear));
                        preTile.planeBottom = MakePlane(new float3(tileRight, tileBottom, -m_FrustumPlanes.zNear), new float3(tileRight, tileBottom, -m_FrustumPlanes.zNear - 1.0f), new float3(tileLeft, tileBottom, -m_FrustumPlanes.zNear));
                        preTile.planeTop = MakePlane(new float3(tileLeft, tileTop, -m_FrustumPlanes.zNear), new float3(tileLeft, tileTop, -m_FrustumPlanes.zNear - 1.0f), new float3(tileRight, tileTop, -m_FrustumPlanes.zNear));

                        m_PreTiles[i + j * m_TileXCount] = preTile;
                    }
                }
            }
        }

        // This differs from CullIntermediateLights in 3 ways:
        // - tile-frustums/light intersection use different algorithm
        // - depth range of the light shape intersecting the tile-frustums is output in the tile list header section
        // - light indices written out are indexing visible_lights, rather than the array of PrePointLights.
        unsafe public void CullFinalLights(ref NativeArray<PrePointLight> pointLights,
                                           ref NativeArray<ushort> lightIndices, int lightStartIndex, int lightCount,
                                           int istart, int iend, int jstart, int jend)
        {
//            Assertions.Assert.IsTrue(m_TileHeader >= 5, "not enough space to store min&max depth information for light list ");

            int tileXStride = m_TileSize;
            int tileYStride = m_TileSize * m_TileXCount;
            int maxLightPerTile = m_TileSize - m_TileHeader;
            int tileHeader = m_TileHeader;
            int lightEndIndex = lightStartIndex + lightCount;
            bool isOrthographic = m_IsOrthographic;

            float2 tileSize = new float2((m_FrustumPlanes.right - m_FrustumPlanes.left) / m_TileXCount,
                                         (m_FrustumPlanes.top - m_FrustumPlanes.bottom) / m_TileYCount);
            float2 tileExtents = tileSize * 0.5f;
            float2 tileExtentsInv = new float2(1.0f / tileExtents.x, 1.0f / tileExtents.y);

            // Store min&max depth range for each light in a tile.
            float2* minMax = stackalloc float2[maxLightPerTile];

            for (int j = jstart; j < jend; ++j)
            {
                float tileYCentre = m_FrustumPlanes.top - (tileExtents.y + j * tileSize.y);

                for (int i = istart; i < iend; ++i)
                {
                    float tileXCentre = m_FrustumPlanes.left + tileExtents.x + i * tileSize.x;

                    PreTile preTile = m_PreTiles[i + j * m_TileXCount];
                    int tileOffset = i * tileXStride + j * tileYStride;
                    ushort tileLightCount = 0;

                    // For the current tile's light list, min&max depth range (absolute values).
                    float listMinDepth = float.MaxValue;
                    float listMaxDepth = -float.MaxValue;

                    for (int vi = lightStartIndex; vi < lightEndIndex; ++vi)
                    {
                        ushort lightIndex = lightIndices[vi];
                        PrePointLight ppl = pointLights[lightIndex];

                        // Offset tileCentre toward the light to calculate a more conservative minMax depth bound,
                        // but it must remains inside the tile and must not pass further than the light centre.
                        float2 tileCentre = new float2(tileXCentre, tileYCentre);
                        float2 dir = ppl.screenPos - tileCentre;
                        float2 d = abs(dir * tileExtentsInv);

                        float sInv = 1.0f / max3(d.x, d.y, 1.0f);
                        float3 tileOffCentre;
                        float3 tileOrigin;

                        if (!isOrthographic)
                        {
                            tileOrigin = new float3(0.0f);
                            tileOffCentre = new float3(tileCentre.x + dir.x * sInv, tileCentre.y + dir.y * sInv, -m_FrustumPlanes.zNear);
                        }
                        else
                        {
                            tileOrigin = new float3(tileCentre.x + dir.x * sInv, tileCentre.y + dir.y * sInv, 0.0f);
                            tileOffCentre = new float3(0, 0, -m_FrustumPlanes.zNear);
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

                        // Because this always output to the finest tiles, contrary to CullLights(),
                        // the result are indices into visibleLights, instead of indices into pointLights.
                        m_Tiles[tileOffset + tileHeader + tileLightCount] = pointLights[lightIndex].visLightIndex;
                        ++tileLightCount;
                    }

                    // Clamp our light list depth range.
                    listMinDepth = max2(listMinDepth, m_FrustumPlanes.zNear);
                    listMaxDepth = min2(listMaxDepth, m_FrustumPlanes.zFar);

                    // Calculate bitmask for 2.5D culling.
                    uint bitMask = 0;
                    float depthRangeInv = 1.0f / (listMaxDepth - listMinDepth);
                    for (int tileLightIndex = 0; tileLightIndex < tileLightCount; ++tileLightIndex)
                    {
                        float lightMinDepth = max2(minMax[tileLightIndex].x, m_FrustumPlanes.zNear);
                        float lightMaxDepth = min2(minMax[tileLightIndex].y, m_FrustumPlanes.zFar);
                        int firstBit = (int)((lightMinDepth - listMinDepth) * 32.0f * depthRangeInv);
                        int lastBit = (int)((lightMaxDepth - listMinDepth) * 32.0f * depthRangeInv);
                        int bitCount = lastBit - firstBit + 1;
                        bitCount = (bitCount > 32 ? 32 : bitCount);
                        bitMask |= (uint)(((1ul << bitCount) - 1) << firstBit);
                    }

                    // As listMinDepth and listMaxDepth are used to calculate the geometry 2.5D bitmask,
                    // we can optimize the shader execution (TileDepthInfo.shader) by refactoring the calculation.
                    //   int bitIndex = 32.0h * (geoDepth - listMinDepth) / (listMaxDepth - listMinDepth);
                    // Equivalent to:
                    //   a =                 32.0 / (listMaxDepth - listMinDepth)
                    //   b = -listMinDepth * 32.0 / (listMaxDepth - listMinDepth)
                    //   int bitIndex = geoDepth * a + b;
                    //
                    float a = 32.0f * depthRangeInv;
                    float b = -listMinDepth * a;

                    m_Tiles[tileOffset] = tileLightCount;
                    m_Tiles[tileOffset + 1] = (ushort)_f32tof16(a);
                    m_Tiles[tileOffset + 2] = (ushort)_f32tof16(b);
                    m_Tiles[tileOffset + 3] = (ushort)(bitMask & 0xFFFF);
                    m_Tiles[tileOffset + 4] = (ushort)((bitMask >> 16) & 0xFFFF);
                }
            }
        }

        // Unity.Mathematics.max() function calls Single_IsNan() which significantly slow down the code (up to 20% of CullFinalLights())!
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float min2(float a, float b)
        {
            return a < b ? a : b;
        }

        // Unity.Mathematics.min() function calls Single_IsNan() which significantly slow down the code (up to 20% of CullFinalLights())!
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float max2(float a, float b)
        {
            return a > b ? a : b;
        }

        // This is copy-pasted from Unity.Mathematics.math.f32tof16(), but use min2() function that does not check for NaN (which would consume 10% of the execution time of CullFinalLights()).
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint _f32tof16(float x)
        {
            const int infinity_32 = 255 << 23;
            const uint msk = 0x7FFFF000u;

            uint ux = asuint(x);
            uint uux = ux & msk;
            uint h = (uint)(asuint(min2(asfloat(uux) * 1.92592994e-34f, 260042752.0f)) + 0x1000) >> 13;   // Clamp to signed infinity if overflowed
            h = select(h, select(0x7c00u, 0x7e00u, (int)uux > infinity_32), (int)uux >= infinity_32);   // NaN->qNaN and Inf->Inf
            return h | (ux & ~msk) >> 16;
        }

        // TODO: finer culling for spot lights
        public void CullIntermediateLights(ref NativeArray<PrePointLight> pointLights,
                                           ref NativeArray<ushort> lightIndices, int lightStartIndex, int lightCount,
                                           int istart, int iend, int jstart, int jend)
        {
            int tileXStride = m_TileSize;
            int tileYStride = m_TileSize * m_TileXCount;
            int maxLightPerTile = m_TileSize - m_TileHeader;
            int tileHeader = m_TileHeader;
            int lightEndIndex = lightStartIndex + lightCount;

            for (int j = jstart; j < jend; ++j)
            {
                for (int i = istart; i < iend; ++i)
                {
                    PreTile preTile = m_PreTiles[i + j * m_TileXCount];
                    int tileOffset = i * tileXStride + j * tileYStride;
                    ushort tileLightCount = 0;

                    for (int vi = lightStartIndex; vi < lightEndIndex; ++vi)
                    {
                        ushort lightIndex = lightIndices[vi];
                        PrePointLight ppl = pointLights[lightIndex];

                        // This is slightly faster than IntersectionLineSphere().
                        if (!Clip(ref preTile, ppl.vsPos, ppl.radius))
                            continue;

                        if (tileLightCount == maxLightPerTile)
                        {
                            // TODO log error: tile is full
                            break;
                        }

                        m_Tiles[tileOffset + tileHeader + tileLightCount] = lightIndex;
                        ++tileLightCount;
                    }

                    m_Tiles[tileOffset] = tileLightCount;
                }
            }
        }

        // Return parametric intersection between a sphere and a line.
        // The intersections points P0 and P1 are:
        // P0 = raySource + rayDirection * t0.
        // P1 = raySource + rayDirection * t1.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe static bool IntersectionLineSphere(float3 centre, float radius, float3 raySource, float3 rayDirection, out float t0, out float t1)
        {
            float3 _centre = *(float3*)&centre;
            float3 _raySource = *(float3*)&raySource;
            float3 _rayDirection = *(float3*)&rayDirection;

            float A = dot(_rayDirection, _rayDirection); // always >= 0
            float B = dot(_raySource - _centre, _rayDirection);
            float C = dot(_raySource, _raySource)
                    + dot(_centre, _centre)
                    - (radius * radius)
                    - 2 * dot(_raySource, _centre);
            float discriminant = (B * B) - A * C;
            if (discriminant > 0)
            {
                float sqrt_discriminant = sqrt(discriminant);
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
        static bool Clip(ref PreTile tile, float3 vsPos, float radius)
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ClipResult ClipPartial(float4 plane, float4 sidePlaneA, float4 sidePlaneB, float3 vsPos, float radius, float radiusSq, ref int insideCount)
        {
            float d = DistanceToPlane(plane, vsPos);
            if (d + radius <= 0.0f) // completely outside
                return ClipResult.Out;
            else if (d < 0.0f) // intersection: further check: only need to consider case where more than half the sphere is outside
            {
                float3 p = vsPos - plane.xyz * d;
                float rSq = radiusSq - d * d;
                if (SignedSq(DistanceToPlane(sidePlaneA, p)) >= -rSq
                 && SignedSq(DistanceToPlane(sidePlaneB, p)) >= -rSq)
                    return ClipResult.In;
            }
            else // consider as good as completely inside
                ++insideCount;

            return ClipResult.Unknown;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float4 MakePlane(float3 pb, float3 pc)
        {
            float3 v0 = pb;
            float3 v1 = pc;
            float3 n = cross(v0, v1);
            n = normalize(n);

            // The planes pass all by the origin.
            return new float4(n.x, n.y, n.z, 0.0f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float4 MakePlane(float3 pa, float3 pb, float3 pc)
        {
            float3 v0 = pb - pa;
            float3 v1 = pc - pa;
            float3 n = cross(v0, v1);
            n = normalize(n);

            return new float4(n.x, n.y, n.z, -dot(n, pa));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float DistanceToPlane(float4 plane, float3 p)
        {
            return plane.x * p.x + plane.y * p.y + plane.z * p.z + plane.w;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float SignedSq(float f)
        {
            // slower!
            //return Mathf.Sign(f) * (f * f);
            return (f < 0.0f ? -1.0f : 1.0f) * (f * f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float max3(float a, float b, float c)
        {
            return a > b ? (a > c ? a : c) : (b > c ? b : c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int Align(int s, int alignment)
        {
            return ((s + alignment - 1) / alignment) * alignment;
        }
    }
}
