using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    [BurstCompile(FloatMode = FloatMode.Default, DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    struct TilingJob : IJobFor
    {
        [ReadOnly]
        public NativeArray<VisibleLight> lights;

        [ReadOnly]
        public NativeArray<VisibleReflectionProbe> reflectionProbes;

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
        public bool isOrthographic;

        InclusiveRange m_TileYRange;
        int m_Offset;
        int m_ViewIndex;
        float2 m_CenterOffset;

        public void Execute(int jobIndex)
        {
            var index = jobIndex % itemsPerTile;
            m_ViewIndex = jobIndex / itemsPerTile;
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
            else { TileReflectionProbe(index); }
        }

        void TileLight(int lightIndex)
        {
            var light = lights[lightIndex];
            if (light.lightType != LightType.Point && light.lightType != LightType.Spot)
            {
                return;
            }

            var lightToWorld = (float4x4)light.localToWorldMatrix;
            var lightPositionVS = math.mul(worldToViews[m_ViewIndex], math.float4(lightToWorld.c3.xyz, 1)).xyz;
            lightPositionVS.z *= -1;
            if (lightPositionVS.z >= near) ExpandY(lightPositionVS);
            var lightDirectionVS = math.normalize(math.mul(worldToViews[m_ViewIndex], math.float4(lightToWorld.c2.xyz, 0)).xyz);
            lightDirectionVS.z *= -1;

            var halfAngle = math.radians(light.spotAngle * 0.5f);
            var range = light.range;
            var rangesq = square(range);
            var cosHalfAngle = math.cos(halfAngle);
            var coneHeight = cosHalfAngle * range;

            // Radius of circle formed by intersection of sphere and near plane.
            // Found using Pythagoras with a right triangle formed by three points:
            // (a) light position
            // (b) light position projected to near plane
            // (c) a point on the near plane at a distance `range` from the light position
            //     (i.e. lies both on the sphere and the near plane)
            // Thus the hypotenuse is formed by (a) and (c) with length `range`, and the known side is formed
            // by (a) and (b) with length equal to the distance between the near plane and the light position.
            // The remaining unknown side is formed by (b) and (c) with length equal to the radius of the circle.
            // m_ClipCircleRadius = sqrt(sq(light.range) - sq(m_Near - m_LightPosition.z));
            var sphereClipRadius = math.sqrt(rangesq - square(near - lightPositionVS.z));

            // Assumes a point on the sphere, i.e. at distance `range` from the light position.
            // If spot light, we check the angle between the direction vector from the light position and the light direction vector.
            // Note that division by range is to normalize the vector, as we know that the resulting vector will have length `range`.
            bool SpherePointIsValid(float3 p) => light.lightType == LightType.Point ||
                math.dot(math.normalize(p - lightPositionVS), lightDirectionVS) >= cosHalfAngle;

            // Project light sphere onto YZ plane, find the horizon points, and re-construct view space position of found points.
            // CalculateSphereYBounds(lightPositionVS, range, near, sphereClipRadius, out var sphereBoundY0, out var sphereBoundY1);
            GetSphereHorizon(lightPositionVS.yz, range, near, sphereClipRadius, out var sphereBoundYZ0, out var sphereBoundYZ1);
            var sphereBoundY0 = math.float3(lightPositionVS.x, sphereBoundYZ0);
            var sphereBoundY1 = math.float3(lightPositionVS.x, sphereBoundYZ1);
            if (SpherePointIsValid(sphereBoundY0)) ExpandY(sphereBoundY0);
            if (SpherePointIsValid(sphereBoundY1)) ExpandY(sphereBoundY1);

            // Project light sphere onto XZ plane, find the horizon points, and re-construct view space position of found points.
            GetSphereHorizon(lightPositionVS.xz, range, near, sphereClipRadius, out var sphereBoundXZ0, out var sphereBoundXZ1);
            var sphereBoundX0 = math.float3(sphereBoundXZ0.x, lightPositionVS.y, sphereBoundXZ0.y);
            var sphereBoundX1 = math.float3(sphereBoundXZ1.x, lightPositionVS.y, sphereBoundXZ1.y);
            if (SpherePointIsValid(sphereBoundX0)) ExpandY(sphereBoundX0);
            if (SpherePointIsValid(sphereBoundX1)) ExpandY(sphereBoundX1);

            if (light.lightType == LightType.Spot)
            {
                // Cone base
                var baseRadius = math.sqrt(range * range - coneHeight * coneHeight);
                var baseCenter = lightPositionVS + lightDirectionVS * coneHeight;

                // Project cone base (a circle) into the YZ plane, find the horizon points, and re-construct view space position of found points.
                // When projecting a circle to a plane, it becomes an ellipse where the major axis is parallel to the line
                // of intersection of the projection plane and the circle plane. We can get this by taking the cross product
                // of the two plane normals, as the line of intersection will have to be a vector in both planes, and thus
                // orthogonal to both normals.
                // If the two plane normals are parallel, the cross product would return 0. In that case, the circle will
                // project to a line segment, so we pick a vector in the plane pointing in the direction we're interested
                // in finding horizon points in.
                var baseUY = math.abs(math.abs(lightDirectionVS.x) - 1) < 1e-6f ? math.float3(0, 1, 0) : math.normalize(math.cross(lightDirectionVS, math.float3(1, 0, 0)));
                var baseVY = math.cross(lightDirectionVS, baseUY);
                GetProjectedCircleHorizon(baseCenter.yz, baseRadius, baseUY.yz, baseVY.yz, out var baseY1UV, out var baseY2UV);
                var baseY1 = baseCenter + baseY1UV.x * baseUY + baseY1UV.y * baseVY;
                var baseY2 = baseCenter + baseY2UV.x * baseUY + baseY2UV.y * baseVY;
                if (baseY1.z >= near) ExpandY(baseY1);
                if (baseY2.z >= near) ExpandY(baseY2);

                // Project cone base into the XZ plane, find the horizon points, and re-construct view space position of found points.
                // See comment for YZ plane for details.
                var baseUX = math.abs(math.abs(lightDirectionVS.y) - 1) < 1e-6f ? math.float3(1, 0, 0) : math.normalize(math.cross(lightDirectionVS, math.float3(0, 1, 0)));
                var baseVX = math.cross(lightDirectionVS, baseUX);
                GetProjectedCircleHorizon(baseCenter.xz, baseRadius, baseUX.xz, baseVX.xz, out var baseX1UV, out var baseX2UV);
                var baseX1 = baseCenter + baseX1UV.x * baseUX + baseX1UV.y * baseVX;
                var baseX2 = baseCenter + baseX2UV.x * baseUX + baseX2UV.y * baseVX;
                if (baseX1.z >= near) ExpandY(baseX1);
                if (baseX2.z >= near) ExpandY(baseX2);

                // Handle base circle clipping by intersecting it with the near-plane if needed.
                if (GetCircleClipPoints(baseCenter, lightDirectionVS, baseRadius, near, out var baseClip0, out var baseClip1))
                {
                    ExpandY(baseClip0);
                    ExpandY(baseClip1);
                }

                bool ConicPointIsValid(float3 p) =>
                    math.dot(math.normalize(p - lightPositionVS), lightDirectionVS) >= 0 &&
                    math.dot(p - lightPositionVS, lightDirectionVS) <= coneHeight;

                // Calculate Z bounds of cone and check if it's overlapping with the near plane.
                // From https://www.iquilezles.org/www/articles/diskbbox/diskbbox.htm
                var baseExtentZ = baseRadius * math.sqrt(1.0f - square(lightDirectionVS.z));
                var coneIsClipping = near >= math.min(baseCenter.z - baseExtentZ, lightPositionVS.z) && near <= math.max(baseCenter.z + baseExtentZ, lightPositionVS.z);

                var coneU = math.cross(lightDirectionVS, lightPositionVS);
                // The cross product will be the 0-vector if the light-direction and camera-to-light-position vectors are parallel.
                // In that case, {1, 0, 0} is orthogonal to the light direction and we use that instead.
                coneU = math.csum(coneU) != 0f ? math.normalize(coneU) : math.float3(1, 0, 0);
                var coneV = math.cross(lightDirectionVS, coneU);

                if (coneIsClipping)
                {
                    var r = baseRadius / coneHeight;

                    // Find the Y bounds of the near-plane cone intersection, i.e. where y' = 0
                    var thetaY = FindNearConicTangentTheta(lightPositionVS.yz, lightDirectionVS.yz, r, coneU.yz, coneV.yz);
                    var p0Y = EvaluateNearConic(near, lightPositionVS, lightDirectionVS, r, coneU, coneV, thetaY.x);
                    var p1Y = EvaluateNearConic(near, lightPositionVS, lightDirectionVS, r, coneU, coneV, thetaY.y);
                    if (ConicPointIsValid(p0Y)) ExpandY(p0Y);
                    if (ConicPointIsValid(p1Y)) ExpandY(p1Y);

                    // Find the X bounds of the near-plane cone intersection, i.e. where x' = 0
                    var thetaX = FindNearConicTangentTheta(lightPositionVS.xz, lightDirectionVS.xz, r, coneU.xz, coneV.xz);
                    var p0X = EvaluateNearConic(near, lightPositionVS, lightDirectionVS, r, coneU, coneV, thetaX.x);
                    var p1X = EvaluateNearConic(near, lightPositionVS, lightDirectionVS, r, coneU, coneV, thetaX.y);
                    if (ConicPointIsValid(p0X)) ExpandY(p0X);
                    if (ConicPointIsValid(p1X)) ExpandY(p1X);
                }

                // Calculate the lines making up the sides of the cone as seen from the camera. `l1` and `l2` form lines
                // from the light position.
                GetConeSideTangentPoints(lightPositionVS, lightDirectionVS, cosHalfAngle, baseRadius, coneHeight, range, coneU, coneV, out var l1, out var l2);

                {
                    var planeNormal = math.float3(0, 1, viewPlaneBottoms[m_ViewIndex]);
                    var l1t = math.dot(-lightPositionVS, planeNormal) / math.dot(l1, planeNormal);
                    var l1x = lightPositionVS + l1 * l1t;
                    if (l1t >= 0 && l1t <= 1 && l1x.z >= near) ExpandY(l1x);
                }
                {
                    var planeNormal = math.float3(0, 1, viewPlaneTops[m_ViewIndex]);
                    var l1t = math.dot(-lightPositionVS, planeNormal) / math.dot(l1, planeNormal);
                    var l1x = lightPositionVS + l1 * l1t;
                    if (l1t >= 0 && l1t <= 1 && l1x.z >= near) ExpandY(l1x);
                }

                m_TileYRange.Clamp(0, (short)(tileCount.y - 1));

                // Calculate tile plane ranges for cone.
                for (var planeIndex = m_TileYRange.start + 1; planeIndex <= m_TileYRange.end; planeIndex++)
                {
                    var planeRange = InclusiveRange.empty;

                    // Y-position on the view plane (Z=1)
                    var planeY = math.lerp(viewPlaneBottoms[m_ViewIndex], viewPlaneTops[m_ViewIndex], planeIndex * tileScaleInv.y);

                    var planeNormal = math.float3(0, 1, -planeY);

                    // Intersect lines with y-plane and clip if needed.
                    var l1t = math.dot(-lightPositionVS, planeNormal) / math.dot(l1, planeNormal);
                    var l1x = lightPositionVS + l1 * l1t;
                    if (l1t >= 0 && l1t <= 1 && l1x.z >= near) planeRange.Expand((short)ViewToTileSpace(l1x).x);

                    var l2t = math.dot(-lightPositionVS, planeNormal) / math.dot(l2, planeNormal);
                    var l2x = lightPositionVS + l2 * l2t;
                    if (l2t >= 0 && l2t <= 1 && l2x.z >= near) planeRange.Expand((short)ViewToTileSpace(l2x).x);

                    if (IntersectCircleYPlane(planeY, baseCenter, lightDirectionVS, baseUY, baseVY, baseRadius, out var circleTile0, out var circleTile1))
                    {
                        if (circleTile0.z >= near) planeRange.Expand((short)ViewToTileSpace(circleTile0).x);
                        if (circleTile1.z >= near) planeRange.Expand((short)ViewToTileSpace(circleTile1).x);
                    }

                    if (coneIsClipping)
                    {
                        var y = planeY * near;
                        var r = baseRadius / coneHeight;
                        var theta = FindNearConicYTheta(near, lightPositionVS, lightDirectionVS, r, coneU, coneV, y);
                        var p0 = math.float3(EvaluateNearConic(near, lightPositionVS, lightDirectionVS, r, coneU, coneV, theta.x).x, y, near);
                        var p1 = math.float3(EvaluateNearConic(near, lightPositionVS, lightDirectionVS, r, coneU, coneV, theta.y).x, y, near);
                        if (ConicPointIsValid(p0)) planeRange.Expand((short)ViewToTileSpace(p0).x);
                        if (ConicPointIsValid(p1)) planeRange.Expand((short)ViewToTileSpace(p1).x);
                    }

                    // Only consider ranges that intersect the tiling extents.
                    // The logic in the below 'if' statement is a simplification of:
                    // !((planeRange.start < 0) && (planeRange.end < 0)) && !((planeRange.start > tileCount.x - 1) && (planeRange.end > tileCount.x - 1))
                    if (((planeRange.start >= 0) || (planeRange.end >= 0)) && ((planeRange.start <= tileCount.x - 1) || (planeRange.end <= tileCount.x - 1)))
                    {
                        // Write to tile ranges above and below the plane. Note that at `m_Offset` we store Y-range.
                        var tileIndex = m_Offset + 1 + planeIndex;
                        planeRange.Clamp(0, (short)(tileCount.x - 1));
                        tileRanges[tileIndex] = InclusiveRange.Merge(tileRanges[tileIndex], planeRange);
                        tileRanges[tileIndex - 1] = InclusiveRange.Merge(tileRanges[tileIndex - 1], planeRange);
                    }
                }
            }

            m_TileYRange.Clamp(0, (short)(tileCount.y - 1));

            // Calculate tile plane ranges for sphere.
            for (var planeIndex = m_TileYRange.start + 1; planeIndex <= m_TileYRange.end; planeIndex++)
            {
                var planeRange = InclusiveRange.empty;

                var planeY = math.lerp(viewPlaneBottoms[m_ViewIndex], viewPlaneTops[m_ViewIndex], planeIndex * tileScaleInv.y);
                GetSphereYPlaneHorizon(lightPositionVS, range, near, sphereClipRadius, planeY, out var sphereTile0, out var sphereTile1);
                if (SpherePointIsValid(sphereTile0)) planeRange.Expand((short)math.clamp(ViewToTileSpace(sphereTile0).x, 0, tileCount.x - 1));
                if (SpherePointIsValid(sphereTile1)) planeRange.Expand((short)math.clamp(ViewToTileSpace(sphereTile1).x, 0, tileCount.x - 1));

                var tileIndex = m_Offset + 1 + planeIndex;
                tileRanges[tileIndex] = InclusiveRange.Merge(tileRanges[tileIndex], planeRange);
                tileRanges[tileIndex - 1] = InclusiveRange.Merge(tileRanges[tileIndex - 1], planeRange);
            }

            tileRanges[m_Offset] = m_TileYRange;
        }

        void TileLightOrthographic(int lightIndex)
        {
            var light = lights[lightIndex];
            var lightToWorld = (float4x4)light.localToWorldMatrix;
            var lightPosVS = math.mul(worldToViews[m_ViewIndex], math.float4(lightToWorld.c3.xyz, 1)).xyz;
            lightPosVS.z *= -1;
            ExpandOrthographic(lightPosVS);
            var lightDirVS = math.mul(worldToViews[m_ViewIndex], math.float4(lightToWorld.c2.xyz, 0)).xyz;
            lightDirVS.z *= -1;
            lightDirVS = math.normalize(lightDirVS);

            var halfAngle = math.radians(light.spotAngle * 0.5f);
            var range = light.range;
            var rangeSq = square(range);
            var cosHalfAngle = math.cos(halfAngle);
            var coneHeight = cosHalfAngle * range;
            var coneHeightSq = square(coneHeight);
            var coneHeightInv = 1f / coneHeight;
            var coneHeightInvSq = square(coneHeightInv);

            bool SpherePointIsValid(float3 p) => light.lightType == LightType.Point ||
                math.dot(math.normalize(p - lightPosVS), lightDirVS) >= cosHalfAngle;

            var sphereBoundY0 = lightPosVS - math.float3(0, range, 0);
            var sphereBoundY1 = lightPosVS + math.float3(0, range, 0);
            var sphereBoundX0 = lightPosVS - math.float3(range, 0, 0);
            var sphereBoundX1 = lightPosVS + math.float3(range, 0, 0);

            if (SpherePointIsValid(sphereBoundY0)) ExpandOrthographic(sphereBoundY0);
            if (SpherePointIsValid(sphereBoundY1)) ExpandOrthographic(sphereBoundY1);
            if (SpherePointIsValid(sphereBoundX0)) ExpandOrthographic(sphereBoundX0);
            if (SpherePointIsValid(sphereBoundX1)) ExpandOrthographic(sphereBoundX1);

            var circleCenter = lightPosVS + lightDirVS * coneHeight;
            var circleRadius = math.sqrt(rangeSq - coneHeightSq);
            var circleRadiusSq = square(circleRadius);
            var circleUp = math.normalize(math.float3(0, 1, 0) - lightDirVS * lightDirVS.y);
            var circleRight = math.normalize(math.float3(1, 0, 0) - lightDirVS * lightDirVS.x);
            var circleBoundY0 = circleCenter - circleUp * circleRadius;
            var circleBoundY1 = circleCenter + circleUp * circleRadius;

            if (light.lightType == LightType.Spot)
            {
                var circleBoundX0 = circleCenter - circleRight * circleRadius;
                var circleBoundX1 = circleCenter + circleRight * circleRadius;
                ExpandOrthographic(circleBoundY0);
                ExpandOrthographic(circleBoundY1);
                ExpandOrthographic(circleBoundX0);
                ExpandOrthographic(circleBoundX1);
            }

            m_TileYRange.Clamp(0, (short)(tileCount.y - 1));

            // Find two lines in screen-space for the cone if the light is a spot.
            float coneDir0X = 0, coneDir0YInv = 0, coneDir1X = 0, coneDir1YInv = 0;
            if (light.lightType == LightType.Spot)
            {
                // Distance from light position to and radius of sphere fitted to the end of the cone.
                var sphereDistance = coneHeight + circleRadiusSq * coneHeightInv;
                var sphereRadius = math.sqrt(square(circleRadiusSq) * coneHeightInvSq + circleRadiusSq);
                var directionXYSqInv = math.rcp(math.lengthsq(lightDirVS.xy));
                var polarIntersection = -circleRadiusSq * coneHeightInv * directionXYSqInv * lightDirVS.xy;
                var polarDir = math.sqrt((square(sphereRadius) - math.lengthsq(polarIntersection)) * directionXYSqInv) * math.float2(lightDirVS.y, -lightDirVS.x);
                var conePBase = lightPosVS.xy + sphereDistance * lightDirVS.xy + polarIntersection;
                var coneP0 = conePBase - polarDir;
                var coneP1 = conePBase + polarDir;

                coneDir0X = coneP0.x - lightPosVS.x;
                coneDir0YInv = math.rcp(coneP0.y - lightPosVS.y);
                coneDir1X = coneP1.x - lightPosVS.x;
                coneDir1YInv = math.rcp(coneP1.y - lightPosVS.y);
            }

            // Tile plane ranges
            for (var planeIndex = m_TileYRange.start + 1; planeIndex <= m_TileYRange.end; planeIndex++)
            {
                var planeRange = InclusiveRange.empty;

                // Sphere
                var planeY = math.lerp(viewPlaneBottoms[m_ViewIndex], viewPlaneTops[m_ViewIndex], planeIndex * tileScaleInv.y);
                var sphereX = math.sqrt(rangeSq - square(planeY - lightPosVS.y));
                var sphereX0 = math.float3(lightPosVS.x - sphereX, planeY, lightPosVS.z);
                var sphereX1 = math.float3(lightPosVS.x + sphereX, planeY, lightPosVS.z);
                if (SpherePointIsValid(sphereX0)) { ExpandRangeOrthographic(ref planeRange, sphereX0.x); }
                if (SpherePointIsValid(sphereX1)) { ExpandRangeOrthographic(ref planeRange, sphereX1.x); }

                if (light.lightType == LightType.Spot)
                {
                    // Circle
                    if (planeY >= circleBoundY0.y && planeY <= circleBoundY1.y)
                    {
                        var intersectionDistance = (planeY - circleCenter.y) / circleUp.y;
                        var closestPointX = circleCenter.x + intersectionDistance * circleUp.x;
                        var intersectionDirX = -lightDirVS.z / math.length(math.float3(-lightDirVS.z, 0, lightDirVS.x));
                        var sideDistance = math.sqrt(square(circleRadius) - square(intersectionDistance));
                        var circleX0 = closestPointX - sideDistance * intersectionDirX;
                        var circleX1 = closestPointX + sideDistance * intersectionDirX;
                        ExpandRangeOrthographic(ref planeRange, circleX0);
                        ExpandRangeOrthographic(ref planeRange, circleX1);
                    }

                    // Cone
                    var deltaY = planeY - lightPosVS.y;
                    var coneT0 = deltaY * coneDir0YInv;
                    var coneT1 = deltaY * coneDir1YInv;
                    if (coneT0 >= 0 && coneT0 <= 1) { ExpandRangeOrthographic(ref planeRange, lightPosVS.x + coneT0 * coneDir0X); }
                    if (coneT1 >= 0 && coneT1 <= 1) { ExpandRangeOrthographic(ref planeRange, lightPosVS.x + coneT1 * coneDir1X); }
                }

                var tileIndex = m_Offset + 1 + planeIndex;
                tileRanges[tileIndex] = InclusiveRange.Merge(tileRanges[tileIndex], planeRange);
                tileRanges[tileIndex - 1] = InclusiveRange.Merge(tileRanges[tileIndex - 1], planeRange);
            }

            tileRanges[m_Offset] = m_TileYRange;
        }

        static readonly float3[] k_CubePoints =
        {
            new(-1, -1, -1),
            new(-1, -1, +1),
            new(-1, +1, -1),
            new(-1, +1, +1),
            new(+1, -1, -1),
            new(+1, -1, +1),
            new(+1, +1, -1),
            new(+1, +1, +1),
        };

        // Each item represents 3 lines, with x being the start index and yzw the end indices.
        static readonly int4[] k_CubeLineIndices =
        {
            // (-1, -1, -1) -> {(+1, -1, -1), (-1, +1, -1), (-1, -1, +1)}
            new(0, 4, 2, 1),

            // (-1, +1, +1) -> {(+1, +1, +1), (-1, -1, +1), (-1, +1, -1)}
            new(3, 7, 1, 2),

            // (+1, -1, +1) -> {(-1, -1, +1), (+1, +1, +1), (+1, -1, -1)}
            new(5, 1, 7, 4),

            // (+1, +1, -1) -> {(-1, +1, -1), (+1, -1, -1), (+1, +1, +1)}
            new(6, 2, 4, 7),
        };

        void TileReflectionProbe(int index)
        {
            // The algorithm used here works by clipping all the lines of the cube against the near-plane, and then
            // projects the resulting points to the view plane. These points are then used to construct a 2D convex
            // hull, which we can iterate linearly to get the lines on screen making up the cube.

            var reflectionProbe = reflectionProbes[index - lights.Length];
            var centerWS = (float3)reflectionProbe.bounds.center;
            var extentsWS = (float3)reflectionProbe.bounds.extents;

            // The vertices of the cube in view space.
            var points = new NativeArray<float3>(k_CubePoints.Length, Allocator.Temp);
            // This is initially filled with just the cube vertices that lie in front of the near plane.
            var clippedPoints = new NativeArray<float2>(k_CubePoints.Length + k_CubeLineIndices.Length * 3, Allocator.Temp);
            var clippedPointsCount = 0;
            var leftmostIndex = 0;
            for (var i = 0; i < k_CubePoints.Length; i++)
            {
                var point = math.mul(worldToViews[m_ViewIndex], math.float4(centerWS + extentsWS * k_CubePoints[i], 1)).xyz;
                point.z *= -1;
                points[i] = point;
                if (point.z >= near)
                {
                    var clippedPoint = isOrthographic ? point.xy : point.xy/point.z;
                    var clippedIndex = clippedPointsCount++;
                    clippedPoints[clippedIndex] = clippedPoint;
                    if (clippedPoint.x < clippedPoints[leftmostIndex].x) leftmostIndex = clippedIndex;
                }
            }

            // Clip the cube's line segments with the near plane, and add the new vertices to clippedPoints. Only lines
            // that are clipped will generate new vertices.
            for (var i = 0; i < k_CubeLineIndices.Length; i++)
            {
                var indices = k_CubeLineIndices[i];
                var p0 = points[indices.x];
                for (var j = 0; j < 3; j++)
                {
                    var p1 = points[indices[j+1]];
                    // The entire line is in front of the near plane.
                    if (p0.z < near && p1.z < near) continue;
                    // Check whether the line needs clipping.
                    if (p0.z < near || p1.z < near)
                    {
                        var d = (near - p0.z) / (p1.z - p0.z);
                        var p = math.lerp(p0, p1, d);
                        var clippedPoint = isOrthographic ? p.xy : p.xy/p.z;
                        var clippedIndex = clippedPointsCount++;
                        clippedPoints[clippedIndex] = clippedPoint;
                        if (clippedPoint.x < clippedPoints[leftmostIndex].x) leftmostIndex = clippedIndex;
                    }
                }
            }

            // Construct the convex hull. It is formed by the line loop consisting of the points in the array.
            var hullPoints = new NativeArray<float2>(clippedPointsCount, Allocator.Temp);
            var hullPointsCount = 0;

            if (clippedPointsCount > 0)
            {
                // Start with the leftmost point, as that is guaranteed to be on the hull.
                var hullPointIndex = leftmostIndex;

                // Find the remaining hull points until we end up back at the leftmost point.
                do
                {
                    var hullPoint = clippedPoints[hullPointIndex];
                    ExpandY(math.float3(hullPoint, 1));
                    hullPoints[hullPointsCount++] = hullPoint;

                    // Find the endpoint resulting in the leftmost turning line. This line will be a part of the hull.
                    var endpointIndex = 0;
                    var endpointLine = clippedPoints[endpointIndex] - hullPoint;
                    for (var i = 0; i < clippedPointsCount; i++)
                    {
                        var candidateLine = clippedPoints[i] - hullPoint;
                        var det = math.determinant(math.float2x2(endpointLine, candidateLine));

                        // Check if point i lies on the left side of the line to the current endpoint, or if it lies
                        // collinear to the current endpoint but farther away.
                        if (endpointIndex == hullPointIndex || det > 0 || (det == 0.0f && math.lengthsq(candidateLine) > math.lengthsq(endpointLine)))
                        {
                            endpointIndex = i;
                            endpointLine = candidateLine;
                        }
                    }

                    hullPointIndex = endpointIndex;
                } while (hullPointIndex != leftmostIndex && hullPointsCount < clippedPointsCount);

                m_TileYRange.Clamp(0, (short)(tileCount.y - 1));

                // Calculate tile plane ranges for sphere.
                for (var planeIndex = m_TileYRange.start + 1; planeIndex <= m_TileYRange.end; planeIndex++)
                {
                    var planeRange = InclusiveRange.empty;

                    var planeY = math.lerp(viewPlaneBottoms[m_ViewIndex], viewPlaneTops[m_ViewIndex], planeIndex * tileScaleInv.y);

                    for (var i = 0; i < hullPointsCount; i++)
                    {
                        var hp0 = hullPoints[i];
                        var hp1 = hullPoints[(i + 1) % hullPointsCount];

                        // planeY = hp0 + t * (hp1 - hp0) => planeY - hp0 = t * (hp1 - hp0) => (planeY - hp0) / (hp1 - hp0) = t
                        var t = (planeY - hp0.y) / (hp1.y - hp0.y);
                        if (t < 0 || t > 1) continue;
                        var x = math.lerp(hp0.x, hp1.x, t);

                        var p = math.float3(x, planeY, 1);
                        var pTS = isOrthographic ? ViewToTileSpaceOrthographic(p) : ViewToTileSpace(p);
                        planeRange.Expand((short)math.clamp(pTS.x, 0, tileCount.x - 1));
                    }

                    var tileIndex = m_Offset + 1 + planeIndex;
                    tileRanges[tileIndex] = InclusiveRange.Merge(tileRanges[tileIndex], planeRange);
                    tileRanges[tileIndex - 1] = InclusiveRange.Merge(tileRanges[tileIndex - 1], planeRange);
                }

                tileRanges[m_Offset] = m_TileYRange;
            }

            hullPoints.Dispose();
            clippedPoints.Dispose();
            points.Dispose();
        }

        /// <summary>
        /// Project onto Z=1, scale and offset into [0, tileCount]
        /// </summary>
        float2 ViewToTileSpace(float3 positionVS)
        {
            return (positionVS.xy / positionVS.z * viewToViewportScaleBiases[m_ViewIndex].xy + viewToViewportScaleBiases[m_ViewIndex].zw) * tileScale;
        }

        /// <summary>
        /// Project onto Z=1, scale and offset into [0, tileCount]
        /// </summary>
        float2 ViewToTileSpaceOrthographic(float3 positionVS)
        {
            return (positionVS.xy * viewToViewportScaleBiases[m_ViewIndex].xy + viewToViewportScaleBiases[m_ViewIndex].zw) * tileScale;
        }

        /// <summary>
        /// Expands the tile Y range and the X range in the row containing the position.
        /// </summary>
        void ExpandY(float3 positionVS)
        {
            // var positionTS = math.clamp(ViewToTileSpace(positionVS), 0, tileCount - 1);
            var positionTS = ViewToTileSpace(positionVS);
            var tileY = (int)positionTS.y;
            var tileX = (int)positionTS.x;
            m_TileYRange.Expand((short)math.clamp(tileY, 0, tileCount.y - 1));
            if (tileY >= 0 && tileY < tileCount.y && tileX >= 0 && tileX < tileCount.x)
            {
                var rowXRange = tileRanges[m_Offset + 1 + tileY];
                rowXRange.Expand((short)tileX);
                tileRanges[m_Offset + 1 + tileY] = rowXRange;
            }
        }

        /// <summary>
        /// Expands the tile Y range and the X range in the row containing the position.
        /// </summary>
        void ExpandOrthographic(float3 positionVS)
        {
            // var positionTS = math.clamp(ViewToTileSpace(positionVS), 0, tileCount - 1);
            var positionTS = ViewToTileSpaceOrthographic(positionVS);
            var tileY = (int)positionTS.y;
            var tileX = (int)positionTS.x;
            m_TileYRange.Expand((short)math.clamp(tileY, 0, tileCount.y - 1));
            if (tileY >= 0 && tileY < tileCount.y && tileX >= 0 && tileX < tileCount.x)
            {
                var rowXRange = tileRanges[m_Offset + 1 + tileY];
                rowXRange.Expand((short)tileX);
                tileRanges[m_Offset + 1 + tileY] = rowXRange;
            }
        }

        void ExpandRangeOrthographic(ref InclusiveRange range, float xVS)
        {
            range.Expand((short)math.clamp(ViewToTileSpaceOrthographic(xVS).x, 0, tileCount.x - 1));
        }

        static float square(float x) => x * x;

        /// <summary>
        /// Finds the two horizon points seen from (0, 0) of a sphere projected onto either XZ or YZ. Takes clipping into account.
        /// </summary>
        static void GetSphereHorizon(float2 center, float radius, float near, float clipRadius, out float2 p0, out float2 p1)
        {
            var direction = math.normalize(center);

            // Distance from camera to center of sphere
            var d = math.length(center);

            // Distance from camera to sphere horizon edge
            var l = math.sqrt(d * d - radius * radius);

            // Height of circle horizon
            var h = l * radius / d;

            // Center of circle horizon
            var c = direction * (l * h / radius);

            p0 = math.float2(float.MinValue, 1f);
            p1 = math.float2(float.MaxValue, 1f);

            // Handle clipping
            if (center.y - radius < near)
            {
                p0 = math.float2(center.x + clipRadius, near);
                p1 = math.float2(center.x - clipRadius, near);
            }

            // Circle horizon points
            var c0 = c + math.float2(-direction.y, direction.x) * h;
            if (square(d) >= square(radius) && c0.y >= near)
            {
                if (c0.x > p0.x) { p0 = c0; }
                if (c0.x < p1.x) { p1 = c0; }
            }

            var c1 = c + math.float2(direction.y, -direction.x) * h;
            if (square(d) >= square(radius) && c1.y >= near)
            {
                if (c1.x > p0.x) { p0 = c1; }
                if (c1.x < p1.x) { p1 = c1; }
            }
        }

        static void GetSphereYPlaneHorizon(float3 center, float sphereRadius, float near, float clipRadius, float y, out float3 left, out float3 right)
        {
            // Note: The y-plane is the plane that is determined by `y` in that it contains the vector (1, 0, 0)
            // and goes through the points (0, y, 1) and (0, 0, 0). This would become a straight line in screen-space, and so it
            // represents the boundary between two rows of tiles.

            // Near-plane clipping - will get overwritten if no clipping is needed.
            // `y` is given for the view plane (Z=1), scale it so that it is on the near plane instead.
            var yNear = y * near;
            // Find the two points of intersection between the clip circle of the sphere and the y-plane.
            // Found using Pythagoras with a right triangle formed by three points:
            // (a) center of the clip circle
            // (b) a point straight above the clip circle center on the y-plane
            // (c) a point that is both on the circle and the y-plane (this is the point we want to find in the end)
            // The hypotenuse is formed by (a) and (c) with length equal to the clip radius. The known side is
            // formed by (a) and (b) and is simply the distance from the center to the y-plane along the y-axis.
            // The remaining side gives us the x-displacement needed to find the intersection points.
            var clipHalfWidth = math.sqrt(square(clipRadius) - square(yNear - center.y));
            left = math.float3(center.x - clipHalfWidth, yNear, near);
            right = math.float3(center.x + clipHalfWidth, yNear, near);

            // Basis vectors in the y-plane for being able to parameterize the plane.
            var planeU = math.normalize(math.float3(0, y, 1));
            var planeV = math.float3(1, 0, 0);

            // Calculate the normal of the y-plane. Found from: (0, y, 1) × (1, 0, 0) = (0, 1, -y)
            // This is used to represent the plane along with the origin, which is just 0 and thus doesn't show up
            // in the calculations.
            var normal = math.normalize(math.float3(0, 1, -y));

            // We want to first find the circle from the intersection of the y-plane and the sphere.

            // The shortest distance from the sphere center and the y-plane. The sign determines which side of the plane
            // the center is on.
            var signedDistance = math.dot(normal, center);

            // Unsigned shortest distance from the sphere center to the plane.
            var distanceToPlane = math.abs(signedDistance);

            // The center of the intersection circle in the y-plane, which is the point on the plane closest to the
            // sphere center. I.e. this is at `distanceToPlane` from the center.
            var centerOnPlane = math.float2(math.dot(center, planeU), math.dot(center, planeV));

            // Distance from origin to the circle center.
            var distanceInPlane = math.length(centerOnPlane);

            // Direction from origin to the circle center.
            var directionPS = centerOnPlane / distanceInPlane;

            // Calculate the radius of the circle using Pythagoras. We know that any point on the circle is a point on
            // the sphere. Thus we can construct a triangle with the sphere center, circle center, and a point on the
            // circle. We then want to find its distance to the circle center, as that will be equal to the radius. As
            // the point is on the sphere, it must be `sphereRadius` from the sphere center, forming the hypotenuse. The
            // other side is between the sphere and circle centers, which we've already calculated to be
            // `distanceToPlane`.
            var circleRadius = math.sqrt(square(sphereRadius) - square(distanceToPlane));

            // Now that we have the circle, we can find the horizon points. Since we've parametrized the plane, we can
            // just do this in 2D.

            // Any of these conditions will yield NaN due to negative square roots. They are signs that clipping is needed,
            // so we fallback on the already calculated values in that case.
            if (square(distanceToPlane) <= square(sphereRadius) && square(circleRadius) <= square(distanceInPlane))
            {
                // Distance from origin to circle horizon edge.
                var l = math.sqrt(square(distanceInPlane) - square(circleRadius));

                // Height of circle horizon.
                var h = l * circleRadius / distanceInPlane;

                // Center of circle horizon.
                var c = directionPS * (l * h / circleRadius);

                // Calculate the horizon points in the plane.
                var leftOnPlane = c + math.float2(directionPS.y, -directionPS.x) * h;
                var rightOnPlane = c + math.float2(-directionPS.y, directionPS.x) * h;

                // Transform horizon points to view space and use if not clipped.
                var leftCandidate = leftOnPlane.x * planeU + leftOnPlane.y * planeV;
                if (leftCandidate.z >= near) left = leftCandidate;

                var rightCandidate = rightOnPlane.x * planeU + rightOnPlane.y * planeV;
                if (rightCandidate.z >= near) right = rightCandidate;
            }
        }

        /// <summary>
        /// Finds the two points of intersection of a 3D circle and the near plane.
        /// </summary>
        static bool GetCircleClipPoints(float3 circleCenter, float3 circleNormal, float circleRadius, float near, out float3 p0, out float3 p1)
        {
            // The intersection of two planes is a line where the direction is the cross product of the two plane normals.
            // In this case, it is the plane containing the circle, and the near plane.
            var lineDirection = math.normalize(math.cross(circleNormal, math.float3(0, 0, 1)));

            // Find a direction on the circle plane towards the nearest point on the intersection line.
            // It has to be perpendicular to the circle normal to be in the circle plane. The direction to the closest
            // point on a line is perpendicular to the line direction. Thus this is given by the cross product of the
            // line direction and the circle normal, as this gives us a vector that is perpendicular to both of those.
            var nearestDirection = math.cross(lineDirection, circleNormal);

            // Distance from circle center to the intersection line along `nearestDirection`.
            // This is done using a ray-plane intersection, where the plane is the near plane.
            // ({0, 0, near} - circleCenter) . {0, 0, 1} / (nearestDirection . {0, 0, 1})
            var distance = (near - circleCenter.z) / nearestDirection.z;

            // The point on the line nearest to the circle center when traveling only in the circle plane.
            var nearestPoint = circleCenter + nearestDirection * distance;

            // Any line through a circle makes a chord where the endpoints are the intersections with the circle.
            // The half length of the circle chord can be found by constructing a right triangle from three points:
            // (a) The circle center.
            // (b) The nearest point.
            // (c) A point that is on circle and the intersection line.
            // The hypotenuse is formed by (a) and (c) and will have length `circleRadius` as it is on the circle.
            // The known side if formed by (a) and (b), which we have already calculated the distance of in `distance`.
            // The unknown side formed by (b) and (c) is then found using Pythagoras.
            var chordHalfLength = math.sqrt(square(circleRadius) - square(distance));
            p0 = nearestPoint + lineDirection * chordHalfLength;
            p1 = nearestPoint - lineDirection * chordHalfLength;

            return math.abs(distance) <= circleRadius;
        }

        static (float, float) IntersectEllipseLine(float a, float b, float3 line)
        {
            // The line is represented as a homogenous 2D line {u, v, w} such that ux + vy + w = 0.
            // The ellipse is represented by the implicit equation x^2/a^2 + y^2/b^2 = 1.
            // We solve the line equation for y:  y = (ux + w) / v
            // We then substitute this into the ellipse equation and expand and re-arrange a bit:
            //   x^2/a^2 + ((ux + w) / v)^2/b^2 = 1 =>
            //   x^2/a^2 + ((ux + w)^2 / v^2)/b^2 = 1 =>
            //   x^2/a^2 + (ux + w)^2/(v^2 b^2) = 1 =>
            //   x^2/a^2 + (u^2 x^2 + w^2 + 2 u x w)/(v^2 b^2) = 1 =>
            //   x^2/a^2 + x^2 u^2 / (v^2 b^2) + w^2/(v^2 b^2) + x 2 u w / (v^2 b^2) = 1 =>
            //   x^2 (1/a^2 + u^2 / (v^2 b^2)) + x 2 u w / (v^2 b^2) + w^2 / (v^2 b^2) - 1 = 0
            // We now have a quadratic equation with:
            //   a = 1/a^2 + u^2 / (v^2 b^2)
            //   b = 2 u w / (v^2 b^2)
            //   c = w^2 / (v^2 b^2) - 1
            var div = math.rcp(square(line.y) * square(b));
            var qa = 1f / square(a) + square(line.x) * div;
            var qb = 2f * line.x * line.z * div;
            var qc = square(line.z) * div - 1f;
            var sqrtD = math.sqrt(qb * qb - 4f * qa * qc);
            var x1 = (-qb + sqrtD) / (2f * qa);
            var x2 = (-qb - sqrtD) / (2f * qa);
            return (x1, x2);
        }

        /// <summary>
        /// Calculates the horizon of a circle orthogonally projected to a plane as seen from the origin on the plane.
        /// </summary>
        /// <param name="center">The center of the circle projected onto the plane.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="U">The major axis of the ellipse formed by the projection of the circle.</param>
        /// <param name="V">The minor axis of the ellipse formed by the projection of the circle.</param>
        /// <param name="uv1">The first horizon point expressed as factors of <paramref name="U"/> and <paramref name="V"/>.</param>
        /// <param name="uv2">The second horizon point expressed as factors of <paramref name="U"/> and <paramref name="V"/>.</param>
        static void GetProjectedCircleHorizon(float2 center, float radius, float2 U, float2 V, out float2 uv1, out float2 uv2)
        {
            // U is assumed to be constructed such that it is never 0, but V can be if the circle projects to a line segment.
            // In that case, the solution can be trivially found using U only.
            var vl = math.length(V);
            if (vl < 1e-6f)
            {
                uv1 = math.float2(radius, 0);
                uv2 = math.float2(-radius, 0);
            }
            else
            {
                var ul = math.length(U);
                var ulinv = math.rcp(ul);
                var vlinv = math.rcp(vl);

                // Normalize U and V in the plane.
                var u = U * ulinv;
                var v = V * vlinv;

                // Major and minor axis of the ellipse.
                var a = ul * radius;
                var b = vl * radius;

                // Project the camera position into a 2D coordinate system with the circle at (0, 0) and
                // the ellipse major and minor axes as the coordinate system axes. This allows us to use the standard
                // form of the ellipse equation, greatly simplifying the calculations.
                var cameraUV = math.float2(math.dot(-center, u), math.dot(-center, v));

                // Find the polar line of the camera position in the normalized UV coordinate system.
                var polar = math.float3(cameraUV.x / square(a), cameraUV.y / square(b), -1);
                var (t1, t2) = IntersectEllipseLine(a, b, polar);

                // Find Y by putting polar into line equation and solving. Denormalize by dividing by U and V lengths.
                uv1 = math.float2(t1 * ulinv, (-polar.x / polar.y * t1 - polar.z / polar.y) * vlinv);
                uv2 = math.float2(t2 * ulinv, (-polar.x / polar.y * t2 - polar.z / polar.y) * vlinv);
            }
        }

        static bool IntersectCircleYPlane(
            float y, float3 circleCenter, float3 circleNormal, float3 circleU, float3 circleV, float circleRadius,
            out float3 p1, out float3 p2)
        {
            p1 = p2 = 0;

            // Intersecting a circle with a plane yields 2 points, or the whole circle if the plane and the plane of the
            // circle are the same, or nothing if the planes are parallel but offset. We're only interested in the first
            // case. Our other tests will catch the other cases.

            // The two points will be on the line of intersection of the two planes. Thus we first have to find that line.

            // Shoot 2 rays along the y-plane and intersect the circle plane. We then transform them into the circle
            // plane, so that we can work in 2D.
            var CdotN = math.dot(circleCenter, circleNormal);
            var h1v = math.float3(1, y, 1) * CdotN / math.dot(math.float3(1, y, 1), circleNormal) - circleCenter;
            var h1 = math.float2(math.dot(h1v, circleU), math.dot(h1v, circleV));
            var h2v = math.float3(-1, y, 1) * CdotN / math.dot(math.float3(-1, y, 1), circleNormal) - circleCenter;
            var h2 = math.float2(math.dot(h2v, circleU), math.dot(h2v, circleV));

            var lineDirection = math.normalize(h2 - h1);
            // We now have the direction of the line, and would like to find the point on it that is closest to the
            // circle center. A line in 2D is similar to a plane in 3D. So we can calculate a normal, which is just a
            // perpendicular/orthogonal direction, and then take the dot product to find the distance. This is similar
            // to when calculating the d-term for a plane in 3D, which is also just calculating the closest distance
            // from the origin to the plane.
            var lineNormal = math.float2(lineDirection.y, -lineDirection.x);
            var distToLine = math.dot(h1, lineNormal);
            // We can then get that point on the line by following our normal with the distance we just calculated.
            var lineCenter = lineNormal * distToLine;

            // Avoid negative square roots, as this means we've hit one of the cases that we do not care about.
            if (distToLine > circleRadius) return false;

            // What's left now is to intersect the line with the circle. We can do so with Pythagoras. Our triangle
            // is made up of `lineCenter`, the circle center and one of the intersection points.
            // We know the distance from `lineCenter` to the circle center (`distToLine`), and the distance from
            // the circle center to one of the intersection points must be the circle radius, as it lies on the
            // circle, forming the hypotenuse.
            var l = math.sqrt(circleRadius * circleRadius - distToLine * distToLine);

            // What we found above is the distance from `lineCenter` to each of the intersection points. So we just
            // scrub along the line in both directions using the found distance, and then transform back into view
            // space.
            var x1 = lineCenter + l * lineDirection;
            var x2 = lineCenter - l * lineDirection;
            p1 = circleCenter + x1.x * circleU + x1.y * circleV;
            p2 = circleCenter + x2.x * circleU + x2.y * circleV;

            return true;
        }

        static void GetConeSideTangentPoints(float3 vertex, float3 axis, float cosHalfAngle, float circleRadius, float coneHeight, float range, float3 circleU, float3 circleV, out float3 l1, out float3 l2)
        {
            l1 = l2 = 0;

            if (math.dot(math.normalize(-vertex), axis) >= cosHalfAngle)
            {
                return;
            }

            var d = -math.dot(vertex, axis);
            // If d is zero, this leads to a numerical instability in the code later on. This is why we make the value
            // an epsilon if it is zero.
            if (d == 0f) d = 1e-6f;
            var sign = d < 0 ? -1f : 1f;
            // sign *= vertex.z < 0 ? -1f : 1f;
            // `origin` is the center of the circular slice we're about to calculate at distance `d` from the `vertex`.
            var origin = vertex + axis * d;
            // Get the radius of the circular slice of the cone at the `origin`.
            var radius = math.abs(d) * circleRadius / coneHeight;
            // `circleU` and `circleV` are the two vectors perpendicular to the cone's axis. `cameraUV` is thus the
            // position of the camera projected onto the plane of the circular slice. This basically creates a new
            // 2D coordinate space, with (0, 0) located at the center of the circular slice, which why this variable
            // is called `origin`.
            var cameraUV = math.float2(math.dot(circleU, -origin), math.dot(circleV, -origin));
            // Use homogeneous coordinates to find the tangents.
            var polar = math.float3(cameraUV, -square(radius));
            var p1 = math.float2(-1, -polar.x / polar.y * (-1) - polar.z / polar.y);
            var p2 = math.float2(1, -polar.x / polar.y * 1 - polar.z / polar.y);
            var lineDirection = math.normalize(p2 - p1);
            var lineNormal = math.float2(lineDirection.y, -lineDirection.x);
            var distToLine = math.dot(p1, lineNormal);
            var lineCenter = lineNormal * distToLine;
            var l = math.sqrt(radius * radius - distToLine * distToLine);
            var x1UV = lineCenter + l * lineDirection;
            var x2UV = lineCenter - l * lineDirection;
            var dir1 = math.normalize((origin + x1UV.x * circleU + x1UV.y * circleV) - vertex) * sign;
            var dir2 = math.normalize((origin + x2UV.x * circleU + x2UV.y * circleV) - vertex) * sign;
            l1 = dir1 * range;
            l2 = dir2 * range;
        }

        static float3 EvaluateNearConic(float near, float3 o, float3 d, float r, float3 u, float3 v, float theta)
        {
            var h = (near - o.z) / (d.z + r * u.z * math.cos(theta) + r * v.z * math.sin(theta));
            return math.float3(o.xy + h * (d.xy + r * u.xy * math.cos(theta) + r * v.xy * math.sin(theta)), near);
        }

        // o, d, u and v are expected to contain {x or y, z}. I.e. pass in x values to find tangents where x' = 0
        // Returns the two theta values as a float2.
        static float2 FindNearConicTangentTheta(float2 o, float2 d, float r, float2 u, float2 v)
        {
            var sqrt = math.sqrt(square(d.x) * square(u.y) + square(d.x) * square(v.y) - 2f * d.x * d.y * u.x * u.y - 2f * d.x * d.y * v.x * v.y + square(d.y) * square(u.x) + square(d.y) * square(v.x) - square(r) * square(u.x) * square(v.y) + 2f * square(r) * u.x * u.y * v.x * v.y - square(r) * square(u.y) * square(v.x));
            var denom = d.x * v.y - d.y * v.x - r * u.x * v.y + r * u.y * v.x;
            return 2 * math.atan((-d.x * u.y + d.y * u.x + math.float2(1, -1) * sqrt) / denom);
        }

        static float2 FindNearConicYTheta(float near, float3 o, float3 d, float r, float3 u, float3 v, float y)
        {
            var sqrt = math.sqrt(-square(d.y) * square(o.z) + 2 * square(d.y) * o.z * near - square(d.y) * square(near) + 2 * d.y * d.z * o.y * o.z - 2 * d.y * d.z * o.y * near - 2 * d.y * d.z * o.z * y + 2 * d.y * d.z * y * near - square(d.z) * square(o.y) + 2 * square(d.z) * o.y * y - square(d.z) * square(y) + square(o.y) * square(r) * square(u.z) + square(o.y) * square(r) * square(v.z) - 2 * o.y * o.z * square(r) * u.y * u.z - 2 * o.y * o.z * square(r) * v.y * v.z - 2 * o.y * y * square(r) * square(u.z) - 2 * o.y * y * square(r) * square(v.z) + 2 * o.y * square(r) * u.y * u.z * near + 2 * o.y * square(r) * v.y * v.z * near + square(o.z) * square(r) * square(u.y) + square(o.z) * square(r) * square(v.y) + 2 * o.z * y * square(r) * u.y * u.z + 2 * o.z * y * square(r) * v.y * v.z - 2 * o.z * square(r) * square(u.y) * near - 2 * o.z * square(r) * square(v.y) * near + square(y) * square(r) * square(u.z) + square(y) * square(r) * square(v.z) - 2 * y * square(r) * u.y * u.z * near - 2 * y * square(r) * v.y * v.z * near + square(r) * square(u.y) * square(near) + square(r) * square(v.y) * square(near));
            var denom = d.y * o.z - d.y * near - d.z * o.y + d.z * y + o.y * r * u.z - o.z * r * u.y - y * r * u.z + r * u.y * near;
            return 2 * math.atan((r * (o.y * v.z - o.z * v.y - y * v.z + v.y * near) + math.float2(1, -1) * sqrt) / denom);
        }
    }
}
