using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    // Static sphere, circle and cone geometry shared by the perspective and orthographic light tiling.
    partial struct TilingJob
    {
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

        /// <summary>
        /// Finds the points where the tangents from the origin touch a 2D circle. Returns false if the origin is inside it.
        /// </summary>
        static bool GetCircleHorizon(float2 center, float radius, out float2 p0, out float2 p1)
        {
            // With d the distance to the center and l the tangent length, the points are (l^2 * center +- l * r * perp(center)) / d^2.
            var dSq = math.lengthsq(center);
            var lSq = dSq - square(radius);
            if (lSq < 0f || dSq == 0f)
            {
                p0 = p1 = 0;
                return false;
            }

            var l = math.sqrt(lSq);
            var side = l * radius * math.float2(-center.y, center.x);
            p0 = (square(l) * center + side) / dSq;
            p1 = (square(l) * center - side) / dSq;
            return true;
        }

        /// <summary>
        /// Where the rays (x, y, 1) of view-plane rows touch a sphere. A ray d touches it where dot(d, c)^2 = |d|^2 * (|c|^2 - r^2),
        /// a quadratic in x, solved here divided by |c|^2 to keep its coefficients near 1; the point is d * dot(d, c) / |d|^2.
        /// </summary>
        internal struct SphereRowTangents
        {
            float3 m_Center;
            float3 m_UnitCenter;
            float m_TangentFraction;

            public SphereRowTangents(float3 center, float radiusSq)
            {
                var centerLengthSq = math.lengthsq(center);
                var tangentLengthSq = centerLengthSq - radiusSq;
                m_Center = center;
                m_UnitCenter = tangentLengthSq > 0f ? center * math.rsqrt(centerLengthSq) : 0f;
                m_TangentFraction = tangentLengthSq > 0f ? tangentLengthSq / centerLengthSq : 0f;
            }

            /// <summary>
            /// Finds the points touched by the rays of the row at height y. Returns which of the two exist; there are none when
            /// the origin is inside the sphere.
            /// </summary>
            public bool2 Find(float y, out float3 p0, out float3 p1)
            {
                p0 = p1 = 0;
                if (m_TangentFraction <= 0f)
                    return false;

                var m = y * m_UnitCenter.y + m_UnitCenter.z;
                var n = y * y + 1f;
                var found = SolveQuadratic(square(m_UnitCenter.x) - m_TangentFraction, m_UnitCenter.x * m, m * m - m_TangentFraction * n, out var x);
                if (!math.any(found))
                    return found;

                var d0 = math.float3(x.x, y, 1f);
                var d1 = math.float3(x.y, y, 1f);
                p0 = d0 * (math.dot(d0, m_Center) / math.lengthsq(d0));
                p1 = d1 * (math.dot(d1, m_Center) / math.lengthsq(d1));
                return found;
            }
        }

        /// <summary>
        /// Finds the points where the plane through the origin and the view-plane row at height planeY crosses the
        /// circle cut from a sphere by the plane z = depth. Returns false if there are none.
        /// </summary>
        static bool GetSphereSectionRowPoints(float3 center, float radiusSq, float depth, float planeY, out float3 p0, out float3 p1)
        {
            var y = planeY * depth;
            var halfWidthSq = radiusSq - square(depth - center.z) - square(y - center.y);
            if (halfWidthSq < 0f)
            {
                p0 = p1 = 0;
                return false;
            }

            var halfWidth = math.sqrt(halfWidthSq);
            p0 = math.float3(center.x - halfWidth, y, depth);
            p1 = math.float3(center.x + halfWidth, y, depth);
            return true;
        }

        /// <summary>
        /// Finds the two points of intersection of a 3D circle and the plane z = near. Returns false if there are none.
        /// </summary>
        static bool GetCircleClipPoints(float3 circleCenter, float3 circleNormal, float circleRadius, float near, out float3 p0, out float3 p1)
        {
            p0 = p1 = 0;

            // The intersection of two planes is a line where the direction is the cross product of the two plane normals.
            // In this case, it is the plane containing the circle, and the near plane. A circle parallel to the plane has none.
            var cross = math.cross(circleNormal, math.float3(0, 0, 1));
            if (math.lengthsq(cross) == 0f)
                return false;
            var lineDirection = math.normalize(cross);

            // Find a direction on the circle plane towards the nearest point on the intersection line.
            // It has to be perpendicular to the circle normal to be in the circle plane. The direction to the closest
            // point on a line is perpendicular to the line direction. Thus this is given by the cross product of the
            // line direction and the circle normal, as this gives us a vector that is perpendicular to both of those.
            var nearestDirection = math.cross(lineDirection, circleNormal);

            // Distance from circle center to the intersection line along `nearestDirection`.
            // This is done using a ray-plane intersection, where the plane is the near plane.
            // ({0, 0, near} - circleCenter) . {0, 0, 1} / (nearestDirection . {0, 0, 1})
            var distance = (near - circleCenter.z) / nearestDirection.z;
            if (math.abs(distance) > circleRadius)
                return false;

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
            return true;
        }

        /// <summary>
        /// Calculates the horizon of a circle orthogonally projected to a plane as seen from the origin on the plane. Returns
        /// false if the origin is inside the projected ellipse.
        /// </summary>
        /// <param name="center">The center of the circle projected onto the plane.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="U">The major axis of the ellipse formed by the projection of the circle.</param>
        /// <param name="V">The minor axis of the ellipse formed by the projection of the circle.</param>
        /// <param name="uv1">The first horizon point expressed as factors of <paramref name="U"/> and <paramref name="V"/>.</param>
        /// <param name="uv2">The second horizon point expressed as factors of <paramref name="U"/> and <paramref name="V"/>.</param>
        internal static bool GetProjectedCircleHorizon(float2 center, float radius, float2 U, float2 V, out float2 uv1, out float2 uv2)
        {
            // U is assumed to be constructed such that it is never 0, but V can be if the circle projects to a line segment.
            // In that case, the solution can be trivially found using U only.
            var vl = math.length(V);
            if (vl < 1e-6f)
            {
                uv1 = math.float2(radius, 0);
                uv2 = math.float2(-radius, 0);
                return true;
            }

            var ul = math.length(U);

            // Normalize U and V in the plane.
            var u = U / ul;
            var v = V / vl;

            // Major and minor axis of the ellipse.
            var a = ul * radius;
            var b = vl * radius;

            // Scale the ellipse to the unit circle, where the tangent points from the camera q are
            // (q +- sqrt(|q|^2 - 1) * perp(q)) / |q|^2. A camera inside the ellipse has no horizon.
            var q = math.float2(math.dot(-center, u), math.dot(-center, v)) / math.float2(a, b);
            var qq = math.lengthsq(q);
            if (qq < 1f)
            {
                uv1 = uv2 = 0;
                return false;
            }

            var side = math.sqrt(qq - 1f) * math.float2(-q.y, q.x);
            uv1 = (q + side) / qq * radius;
            uv2 = (q - side) / qq * radius;
            return true;
        }

        /// <summary>
        /// Intersects a 3D circle with a plane, given as the line a * s + b * t + k = 0 in the circle's (s, t) coordinates, as the
        /// view-space x and z of the two points. Returns false if the line misses the circle.
        /// </summary>
        static bool IntersectCircleLine(float a, float b, float k, float3 circleCenter, float3 circleU, float3 circleV, float circleRadiusSq, out float2 xz0, out float2 xz1)
        {
            xz0 = xz1 = 0;

            // The points are (-k * (a, b) +- sqrt(r^2 * (a^2 + b^2) - k^2) * (-b, a)) / (a^2 + b^2). Planes parallel to the circle
            // (a = b = 0) give the whole circle or nothing, which the other bounds cover.
            var lengthSq = a * a + b * b;
            var discriminant = circleRadiusSq * lengthSq - k * k;
            if (lengthSq == 0f || discriminant < 0f)
                return false;

            var root = math.sqrt(discriminant);
            var invLengthSq = math.rcp(lengthSq);
            var s = (-k * a + math.float2(-root, root) * b) * invLengthSq;
            var t = (-k * b + math.float2(root, -root) * a) * invLengthSq;
            xz0 = circleCenter.xz + s.x * circleU.xz + t.x * circleV.xz;
            xz1 = circleCenter.xz + s.y * circleU.xz + t.y * circleV.xz;
            return true;
        }

        /// <summary>
        /// Finds the cone's silhouette lines from its vertex, as seen along toCamera: the vector from the vertex to the
        /// camera, or the direction towards the camera for an orthographic projection. Returns false if there are none.
        /// </summary>
        static bool GetConeSideTangentPoints(float3 toCamera, float3 axis, float cosHalfAngle, float circleRadius, float coneHeight, float range, float3 circleU, float3 circleV, out float3 l1, out float3 l2)
        {
            l1 = l2 = 0;

            if (math.lengthsq(toCamera) == 0f || math.dot(math.normalize(toCamera), axis) >= cosHalfAngle)
            {
                return false;
            }

            // Silhouette generators satisfy dot(e, w.xy) = tan * w.z for unit e in the u/v plane, w = toCamera in the cone frame.
            // Solved directly so it stays stable when the camera lies near the plane through the vertex.
            var w = math.float3(math.dot(toCamera, circleU), math.dot(toCamera, circleV), math.dot(toCamera, axis));
            var distToAxisSq = math.lengthsq(w.xy);
            if (distToAxisSq == 0f)
            {
                return false;
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
            return true;
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
            /// Finds the points with extreme y. Returns which of the two exist.
            /// </summary>
            public bool2 GetYExtremes(out float3 p0, out float3 p1)
            {
                var d = m_Axis;
                var found = SolveQuadratic(d.z * d.z - m_SinSq, -d.y * d.z * m_Z, m_Z * m_Z * (d.y * d.y - m_SinSq), out var y);
                var denominator = d.x * d.x - m_CosSq;
                if (denominator == 0f)
                {
                    p0 = p1 = 0;
                    return false;
                }

                var x = -d.x * (d.y * y + d.z * m_Z) / denominator;
                p0 = ToView(x.x, y.x);
                p1 = ToView(x.y, y.y);
                return found;
            }

            /// <summary>
            /// Finds the points with extreme x. Returns which of the two exist.
            /// </summary>
            public bool2 GetXExtremes(out float3 p0, out float3 p1)
            {
                var d = m_Axis;
                var found = SolveQuadratic(d.z * d.z - m_SinSq, -d.x * d.z * m_Z, m_Z * m_Z * (d.x * d.x - m_SinSq), out var x);
                var denominator = d.y * d.y - m_CosSq;
                if (denominator == 0f)
                {
                    p0 = p1 = 0;
                    return false;
                }

                var y = -d.y * (d.x * x + d.z * m_Z) / denominator;
                p0 = ToView(x.x, y.x);
                p1 = ToView(x.y, y.y);
                return found;
            }

            /// <summary>
            /// Finds the points at view-space height y. Returns which of the two exist.
            /// </summary>
            public bool2 IntersectRow(float yVS, out float3 p0, out float3 p1)
            {
                var d = m_Axis;
                var y = yVS - m_Vertex.y;
                var k = d.y * y + d.z * m_Z;
                var found = SolveQuadratic(d.x * d.x - m_CosSq, d.x * k, k * k - m_CosSq * (y * y + m_Z * m_Z), out var x);
                p0 = ToView(x.x, y);
                p1 = ToView(x.y, y);
                return found;
            }

            float3 ToView(float x, float y) => math.float3(m_Vertex.xy + math.float2(x, y), m_Depth);
        }

        /// <summary>
        /// Finds the roots of a * t^2 + 2 * b * t + c = 0 and returns which of the two exist: none for a negative
        /// discriminant, and only c / q if a is 0.
        /// </summary>
        static bool2 SolveQuadratic(float a, float b, float c, out float2 roots)
        {
            roots = 0;
            var discriminant = b * b - a * c;
            if (discriminant < 0f)
                return false;

            // Avoids the cancellation in -b + sqrt(b^2 - ac) by computing that root as c / q.
            var q = -(b + (b < 0f ? -math.sqrt(discriminant) : math.sqrt(discriminant)));
            var found = math.bool2(a != 0f, q != 0f);
            roots = math.select(0f, math.float2(q, c) / math.select(1f, math.float2(a, q), found), found);
            return found;
        }
    }
}
