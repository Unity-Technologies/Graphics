using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    // Perspective point/spot light tiling: the sphere, and for spots the cone base, sides and near/far sections.
    partial struct TilingJob
    {
        void TileLight(int lightIndex)
        {
            var light = lights[lightIndex];
            if (!HasNonZeroVolume(light))
            {
                return;
            }

            var lightToWorld = (float4x4)light.localToWorldMatrix;
            var lightPositionVS = math.mul(worldToViews[m_ViewIndex], math.float4(lightToWorld.c3.xyz, 1)).xyz;
            lightPositionVS.z *= -1;
            var range = math.min(light.range, k_MaxDistance);
            if (lightPositionVS.z - range > far || lightPositionVS.z + range < near)
                return;
            if (light.lightType == LightType.Point && SphereContainsNearPlane(lightPositionVS, range))
            {
                CoverAllTiles();
                return;
            }
            if (IsInDepthRange(lightPositionVS)) ExpandY(lightPositionVS);
            var lightDirectionVS = math.normalize(math.mul(worldToViews[m_ViewIndex], math.float4(lightToWorld.c2.xyz, 0)).xyz);
            lightDirectionVS.z *= -1;

            var halfAngle = math.radians(light.spotAngle * 0.5f);
            var rangesq = square(range);
            var cosHalfAngle = math.cos(halfAngle);
            var coneHeight = cosHalfAngle * range;
            var isSpot = light.lightType == LightType.Spot;

            // Outline of the sphere projected onto the YZ and XZ planes: the horizon as seen from the camera, and the
            // extremes of the circles cut by the near and far planes.
            if (GetCircleHorizon(lightPositionVS.yz, range, out var horizonY0, out var horizonY1))
            {
                ExpandYIfInSphereCap(math.float3(lightPositionVS.x, horizonY0), lightPositionVS, lightDirectionVS, cosHalfAngle, isSpot);
                ExpandYIfInSphereCap(math.float3(lightPositionVS.x, horizonY1), lightPositionVS, lightDirectionVS, cosHalfAngle, isSpot);
            }
            if (GetCircleHorizon(lightPositionVS.xz, range, out var horizonX0, out var horizonX1))
            {
                ExpandYIfInSphereCap(math.float3(horizonX0.x, lightPositionVS.y, horizonX0.y), lightPositionVS, lightDirectionVS, cosHalfAngle, isSpot);
                ExpandYIfInSphereCap(math.float3(horizonX1.x, lightPositionVS.y, horizonX1.y), lightPositionVS, lightDirectionVS, cosHalfAngle, isSpot);
            }

            var sphereRowTangents = new SphereRowTangents(lightPositionVS, rangesq);
            var sectionRadiusSq = rangesq - math.float2(square(near - lightPositionVS.z), square(far - lightPositionVS.z));
            var sphereCrossesPlane = sectionRadiusSq >= 0f;
            for (var i = 0; i < 2; i++)
            {
                if (!sphereCrossesPlane[i])
                    continue;
                var depth = i == 0 ? near : far;
                var sectionRadius = math.sqrt(sectionRadiusSq[i]);
                ExpandYIfInSphereCap(math.float3(lightPositionVS.x, lightPositionVS.y - sectionRadius, depth), lightPositionVS, lightDirectionVS, cosHalfAngle, isSpot);
                ExpandYIfInSphereCap(math.float3(lightPositionVS.x, lightPositionVS.y + sectionRadius, depth), lightPositionVS, lightDirectionVS, cosHalfAngle, isSpot);
                ExpandYIfInSphereCap(math.float3(lightPositionVS.x - sectionRadius, lightPositionVS.y, depth), lightPositionVS, lightDirectionVS, cosHalfAngle, isSpot);
                ExpandYIfInSphereCap(math.float3(lightPositionVS.x + sectionRadius, lightPositionVS.y, depth), lightPositionVS, lightDirectionVS, cosHalfAngle, isSpot);
            }

            if (light.lightType == LightType.Spot)
            {
                // Cone base
                var baseRadiusSq = math.max(range * range - coneHeight * coneHeight, 0f);
                var baseRadius = math.sqrt(baseRadiusSq);
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
                if (GetProjectedCircleHorizon(baseCenter.yz, baseRadius, baseUY.yz, baseVY.yz, out var baseY1UV, out var baseY2UV))
                {
                    var baseY1 = baseCenter + baseY1UV.x * baseUY + baseY1UV.y * baseVY;
                    var baseY2 = baseCenter + baseY2UV.x * baseUY + baseY2UV.y * baseVY;
                    if (IsInDepthRange(baseY1)) ExpandY(baseY1);
                    if (IsInDepthRange(baseY2)) ExpandY(baseY2);
                }

                // Project cone base into the XZ plane, find the horizon points, and re-construct view space position of found points.
                // See comment for YZ plane for details.
                var baseUX = math.abs(math.abs(lightDirectionVS.y) - 1) < 1e-6f ? math.float3(1, 0, 0) : math.normalize(math.cross(lightDirectionVS, math.float3(0, 1, 0)));
                var baseVX = math.cross(lightDirectionVS, baseUX);
                if (GetProjectedCircleHorizon(baseCenter.xz, baseRadius, baseUX.xz, baseVX.xz, out var baseX1UV, out var baseX2UV))
                {
                    var baseX1 = baseCenter + baseX1UV.x * baseUX + baseX1UV.y * baseVX;
                    var baseX2 = baseCenter + baseX2UV.x * baseUX + baseX2UV.y * baseVX;
                    if (IsInDepthRange(baseX1)) ExpandY(baseX1);
                    if (IsInDepthRange(baseX2)) ExpandY(baseX2);
                }

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
                    var foundY = section.GetYExtremes(out var p0Y, out var p1Y);
                    if (foundY.x && ConicPointIsValid(p0Y)) ExpandY(p0Y);
                    if (foundY.y && ConicPointIsValid(p1Y)) ExpandY(p1Y);

                    var foundX = section.GetXExtremes(out var p0X, out var p1X);
                    if (foundX.x && ConicPointIsValid(p0X)) ExpandY(p0X);
                    if (foundX.y && ConicPointIsValid(p1X)) ExpandY(p1X);
                }

                // Calculate the lines making up the sides of the cone as seen from the camera. `l1` and `l2` form lines
                // from the light position.
                var hasSilhouette = GetConeSideTangentPoints(-lightPositionVS, lightDirectionVS, cosHalfAngle, baseRadius, coneHeight, range, coneU, coneV, out var l1, out var l2);

                // Where the cone sides cross the bottom and top of the screen.
                if (hasSilhouette)
                {
                    ExpandYAtViewPlane(lightPositionVS, l1, viewPlaneBottoms[m_ViewIndex]);
                    ExpandYAtViewPlane(lightPositionVS, l1, viewPlaneTops[m_ViewIndex]);
                    ExpandYAtViewPlane(lightPositionVS, l2, viewPlaneBottoms[m_ViewIndex]);
                    ExpandYAtViewPlane(lightPositionVS, l2, viewPlaneTops[m_ViewIndex]);
                }

                m_TileYRange.Clamp(0, (short)(tileCount.y - 1));

                // Calculate tile plane ranges for cone and sphere.
                for (var planeIndex = m_TileYRange.start; planeIndex <= m_TileYRange.end + 1; planeIndex++)
                {
                    var planeRange = InclusiveRange.empty;

                    // Y-position on the view plane (Z=1)
                    var planeY = math.lerp(viewPlaneBottoms[m_ViewIndex], viewPlaneTops[m_ViewIndex], planeIndex * tileScaleInv.y);

                    ExpandSphereRow(ref planeRange, planeY, sphereRowTangents, lightPositionVS, range, lightDirectionVS, cosHalfAngle, isSpot, sphereCrossesPlane);

                    // Intersect lines with y-plane and clip if needed.
                    if (hasSilhouette)
                    {
                        var planeNormal = math.float3(0, 1, -planeY);
                        ExpandTileXAtRowPlane(ref planeRange, lightPositionVS, l1, planeNormal);
                        ExpandTileXAtRowPlane(ref planeRange, lightPositionVS, l2, planeNormal);
                    }

                    // The row plane through the camera has normal (0, 1, -planeY).
                    var circleLineA = baseUY.y - planeY * baseUY.z;
                    var circleLineB = baseVY.y - planeY * baseVY.z;
                    var circleLineK = baseCenter.y - planeY * baseCenter.z;
                    if (IntersectCircleLine(circleLineA, circleLineB, circleLineK, baseCenter, baseUY, baseVY, baseRadiusSq, out var circleXZ0, out var circleXZ1))
                    {
                        ExpandTileXIfInDepthRange(ref planeRange, circleXZ0);
                        ExpandTileXIfInDepthRange(ref planeRange, circleXZ1);
                    }

                    if (coneIsClipping)
                    {
                        var found = nearSection.IntersectRow(planeY * near, out var p0, out var p1);
                        if (found.x && ConicPointIsValid(p0)) ExpandTileX(ref planeRange, ViewToTileSpace(p0).x);
                        if (found.y && ConicPointIsValid(p1)) ExpandTileX(ref planeRange, ViewToTileSpace(p1).x);
                    }

                    if (coneCrossesFar)
                    {
                        var found = farSection.IntersectRow(planeY * far, out var p0, out var p1);
                        if (found.x && ConicPointIsValid(p0)) ExpandTileX(ref planeRange, ViewToTileSpace(p0).x);
                        if (found.y && ConicPointIsValid(p1)) ExpandTileX(ref planeRange, ViewToTileSpace(p1).x);
                    }

                    MergeTileRangeRows(planeIndex, planeRange);
                }
            }

            m_TileYRange.Clamp(0, (short)(tileCount.y - 1));

            // Spots already swept the sphere together with the cone above.
            if (!isSpot)
            {
                for (var planeIndex = m_TileYRange.start; planeIndex <= m_TileYRange.end + 1; planeIndex++)
                {
                    var planeRange = InclusiveRange.empty;
                    var planeY = math.lerp(viewPlaneBottoms[m_ViewIndex], viewPlaneTops[m_ViewIndex], planeIndex * tileScaleInv.y);
                    ExpandSphereRow(ref planeRange, planeY, sphereRowTangents, lightPositionVS, range, lightDirectionVS, cosHalfAngle, isSpot, sphereCrossesPlane);
                    MergeTileRangeRows(planeIndex, planeRange);
                }
            }

            tileRanges[m_Offset] = m_TileYRange;
        }

        // Every tile's frustum starts on the near-plane rectangle, so a sphere containing it covers all tiles.
        bool SphereContainsNearPlane(float3 center, float radius)
        {
            var viewportToNear = (math.float4(0, 0, 1, 1) - m_ViewToViewportScaleBias.zwzw) / m_ViewToViewportScaleBias.xyxy * near;
            var dx = math.max(math.abs(viewportToNear.x - center.x), math.abs(viewportToNear.z - center.x));
            var dy = math.max(math.abs(viewportToNear.y - center.y), math.abs(viewportToNear.w - center.y));
            return square(dx) + square(dy) + square(near - center.z) <= square(radius);
        }

        void CoverAllTiles()
        {
            var fullRow = new InclusiveRange(0, (short)(tileCount.x - 1));
            for (var row = 0; row < tileCount.y; row++)
                tileRanges[m_Offset + 1 + row] = fullRow;
            tileRanges[m_Offset] = new InclusiveRange(0, (short)(tileCount.y - 1));
        }

        // Where the plane through the origin and the view-plane row at height planeY crosses the outline of the sphere.
        // Runs per row per light; Burst does not inline it on its own.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ExpandSphereRow(ref InclusiveRange planeRange, float planeY, in SphereRowTangents sphereRowTangents, float3 lightPositionVS, float range, float3 lightDirectionVS, float cosHalfAngle, bool isSpot, bool2 sphereCrossesPlane)
        {
            var found = sphereRowTangents.Find(planeY, out var sphereTile0, out var sphereTile1);
            if (found.x) ExpandTileXIfInSphereCap(ref planeRange, sphereTile0, lightPositionVS, lightDirectionVS, cosHalfAngle, isSpot);
            if (found.y) ExpandTileXIfInSphereCap(ref planeRange, sphereTile1, lightPositionVS, lightDirectionVS, cosHalfAngle, isSpot);
            for (var i = 0; i < 2; i++)
            {
                // Checked once per light, so most rows skip the near and far sections entirely.
                if (sphereCrossesPlane[i] && GetSphereSectionRowPoints(lightPositionVS, square(range), i == 0 ? near : far, planeY, out var section0, out var section1))
                {
                    ExpandTileXIfInSphereCap(ref planeRange, section0, lightPositionVS, lightDirectionVS, cosHalfAngle, isSpot);
                    ExpandTileXIfInSphereCap(ref planeRange, section1, lightPositionVS, lightDirectionVS, cosHalfAngle, isSpot);
                }
            }
        }

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
            var denominator = math.dot(line, planeNormal);
            if (denominator == 0f)
                return;
            var t = math.dot(-origin, planeNormal) / denominator;
            var positionVS = origin + line * t;
            if (t >= 0 && t <= 1 && IsInDepthRange(positionVS)) ExpandY(positionVS);
        }

        void ExpandTileXIfInDepthRange(ref InclusiveRange planeRange, float2 xzVS)
        {
            if (IsDepthInRange(xzVS.y)) ExpandTileX(ref planeRange, ViewToTileSpace(math.float3(xzVS.x, 0f, xzVS.y)).x);
        }

        // Expands a row range where the segment from origin to origin + line crosses the row plane through the camera.
        void ExpandTileXAtRowPlane(ref InclusiveRange planeRange, float3 origin, float3 line, float3 planeNormal)
        {
            var denominator = math.dot(line, planeNormal);
            if (denominator == 0f)
                return;
            var t = math.dot(-origin, planeNormal) / denominator;
            var positionVS = origin + line * t;
            if (t >= 0 && t <= 1 && IsInDepthRange(positionVS)) ExpandTileX(ref planeRange, ViewToTileSpace(positionVS).x);
        }
    }
}
