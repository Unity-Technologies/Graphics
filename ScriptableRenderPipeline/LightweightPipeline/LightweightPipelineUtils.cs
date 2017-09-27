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

    [Flags]
    public enum RenderingConfiguration
    {
        None = 0,
        Stereo = (1 << 0),
        Msaa = (1 << 1),
        PostProcess = (1 << 2),
        DefaultViewport = (1 << 3),
        IntermediateTexture = (1 << 4),
        IntermediateTextureArray = (1 << 5),
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

        public static bool HasFlag(RenderingConfiguration mask, RenderingConfiguration flag)
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
