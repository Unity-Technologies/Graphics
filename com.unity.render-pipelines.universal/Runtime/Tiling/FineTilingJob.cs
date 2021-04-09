using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    [BurstCompile]
    unsafe struct FineTilingJob : IJobFor
    {
        [ReadOnly]
        public NativeArray<LightMinMaxZ> minMaxZs;

        [ReadOnly]
        public NativeArray<VisibleLight> lights;

        [ReadOnly]
        public NativeArray<float4x4> worldToLightMatrices;

        [ReadOnly]
        public NativeArray<float3> viewOriginLs;

        [ReadOnly]
        public NativeArray<SphereShape> sphereShapes;

        [ReadOnly]
        public NativeArray<ConeShape> coneShapes;

        public int lightsPerTile;

        [NativeDisableParallelForRestriction]
        public NativeArray<uint> tiles;

        public int2 screenResolution;

        public int2 groupResolution;

        public int2 tileResolution;

        public int tileWidth;

        public float3 viewOrigin;

        public float3 viewForward;

        public float3 viewRight;

        public float3 viewUp;

        public float tileAperture;

        public const int groupWidth = 4;

        public const int groupLength = groupWidth * groupWidth;

        public void Execute(int groupIndex)
        {
            // Space suffixes:
            // - G group
            // - S screen space normalized
            // - P screen space pixels
            // - T tile
            // - W world
            var groupWidthP = groupWidth * tileWidth;
            var groupIdG = math.int2(groupIndex % groupResolution.x, groupIndex / groupResolution.x);
            var groupIdP = groupIdG * groupWidthP;
            var groupIdT = groupIdG * groupWidth;
            var groupRectS = new Rect((float2)groupIdP / screenResolution, math.float2(groupWidthP, groupWidthP) / screenResolution);

            var actives = stackalloc bool[groupLength];
            var directionWs = stackalloc float3[groupLength];
            var apertures = stackalloc float[groupLength];
            var tilesOffsets = stackalloc int[groupLength];
            var groupTiles = stackalloc uint[groupLength];

            for (var coneIndexG = 0; coneIndexG < groupLength; coneIndexG++)
            {
                var coneIdG = math.int2(coneIndexG % groupWidth, coneIndexG / groupWidth);
                var coneIdT = groupIdT + coneIdG;
                var coneCenterNDC = ((float)tileWidth * ((float2)coneIdT + 0.5f) / (float2)screenResolution) * 2.0f - 1.0f;
                var nearPlanePosition = viewForward + viewRight * coneCenterNDC.x + viewUp * coneCenterNDC.y;
                directionWs[coneIndexG] = nearPlanePosition / math.length(nearPlanePosition);
                apertures[coneIndexG] = tileAperture / math.length(nearPlanePosition);
                actives[coneIndexG] = math.all(coneIdT < tileResolution);
                var coneIndexT = coneIdT.y * tileResolution.x + coneIdT.x;
                tilesOffsets[coneIndexG] = coneIndexT * (lightsPerTile / 32);
            }

            var wordCount = lights.Length / 32;
            for (var wordIndex = 0; wordIndex < wordCount; wordIndex++)
            {
                UnsafeUtility.MemSet(groupTiles, 0, sizeof(uint) * groupLength);

                for (var i = 0; i < 32; i++)
                {
                    var lightIndex = wordIndex * 32 + i;
                    var light = lights[lightIndex];

                    if (!light.screenRect.Overlaps(groupRectS))
                    {
                        continue;
                    }

                    var lightOffset = wordIndex;
                    var lightMask = 1u << (lightIndex % lightsPerTile);

                    if (light.lightType == LightType.Point)
                    {
                        ConeMarch(sphereShapes[lightIndex], lightIndex, lightMask, directionWs, apertures, groupTiles);
                    }
                    else if (light.lightType == LightType.Spot)
                    {
                        ConeMarch(coneShapes[lightIndex], lightIndex, lightMask, directionWs, apertures, groupTiles);
                    }
                }

                for (var coneIndexG = 0; coneIndexG < groupLength; coneIndexG++)
                {
                    if (actives[coneIndexG])
                    {
                        var tileLightsIndex = tilesOffsets[coneIndexG] + wordIndex;
                        tiles[tileLightsIndex] = tiles[tileLightsIndex] | groupTiles[coneIndexG];
                    }
                }
            }
        }

        void ConeMarch<T>(
            T shape,
            int lightIndex,
            uint lightMask,
            [NoAlias] float3* directionWs,
            [NoAlias] float* apertures,
            [NoAlias] uint* groupTiles
        ) where T : ICullingShape
        {
            var worldToLight = worldToLightMatrices[lightIndex];
            var lightMinMax = minMaxZs[lightIndex];
            var originL = viewOriginLs[lightIndex];

            for (var coneIndexG = 0; coneIndexG < groupLength; coneIndexG++)
            {
                var directionW = directionWs[coneIndexG];
                var t = math.dot(lightMinMax.minZ * viewForward, directionW);
                var tMax = math.dot(lightMinMax.maxZ * viewForward, directionW);
                var hit = false;
                var aperture = apertures[coneIndexG];

                var directionL = math.mul(worldToLight, math.float4(directionW, 0f)).xyz;

                while (t < tMax)
                {
                    var positionL = originL + directionL * t;
                    var distance = shape.SampleDistance(positionL);
                    t += distance;
                    if (distance < tileAperture * t)
                    {
                        hit = true;
                        break;
                    }
                }

                if (hit)
                {
                    groupTiles[coneIndexG] |= lightMask;
                }
            }
        }
    }
}
