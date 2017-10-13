using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

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

        // Sorts on the following priority:
        // Directionals have priority over local lights
        // ShadowLight type
        // Has Cookie
        // Intensity if Directional, Distance to camera otherwise
        public int Compare(VisibleLight lhs, VisibleLight rhs)
        {
            Light lhsLight = lhs.light;
            Light rhsLight = rhs.light;

            if (lhs.lightType != rhs.lightType)
            {
                if (lhs.lightType == LightType.Directional) return -1;
                if (rhs.lightType == LightType.Directional) return 1;
            }

            // In the following priority: Soft, Hard, None
            if (lhsLight.shadows != rhsLight.shadows)
                return (int)rhsLight.shadows - (int)lhsLight.shadows;

            if (lhsLight.cookie != rhsLight.cookie)
                return (lhsLight.cookie != null) ? -1 : 1;

            if (lhs.lightType == LightType.Directional)
                return (int)(lhsLight.intensity*100.0f) - (int)(rhsLight.intensity*100.0f);
            else
                return (int)(SquaredDistanceToCamera(lhsLight.transform.position) - SquaredDistanceToCamera(rhsLight.transform.position));
        }

        public float SquaredDistanceToCamera(Vector3 lightPos)
        {
            Vector3 lightCameraVector = lightPos - CurrCamera.transform.position;
            return Vector3.Dot(lightCameraVector, lightCameraVector);
        }
    }

    [Flags]
    public enum FrameRenderingConfiguration
    {
        None = 0,
        Stereo = (1 << 0),
        Msaa = (1 << 1),
        PostProcess = (1 << 2),
        RequireDepth = (1 << 3),
        DefaultViewport = (1 << 4),
        IntermediateTexture = (1 << 5),
    }

    public static class LightweightUtils
    {
        public static void SetKeyword(CommandBuffer cmd, string keyword, bool enable)
        {
            if (enable)
                cmd.EnableShaderKeyword(keyword);
            else
                cmd.DisableShaderKeyword(keyword);
        }

        public static bool IsSupportedShadowType(LightType lightType)
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
                new Vector3( 1.0f, -1.0f, 0.0f),
                new Vector3( 1.0f,  1.0f, 0.0f)
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
