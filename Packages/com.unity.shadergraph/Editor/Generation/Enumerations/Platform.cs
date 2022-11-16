using System;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal enum Platform
    {
        D3D11,
        GLCore,
        GLES3,
        Metal,
        Vulkan,
        D3D9,
        XboxOne,
        GameCoreXboxOne,
        GameCoreXboxSeries,
        Playstation,
        Switch,
        PS5,
    }

    [GenerationAPI]
    internal static class PlatformExtensions
    {
        public static string ToShaderString(this Platform platform)
        {
            switch (platform)
            {
                case Platform.D3D11:
                    return "d3d11";
                case Platform.GLCore:
                    return "glcore";
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
                case Platform.GameCoreXboxOne:
                    return "xboxone";
                case Platform.GameCoreXboxSeries:
                    return "xboxseries";
                case Platform.Playstation:
                    return "playstation";
                case Platform.Switch:
                    return "switch";
                case Platform.PS5:
                    return "ps5";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    internal static class PragmaRenderers
    {
        // Return high end platform list for the only renderer directive (The list use by HDRP)
        internal static Platform[] GetHighEndPlatformArray()
        {
            return new Platform[] { Platform.D3D11, Platform.Playstation, Platform.XboxOne, Platform.GameCoreXboxSeries, Platform.Vulkan, Platform.Metal, Platform.Switch };
        }

        // Return platform list not compatible with DXC (The list use by HDRP)
        internal static Platform[] GetNeverUseDXCPlatformArray()
        {
            return new Platform[] { Platform.Metal };
        }
    }
}
