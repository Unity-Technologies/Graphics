using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    // Culls slices along one axis of the screen.
    [BurstCompile]
    unsafe struct SliceCullingJob : IJobFor
    {
        public float scale;
        public float3 viewOrigin;
        public float3 viewForward;
        public float3 viewRight;
        public float3 viewUp;

        [ReadOnly]
        public NativeArray<LightType> lightTypes;

        [ReadOnly]
        public NativeArray<float> radiuses;

        [ReadOnly]
        public NativeArray<float3> directions;

        [ReadOnly]
        public NativeArray<float3> positions;

        [ReadOnly]
        public NativeArray<float> coneRadiuses;

        public int lightsPerTile;

        [NativeDisableParallelForRestriction]
        public NativeArray<uint> sliceLightMasks;

        public void Execute(int index)
        {
            var leftX = (((float)index) * scale) * 2f - 1f;
            var rightX = (((float)index + 1f) * scale) * 2f - 1f;
            var leftPlane = ComputePlane(viewOrigin,
                viewOrigin + viewForward + viewRight * leftX + viewUp,
                viewOrigin + viewForward + viewRight * leftX - viewUp);
            var rightPlane = ComputePlane(viewOrigin,
                viewOrigin + viewForward + viewRight * rightX - viewUp,
                viewOrigin + viewForward + viewRight * rightX + viewUp);

            var lightCount = lightTypes.Length;
            var lightWordCount = (lightCount + 31) / 32;

            var sectionOffset = index * lightsPerTile / 32;

            // Handle lights in multiples of 32
            for (var lightWordIndex = 0; lightWordIndex < lightWordCount; lightWordIndex++)
            {
                var wordLightMask = 0u;
                var lightsInWord = math.min(32, lightCount - lightWordIndex * 32);
                for (var bitIndex = 0; bitIndex < lightsInWord; bitIndex++)
                {
                    var lightIndex = lightWordIndex * 32 + bitIndex;
                    if (ContainsLight(leftPlane, rightPlane, lightIndex))
                    {
                        wordLightMask |= 1u << bitIndex;
                    }
                }

                var wordIndex = sectionOffset + lightWordIndex;
                sliceLightMasks[wordIndex] = sliceLightMasks[wordIndex] | wordLightMask;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool ContainsLight(Plane leftPlane, Plane rightPlane, int lightIndex)
        {
            var hit = true;

            var sphere = new Sphere
            {
                center = positions[lightIndex],
                radius = radiuses[lightIndex]
            };

            if (SphereBehindPlane(sphere, leftPlane) || SphereBehindPlane(sphere, rightPlane))
            {
                hit = false;
            }

            if (hit && lightTypes[lightIndex] == LightType.Spot)
            {
                var cone = new Cone
                {
                    tip = sphere.center,
                    direction = directions[lightIndex],
                    height = radiuses[lightIndex],
                    radius = coneRadiuses[lightIndex]
                };
                if (ConeBehindPlane(cone, leftPlane) || ConeBehindPlane(cone, rightPlane))
                {
                    hit = false;
                }
            }

            return hit;
        }

        struct Cone
        {
            public float3 tip;
            public float3 direction;
            public float height;
            public float radius;
        }

        struct Sphere
        {
            public float3 center;
            public float radius;
        }

        struct Plane
        {
            public float3 normal;
            public float distanceToOrigin;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Plane ComputePlane(float3 p0, float3 p1, float3 p2)
        {
            Plane plane;

            float3 v0 = p1 - p0;
            float3 v2 = p2 - p0;

            plane.normal = math.normalize(math.cross(v0, v2));

            // Compute the distance to the origin using p0.
            plane.distanceToOrigin = math.dot(plane.normal, p0);

            return plane;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool SphereBehindPlane(Sphere sphere, Plane plane)
        {
            float dist = math.dot(sphere.center, plane.normal) - plane.distanceToOrigin;
            return dist < -sphere.radius;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool PointBehindPlane(float3 p, Plane plane)
        {
            return math.dot(plane.normal, p) - plane.distanceToOrigin < 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool ConeBehindPlane(Cone cone, Plane plane)
        {
            float3 furthestPointDirection = math.cross(math.cross(plane.normal, cone.direction), cone.direction);
            float3 furthestPointOnCircle = cone.tip + cone.direction * cone.height - furthestPointDirection * cone.radius;
            return PointBehindPlane(cone.tip, plane) && PointBehindPlane(furthestPointOnCircle, plane);
        }
    }
}
