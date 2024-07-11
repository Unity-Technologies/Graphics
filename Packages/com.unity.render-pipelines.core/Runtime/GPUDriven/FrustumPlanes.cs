using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.Rendering
{
    // 6-component representation of a (infinite length) line in 3D space
    internal struct Line
    {
        // for the line to be valid, dot(m, t) == 0
        public float3 m;
        public float3 t;

        internal static Line LineOfPlaneIntersectingPlane(float4 a, float4 b)
        {
            // planes do not need to have a unit length normal
            return new Line {
                m = a.w*b.xyz - b.w*a.xyz,
                t = math.cross(a.xyz, b.xyz),
            };
        }

        internal static float4 PlaneContainingLineAndPoint(Line a, float3 b)
        {
            // the resulting plane will not have a unit length normal (and the normal will be approximately zero when no plane exists)
            return new float4(a.m + math.cross(a.t, b), -math.dot(a.m, b));
        }

        internal static float4 PlaneContainingLineWithNormalPerpendicularToVector(Line a, float3 b)
        {
            // the resulting plane will not have a unit length normal (and the normal will be approximately zero when no plane exists)
            return new float4(math.cross(a.t, b), -math.dot(a.m, b));
        }
    }

    internal struct ReceiverPlanes
    {
        public NativeList<Plane> planes;
        public int lightFacingPlaneCount;

        private static bool IsSignBitSet(float x)
        {
            uint i = math.asuint(x);
            return (i >> 31) != 0;
        }

        internal NativeArray<Plane> LightFacingFrustumPlaneSubArray()
        {
            return planes.AsArray().GetSubArray(0, lightFacingPlaneCount);
        }

        internal NativeArray<Plane> SilhouettePlaneSubArray()
        {
            return planes.AsArray().GetSubArray(lightFacingPlaneCount, planes.Length - lightFacingPlaneCount);
        }

        internal static ReceiverPlanes CreateEmptyForTesting(Allocator allocator)
        {
            return new ReceiverPlanes()
            {
                planes = new NativeList<Plane>(allocator),
                lightFacingPlaneCount = 0,
            };
        }

        internal void Dispose(JobHandle job)
        {
            planes.Dispose(job);
        }

        internal static ReceiverPlanes Create(in BatchCullingContext cc, Allocator allocator)
        {
            var result = new ReceiverPlanes()
            {
                planes = new NativeList<Plane>(allocator),
                lightFacingPlaneCount = 0,
            };

            if (cc.viewType == BatchCullingViewType.Light && cc.receiverPlaneCount != 0)
            {
                bool isLightOrthographic = false;
                if (cc.cullingSplits.Length > 0)
                {
                    Matrix4x4 m = cc.cullingSplits[0].cullingMatrix;
                    isLightOrthographic = m[15] == 1.0f && m[11] == 0.0f && m[7] == 0.0f && m[3] == 0.0f;
                }
                if (isLightOrthographic)
                {
                    Vector3 lightDir = -cc.localToWorldMatrix.GetColumn(2);

                    // cache result for each plane, add planes facing towards the light
                    int planeSignBits = 0;
                    for (int i = 0; i < cc.receiverPlaneCount; ++i)
                    {
                        var plane = cc.cullingPlanes[cc.receiverPlaneOffset + i];
                        float facingTerm = Vector3.Dot(plane.normal, lightDir);
                        if (IsSignBitSet(facingTerm))
                            planeSignBits |= (1 << i);
                        else
                            result.planes.Add(plane);
                    }
                    result.lightFacingPlaneCount = result.planes.Length;

                    // assume ordering +/-x, +/-y, +/-z for frustum planes, test pairs for silhouette edges
                    if (cc.receiverPlaneCount == 6)
                    {
                        for (int i = 0; i < cc.receiverPlaneCount; ++i)
                        {
                            for (int j = i + 1; j < cc.receiverPlaneCount; ++j)
                            {
                                // skip pairs that are from the same frustum axis (i.e. both xs, both ys or both zs)
                                if ((i / 2) == (j / 2))
                                    continue;

                                // silhouette edges occur when the planes have opposing signs
                                int signCheck = ((planeSignBits >> i) ^ (planeSignBits >> j)) & 1;
                                if (signCheck == 0)
                                    continue;

                                // process in consistent order for consistent plane normal in the result
                                var (indexA, indexB) = (((planeSignBits >> i) & 1) == 0) ? (i, j) : (j, i);
                                var planeA = cc.cullingPlanes[cc.receiverPlaneOffset + indexA];
                                var planeB = cc.cullingPlanes[cc.receiverPlaneOffset + indexB];

                                // construct a plane that contains the light origin and this silhouette edge
                                var planeEqA = new float4(planeA.normal, planeA.distance);
                                var planeEqB = new float4(planeB.normal, planeB.distance);
                                var silhouetteEdge = Line.LineOfPlaneIntersectingPlane(planeEqA, planeEqB);
                                var silhouettePlaneEq = Line.PlaneContainingLineWithNormalPerpendicularToVector(silhouetteEdge, lightDir);

                                // try to normalize
                                silhouettePlaneEq = silhouettePlaneEq / math.length(silhouettePlaneEq.xyz);
                                if (!math.any(math.isnan(silhouettePlaneEq)))
                                     result.planes.Add(new Plane(silhouettePlaneEq.xyz, silhouettePlaneEq.w));
                            }
                        }
                    }
                }
                else
                {
                    var lightPos = cc.localToWorldMatrix.GetPosition();

                    // cache result for each plane, add planes facing towards the light
                    int planeSignBits = 0;
                    for (int i = 0; i < cc.receiverPlaneCount; ++i)
                    {
                        var plane = cc.cullingPlanes[cc.receiverPlaneOffset + i];
                        float distance = plane.GetDistanceToPoint(lightPos);
                        if (IsSignBitSet(distance))
                            planeSignBits |= (1 << i);
                        else
                            result.planes.Add(plane);
                    }
                    result.lightFacingPlaneCount = result.planes.Length;

                    // assume ordering +/-x, +/-y, +/-z for frustum planes, test pairs for silhouette edges
                    if (cc.receiverPlaneCount == 6)
                    {
                        for (int i = 0; i < cc.receiverPlaneCount; ++i)
                        {
                            for (int j = i + 1; j < cc.receiverPlaneCount; ++j)
                            {
                                // skip pairs that are from the same frustum axis (i.e. both xs, both ys or both zs)
                                if ((i / 2) == (j / 2))
                                    continue;

                                // silhouette edges occur when the planes have opposing signs
                                int signCheck = ((planeSignBits >> i) ^ (planeSignBits >> j)) & 1;
                                if (signCheck == 0)
                                    continue;

                                // process in consistent order for consistent plane normal in the result
                                var (indexA, indexB) = (((planeSignBits >> i) & 1) == 0) ? (i, j) : (j, i);
                                var planeA = cc.cullingPlanes[cc.receiverPlaneOffset + indexA];
                                var planeB = cc.cullingPlanes[cc.receiverPlaneOffset + indexB];

                                // construct a plane that contains the light origin and this silhouette edge
                                var planeEqA = new float4(planeA.normal, planeA.distance);
                                var planeEqB = new float4(planeB.normal, planeB.distance);
                                var silhouetteEdge = Line.LineOfPlaneIntersectingPlane(planeEqA, planeEqB);
                                var silhouettePlaneEq = Line.PlaneContainingLineAndPoint(silhouetteEdge, lightPos);

                                // try to normalize
                                silhouettePlaneEq = silhouettePlaneEq / math.length(silhouettePlaneEq.xyz);
                                if (!math.any(math.isnan(silhouettePlaneEq)))
                                     result.planes.Add(new Plane(silhouettePlaneEq.xyz, silhouettePlaneEq.w));
                            }
                        }
                    }
                }
            }

            return result;
        }
    }

    internal struct FrustumPlaneCuller
    {
        internal struct PlanePacket4
        {
            public float4 nx;
            public float4 ny;
            public float4 nz;
            public float4 d;
            // Store absolute values of plane normals to avoid recalculating per instance
            public float4 nxAbs;
            public float4 nyAbs;
            public float4 nzAbs;

            public PlanePacket4(NativeArray<Plane> planes, int offset, int limit)
            {
                Plane p0 = planes[Mathf.Min(offset + 0, limit)];
                Plane p1 = planes[Mathf.Min(offset + 1, limit)];
                Plane p2 = planes[Mathf.Min(offset + 2, limit)];
                Plane p3 = planes[Mathf.Min(offset + 3, limit)];
                nx = new float4(p0.normal.x, p1.normal.x, p2.normal.x, p3.normal.x);
                ny = new float4(p0.normal.y, p1.normal.y, p2.normal.y, p3.normal.y);
                nz = new float4(p0.normal.z, p1.normal.z, p2.normal.z, p3.normal.z);
                d = new float4(p0.distance, p1.distance, p2.distance, p3.distance);
                nxAbs = math.abs(nx);
                nyAbs = math.abs(ny);
                nzAbs = math.abs(nz);
            }
        }

        internal struct SplitInfo
        {
            public int packetCount;
        }

        public NativeList<PlanePacket4> planePackets;
        public NativeList<SplitInfo> splitInfos;

        internal void Dispose(JobHandle job)
        {
            planePackets.Dispose(job);
            splitInfos.Dispose(job);
        }

        internal static FrustumPlaneCuller Create(in BatchCullingContext cc, NativeArray<Plane> receiverPlanes, in ReceiverSphereCuller receiverSphereCuller, Allocator allocator)
        {
            int splitCount = cc.cullingSplits.Length;

            int totalPacketCount = 0;
            for (int splitIndex = 0; splitIndex < splitCount; ++splitIndex)
            {
                int planeCount = receiverPlanes.Length + cc.cullingSplits[splitIndex].cullingPlaneCount;
                totalPacketCount += (planeCount + 3)/4;
            }

            FrustumPlaneCuller result = new FrustumPlaneCuller()
            {
                planePackets = new NativeList<PlanePacket4>(totalPacketCount, allocator),
                splitInfos = new NativeList<SplitInfo>(splitCount, allocator),
            };
            result.planePackets.ResizeUninitialized(totalPacketCount);
            result.splitInfos.ResizeUninitialized(splitCount);

            var tmpPlanes = new NativeList<Plane>(Allocator.Temp);
            int packetBase = 0;
            for (int splitIndex = 0; splitIndex < splitCount; ++splitIndex)
            {
                CullingSplit split = cc.cullingSplits[splitIndex];

                tmpPlanes.Clear();

                // use all culling planes
                for (int i = 0; i < split.cullingPlaneCount; ++i)
                    tmpPlanes.Add(cc.cullingPlanes[split.cullingPlaneOffset + i]);

                // conditionally use receiver planes
                if (receiverSphereCuller.UseReceiverPlanes())
                    tmpPlanes.AddRange(receiverPlanes);

                int packetCount = (tmpPlanes.Length + 3)/4;
                result.splitInfos[splitIndex] = new SplitInfo()
                {
                    packetCount = packetCount,
                };

                for (int i = 0; i < packetCount; ++i)
                    result.planePackets[packetBase + i] = new PlanePacket4(tmpPlanes.AsArray(), 4*i, tmpPlanes.Length - 1);
                packetBase += packetCount;
            }
            tmpPlanes.Dispose();
            return result;
        }

        internal static uint ComputeSplitVisibilityMask(NativeArray<PlanePacket4> planePackets, NativeArray<SplitInfo> splitInfos, in AABB bounds)
        {
            float4 cx = bounds.center.xxxx;
            float4 cy = bounds.center.yyyy;
            float4 cz = bounds.center.zzzz;

            float4 ex = bounds.extents.xxxx;
            float4 ey = bounds.extents.yyyy;
            float4 ez = bounds.extents.zzzz;

            uint splitVisibilityMask = 0;

            int packetBase = 0;
            int splitCount = splitInfos.Length;
            for (int splitIndex = 0; splitIndex < splitCount; ++splitIndex)
            {
                SplitInfo splitInfo = splitInfos[splitIndex];

                bool4 isCulled = new bool4(false);
                for (int i = 0; i < splitInfo.packetCount; ++i)
                {
                    PlanePacket4 p = planePackets[packetBase + i];
                    float4 distances = p.nx*cx + p.ny*cy + p.nz*cz + p.d;
                    float4 radii = p.nxAbs*ex + p.nyAbs*ey + p.nzAbs*ez;

                    isCulled = isCulled | (distances + radii < float4.zero);
                }
                if (!math.any(isCulled))
                    splitVisibilityMask |= 1U << splitIndex;

                packetBase += splitInfo.packetCount;
            }

            return splitVisibilityMask;
        }
    }

    internal struct ReceiverSphereCuller
    {
        internal struct SplitInfo
        {
            public float4 receiverSphereLightSpace;
            public float cascadeBlendCullingFactor;
        }

        public NativeList<SplitInfo> splitInfos;
        public float3x3 worldToLightSpaceRotation;

        internal static ReceiverSphereCuller CreateEmptyForTesting(Allocator allocator)
        {
            return new ReceiverSphereCuller()
            {
                splitInfos = new NativeList<SplitInfo>(0, allocator),
                worldToLightSpaceRotation = float3x3.identity,
            };
        }

        internal void Dispose(JobHandle job)
        {
            splitInfos.Dispose(job);
        }

        internal bool UseReceiverPlanes()
        {
            // only use receiver planes if there are no receiver spheres
            // (if spheres are present, then this is directional light cascades and Unity has already added receiver planes to the culling planes)
            return splitInfos.Length == 0;
        }

        internal static ReceiverSphereCuller Create(in BatchCullingContext cc, Allocator allocator)
        {
            int splitCount = cc.cullingSplits.Length;

            // only set up sphere culling when there are multiple splits and all splits have valid spheres
            bool allSpheresValid = (splitCount > 1);
            for (int splitIndex = 0; splitIndex < splitCount; ++splitIndex)
            {
                // ensure that NaN is not considered as valid
                if (!(cc.cullingSplits[splitIndex].sphereRadius > 0.0f))
                    allSpheresValid = false;
            }
            if (!allSpheresValid)
                splitCount = 0;

            float3x3 lightToWorldSpaceRotation = (float3x3)(float4x4)cc.localToWorldMatrix;
            ReceiverSphereCuller result = new ReceiverSphereCuller()
            {
                splitInfos = new NativeList<SplitInfo>(splitCount, allocator),
                worldToLightSpaceRotation = math.transpose(lightToWorldSpaceRotation),
            };
            result.splitInfos.ResizeUninitialized(splitCount);

            for (int splitIndex = 0; splitIndex < splitCount; ++splitIndex)
            {
                var cullingSplit = cc.cullingSplits[splitIndex];

                float4 receiverSphereLightSpace = new float4(
                    math.mul(result.worldToLightSpaceRotation, cullingSplit.sphereCenter),
                    cullingSplit.sphereRadius);

                result.splitInfos[splitIndex] = new SplitInfo()
                {
                    receiverSphereLightSpace = receiverSphereLightSpace,
                    cascadeBlendCullingFactor = cullingSplit.cascadeBlendCullingFactor,
                };
            }

            return result;
        }

        internal static float DistanceUntilCylinderFullyCrossesPlane(
            float3 cylinderCenter,
            float3 cylinderDirection,
            float cylinderRadius,
            Plane plane)
        {
            float cosEpsilon = 0.001f; // clamp the cosine of glancing angles

            // compute the distance until the center intersects the plane
            float cosTheta = math.max(math.abs(math.dot(plane.normal, cylinderDirection)), cosEpsilon);
            float heightAbovePlane = math.dot(plane.normal, cylinderCenter) + plane.distance;
            float centerDistanceToPlane = heightAbovePlane/cosTheta;

            // compute the additional distance until the edge of the cylinder intersects the plane
            float sinTheta = math.sqrt(math.max(1.0f - cosTheta*cosTheta, 0.0f));
            float edgeDistanceToPlane = cylinderRadius*sinTheta/cosTheta;

            return centerDistanceToPlane + edgeDistanceToPlane;
        }

        internal static uint ComputeSplitVisibilityMask(
            NativeArray<Plane> lightFacingFrustumPlanes,
            NativeArray<SplitInfo> splitInfos,
            float3x3 worldToLightSpaceRotation,
            in AABB bounds)
        {
            float3 casterCenterWorldSpace = bounds.center;
            float3 casterCenterLightSpace = math.mul(worldToLightSpaceRotation, bounds.center);
            float casterRadius = math.length(bounds.extents);

            // push the (light-facing) frustum planes back by the caster radius, then intersect with a line through the caster capsule center,
            // to compute the length of the shadow that will cover all possible receivers within the whole frustum (not just this split)
            float3 shadowDirection = math.transpose(worldToLightSpaceRotation).c2;
            float shadowLength = math.INFINITY;
            for (int i = 0; i < lightFacingFrustumPlanes.Length; ++i)
            {
                shadowLength = math.min(shadowLength, DistanceUntilCylinderFullyCrossesPlane(
                    casterCenterWorldSpace,
                    shadowDirection,
                    casterRadius,
                    lightFacingFrustumPlanes[i]));
            }
            shadowLength = math.max(shadowLength, 0.0f);

            uint splitVisibilityMask = 0;
            int splitCount = splitInfos.Length;
            for (int splitIndex = 0; splitIndex < splitCount; ++splitIndex)
            {
                SplitInfo splitInfo = splitInfos[splitIndex];
                float3 receiverCenterLightSpace = splitInfo.receiverSphereLightSpace.xyz;
                float receiverRadius = splitInfo.receiverSphereLightSpace.w;
                float3 receiverToCasterLightSpace = casterCenterLightSpace - receiverCenterLightSpace;

                // compute the light space z coordinate where the caster sphere and receiver sphere just intersect
                float sphereIntersectionMaxDistance = casterRadius + receiverRadius;
                float zSqAtSphereIntersection = math.lengthsq(sphereIntersectionMaxDistance) - math.lengthsq(receiverToCasterLightSpace.xy);

                // if this is negative, the spheres do not overlap as circles in the XY plane, so cull the caster
                if (zSqAtSphereIntersection < 0.0f)
                    continue;

                // if the caster is outside of the receiver sphere in the light direction, it cannot cast a shadow on it, so cull it
                if (receiverToCasterLightSpace.z > 0.0f && math.lengthsq(receiverToCasterLightSpace.z) > zSqAtSphereIntersection)
                    continue;

                // render the caster in this split
                splitVisibilityMask |= 1U << splitIndex;

                // culling assumes that shaders will always sample from the cascade with the lowest index,
                // so if the caster capsule is fully contained within the "core" sphere where only this split index is sampled,
                // then cull this caster from all the larger index splits (break from this loop)
                // (it is sufficient to test that only the capsule start and end spheres are within the "core" receiver sphere)
                float coreRadius = receiverRadius * splitInfo.cascadeBlendCullingFactor;
                float3 receiverToShadowEndLightSpace = receiverToCasterLightSpace + new float3(0.0f, 0.0f, shadowLength);
                float capsuleMaxDistance = coreRadius - casterRadius;
                float capsuleDistanceSq = math.max(math.lengthsq(receiverToCasterLightSpace), math.lengthsq(receiverToShadowEndLightSpace));
                if (capsuleMaxDistance > 0.0f && capsuleDistanceSq < math.lengthsq(capsuleMaxDistance))
                    break;
            }
            return splitVisibilityMask;
        }
    }
}
