using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
    unsafe struct FineTilingJob : IJobFor
    {
        [ReadOnly]
        public NativeArray<LightMinMaxZ> minMaxZs;

        [ReadOnly]
        public NativeArray<TilingLightData> lights;

        public int lightsPerTile;

        [NativeDisableParallelForRestriction]
        public NativeArray<uint> tiles;

        [ReadOnly]
        public NativeArray<uint> groupTilesHit;

        [ReadOnly]
        public NativeArray<uint> groupTilesActive;

        public int2 screenResolution;

        public int2 groupResolution;

        public int2 tileResolution;

        public int tileWidth;

        public float3 viewOrigin;

        public float3 viewForward;

        public float3 viewRight;

        public float3 viewUp;

        public float4x4 worldToViewMatrix;

        public float2 fovHalf;

        public float farPlane;

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
            var groupIdG = math.int2(groupIndex % groupResolution.x, groupIndex / groupResolution.x);
            var groupIdT = groupIdG * groupWidth;
            var groupOffset = groupIndex * (lightsPerTile / 32);

            var groupWidthP = tileWidth * groupWidth;
            var groupIdP = groupIdG * groupWidthP;
            var groupRectS = new Rect((float2)groupIdP / screenResolution, math.float2(groupWidthP, groupWidthP) / screenResolution);

            if (math.any(groupIdP >= screenResolution)) return;

            var tilesOffsets = stackalloc int[groupLength];
            var currentLights = stackalloc uint[groupLength];
            var cs = stackalloc float2[groupLength];
            var worldToConeMatrices = stackalloc float4x4[groupLength];

            for (var coneIndexG = 0; coneIndexG < groupLength; coneIndexG++)
            {
                var coneIdG = math.int2(coneIndexG % groupWidth, coneIndexG / groupWidth);
                var coneIdT = groupIdT + coneIdG;
                var coneCenterNDC = (((float)tileWidth * ((float2)coneIdT + 0.5f)) / (float2)screenResolution) * 2.0f - 1.0f;
                var rotationAroundX = -math.atan(coneCenterNDC.y * fovHalf.y);
                var rotationAroundY = math.atan(coneCenterNDC.x * fovHalf.x);
                var worldToConeMatrix = math.mul(float4x4.EulerXYZ(rotationAroundX, rotationAroundY, 0), worldToViewMatrix);
                worldToConeMatrices[coneIndexG] = worldToConeMatrix;
                var nearPlanePosition = viewForward + viewRight * coneCenterNDC.x + viewUp * coneCenterNDC.y;
                var aperture = tileAperture / math.length(nearPlanePosition);
                var angle = math.atan(aperture);
                var c = math.float2(math.sin(angle), math.cos(angle));
                cs[coneIndexG] = c;
                var coneIndexT = coneIdT.y * tileResolution.x + coneIdT.x;
                tilesOffsets[coneIndexG] = coneIndexT * (lightsPerTile / 32);
            }

            var lightCount = lights.Length;
            var wordCount = (lightCount + 31) / 32;
            for (var wordIndex = 0; wordIndex < wordCount; wordIndex++)
            {
                var groupTilesIndex = groupOffset + wordIndex;
                var hit = groupTilesHit[groupTilesIndex];
                var active = groupTilesActive[groupTilesIndex];
                var remaining = active;

                var lightsInWord = wordIndex == wordCount - 1 ? lightCount % 32 : 32;
                var lightRangeMask = 0xFFFFFFFFu >> -lightsInWord;

                while (remaining != 0)
                {
                    var bitIndex = math.tzcnt(remaining);
                    var lightMask = 1u << bitIndex;
                    remaining ^= lightMask;

                    var lightIndex = wordIndex * 32 + bitIndex;
                    var light = lights[lightIndex];

                    if (false && !light.screenRect.Overlaps(groupRectS))
                    {
                        hit &= ~lightMask;
                        active ^= lightMask;
                        continue;
                    }

                    if (light.lightType == LightType.Point)
                    {
                        TestPointLight(ref light, lightMask, worldToConeMatrices, cs, currentLights);
                    }
                    else if (light.lightType == LightType.Spot)
                    {
                        TestSpotLight(ref light, lightMask, worldToConeMatrices, cs, currentLights);
                    }
                }

                for (var coneIndexG = 0; coneIndexG < groupLength; coneIndexG++)
                {
                    var tilesIndex = tilesOffsets[coneIndexG] + wordIndex;
                    tiles[tilesIndex] = tiles[tilesIndex] | currentLights[coneIndexG] | (~active & hit & lightRangeMask);
                    currentLights[coneIndexG] = 0;
                }
            }
        }

        void TestPointLight(
            ref TilingLightData light,
            uint lightMask,
            [NoAlias] float4x4* worldToConeMatrices,
            [NoAlias] float2* cs,
            [NoAlias] uint* currentLights
        )
        {
            for (var coneIndexG = 0; coneIndexG < groupLength; coneIndexG++)
            {
                var worldToConeMatrix = worldToConeMatrices[coneIndexG];
                var c = cs[coneIndexG];
                var originC = math.mul(worldToConeMatrix, math.float4(light.originW, 1)).xyz;

                var p = originC;
                var q = math.float2(math.length(p.xy), -p.z);
                var d = math.lengthsq(q - c * math.max(math.dot(q, c), 0f));
                var dist = d * ((q.x * c.y - q.y * c.x < 0f ? -1f : 1f));

                if (dist < light.radius * light.radius)
                {
                    currentLights[coneIndexG] |= lightMask;
                }
            }
        }

        void TestSpotLight(
            ref TilingLightData light,
            uint lightMask,
            [NoAlias] float4x4* worldToConeMatrices,
            [NoAlias] float2* cs,
            [NoAlias] uint* currentLights
        )
        {
            for (var coneIndexG = 0; coneIndexG < groupLength; coneIndexG++)
            {
                var hit = false;

                // if (false && light.lightType == LightType.Point)
                // {
                //     // Sphere-cone test
                //     var a = -math.pow(aperture, 2f) + 1f;
                //     var b = -2f * aperture * light.radius + 2f * math.dot(directionL, light.viewOriginL);
                //     var c = math.lengthsq(light.viewOriginL) - math.pow(light.radius, 2f);
                //     var d = math.pow(b, 2f) - 4f * a * c;
                //     hit = d >= 0;
                // }

                var worldToConeMatrix = worldToConeMatrices[coneIndexG];

                var c = cs[coneIndexG];
                var originC = math.mul(worldToConeMatrix, math.float4(light.originW, 1)).xyz;
                {
                    var p = originC;
                    var q = math.float2(math.length(p.xy), -p.z);
                    var d = math.lengthsq(q - c * math.max(math.dot(q, c), 0f));
                    var dist = d * ((q.x * c.y - q.y * c.x < 0f ? -1f : 1f));
                    hit = dist < light.radius * light.radius;
                }

                if (hit)
                {
                    hit = false;
                    var spotDirection = math.mul(worldToConeMatrix, math.float4(light.directionW, 0)).xyz;
                    var tMax = light.coneHeight;
                    var S = light.S;
                    var C = light.C;
                    var t = 0f;
                    var maxIterations = 16;
                    int i;
                    for (i = 0; i < maxIterations && t < tMax; i++)
                    {
                        var p = originC + t * spotDirection;
                        var q = math.float2(math.length(p.xy), -p.z);
                        var d = math.length(q - c * math.max(math.dot(q, c), 0f));
                        var dist = d * ((q.x * c.y - q.y * c.x < 0f ? -1f : 1f));
                        if (dist <= t * S)
                        {
                            hit = true;
                            break;
                        }
                        t = (t + 1e-3f + math.abs(dist)) * C;
                    }
                    hit |= i == maxIterations;
                }

                if (hit)
                {
                    currentLights[coneIndexG] |= lightMask;
                }
            }
        }
    }
}
