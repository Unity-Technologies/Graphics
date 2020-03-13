using System;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal enum Platform
    {
        D3D11,
        GLCore,
        GLES,
        GLES3,
        Metal,
        Vulkan,
        D3D9,
        XboxOne,
        PS4,
        Switch,
    }

    [GenerationAPI]
    internal static class PlatformExtensions
    {
        public static string ToShaderString(this Platform platform)
        {
            switch(platform)
            {
                case Platform.D3D11:
                    return "d3d11";
                case Platform.GLCore:
                    return "glcore";
                case Platform.GLES:
                    return "gles";
                case Platform.GLES3:
                    return "gles3";
                case Platform.Metal:
                    return "metal";
                case Platform.Vulkan:
                    return "vulkan";
                case Platform.D3D9:
                    return "d3d11_9x";
                case Platform.XboxOne:
                    return "xboxone";
                case Platform.PS4:
                    return "ps4";
                case Platform.Switch:
                    return "switch";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
