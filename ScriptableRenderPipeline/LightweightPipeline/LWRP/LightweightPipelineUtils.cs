using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class CameraComparer : IComparer<Camera>
    {
        public int Compare(Camera lhs, Camera rhs)
        {
            return (int)(lhs.depth - rhs.depth);
        }
    }

    public class LightComparer : IComparer<VisibleLight>
    {
        public Camera CurrCamera { get; set; }

        public int Compare(VisibleLight lhs, VisibleLight rhs)
        {
            Light lhsLight = lhs.light;
            Light rhsLight = rhs.light;

            // Particle Lights have the Light reference set to null
            // They are at the end of the priority
            if (lhsLight == null) return 1;
            if (rhsLight == null) return -1;

            // Prioritize lights marked as important
            if (lhsLight.renderMode != rhsLight.renderMode)
            {
                if (lhsLight.renderMode == LightRenderMode.ForcePixel) return -1;
                if (rhsLight.renderMode == LightRenderMode.ForcePixel) return 1;
            }

            // Prioritize Directional Lights
            if (lhs.lightType != rhs.lightType)
            {
                if (lhs.lightType == LightType.Directional) return -1;
                if (rhs.lightType == LightType.Directional) return 1;
            }

            // Prioritize Shadows Lights Soft > Hard > None
            if (lhsLight.shadows != rhsLight.shadows)
                return (int)rhsLight.shadows - (int)lhsLight.shadows;

            // Prioritize lights with cookies
            if (lhsLight.cookie != rhsLight.cookie)
                return (lhsLight.cookie != null) ? -1 : 1;

            // If directional sort by intensity
            if (lhs.lightType == LightType.Directional)
            {
                return (int)(rhsLight.intensity * 100.0f) - (int)(lhsLight.intensity * 100.0f);
            }

            // Punctual lights are sorted per-object by the engine based on distance to object center + luminance
            // Here we sort globally the light list per camera distance to fit the closest lights in the global light buffer
            // Check MAX_VISIBLE_LIGHTS in the LightweightLighting.cginc to see the max global buffer list size
            int lhsDistance = (int)(SquaredDistanceToCamera(lhsLight.transform.position) * 100.0f);
            int rhsDistance = (int)(SquaredDistanceToCamera(rhsLight.transform.position) * 100.0f);
            int result = lhsDistance - rhsDistance;
            return result;
        }

        public float SquaredDistanceToCamera(Vector3 lightPos)
        {
            Vector3 lightCameraVector = lightPos - CurrCamera.transform.position;
            return Vector3.Dot(lightCameraVector, lightCameraVector);
        }
    }

    public class LightEqualityComparer : IEqualityComparer<VisibleLight>
    {
        public bool Equals(VisibleLight x, VisibleLight y)
        {
            if (x.light == null && y.light == null)
                return true;

            if (x.light == null || y.light == null)
                return false;

            return x.light.GetInstanceID() == y.light.GetInstanceID();
        }

        public int GetHashCode(VisibleLight obj)
        {
            if (obj.light == null) // Particle light weirdness
                return obj.GetHashCode();

            return obj.light.GetInstanceID();
        }
    }

    [Flags]
    public enum FrameRenderingConfiguration
    {
        None = 0,
        Stereo = (1 << 0),
        Msaa = (1 << 1),
        BeforeTransparentPostProcess = (1 << 2),
        PostProcess = (1 << 3),
        DepthPrePass = (1 << 4),
        DepthCopy = (1 << 5),
        DefaultViewport = (1 << 6),
        IntermediateTexture = (1 << 7)
    }

    public static class LightweightUtils
    {
        public static void GetLightCookieMatrix(VisibleLight light, out Matrix4x4 cookieMatrix)
        {
            cookieMatrix = Matrix4x4.Inverse(light.localToWorld);

            if (light.lightType == LightType.Directional)
            {
                float scale = 1.0f / light.light.cookieSize;

                // apply cookie scale and offset by 0.5 to convert from [-0.5, 0.5] to texture space [0, 1]
                Vector4 row0 = cookieMatrix.GetRow(0);
                Vector4 row1 = cookieMatrix.GetRow(1);
                cookieMatrix.SetRow(0, new Vector4(row0.x * scale, row0.y * scale, row0.z * scale, row0.w * scale + 0.5f));
                cookieMatrix.SetRow(1, new Vector4(row1.x * scale, row1.y * scale, row1.z * scale, row1.w * scale + 0.5f));
            }
            else if (light.lightType == LightType.Spot)
            {
                // we want out.w = 2.0 * in.z / m_CotanHalfSpotAngle
                // c = cotHalfSpotAngle
                // 1 0 0 0
                // 0 1 0 0
                // 0 0 1 0
                // 0 0 2/c 0
                // the "2" will be used to scale .xy for the cookie as in .xy/2 + 0.5
                float scale = 1.0f / light.range;
                float halfSpotAngleRad = Mathf.Deg2Rad * light.spotAngle * 0.5f;
                float cs = Mathf.Cos(halfSpotAngleRad);
                float ss = Mathf.Sin(halfSpotAngleRad);
                float cotHalfSpotAngle = cs / ss;

                Matrix4x4 scaleMatrix = Matrix4x4.identity;
                scaleMatrix.m00 = scaleMatrix.m11 = scaleMatrix.m22 = scale;
                scaleMatrix.m33 = 0.0f;
                scaleMatrix.m32 = scale * (2.0f / cotHalfSpotAngle);

                cookieMatrix = scaleMatrix * cookieMatrix;
            }

            // Remaining light types don't support cookies
        }

        public static bool IsSupportedShadowType(LightType lightType)
        {
            return lightType == LightType.Directional || lightType == LightType.Spot;
        }

        public static bool IsSupportedCookieType(LightType lightType)
        {
            return lightType == LightType.Directional || lightType == LightType.Spot;
        }

        public static bool PlatformSupportsMSAABackBuffer()
        {
#if UNITY_ANDROID || UNITY_IPHONE || UNITY_TVOS || UNITY_SAMSUNGTV
            return true;
#else
            return false;
#endif
        }

        public static bool HasFlag(FrameRenderingConfiguration mask, FrameRenderingConfiguration flag)
        {
            return (mask & flag) != 0;
        }

        public static Mesh CreateQuadMesh(bool uvStartsAtTop)
        {
            float topV, bottomV;
            if (uvStartsAtTop)
            {
                topV = 0.0f;
                bottomV = 1.0f;
            }
            else
            {
                topV = 1.0f;
                bottomV = 0.0f;
            }

            Mesh mesh = new Mesh();
            mesh.vertices = new Vector3[]
            {
                new Vector3(-1.0f, -1.0f, 0.0f),
                new Vector3(-1.0f,  1.0f, 0.0f),
                new Vector3(1.0f, -1.0f, 0.0f),
                new Vector3(1.0f,  1.0f, 0.0f)
            };

            mesh.uv = new Vector2[]
            {
                new Vector2(0.0f, bottomV),
                new Vector2(0.0f, topV),
                new Vector2(1.0f, bottomV),
                new Vector2(1.0f, topV)
            };

            mesh.triangles = new int[] { 0, 1, 2, 2, 1, 3 };
            return mesh;
        }
    }
}
