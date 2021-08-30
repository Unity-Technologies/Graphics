using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Frustum class.
    /// </summary>
    public struct Frustum
    {
        /// <summary>
        /// Frustum planes.
        /// In order: left, right, top, bottom, near, far.
        /// </summary>
        public Plane[] planes;  // Left, right, top, bottom, near, far
        /// <summary>
        /// Frustum corner points.
        /// </summary>
        public Vector3[] corners; // Positions of the 8 corners

        static Vector3 IntersectFrustumPlanes(Plane p0, Plane p1, Plane p2)
        {
            Vector3 n0 = p0.normal;
            Vector3 n1 = p1.normal;
            Vector3 n2 = p2.normal;

            float det = Vector3.Dot(Vector3.Cross(n0, n1), n2);
            return (Vector3.Cross(n2, n1) * p0.distance + Vector3.Cross(n0, n2) * p1.distance - Vector3.Cross(n0, n1) * p2.distance) * (1.0f / det);
        }

        /// <summary>
        /// Creates a frustum.
        /// Note: when using a camera-relative matrix, the frustum will be camera-relative.
        /// </summary>
        /// <param name="frustum">Inout frustum.</param>
        /// <param name="viewProjMatrix">View projection matrix from which to build the frustum.</param>
        /// <param name="viewPos">View position of the frustum.</param>
        /// <param name="viewDir">Direction of the frustum.</param>
        /// <param name="nearClipPlane">Near clip plane of the frustum.</param>
        /// <param name="farClipPlane">Far clip plane of the frustum.</param>
        public static void Create(ref Frustum frustum, Matrix4x4 viewProjMatrix, Vector3 viewPos, Vector3 viewDir, float nearClipPlane, float farClipPlane)
        {
            GeometryUtility.CalculateFrustumPlanes(viewProjMatrix, frustum.planes);

            // We need to recalculate the near and far planes otherwise it does not work for oblique projection matrices used for reflection.
            Plane nearPlane = new Plane();
            nearPlane.SetNormalAndPosition(viewDir, viewPos);
            nearPlane.distance -= nearClipPlane;

            Plane farPlane = new Plane();
            farPlane.SetNormalAndPosition(-viewDir, viewPos);
            farPlane.distance += farClipPlane;

            frustum.planes[4] = nearPlane;
            frustum.planes[5] = farPlane;

            // Compute corners from the planes instead of projection matrix. Otherwise you get the same issue with near and far for oblique projection.
            frustum.corners[0] = IntersectFrustumPlanes(frustum.planes[0], frustum.planes[3], frustum.planes[4]);
            frustum.corners[1] = IntersectFrustumPlanes(frustum.planes[1], frustum.planes[3], frustum.planes[4]);
            frustum.corners[2] = IntersectFrustumPlanes(frustum.planes[0], frustum.planes[2], frustum.planes[4]);
            frustum.corners[3] = IntersectFrustumPlanes(frustum.planes[1], frustum.planes[2], frustum.planes[4]);
            frustum.corners[4] = IntersectFrustumPlanes(frustum.planes[0], frustum.planes[3], frustum.planes[5]);
            frustum.corners[5] = IntersectFrustumPlanes(frustum.planes[1], frustum.planes[3], frustum.planes[5]);
            frustum.corners[6] = IntersectFrustumPlanes(frustum.planes[0], frustum.planes[2], frustum.planes[5]);
            frustum.corners[7] = IntersectFrustumPlanes(frustum.planes[1], frustum.planes[2], frustum.planes[5]);
        }
    } // struct Frustum

    [GenerateHLSL]
    struct OrientedBBox
    {
        // 3 x float4 = 48 bytes.
        // TODO: pack the axes into 16-bit UNORM per channel, and consider a quaternionic representation.
        public Vector3 right;
        public float extentX;
        public Vector3 up;
        public float extentY;
        public Vector3 center;
        public float extentZ;

        public Vector3 forward { get { return Vector3.Cross(up, right); } }

        public OrientedBBox(Matrix4x4 trs)
        {
            Vector3 vecX = trs.GetColumn(0);
            Vector3 vecY = trs.GetColumn(1);
            Vector3 vecZ = trs.GetColumn(2);

            center = trs.GetColumn(3);
            right = vecX * (1.0f / vecX.magnitude);
            up = vecY * (1.0f / vecY.magnitude);

            extentX = 0.5f * vecX.magnitude;
            extentY = 0.5f * vecY.magnitude;
            extentZ = 0.5f * vecZ.magnitude;
        }
    } // struct OrientedBBox

    static class GeometryUtils
    {
        // Returns 'true' if the OBB intersects (or is inside) the frustum, 'false' otherwise.
        public unsafe static bool Overlap(OrientedBBox obb, Frustum frustum, int numPlanes, int numCorners)
        {
            bool overlap = true;

            // Test the OBB against frustum planes. Frustum planes are inward-facing.
            // The OBB is outside if it's entirely behind one of the frustum planes.
            // See "Real-Time Rendering", 3rd Edition, 16.10.2.
            for (int i = 0; overlap && i < numPlanes; i++)
            {
                Vector3 n = frustum.planes[i].normal;
                float d = frustum.planes[i].distance;

                // Max projection of the half-diagonal onto the normal (always positive).
                float maxHalfDiagProj = obb.extentX * Mathf.Abs(Vector3.Dot(n, obb.right))
                    + obb.extentY * Mathf.Abs(Vector3.Dot(n, obb.up))
                    + obb.extentZ * Mathf.Abs(Vector3.Dot(n, obb.forward));

                // Positive distance -> center in front of the plane.
                // Negative distance -> center behind the plane (outside).
                float centerToPlaneDist = Vector3.Dot(n, obb.center) + d;

                // outside = maxHalfDiagProj < -centerToPlaneDist
                // outside = maxHalfDiagProj + centerToPlaneDist < 0
                // overlap = overlap && !outside
                overlap = overlap && (maxHalfDiagProj + centerToPlaneDist >= 0);
            }

            if (numCorners == 0) return overlap;

            // Test the frustum corners against OBB planes. The OBB planes are outward-facing.
            // The frustum is outside if all of its corners are entirely in front of one of the OBB planes.
            // See "Correct Frustum Culling" by Inigo Quilez.
            // We can exploit the symmetry of the box by only testing against 3 planes rather than 6.
            var planes = stackalloc Plane[3];

            planes[0].normal = obb.right;
            planes[0].distance = obb.extentX;
            planes[1].normal = obb.up;
            planes[1].distance = obb.extentY;
            planes[2].normal = obb.forward;
            planes[2].distance = obb.extentZ;

            for (int i = 0; overlap && i < 3; i++)
            {
                Plane plane = planes[i];

                // We need a separate counter for the "box fully inside frustum" case.
                bool outsidePos = true; // Positive normal
                bool outsideNeg = true; // Reversed normal

                // Merge 2 loops. Continue as long as all points are outside either plane.
                for (int j = 0; j < numCorners; j++)
                {
                    float proj = Vector3.Dot(plane.normal, frustum.corners[j] - obb.center);
                    outsidePos = outsidePos && (proj > plane.distance);
                    outsideNeg = outsideNeg && (-proj > plane.distance);
                }

                overlap = overlap && !(outsidePos || outsideNeg);
            }

            return overlap;
        }

        public static Vector4 Plane(Vector3 position, Vector3 normal)
        {
            var n = normal;
            var d = -Vector3.Dot(n, position);
            var plane = new Vector4(n.x, n.y, n.z, d);
            return plane;
        }

        public static Vector4 CameraSpacePlane(Matrix4x4 worldToCamera, Vector3 positionWS, Vector3 normalWS, float sideSign = 1, float clipPlaneOffset = 0)
        {
            var offsetPosWS = positionWS + normalWS * clipPlaneOffset;
            var posCS = worldToCamera.MultiplyPoint(offsetPosWS);
            var normalCS = worldToCamera.MultiplyVector(normalWS).normalized * sideSign;
            return new Vector4(normalCS.x, normalCS.y, normalCS.z, -Vector3.Dot(posCS, normalCS));
        }

        public static Matrix4x4 CalculateWorldToCameraMatrixRHS(Vector3 position, Quaternion rotation)
        {
            return Matrix4x4.Scale(new Vector3(1, 1, -1)) * Matrix4x4.TRS(position, rotation, Vector3.one).inverse;
        }

        public static Matrix4x4 CalculateWorldToCameraMatrixRHS(Transform transform)
        {
            return Matrix4x4.Scale(new Vector3(1, 1, -1)) * transform.localToWorldMatrix.inverse;
        }

        public static Matrix4x4 CalculateObliqueMatrix(Matrix4x4 sourceProjection, Vector4 clipPlane)
        {
            var projection = sourceProjection;
            var inversion = sourceProjection.inverse;

            var cps = new Vector4(
                Mathf.Sign(clipPlane.x),
                Mathf.Sign(clipPlane.y),
                1.0f,
                1.0f);
            var q = inversion * cps;
            Vector4 M4 = new Vector4(projection[3], projection[7], projection[11], projection[15]);

            var c = clipPlane * ((2.0f * Vector4.Dot(M4, q)) / Vector4.Dot(clipPlane, q));

            projection[2] = c.x - M4.x;
            projection[6] = c.y - M4.y;
            projection[10] = c.z - M4.z;
            projection[14] = c.w - M4.w;

            return projection;
        }

        public static Matrix4x4 CalculateReflectionMatrix(Vector3 position, Vector3 normal)
        {
            return CalculateReflectionMatrix(Plane(position, normal.normalized));
        }

        public static Matrix4x4 CalculateReflectionMatrix(Vector4 plane)
        {
            var reflectionMat = new Matrix4x4();

            reflectionMat.m00 = (1F - 2F * plane[0] * plane[0]);
            reflectionMat.m01 = (-2F * plane[0] * plane[1]);
            reflectionMat.m02 = (-2F * plane[0] * plane[2]);
            reflectionMat.m03 = (-2F * plane[3] * plane[0]);

            reflectionMat.m10 = (-2F * plane[1] * plane[0]);
            reflectionMat.m11 = (1F - 2F * plane[1] * plane[1]);
            reflectionMat.m12 = (-2F * plane[1] * plane[2]);
            reflectionMat.m13 = (-2F * plane[3] * plane[1]);

            reflectionMat.m20 = (-2F * plane[2] * plane[0]);
            reflectionMat.m21 = (-2F * plane[2] * plane[1]);
            reflectionMat.m22 = (1F - 2F * plane[2] * plane[2]);
            reflectionMat.m23 = (-2F * plane[3] * plane[2]);

            reflectionMat.m30 = 0F;
            reflectionMat.m31 = 0F;
            reflectionMat.m32 = 0F;
            reflectionMat.m33 = 1F;

            return reflectionMat;
        }

        public static bool IsProjectionMatrixOblique(Matrix4x4 projectionMatrix)
        {
            return projectionMatrix[2] != 0 || projectionMatrix[6] != 0;
        }

        public static Matrix4x4 CalculateProjectionMatrix(Camera camera)
        {
            if (camera.orthographic)
            {
                var h = camera.orthographicSize;
                var w = camera.orthographicSize * camera.aspect;
                return Matrix4x4.Ortho(-w, w, -h, h, camera.nearClipPlane, camera.farClipPlane);
            }
            else
                return Matrix4x4.Perspective(camera.GetGateFittedFieldOfView(), camera.aspect, camera.nearClipPlane, camera.farClipPlane);
        }
    } // class GeometryUtils
}
