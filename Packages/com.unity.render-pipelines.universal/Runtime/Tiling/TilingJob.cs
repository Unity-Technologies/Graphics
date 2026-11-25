using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    // Projects each light/probe into per-row tile ranges; TileRangeExpansionJob turns them into tile masks.
    // tileRanges per entity: [m_Offset] = tile row (Y) range, [m_Offset + 1 + row] = column (X) range.
    [BurstCompile(FloatMode = FloatMode.Fast, DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    partial struct TilingJob : IJobFor
    {
        [ReadOnly]
        public NativeArray<VisibleLight> lights;

        [ReadOnly]
        public NativeArray<VisibleReflectionProbe> reflectionProbes;

        [ReadOnly]
        public bool reflectionProbeRotation;

        [NativeDisableParallelForRestriction]
        public NativeArray<InclusiveRange> tileRanges;

        public int itemsPerTile;
        public int rangesPerItem;

        public Fixed2<float4x4> worldToViews;

        public float2 tileScale;
        public float2 tileScaleInv;
        public Fixed2<float> viewPlaneBottoms;
        public Fixed2<float> viewPlaneTops;
        public Fixed2<float4> viewToViewportScaleBiases;
        public int2 tileCount;
        public float near;
        public float far;
        public bool isOrthographic;

        InclusiveRange m_TileYRange;
        int m_Offset;
        int m_ViewIndex;
        float4 m_ViewToViewportScaleBias;
        float2 m_CenterOffset;

        public void Execute(int jobIndex)
        {
            var index = jobIndex % itemsPerTile;
            m_ViewIndex = jobIndex / itemsPerTile;
            m_ViewToViewportScaleBias = viewToViewportScaleBiases[m_ViewIndex];
            m_Offset = jobIndex * rangesPerItem;

            m_TileYRange = new InclusiveRange(short.MaxValue, short.MinValue);

            for (var i = 0; i < rangesPerItem; i++)
            {
                tileRanges[m_Offset + i] = new InclusiveRange(short.MaxValue, short.MinValue);
            }


            if (index < lights.Length)
            {
                if (isOrthographic) { TileLightOrthographic(index); }
                else { TileLight(index); }
            }
            else
            {
                TileReflectionProbe(index);
            }
        }

        /// <summary>
        /// Project onto Z=1, scale and offset into [0, tileCount]
        /// </summary>
        float2 ViewToTileSpace(float3 positionVS)
        {
            return (positionVS.xy / positionVS.z * m_ViewToViewportScaleBias.xy + m_ViewToViewportScaleBias.zw) * tileScale;
        }

        /// <summary>
        /// Project onto Z=1, scale and offset into [0, tileCount]
        /// </summary>
        float2 ViewToTileSpaceOrthographic(float3 positionVS)
        {
            return (positionVS.xy * m_ViewToViewportScaleBias.xy + m_ViewToViewportScaleBias.zw) * tileScale;
        }

        void ExpandY(float3 positionVS) => ExpandTile(ViewToTileSpace(positionVS));

        bool IsInDepthRange(float3 positionVS) => IsDepthInRange(positionVS.z);

        bool IsDepthInRange(float depth) => depth >= near && depth <= far;

        // For points on a light's sphere: skips points outside the depth range and, for spot lights, outside the cone.
        bool IsInSphereCap(float3 positionVS, float3 lightPositionVS, float3 lightDirectionVS, float cosHalfAngle, bool isSpot) =>
            IsInDepthRange(positionVS) && (!isSpot || math.dot(math.normalize(positionVS - lightPositionVS), lightDirectionVS) >= cosHalfAngle);

        void ExpandOrthographic(float3 positionVS) => ExpandTile(ViewToTileSpaceOrthographic(positionVS));

        void ExpandRangeOrthographic(ref InclusiveRange range, float xVS) => ExpandTileX(ref range, ViewToTileSpaceOrthographic(xVS).x);

        /// <summary>
        /// Expands the tile Y range and the X range in the row containing the tile-space position.
        /// </summary>
        void ExpandTile(float2 positionTS)
        {
            // Floor, not truncate, so (-1, 0) maps off-screen; clamp before casting to keep the cast in range.
            var tile = (int2)math.clamp(math.floor(positionTS), -1f, (float2)tileCount);
            m_TileYRange.Expand((short)math.clamp(tile.y, 0, tileCount.y - 1));
            // Keep one tile of off-screen slack; TileRangeExpansionJob only sets on-screen columns,
            // so fully off-screen rows cover nothing while rows reaching an edge keep the edge tile.
            if (tile.y >= 0 && tile.y < tileCount.y)
            {
                var rowXRange = tileRanges[m_Offset + 1 + tile.y];
                rowXRange.Expand((short)tile.x);
                tileRanges[m_Offset + 1 + tile.y] = rowXRange;
            }
        }

        // Tile-space X counterpart of ExpandTile, for a row-boundary range.
        void ExpandTileX(ref InclusiveRange range, float tileX)
        {
            range.Expand((short)math.clamp(math.floor(tileX), -1f, tileCount.x));
        }

        // Merge planeRange into the rows above and below boundary planeIndex; m_Offset holds the Y range.
        void MergeTileRangeRows(int planeIndex, InclusiveRange planeRange)
        {
            if (planeIndex < tileCount.y)
            {
                var above = m_Offset + 1 + planeIndex;
                tileRanges[above] = InclusiveRange.Merge(tileRanges[above], planeRange);
            }
            if (planeIndex > 0)
            {
                var below = m_Offset + planeIndex;
                tileRanges[below] = InclusiveRange.Merge(tileRanges[below], planeRange);
            }
        }

        static float square(float x) => x * x;

        // Bound on view-space distances and light ranges, so powers of them stay finite under FloatMode.Fast; float precision
        // at this distance is already far coarser than anything a camera renders.
        internal const float k_MaxDistance = 1e9f;

        // Items without a volume affect no pixel and are skipped. Only point and spot lights are clustered.
        internal static bool HasNonZeroVolume(in VisibleLight light) =>
            light.range > 0f && (light.lightType == LightType.Point || (light.lightType == LightType.Spot && light.spotAngle > 0f));

        internal static bool HasNonZeroVolume(in VisibleReflectionProbe probe) => math.all((float3)probe.bounds.extents > 0f);
    }
}
