namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public static class CameraUtils
    {
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

        public static Matrix4x4 CalculateWorldToCameraMatrix(Vector3 position, Quaternion rotation)
        {
            return Matrix4x4.Scale(new Vector3(1, 1, -1)) * Matrix4x4.TRS(position, rotation, Vector3.one).inverse;
        }

        public static Matrix4x4 CalculateWorldToCameraMatrix(Transform transform)
        {
            return Matrix4x4.Scale(new Vector3(1, 1, -1)) * transform.localToWorldMatrix.inverse;
        }

        public static Matrix4x4 CalculateObliqueMatrix(Matrix4x4 sourceProjection, Vector4 clipPlane)
        {
            var projection = sourceProjection;
            var inversion = sourceProjection.inverse;

            var cps = new Vector4(
                (clipPlane.x > 0 ? 1 : 0) - (clipPlane.x < 0 ? 1 : 0), 
                (clipPlane.y > 0 ? 1 : 0) - (clipPlane.y < 0 ? 1 : 0), 
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
            return CalculateReflectionMatrix(Plane(position, normal));
        }

        public static Matrix4x4 CalculateWorldToCameraMatrixMirror(Matrix4x4 worldToCamera, Matrix4x4 reflection)
        {
            return  worldToCamera * reflection;
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
    }
}
