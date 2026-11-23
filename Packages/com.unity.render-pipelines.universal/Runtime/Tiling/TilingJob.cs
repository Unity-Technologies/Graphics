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
            else
            {
                TileReflectionProbe(index);
            }
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
            if (IsInDepthRange(lightPositionVS)) ExpandY(lightPositionVS);
            var lightDirectionVS = math.normalize(math.mul(worldToViews[m_ViewIndex], math.float4(lightToWorld.c2.xyz, 0)).xyz);
            lightDirectionVS.z *= -1;

            var halfAngle = math.radians(light.spotAngle * 0.5f);
            var range = light.range;
            var rangesq = square(range);
            var cosHalfAngle = math.cos(halfAngle);
            var coneHeight = cosHalfAngle * range;
            var isSpot = light.lightType == LightType.Spot;

            // Outline of the sphere projected onto the YZ and XZ planes: the horizon as seen from the camera, and the
            // extremes of the circles cut by the near and far planes. Missing points are NaN.
            GetCircleHorizon(lightPositionVS.yz, range, out var horizonY0, out var horizonY1);
            GetCircleHorizon(lightPositionVS.xz, range, out var horizonX0, out var horizonX1);
            ExpandYIfInSphereCap(math.float3(lightPositionVS.x, horizonY0), lightPositionVS, lightDirectionVS, cosHalfAngle, isSpot);
            ExpandYIfInSphereCap(math.float3(lightPositionVS.x, horizonY1), lightPositionVS, lightDirectionVS, cosHalfAngle, isSpot);
            ExpandYIfInSphereCap(math.float3(horizonX0.x, lightPositionVS.y, horizonX0.y), lightPositionVS, lightDirectionVS, cosHalfAngle, isSpot);
            ExpandYIfInSphereCap(math.float3(horizonX1.x, lightPositionVS.y, horizonX1.y), lightPositionVS, lightDirectionVS, cosHalfAngle, isSpot);
            for (var i = 0; i < 2; i++)
            {
                var depth = i == 0 ? near : far;
                var sectionRadius = math.sqrt(rangesq - square(depth - lightPositionVS.z));
                ExpandYIfInSphereCap(math.float3(lightPositionVS.x, lightPositionVS.y - sectionRadius, depth), lightPositionVS, lightDirectionVS, cosHalfAngle, isSpot);
                ExpandYIfInSphereCap(math.float3(lightPositionVS.x, lightPositionVS.y + sectionRadius, depth), lightPositionVS, lightDirectionVS, cosHalfAngle, isSpot);
                ExpandYIfInSphereCap(math.float3(lightPositionVS.x - sectionRadius, lightPositionVS.y, depth), lightPositionVS, lightDirectionVS, cosHalfAngle, isSpot);
                ExpandYIfInSphereCap(math.float3(lightPositionVS.x + sectionRadius, lightPositionVS.y, depth), lightPositionVS, lightDirectionVS, cosHalfAngle, isSpot);
            }

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
                if (IsInDepthRange(baseY1)) ExpandY(baseY1);
                if (IsInDepthRange(baseY2)) ExpandY(baseY2);

                // Project cone base into the XZ plane, find the horizon points, and re-construct view space position of found points.
                // See comment for YZ plane for details.
                var baseUX = math.abs(math.abs(lightDirectionVS.y) - 1) < 1e-6f ? math.float3(1, 0, 0) : math.normalize(math.cross(lightDirectionVS, math.float3(0, 1, 0)));
                var baseVX = math.cross(lightDirectionVS, baseUX);
                GetProjectedCircleHorizon(baseCenter.xz, baseRadius, baseUX.xz, baseVX.xz, out var baseX1UV, out var baseX2UV);
                var baseX1 = baseCenter + baseX1UV.x * baseUX + baseX1UV.y * baseVX;
                var baseX2 = baseCenter + baseX2UV.x * baseUX + baseX2UV.y * baseVX;
                if (IsInDepthRange(baseX1)) ExpandY(baseX1);
                if (IsInDepthRange(baseX2)) ExpandY(baseX2);

                // Handle base circle clipping by intersecting it with the near and far planes if needed.
                if (GetCircleClipPoints(baseCenter, lightDirectionVS, baseRadius, near, out var baseClip0, out var baseClip1))
                {
                    ExpandY(baseClip0);
                    ExpandY(baseClip1);
                }
                if (GetCircleClipPoints(baseCenter, lightDirectionVS, baseRadius, far, out var baseFarClip0, out var baseFarClip1))
                {
                    ExpandY(baseFarClip0);
                    ExpandY(baseFarClip1);
                }

                var sinHalfAngle = math.sin(halfAngle);
                var coneDistanceTolerance = 1e-5f * (math.length(lightPositionVS) + range);
                bool ConicPointIsValid(float3 p) => IsOnCone(p, lightPositionVS, lightDirectionVS, cosHalfAngle, sinHalfAngle, coneHeight, coneDistanceTolerance);

                // Calculate Z bounds of cone and check if it's overlapping with the near and far planes.
                // From https://www.iquilezles.org/www/articles/diskbbox/diskbbox.htm
                var baseExtentZ = baseRadius * math.sqrt(math.saturate(1.0f - square(lightDirectionVS.z)));
                var coneMinZ = math.min(baseCenter.z - baseExtentZ, lightPositionVS.z);
                var coneMaxZ = math.max(baseCenter.z + baseExtentZ, lightPositionVS.z);
                var coneIsClipping = near >= coneMinZ && near <= coneMaxZ;
                var coneCrossesFar = far >= coneMinZ && far <= coneMaxZ;

                // Any frame orthogonal to the axis works; cross with the view axis least aligned with it to keep it well conditioned.
                var coneU = math.normalize(math.cross(lightDirectionVS, math.abs(lightDirectionVS.x) < 0.5f ? math.float3(1, 0, 0) : math.float3(0, 1, 0)));
                var coneV = math.cross(lightDirectionVS, coneU);

                var nearSection = new ConeSection(near, lightPositionVS, lightDirectionVS, cosHalfAngle, sinHalfAngle);
                var farSection = new ConeSection(far, lightPositionVS, lightDirectionVS, cosHalfAngle, sinHalfAngle);
                for (var i = 0; i < 2; i++)
                {
                    if (i == 0 ? !coneIsClipping : !coneCrossesFar)
                        continue;
                    var section = i == 0 ? nearSection : farSection;
                    section.GetYExtremes(out var p0Y, out var p1Y);
                    if (ConicPointIsValid(p0Y)) ExpandY(p0Y);
                    if (ConicPointIsValid(p1Y)) ExpandY(p1Y);

                    section.GetXExtremes(out var p0X, out var p1X);
                    if (ConicPointIsValid(p0X)) ExpandY(p0X);
                    if (ConicPointIsValid(p1X)) ExpandY(p1X);
                }

                // Calculate the lines making up the sides of the cone as seen from the camera. `l1` and `l2` form lines
                // from the light position.
                GetConeSideTangentPoints(-lightPositionVS, lightDirectionVS, cosHalfAngle, baseRadius, coneHeight, range, coneU, coneV, out var l1, out var l2);

                // Where the cone sides cross the bottom and top of the screen.
                ExpandYAtViewPlane(lightPositionVS, l1, viewPlaneBottoms[m_ViewIndex]);
                ExpandYAtViewPlane(lightPositionVS, l1, viewPlaneTops[m_ViewIndex]);
                ExpandYAtViewPlane(lightPositionVS, l2, viewPlaneBottoms[m_ViewIndex]);
                ExpandYAtViewPlane(lightPositionVS, l2, viewPlaneTops[m_ViewIndex]);

                m_TileYRange.Clamp(0, (short)(tileCount.y - 1));

                // Calculate tile plane ranges for cone.
                for (var planeIndex = m_TileYRange.start; planeIndex <= m_TileYRange.end + 1; planeIndex++)
                {
                    var planeRange = InclusiveRange.empty;

                    // Y-position on the view plane (Z=1)
                    var planeY = math.lerp(viewPlaneBottoms[m_ViewIndex], viewPlaneTops[m_ViewIndex], planeIndex * tileScaleInv.y);

                    var planeNormal = math.float3(0, 1, -planeY);

                    // Intersect lines with y-plane and clip if needed.
                    var l1t = math.dot(-lightPositionVS, planeNormal) / math.dot(l1, planeNormal);
                    var l1x = lightPositionVS + l1 * l1t;
                    if (l1t >= 0 && l1t <= 1 && IsInDepthRange(l1x)) ExpandTileX(ref planeRange, ViewToTileSpace(l1x).x);

                    var l2t = math.dot(-lightPositionVS, planeNormal) / math.dot(l2, planeNormal);
                    var l2x = lightPositionVS + l2 * l2t;
                    if (l2t >= 0 && l2t <= 1 && IsInDepthRange(l2x)) ExpandTileX(ref planeRange, ViewToTileSpace(l2x).x);

                    if (IntersectCirclePlane(math.float4(0, 1, -planeY, 0), baseCenter, baseUY, baseVY, baseRadius, out var circleTile0, out var circleTile1))
                    {
                        if (IsInDepthRange(circleTile0)) ExpandTileX(ref planeRange, ViewToTileSpace(circleTile0).x);
                        if (IsInDepthRange(circleTile1)) ExpandTileX(ref planeRange, ViewToTileSpace(circleTile1).x);
                    }

                    if (coneIsClipping)
                    {
                        nearSection.IntersectRow(planeY * near, out var p0, out var p1);
                        if (ConicPointIsValid(p0)) ExpandTileX(ref planeRange, ViewToTileSpace(p0).x);
                        if (ConicPointIsValid(p1)) ExpandTileX(ref planeRange, ViewToTileSpace(p1).x);
                    }

                    if (coneCrossesFar)
                    {
                        farSection.IntersectRow(planeY * far, out var p0, out var p1);
                        if (ConicPointIsValid(p0)) ExpandTileX(ref planeRange, ViewToTileSpace(p0).x);
                        if (ConicPointIsValid(p1)) ExpandTileX(ref planeRange, ViewToTileSpace(p1).x);
                    }

                    MergeTileRangeRows(planeIndex, planeRange);
                }
            }

            m_TileYRange.Clamp(0, (short)(tileCount.y - 1));

            // Calculate tile plane ranges for sphere.
            for (var planeIndex = m_TileYRange.start; planeIndex <= m_TileYRange.end + 1; planeIndex++)
            {
                var planeRange = InclusiveRange.empty;

                var planeY = math.lerp(viewPlaneBottoms[m_ViewIndex], viewPlaneTops[m_ViewIndex], planeIndex * tileScaleInv.y);
                GetSphereYPlaneHorizon(lightPositionVS, range, planeY, out var sphereTile0, out var sphereTile1);
                ExpandTileXIfInSphereCap(ref planeRange, sphereTile0, lightPositionVS, lightDirectionVS, cosHalfAngle, isSpot);
                ExpandTileXIfInSphereCap(ref planeRange, sphereTile1, lightPositionVS, lightDirectionVS, cosHalfAngle, isSpot);
                for (var i = 0; i < 2; i++)
                {
                    GetSphereSectionRowPoints(lightPositionVS, rangesq, i == 0 ? near : far, planeY, out var section0, out var section1);
                    ExpandTileXIfInSphereCap(ref planeRange, section0, lightPositionVS, lightDirectionVS, cosHalfAngle, isSpot);
                    ExpandTileXIfInSphereCap(ref planeRange, section1, lightPositionVS, lightDirectionVS, cosHalfAngle, isSpot);
                }

                MergeTileRangeRows(planeIndex, planeRange);
            }

            tileRanges[m_Offset] = m_TileYRange;
        }

        void TileLightOrthographic(int lightIndex)
        {
            var light = lights[lightIndex];
            if (light.lightType != LightType.Point && light.lightType != LightType.Spot)
            {
                return;
            }

            var lightToWorld = (float4x4)light.localToWorldMatrix;
            var lightPosVS = math.mul(worldToViews[m_ViewIndex], math.float4(lightToWorld.c3.xyz, 1)).xyz;
            lightPosVS.z *= -1;
            if (IsInDepthRange(lightPosVS)) ExpandOrthographic(lightPosVS);
            var lightDirVS = math.mul(worldToViews[m_ViewIndex], math.float4(lightToWorld.c2.xyz, 0)).xyz;
            lightDirVS.z *= -1;
            lightDirVS = math.normalize(lightDirVS);

            var isSpot = light.lightType == LightType.Spot;
            var halfAngle = math.radians(light.spotAngle * 0.5f);
            var range = light.range;
            var rangeSq = square(range);
            var cosHalfAngle = math.cos(halfAngle);
            var sinHalfAngle = math.sin(halfAngle);
            var coneHeight = cosHalfAngle * range;

            // The outline of the sphere is its equator facing the camera, clipped to the depth range, and the circles cut by
            // the near and far planes.
            var sphereOutline = math.bool3(IsInDepthRange(lightPosVS), math.abs(near - lightPosVS.z) <= range, math.abs(far - lightPosVS.z) <= range);
            for (var i = 0; i < 3; i++)
            {
                if (!sphereOutline[i])
                    continue;
                var depth = i == 0 ? lightPosVS.z : i == 1 ? near : far;
                var radius = math.sqrt(rangeSq - square(depth - lightPosVS.z));
                ExpandOrthographicIfInSphereCap(math.float3(lightPosVS.x - radius, lightPosVS.y, depth), lightPosVS, lightDirVS, cosHalfAngle, isSpot);
                ExpandOrthographicIfInSphereCap(math.float3(lightPosVS.x + radius, lightPosVS.y, depth), lightPosVS, lightDirVS, cosHalfAngle, isSpot);
                ExpandOrthographicIfInSphereCap(math.float3(lightPosVS.x, lightPosVS.y - radius, depth), lightPosVS, lightDirVS, cosHalfAngle, isSpot);
                ExpandOrthographicIfInSphereCap(math.float3(lightPosVS.x, lightPosVS.y + radius, depth), lightPosVS, lightDirVS, cosHalfAngle, isSpot);
            }

            var circleCenter = lightPosVS + lightDirVS * coneHeight;
            var circleRadius = range * sinHalfAngle;
            var coneU = math.normalize(math.cross(lightDirVS, math.abs(lightDirVS.x) < 0.5f ? math.float3(1, 0, 0) : math.float3(0, 1, 0)));
            var coneV = math.cross(lightDirVS, coneU);
            var nearSection = new ConeSection(near, lightPosVS, lightDirVS, cosHalfAngle, sinHalfAngle);
            var farSection = new ConeSection(far, lightPosVS, lightDirVS, cosHalfAngle, sinHalfAngle);
            var circleExtentZ = circleRadius * math.sqrt(math.saturate(1.0f - square(lightDirVS.z)));
            var coneMinZ = math.min(circleCenter.z - circleExtentZ, lightPosVS.z);
            var coneMaxZ = math.max(circleCenter.z + circleExtentZ, lightPosVS.z);
            var coneCrosses = math.bool2(near >= coneMinZ && near <= coneMaxZ, far >= coneMinZ && far <= coneMaxZ);
            float3 l1 = 0, l2 = 0;
            var coneDistanceTolerance = 1e-5f * (math.length(lightPosVS) + range);

            if (isSpot)
            {
                // Extremes of the cone base circle, and its points on the near and far planes.
                var circleUp = math.normalize(math.float3(0, 1, 0) - lightDirVS * lightDirVS.y) * circleRadius;
                var circleRight = math.normalize(math.float3(1, 0, 0) - lightDirVS * lightDirVS.x) * circleRadius;
                ExpandOrthographicIfInDepthRange(circleCenter - circleUp);
                ExpandOrthographicIfInDepthRange(circleCenter + circleUp);
                ExpandOrthographicIfInDepthRange(circleCenter - circleRight);
                ExpandOrthographicIfInDepthRange(circleCenter + circleRight);
                for (var i = 0; i < 2; i++)
                {
                    if (GetCircleClipPoints(circleCenter, lightDirVS, circleRadius, i == 0 ? near : far, out var clip0, out var clip1))
                    {
                        ExpandOrthographic(clip0);
                        ExpandOrthographic(clip1);
                    }
                }

                // Extremes of the cone sections with the near and far planes.
                for (var i = 0; i < 2; i++)
                {
                    if (!coneCrosses[i])
                        continue;
                    var section = i == 0 ? nearSection : farSection;
                    section.GetYExtremes(out var p0Y, out var p1Y);
                    section.GetXExtremes(out var p0X, out var p1X);
                    if (IsOnCone(p0Y, lightPosVS, lightDirVS, cosHalfAngle, sinHalfAngle, coneHeight, coneDistanceTolerance)) ExpandOrthographic(p0Y);
                    if (IsOnCone(p1Y, lightPosVS, lightDirVS, cosHalfAngle, sinHalfAngle, coneHeight, coneDistanceTolerance)) ExpandOrthographic(p1Y);
                    if (IsOnCone(p0X, lightPosVS, lightDirVS, cosHalfAngle, sinHalfAngle, coneHeight, coneDistanceTolerance)) ExpandOrthographic(p0X);
                    if (IsOnCone(p1X, lightPosVS, lightDirVS, cosHalfAngle, sinHalfAngle, coneHeight, coneDistanceTolerance)) ExpandOrthographic(p1X);
                }

                // Silhouette lines of the cone as seen along the view direction.
                GetConeSideTangentPoints(math.float3(0, 0, -1), lightDirVS, cosHalfAngle, circleRadius, coneHeight, range, coneU, coneV, out l1, out l2);
            }

            m_TileYRange.Clamp(0, (short)(tileCount.y - 1));

            // Tile plane ranges; the rows are the planes y = planeY.
            for (var planeIndex = m_TileYRange.start; planeIndex <= m_TileYRange.end + 1; planeIndex++)
            {
                var planeRange = InclusiveRange.empty;
                var planeY = math.lerp(viewPlaneBottoms[m_ViewIndex], viewPlaneTops[m_ViewIndex], planeIndex * tileScaleInv.y);

                for (var i = 0; i < 3; i++)
                {
                    if (!sphereOutline[i])
                        continue;
                    var depth = i == 0 ? lightPosVS.z : i == 1 ? near : far;
                    var halfWidth = math.sqrt(rangeSq - square(depth - lightPosVS.z) - square(planeY - lightPosVS.y));
                    ExpandRangeOrthographicIfInSphereCap(ref planeRange, math.float3(lightPosVS.x - halfWidth, planeY, depth), lightPosVS, lightDirVS, cosHalfAngle, isSpot);
                    ExpandRangeOrthographicIfInSphereCap(ref planeRange, math.float3(lightPosVS.x + halfWidth, planeY, depth), lightPosVS, lightDirVS, cosHalfAngle, isSpot);
                }

                if (isSpot)
                {
                    if (IntersectCirclePlane(math.float4(0, 1, 0, -planeY), circleCenter, coneU, coneV, circleRadius, out var circle0, out var circle1))
                    {
                        if (IsInDepthRange(circle0)) ExpandRangeOrthographic(ref planeRange, circle0.x);
                        if (IsInDepthRange(circle1)) ExpandRangeOrthographic(ref planeRange, circle1.x);
                    }

                    var line0 = lightPosVS + l1 * ((planeY - lightPosVS.y) / l1.y);
                    var line1 = lightPosVS + l2 * ((planeY - lightPosVS.y) / l2.y);
                    if (IsOnSegment(line0, lightPosVS, l1)) ExpandRangeOrthographic(ref planeRange, line0.x);
                    if (IsOnSegment(line1, lightPosVS, l2)) ExpandRangeOrthographic(ref planeRange, line1.x);

                    for (var i = 0; i < 2; i++)
                    {
                        if (!coneCrosses[i])
                            continue;
                        (i == 0 ? nearSection : farSection).IntersectRow(planeY, out var p0, out var p1);
                        if (IsOnCone(p0, lightPosVS, lightDirVS, cosHalfAngle, sinHalfAngle, coneHeight, coneDistanceTolerance)) ExpandRangeOrthographic(ref planeRange, p0.x);
                        if (IsOnCone(p1, lightPosVS, lightDirVS, cosHalfAngle, sinHalfAngle, coneHeight, coneDistanceTolerance)) ExpandRangeOrthographic(ref planeRange, p1.x);
                    }
                }

                MergeTileRangeRows(planeIndex, planeRange);
            }

            tileRanges[m_Offset] = m_TileYRange;
        }

        // Whether p = origin + t * line lies on the segment (t in [0, 1]) and in the depth range.
        bool IsOnSegment(float3 p, float3 origin, float3 line)
        {
            var t = math.dot(p - origin, line) / math.lengthsq(line);
            return t >= 0 && t <= 1 && IsInDepthRange(p);
        }

        void ExpandOrthographicIfInDepthRange(float3 positionVS)
        {
            if (IsInDepthRange(positionVS)) ExpandOrthographic(positionVS);
        }

        void ExpandOrthographicIfInSphereCap(float3 positionVS, float3 lightPositionVS, float3 lightDirectionVS, float cosHalfAngle, bool isSpot)
        {
            if (IsInSphereCap(positionVS, lightPositionVS, lightDirectionVS, cosHalfAngle, isSpot)) ExpandOrthographic(positionVS);
        }

        void ExpandRangeOrthographicIfInSphereCap(ref InclusiveRange range, float3 positionVS, float3 lightPositionVS, float3 lightDirectionVS, float cosHalfAngle, bool isSpot)
        {
            if (IsInSphereCap(positionVS, lightPositionVS, lightDirectionVS, cosHalfAngle, isSpot)) ExpandRangeOrthographic(ref range, positionVS.x);
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
            quaternion rotation;
            if (reflectionProbeRotation)
                rotation = (quaternion)reflectionProbe.localToWorldMatrix.rotation;
            else
                rotation = quaternion.identity;

            // The vertices of the cube in view space.
            var points = new NativeArray<float3>(k_CubePoints.Length, Allocator.Temp);
            // This is initially filled with just the cube vertices that lie between the near and far planes. Each edge can add
            // two more points where it crosses them.
            var clippedPoints = new NativeArray<float2>(k_CubePoints.Length + k_CubeLineIndices.Length * 3 * 2, Allocator.Temp);
            var clippedPointsCount = 0;
            var leftmostIndex = 0;
            for (var i = 0; i < k_CubePoints.Length; i++)
            {
                var rotatedPointsWS = centerWS + math.rotate(rotation, extentsWS * k_CubePoints[i]);
                var point = math.mul(worldToViews[m_ViewIndex], math.float4(rotatedPointsWS, 1)).xyz;
                point.z *= -1;
                points[i] = point;
                if (IsInDepthRange(point))
                {
                    var clippedPoint = isOrthographic ? point.xy : point.xy/point.z;
                    var clippedIndex = clippedPointsCount++;
                    clippedPoints[clippedIndex] = clippedPoint;
                    if (clippedPoint.x < clippedPoints[leftmostIndex].x) leftmostIndex = clippedIndex;
                }
            }

            // Clip the cube's line segments with the near and far planes, and add the new vertices to clippedPoints. Only
            // lines that cross a plane generate new vertices.
            for (var i = 0; i < k_CubeLineIndices.Length; i++)
            {
                var indices = k_CubeLineIndices[i];
                var p0 = points[indices.x];
                for (var j = 0; j < 3; j++)
                {
                    var p1 = points[indices[j+1]];
                    for (var k = 0; k < 2; k++)
                    {
                        var depth = k == 0 ? near : far;
                        if ((p0.z < depth) == (p1.z < depth)) continue;
                        var d = (depth - p0.z) / (p1.z - p0.z);
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
                for (var planeIndex = m_TileYRange.start; planeIndex <= m_TileYRange.end + 1; planeIndex++)
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
                        ExpandTileX(ref planeRange, pTS.x);
                    }

                    MergeTileRangeRows(planeIndex, planeRange);
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

        void ExpandY(float3 positionVS) => ExpandTile(ViewToTileSpace(positionVS));

        bool IsInDepthRange(float3 positionVS) => positionVS.z >= near && positionVS.z <= far;

        // Whether a point of a cone section lies on the cone, up to its base. Also rejects points off the cone, which
        // degenerate sections can produce. Tolerates ~1e-4 rad, or a distance from the surface on the order of the float
        // precision of the light's coordinates.
        internal static bool IsOnCone(float3 p, float3 vertex, float3 axis, float cosHalfAngle, float sinHalfAngle, float coneHeight, float distanceTolerance)
        {
            var q = p - vertex;
            var length = math.length(q);
            var axial = math.dot(q, axis);
            return axial >= cosHalfAngle * length - sinHalfAngle * (1e-4f * length + distanceTolerance) && axial <= coneHeight;
        }

        // For points on a light's sphere: skips points outside the depth range and, for spot lights, outside the cone.
        bool IsInSphereCap(float3 positionVS, float3 lightPositionVS, float3 lightDirectionVS, float cosHalfAngle, bool isSpot) =>
            IsInDepthRange(positionVS) && (!isSpot || math.dot(math.normalize(positionVS - lightPositionVS), lightDirectionVS) >= cosHalfAngle);

        void ExpandYIfInSphereCap(float3 positionVS, float3 lightPositionVS, float3 lightDirectionVS, float cosHalfAngle, bool isSpot)
        {
            if (IsInSphereCap(positionVS, lightPositionVS, lightDirectionVS, cosHalfAngle, isSpot)) ExpandY(positionVS);
        }

        void ExpandTileXIfInSphereCap(ref InclusiveRange range, float3 positionVS, float3 lightPositionVS, float3 lightDirectionVS, float cosHalfAngle, bool isSpot)
        {
            if (IsInSphereCap(positionVS, lightPositionVS, lightDirectionVS, cosHalfAngle, isSpot)) ExpandTileX(ref range, ViewToTileSpace(positionVS).x);
        }

        // Expands the tile Y range where the segment from origin to origin + line crosses the view plane y = planeY * z.
        void ExpandYAtViewPlane(float3 origin, float3 line, float planeY)
        {
            var planeNormal = math.float3(0, 1, -planeY);
            var t = math.dot(-origin, planeNormal) / math.dot(line, planeNormal);
            var positionVS = origin + line * t;
            if (t >= 0 && t <= 1 && IsInDepthRange(positionVS)) ExpandY(positionVS);
        }

        void ExpandOrthographic(float3 positionVS) => ExpandTile(ViewToTileSpaceOrthographic(positionVS));

        void ExpandRangeOrthographic(ref InclusiveRange range, float xVS) => ExpandTileX(ref range, ViewToTileSpaceOrthographic(xVS).x);

        /// <summary>
        /// Expands the tile Y range and the X range in the row containing the tile-space position.
        /// </summary>
        void ExpandTile(float2 positionTS)
        {
            // NaN marks a missed intersection; skip it, as float-to-int casts of NaN differ across Burst, IL2CPP and Mono.
            if (math.any(math.isnan(positionTS))) return;
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
            if (math.isnan(tileX)) return;
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

        /// <summary>
        /// Returns the points where the tangents from the origin touch a 2D circle, or NaN if the origin is inside it.
        /// </summary>
        static void GetCircleHorizon(float2 center, float radius, out float2 p0, out float2 p1)
        {
            // With d the distance to the center and l the tangent length, the points are (l^2 * center +- l * r * perp(center)) / d^2.
            var dSq = math.lengthsq(center);
            var l = math.sqrt(dSq - square(radius));
            var side = l * radius * math.float2(-center.y, center.x);
            p0 = (square(l) * center + side) / dSq;
            p1 = (square(l) * center - side) / dSq;
        }

        /// <summary>
        /// Returns the horizon points, as seen from the origin, of the circle cut from a sphere by the plane through the
        /// origin and the view-plane row at height y. NaN if the plane misses the sphere or the origin is inside the circle.
        /// </summary>
        static void GetSphereYPlaneHorizon(float3 center, float sphereRadius, float y, out float3 left, out float3 right)
        {
            // Basis vectors in the y-plane, which contains (1, 0, 0) and (0, y, 1), and its normal (0, 1, -y).
            var planeU = math.normalize(math.float3(0, y, 1));
            var planeV = math.float3(1, 0, 0);
            var normal = math.normalize(math.float3(0, 1, -y));

            // The section circle is centered at the point of the plane closest to the sphere center.
            var distanceToPlane = math.dot(normal, center);
            var centerOnPlane = math.float2(math.dot(center, planeU), math.dot(center, planeV));
            var circleRadius = math.sqrt(square(sphereRadius) - square(distanceToPlane));

            GetCircleHorizon(centerOnPlane, circleRadius, out var leftOnPlane, out var rightOnPlane);
            left = leftOnPlane.x * planeU + leftOnPlane.y * planeV;
            right = rightOnPlane.x * planeU + rightOnPlane.y * planeV;
        }

        /// <summary>
        /// Returns the points where the plane through the origin and the view-plane row at height planeY crosses the
        /// circle cut from a sphere by the plane z = depth, or NaN if there are none.
        /// </summary>
        static void GetSphereSectionRowPoints(float3 center, float radiusSq, float depth, float planeY, out float3 p0, out float3 p1)
        {
            var y = planeY * depth;
            var halfWidth = math.sqrt(radiusSq - square(depth - center.z) - square(y - center.y));
            p0 = math.float3(center.x - halfWidth, y, depth);
            p1 = math.float3(center.x + halfWidth, y, depth);
        }

        /// <summary>
        /// Finds the two points of intersection of a 3D circle and the plane z = near.
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

        /// <summary>
        /// Calculates the horizon of a circle orthogonally projected to a plane as seen from the origin on the plane.
        /// </summary>
        /// <param name="center">The center of the circle projected onto the plane.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="U">The major axis of the ellipse formed by the projection of the circle.</param>
        /// <param name="V">The minor axis of the ellipse formed by the projection of the circle.</param>
        /// <param name="uv1">The first horizon point expressed as factors of <paramref name="U"/> and <paramref name="V"/>.</param>
        /// <param name="uv2">The second horizon point expressed as factors of <paramref name="U"/> and <paramref name="V"/>.</param>
        internal static void GetProjectedCircleHorizon(float2 center, float radius, float2 U, float2 V, out float2 uv1, out float2 uv2)
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

                // Normalize U and V in the plane.
                var u = U / ul;
                var v = V / vl;

                // Major and minor axis of the ellipse.
                var a = ul * radius;
                var b = vl * radius;

                // Scale the ellipse to the unit circle, where the tangent points from the camera q are
                // (q +- sqrt(|q|^2 - 1) * perp(q)) / |q|^2. A camera inside the ellipse has no horizon and yields NaN.
                var q = math.float2(math.dot(-center, u), math.dot(-center, v)) / math.float2(a, b);
                var qq = math.lengthsq(q);
                var side = math.sqrt(qq - 1f) * math.float2(-q.y, q.x);
                uv1 = (q + side) / qq * radius;
                uv2 = (q - side) / qq * radius;
            }
        }

        /// <summary>
        /// Intersects a 3D circle with the plane dot(plane.xyz, p) + plane.w = 0.
        /// </summary>
        static bool IntersectCirclePlane(
            float4 plane, float3 circleCenter, float3 circleU, float3 circleV, float circleRadius,
            out float3 p1, out float3 p2)
        {
            p1 = p2 = 0;

            // In circle coordinates (s, t), the plane is the line a * s + b * t + k = 0. Parallel planes (a = b = 0)
            // give the whole circle or nothing, which the other bounds cover.
            var lineNormal = math.float2(math.dot(plane.xyz, circleU), math.dot(plane.xyz, circleV));
            var lineNormalLengthSq = math.lengthsq(lineNormal);
            if (lineNormalLengthSq == 0f) return false;

            var k = math.dot(plane.xyz, circleCenter) + plane.w;
            var distToLineSq = k * k / lineNormalLengthSq;
            if (distToLineSq > circleRadius * circleRadius) return false;

            var lineCenter = -k / lineNormalLengthSq * lineNormal;
            var lineDirection = math.float2(-lineNormal.y, lineNormal.x) * math.rsqrt(lineNormalLengthSq);
            var l = math.sqrt(circleRadius * circleRadius - distToLineSq);
            var x1 = lineCenter + l * lineDirection;
            var x2 = lineCenter - l * lineDirection;
            p1 = circleCenter + x1.x * circleU + x1.y * circleV;
            p2 = circleCenter + x2.x * circleU + x2.y * circleV;

            return true;
        }

        /// <summary>
        /// Finds the cone's silhouette lines from its vertex, as seen along toCamera: the vector from the vertex to the
        /// camera, or the direction towards the camera for an orthographic projection.
        /// </summary>
        static void GetConeSideTangentPoints(float3 toCamera, float3 axis, float cosHalfAngle, float circleRadius, float coneHeight, float range, float3 circleU, float3 circleV, out float3 l1, out float3 l2)
        {
            l1 = l2 = 0;

            if (math.dot(math.normalize(toCamera), axis) >= cosHalfAngle)
            {
                return;
            }

            // Silhouette generators satisfy dot(e, w.xy) = tan * w.z for unit e in the u/v plane, w = toCamera in the cone frame.
            // Solved directly so it stays stable when the camera lies near the plane through the vertex.
            var w = math.float3(math.dot(toCamera, circleU), math.dot(toCamera, circleV), math.dot(toCamera, axis));
            var distToAxisSq = math.lengthsq(w.xy);
            if (distToAxisSq == 0f)
            {
                return;
            }

            var invDistToAxis = math.rsqrt(distToAxisSq);
            var n = w.xy * invDistToAxis;
            var c = math.clamp(circleRadius / coneHeight * w.z * invDistToAxis, -1f, 1f);
            var s = math.sqrt(1f - c * c);
            var e1 = c * n + s * math.float2(-n.y, n.x);
            var e2 = c * n - s * math.float2(-n.y, n.x);
            var sinHalfAngle = circleRadius / range;
            l1 = (cosHalfAngle * axis + sinHalfAngle * (e1.x * circleU + e1.y * circleV)) * range;
            l2 = (cosHalfAngle * axis + sinHalfAngle * (e2.x * circleU + e2.y * circleV)) * range;
        }

        /// <summary>
        /// Section of a cone with the plane z = depth. With q = p - vertex = (x, y, z) and z = depth - vertex.z fixed, the cone
        /// is dot(q, axis)^2 = cos^2 * dot(q, q), which is solved directly in the plane to stay stable when the vertex is close to it.
        /// </summary>
        internal struct ConeSection
        {
            float3 m_Vertex;
            float3 m_Axis;
            float m_Depth;
            float m_Z;
            float m_CosSq;
            float m_SinSq;

            public ConeSection(float depth, float3 vertex, float3 axis, float cosHalfAngle, float sinHalfAngle)
            {
                m_Vertex = vertex;
                m_Axis = axis;
                m_Depth = depth;
                m_Z = depth - vertex.z;
                m_CosSq = cosHalfAngle * cosHalfAngle;
                m_SinSq = sinHalfAngle * sinHalfAngle;
            }

            /// <summary>
            /// Returns the points with extreme y, NaN or infinite where there are none.
            /// </summary>
            public void GetYExtremes(out float3 p0, out float3 p1)
            {
                var d = m_Axis;
                var y = SolveQuadratic(d.z * d.z - m_SinSq, -d.y * d.z * m_Z, m_Z * m_Z * (d.y * d.y - m_SinSq));
                var x = -d.x * (d.y * y + d.z * m_Z) / (d.x * d.x - m_CosSq);
                p0 = ToView(x.x, y.x);
                p1 = ToView(x.y, y.y);
            }

            /// <summary>
            /// Returns the points with extreme x, NaN or infinite where there are none.
            /// </summary>
            public void GetXExtremes(out float3 p0, out float3 p1)
            {
                var d = m_Axis;
                var x = SolveQuadratic(d.z * d.z - m_SinSq, -d.x * d.z * m_Z, m_Z * m_Z * (d.x * d.x - m_SinSq));
                var y = -d.y * (d.x * x + d.z * m_Z) / (d.y * d.y - m_CosSq);
                p0 = ToView(x.x, y.x);
                p1 = ToView(x.y, y.y);
            }

            /// <summary>
            /// Returns the points at view-space height y, NaN or infinite where there are none.
            /// </summary>
            public void IntersectRow(float yVS, out float3 p0, out float3 p1)
            {
                var d = m_Axis;
                var y = yVS - m_Vertex.y;
                var k = d.y * y + d.z * m_Z;
                var x = SolveQuadratic(d.x * d.x - m_CosSq, d.x * k, k * k - m_CosSq * (y * y + m_Z * m_Z));
                p0 = ToView(x.x, y);
                p1 = ToView(x.y, y);
            }

            float3 ToView(float x, float y) => math.float3(m_Vertex.xy + math.float2(x, y), m_Depth);
        }

        /// <summary>
        /// Returns the roots of a * t^2 + 2 * b * t + c = 0: NaN if there are none, and one is infinite if a is 0.
        /// </summary>
        static float2 SolveQuadratic(float a, float b, float c)
        {
            // Avoids the cancellation in -b + sqrt(b^2 - ac) by computing that root as c / q.
            var q = -(b + (b < 0f ? -math.sqrt(b * b - a * c) : math.sqrt(b * b - a * c)));
            return math.float2(q / a, c / q);
        }
    }
}
