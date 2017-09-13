using System.Collections.Generic;
using UnityEngine;
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

    public static class LightweightUtils
    {
        public static void SetKeyword(CommandBuffer cmd, string keyword, bool enable)
        {
            if (enable)
                cmd.EnableShaderKeyword(keyword);
            else
                cmd.DisableShaderKeyword(keyword);
        }

        public static bool PlatformSupportsMSAABackBuffer()
        {
#if UNITY_ANDROID || UNITY_IPHONE || UNITY_TVOS || UNITY_SAMSUNGTV
            return true;
#else
            return false;
#endif
        }
    }
}
