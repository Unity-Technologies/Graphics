using System;

namespace UnityEngine.Experimental.Rendering
{
    public struct Frustum
    {
        public Plane[]   planes;  // Left, right, top, bottom, near, far
        public Vector3[] corners; // Positions of the 8 corners

        // The frustum will be camera-relative if given a camera-relative VP matrix.
        public static Frustum Create(Matrix4x4 viewProjMatrix, bool depth_0_1, bool reverseZ)
        {
            Frustum frustum = new Frustum();

            frustum.planes  = new Plane[6];
            frustum.corners = new Vector3[8];

            GeometryUtility.CalculateFrustumPlanes(viewProjMatrix, frustum.planes);

            float nd = -1.0f;

            if (depth_0_1)
            {
                nd = 0.0f;

                // See "Fast Extraction of Viewing Frustum Planes" by Gribb and Hartmann.
                Vector3 f  = new Vector3(viewProjMatrix.m20, viewProjMatrix.m21, viewProjMatrix.m22);
                float   s  = (float)(1.0 / Math.Sqrt(f.sqrMagnitude));
                Plane   np = new Plane(s * f, s * viewProjMatrix.m23);

                frustum.planes[4] = np;
            }

            if (reverseZ)
            {
                Plane tmp         = frustum.planes[4];
                frustum.planes[4] = frustum.planes[5];
                frustum.planes[5] = tmp;
            }

            Matrix4x4 invViewProjMatrix = viewProjMatrix.inverse;

            // Unproject 8 frustum points.
            frustum.corners[0] = invViewProjMatrix.MultiplyPoint(new Vector3(-1, -1, 1));
            frustum.corners[1] = invViewProjMatrix.MultiplyPoint(new Vector3( 1, -1, 1));
            frustum.corners[2] = invViewProjMatrix.MultiplyPoint(new Vector3(-1,  1, 1));
            frustum.corners[3] = invViewProjMatrix.MultiplyPoint(new Vector3( 1,  1, 1));
            frustum.corners[4] = invViewProjMatrix.MultiplyPoint(new Vector3(-1, -1, nd));
            frustum.corners[5] = invViewProjMatrix.MultiplyPoint(new Vector3( 1, -1, nd));
            frustum.corners[6] = invViewProjMatrix.MultiplyPoint(new Vector3(-1,  1, nd));
            frustum.corners[7] = invViewProjMatrix.MultiplyPoint(new Vector3( 1,  1, nd));

            return frustum;
        }
    } // struct Frustum

    [GenerateHLSL]
    public struct OrientedBBox
    {
        public Vector3 center;
        public float   extentX;
        public Vector3 right;
        public float   extentY;
        public Vector3 up;
        public float   extentZ;

        public static OrientedBBox Create(Transform t)
        {
            OrientedBBox obb = new OrientedBBox();

            obb.center  = t.position;
            obb.right   = t.right;
            obb.up      = t.up;
            obb.extentX = 0.5f * t.localScale.x;
            obb.extentY = 0.5f * t.localScale.y;
            obb.extentZ = 0.5f * t.localScale.z;

            return obb;
        }
    } // struct OrientedBBox

    public static class GeometryUtils
    {
        // Returns 'true' if the OBB intersects (or is inside) the frustum, 'false' otherwise.
        // 'cameraRelativeOffset' can be used to intersect a world-space OBB with a camera-relative frustum.
        public static bool Overlap(OrientedBBox obb, Vector3 cameraRelativeOffset,
                                   Frustum frustum, int numPlanes, int numCorners)
        {
            Vector3 center  = obb.center + cameraRelativeOffset;
            Vector3 forward = Vector3.Cross(obb.up, obb.right);

            bool overlap = true;

            // Test the OBB against frustum planes. Frustum planes have inward-facing.
            // The OBB is outside if it's entirely behind one of the frustum planes.
            // See "Real-Time Rendering", 3rd Edition, 16.10.2.
            for (int i = 0; overlap && i < numPlanes; i++)
            {
                Vector3 n = frustum.planes[i].normal;
                float   d = frustum.planes[i].distance;

                // Max projection of the half-diagonal onto the normal (always positive).
                float maxHalfDiagProj = obb.extentX * Mathf.Abs(Vector3.Dot(n, obb.right))
                                      + obb.extentY * Mathf.Abs(Vector3.Dot(n, obb.up)) 
                                      + obb.extentZ * Mathf.Abs(Vector3.Dot(n, forward));

                // Negative distance -> center behind the plane (outside).
                float centerToPlaneDist = Vector3.Dot(n, center) + d;

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
            Plane[] planes = new Plane[3];

            planes[0].normal   = obb.right;
            planes[0].distance = obb.extentX;
            planes[1].normal   = obb.up;
            planes[1].distance = obb.extentY;
            planes[2].normal   = forward;
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
                    float proj = Vector3.Dot(plane.normal, frustum.corners[j] - center);
                    outsidePos = outsidePos && ( proj > plane.distance);
                    outsideNeg = outsideNeg && (-proj > plane.distance);
                }

                overlap = overlap && !(outsidePos || outsideNeg);
            }

            return overlap;
        }

        public static readonly Matrix4x4 FlipMatrixLHSRHS = Matrix4x4.Scale(new Vector3(1, 1, -1));

        public static Vector4 Plane(Vector3 position, Vector3 normal)
        {
            var n = normal;
            var d = -Vector3.Dot(n, position);
            var plane = new Vector4(n.x, n.y, n.z, d);
            return plane;
        }

        public static Vector4 CameraSpacePlane(Matrix4x4 worldToCamera, Vector3 pos, Vector3 normal, float sideSign = 1, float clipPlaneOffset = 0)
        {
            var offsetPos = pos + normal * clipPlaneOffset;
            var cpos = worldToCamera.MultiplyPoint(offsetPos);
            var cnormal = worldToCamera.MultiplyVector(normal).normalized * sideSign;
            return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
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
            var c = clipPlane * (2.0f / Vector4.Dot(clipPlane, q));

            projection[2] = c.x - projection[3];
            projection[6] = c.y - projection[7];
            projection[10] = c.z - projection[11];
            projection[14] = c.w - projection[15];

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

        public static Matrix4x4 GetWorldToCameraMatrixLHS(this Camera camera)
        {
            return FlipMatrixLHSRHS * camera.worldToCameraMatrix;
        }

        public static Matrix4x4 GetProjectionMatrixLHS(this Camera camera)
        {
            return camera.projectionMatrix * FlipMatrixLHSRHS;
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
                return Matrix4x4.Perspective(camera.fieldOfView, camera.aspect, camera.nearClipPlane, camera.farClipPlane);
        }
    } // class GeometryUtils
}
