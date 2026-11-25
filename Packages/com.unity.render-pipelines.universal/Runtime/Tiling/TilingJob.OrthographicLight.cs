using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    // Orthographic point/spot light tiling: the same outline as the perspective case, without a perspective divide.
    partial struct TilingJob
    {
        void TileLightOrthographic(int lightIndex)
        {
            var light = lights[lightIndex];
            if (!HasNonZeroVolume(light))
            {
                return;
            }

            var lightToWorld = (float4x4)light.localToWorldMatrix;
            var lightPosVS = math.mul(worldToViews[m_ViewIndex], math.float4(lightToWorld.c3.xyz, 1)).xyz;
            lightPosVS.z *= -1;
            var range = math.min(light.range, k_MaxDistance);
            if (lightPosVS.z - range > far || lightPosVS.z + range < near)
                return;
            if (IsInDepthRange(lightPosVS)) ExpandOrthographic(lightPosVS);
            var lightDirVS = math.mul(worldToViews[m_ViewIndex], math.float4(lightToWorld.c2.xyz, 0)).xyz;
            lightDirVS.z *= -1;
            lightDirVS = math.normalize(lightDirVS);

            var isSpot = light.lightType == LightType.Spot;
            var halfAngle = math.radians(light.spotAngle * 0.5f);
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
            var hasSilhouette = false;
            var coneDistanceTolerance = 1e-5f * (math.length(lightPosVS) + range);

            if (isSpot)
            {
                // Extremes of the cone base circle, and its points on the near and far planes. An axis along y or x leaves
                // the circle no extent along that axis.
                var circleUp = math.float3(0, 1, 0) - lightDirVS * lightDirVS.y;
                if (math.lengthsq(circleUp) > 0f)
                {
                    circleUp = math.normalize(circleUp) * circleRadius;
                    ExpandOrthographicIfInDepthRange(circleCenter - circleUp);
                    ExpandOrthographicIfInDepthRange(circleCenter + circleUp);
                }
                var circleRight = math.float3(1, 0, 0) - lightDirVS * lightDirVS.x;
                if (math.lengthsq(circleRight) > 0f)
                {
                    circleRight = math.normalize(circleRight) * circleRadius;
                    ExpandOrthographicIfInDepthRange(circleCenter - circleRight);
                    ExpandOrthographicIfInDepthRange(circleCenter + circleRight);
                }
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
                    var foundY = section.GetYExtremes(out var p0Y, out var p1Y);
                    var foundX = section.GetXExtremes(out var p0X, out var p1X);
                    if (foundY.x && IsOnCone(p0Y, lightPosVS, lightDirVS, cosHalfAngle, sinHalfAngle, coneHeight, coneDistanceTolerance)) ExpandOrthographic(p0Y);
                    if (foundY.y && IsOnCone(p1Y, lightPosVS, lightDirVS, cosHalfAngle, sinHalfAngle, coneHeight, coneDistanceTolerance)) ExpandOrthographic(p1Y);
                    if (foundX.x && IsOnCone(p0X, lightPosVS, lightDirVS, cosHalfAngle, sinHalfAngle, coneHeight, coneDistanceTolerance)) ExpandOrthographic(p0X);
                    if (foundX.y && IsOnCone(p1X, lightPosVS, lightDirVS, cosHalfAngle, sinHalfAngle, coneHeight, coneDistanceTolerance)) ExpandOrthographic(p1X);
                }

                // Silhouette lines of the cone as seen along the view direction.
                hasSilhouette = GetConeSideTangentPoints(math.float3(0, 0, -1), lightDirVS, cosHalfAngle, circleRadius, coneHeight, range, coneU, coneV, out l1, out l2);
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
                    var halfWidthSq = rangeSq - square(depth - lightPosVS.z) - square(planeY - lightPosVS.y);
                    if (halfWidthSq < 0f)
                        continue;
                    var halfWidth = math.sqrt(halfWidthSq);
                    ExpandRangeOrthographicIfInSphereCap(ref planeRange, math.float3(lightPosVS.x - halfWidth, planeY, depth), lightPosVS, lightDirVS, cosHalfAngle, isSpot);
                    ExpandRangeOrthographicIfInSphereCap(ref planeRange, math.float3(lightPosVS.x + halfWidth, planeY, depth), lightPosVS, lightDirVS, cosHalfAngle, isSpot);
                }

                if (isSpot)
                {
                    // The row is the plane y = planeY.
                    if (IntersectCircleLine(coneU.y, coneV.y, circleCenter.y - planeY, circleCenter, coneU, coneV, square(circleRadius), out var circle0, out var circle1))
                    {
                        if (IsDepthInRange(circle0.y)) ExpandRangeOrthographic(ref planeRange, circle0.x);
                        if (IsDepthInRange(circle1.y)) ExpandRangeOrthographic(ref planeRange, circle1.x);
                    }

                    if (hasSilhouette)
                    {
                        ExpandRangeOrthographicAtRow(ref planeRange, lightPosVS, l1, planeY);
                        ExpandRangeOrthographicAtRow(ref planeRange, lightPosVS, l2, planeY);
                    }

                    for (var i = 0; i < 2; i++)
                    {
                        if (!coneCrosses[i])
                            continue;
                        var found = (i == 0 ? nearSection : farSection).IntersectRow(planeY, out var p0, out var p1);
                        if (found.x && IsOnCone(p0, lightPosVS, lightDirVS, cosHalfAngle, sinHalfAngle, coneHeight, coneDistanceTolerance)) ExpandRangeOrthographic(ref planeRange, p0.x);
                        if (found.y && IsOnCone(p1, lightPosVS, lightDirVS, cosHalfAngle, sinHalfAngle, coneHeight, coneDistanceTolerance)) ExpandRangeOrthographic(ref planeRange, p1.x);
                    }
                }

                MergeTileRangeRows(planeIndex, planeRange);
            }

            tileRanges[m_Offset] = m_TileYRange;
        }

        // Expands a row range where the segment from origin to origin + line crosses the row plane y = planeY.
        void ExpandRangeOrthographicAtRow(ref InclusiveRange range, float3 origin, float3 line, float planeY)
        {
            if (line.y == 0f)
                return;
            var p = origin + line * ((planeY - origin.y) / line.y);
            if (IsOnSegment(p, origin, line)) ExpandRangeOrthographic(ref range, p.x);
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
    }
}
