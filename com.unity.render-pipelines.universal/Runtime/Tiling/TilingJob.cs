using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    struct TilingJob : IJobFor
    {
        [ReadOnly]
        public NativeArray<VisibleLight> lights;

        [ReadOnly]
        public NativeArray<LightMinMaxZ> minMaxZs;

        [NativeDisableParallelForRestriction]
        public NativeArray<InclusiveRange> tileRanges;

        public int itemsPerLight;

        public float4x4 worldToViewMatrix;

        public float viewTileScale;
        public float viewTileOffset;
        public float near;

        InclusiveRange m_TileYRange;
        int m_Offset;

        public void Execute(int index)
        {
            m_Offset = index * itemsPerLight;
            var light = lights[index];
            var minMaxZ = minMaxZs[index];
            var near = this.near;

            m_TileYRange = new InclusiveRange(short.MaxValue, short.MinValue);

            var lightToWorld = (float4x4)light.localToWorldMatrix;
            var lightPositionVS = math.mul(worldToViewMatrix, math.float4(lightToWorld.c3.xyz, 1)).xyz;
            if (lightPositionVS.z >= near) ExpandY(lightPositionVS);
            var lightDirectionVS = math.mul(worldToViewMatrix, math.float4(lightToWorld.c2.xyz, 0)).xyz;

            var halfAngle = math.radians(light.spotAngle * 0.5f);
            var range = light.range;
            var rangesq = pow2(range);
            var rangeinv = math.rcp(range);
            var cosHalfAngle = math.cos(halfAngle);
            var coneHeight = cosHalfAngle * range;

            // Cone base
            var baseRadius = math.sqrt(range * range - coneHeight * coneHeight);
            var baseCenter = lightPositionVS + lightDirectionVS * coneHeight;

            // Radius of circle formed by intersection of sphere and near plane.
            // Found using Pythagoras with a right triangle formed by three points:
            // (a) light position
            // (b) light position projected to near plane
            // (c) a point on the near plane at a distance `range` from the light position
            //     (i.e. lies both on the sphere and the near plane)
            // Thus the hypotenuse is formed by (a) and (c) with length `range`, and the known side is formed
            // by (a) and (b) with length equal to the distance between the near plane and the light position.
            // The remaining unknown side is formed by (b) and (c) with length equal to the radius of the circle.
            var sphereClipRadius = math.sqrt(pow2(range) - pow2(near - lightPositionVS.z));

            // Assumes a point on the sphere, i.e. at distance `range` from the light position.
            // If spot light, we check the angle between the direction vector from the light position and the light direction vector.
            // Note that division by range is to normalize the vector, as we know that the resulting vector will have length `range`.
            bool SpherePointIsValid(float3 p) => light.lightType == LightType.Point ||
                math.dot((p - lightPositionVS) * rangeinv, lightDirectionVS) >= cosHalfAngle;

            // Project light sphere onto YZ plane, find the horizon points, and re-construct view space position of found points.
            GetSphereHorizon(lightPositionVS.yz, range, near, sphereClipRadius, out var sphereBoundYZ0, out var sphereBoundYZ1);
            var sphereBoundY0 = math.float3(lightPositionVS.x, sphereBoundYZ0);
            var sphereBoundY1 = math.float3(lightPositionVS.x, sphereBoundYZ1);
            if (SpherePointIsValid(sphereBoundY0)) ExpandY(sphereBoundY0);
            if (SpherePointIsValid(sphereBoundY1)) ExpandY(sphereBoundY1);

            // Project light sphere onto XZ plane, find the horizon points, and re-construct view space position of found points.
            GetSphereHorizon(lightPositionVS.xz, range, near, sphereClipRadius, out var sphereBoundXZ0, out var sphereBoundXZ1);
            var sphereBoundX0 = math.float3(sphereBoundXZ0.x, lightPositionVS.y, sphereBoundXZ0.y);
            var sphereBoundX1 = math.float3(sphereBoundXZ0.x, lightPositionVS.y, sphereBoundXZ1.y);
            if (SpherePointIsValid(sphereBoundX0)) ExpandX(sphereBoundX0);
            if (SpherePointIsValid(sphereBoundX1)) ExpandX(sphereBoundX1);

            if (light.lightType == LightType.Spot)
            {
                // Project cone base into the YZ plane, find the horizon points, and re-construct view space position of found points.
                // When projecting a circle to a plane, it becomes an ellipse where the major axis is parallel to the line
                // of intersection of the projection plane and the circle plane. We can get this by taking the cross product
                // of the two plane normals, as the line of intersection will have to be a vector in both planes, and thus
                // orthogonal to both normals.
                // If the two plane normals are parallel, the cross product would return 0. In that case, the circle will
                // project to a line segment, so we pick a vector in the plane pointing in the direction we're interested
                // in finding horizon points in.
                var baseUY = math.abs(math.abs(lightDirectionVS.x) - 1) < 1e-6f ? math.float3(0, 1, 0) : math.normalize(math.cross(lightDirectionVS, math.float3(1, 0, 0)));
                var baseVY = math.cross(lightDirectionVS, baseUY);
                GetProjectedCircleHorizon(baseCenter.xz, baseRadius, baseUY.xz, baseVY.xz, out var baseY1UV, out var baseY2UV);
                var baseY1 = baseCenter + baseY1UV.x * baseUY + baseY1UV.y * baseVY;
                var baseY2 = baseCenter + baseY2UV.x * baseUY + baseY2UV.y * baseVY;
                if (baseY1.z >= near) ExpandY(baseY1);
                if (baseY2.z >= near) ExpandY(baseY2);

                // Project cone base into the XZ plane, find the horizon points, and re-construct view space position of found points.
                var baseUX = math.abs(math.abs(lightDirectionVS.y) - 1) < 1e-6f ? math.float3(1, 0, 0) : math.normalize(math.cross(lightDirectionVS, math.float3(0, 1, 0)));
                var baseVX = math.cross(lightDirectionVS, baseUX);
                GetProjectedCircleHorizon(baseCenter.xz, baseRadius, baseUX.xz, baseVX.xz, out var baseX1UV, out var baseX2UV);
                var baseX1 = baseCenter + baseX1UV.x * baseUX + baseX1UV.y * baseVX;
                var baseX2 = baseCenter + baseX2UV.x * baseUX + baseX2UV.y * baseVX;
                if (baseX1.z >= near) ExpandX(baseX1);
                if (baseX2.z >= near) ExpandX(baseX2);

                // Handle base circle clipping by intersecting it with the near-plane if needed.
                if (GetCircleClipPoints(baseCenter, lightDirectionVS, baseRadius, near, out var baseClip0, out var baseClip1))
                {
                    ExpandY(baseClip0);
                    ExpandY(baseClip1);
                }
            }

            for (var tileY = m_TileYRange.start; tileY <= m_TileYRange.end; tileY++)
            {
                var rowXRange = tileRanges[m_Offset + tileY];

                tileRanges[m_Offset + tileY] = rowXRange;
            }

            tileRanges[m_Offset] = m_TileYRange;
        }

        /// <summary>
        /// Project onto Z=1, scale and offset into [0, tileCount]
        /// </summary>
        float2 ViewToTileSpace(float3 positionVS)
        {
            return positionVS.xy / positionVS.z * viewTileScale + viewTileOffset;
        }

        /// <summary>
        /// Expands the tile Y range and the X range in the row containing the position.
        /// </summary>
        void ExpandY(float3 positionVS)
        {
            var positionTS = ViewToTileSpace(positionVS);
            var tileY = (short)positionTS.y;
            m_TileYRange.Expand(tileY);
            var rowXRange = tileRanges[m_Offset + tileY];
            rowXRange.Expand((short)positionTS.x);
            tileRanges[m_Offset + tileY] = rowXRange;
        }

        /// <summary>
        /// Expands the X range in the row containing the position.
        /// </summary>
        void ExpandX(float3 positionVS)
        {
            var positionTS = ViewToTileSpace(positionVS);
            var tileY = (short)positionTS.y;
            var rowXRange = tileRanges[m_Offset + tileY];
            rowXRange.Expand((short)positionTS.x);
            tileRanges[m_Offset + tileY] = rowXRange;
        }

        static float pow2(float x) => x * x;

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

            // Circle horizon points
            p0 = c + math.float2(-direction.y, direction.x) * h;
            p1 = c + math.float2(direction.y, -direction.x) * h;

            // Handle clipping
            if (math.lengthsq(p0) < pow2(radius) || p0.y < near) p0 = math.float2(center.x + clipRadius, near);
            if (math.lengthsq(p1) < pow2(radius) || p1.y < near) p1 = math.float2(center.x - clipRadius, near);
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
            var chordHalfLength = math.sqrt(pow2(circleRadius) - pow2(distance));
            p0 = nearestPoint + lineDirection * chordHalfLength;
            p1 = nearestPoint - lineDirection * chordHalfLength;

            return math.abs(distance) <= circleRadius;
        }

        static (float, float) IntersectEllipseLine(float a, float b, float3 line)
        {
            var qa = 1f / pow2(a) + pow2(line.x) / (pow2(line.y) * pow2(b));
            var qb = 2f * line.x * line.z / (pow2(line.y) * pow2(b));
            var qc = line.z * line.z / (pow2(line.y) * pow2(b)) - 1f;
            var sqrtD = math.sqrt(qb * qb - 4f * qa * qc);
            var x1 = (-qb + sqrtD) / (2f * qa);
            var x2 = (-qb - sqrtD) / (2f * qa);
            return (x1, x2);
        }

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
                var polar = math.float3(cameraUV.x / pow2(a), cameraUV.y / pow2(b), -1);
                var (t1, t2) = IntersectEllipseLine(a, b, polar);

                // Find Y by putting polar into line equation and solving. Denormalize by dividing by U and V lengths.
                uv1 = math.float2(t1 * ulinv, (-polar.x / polar.y * t1 - polar.z / polar.y) * vlinv);
                uv2 = math.float2(t2 * ulinv, (-polar.x / polar.y * t2 - polar.z / polar.y) * vlinv);
            }
        }
    }
}
