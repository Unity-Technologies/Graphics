namespace UnityEditor.ShaderGraph
{
    enum ScreenSpaceType
    {
        Default,        // screenpos.xy / w ==>  [0, 1] across screen
        Raw,            // screenpos.xyzw ==> scales on distance, requires divide by w
        Center,         // Default, but remapped to [-1, 1]
        Tiled,          // frac(Center)
        Pixel           // Default * _ScreenParams.xy;   [0 .. width-1, 0.. height-1]
    };

    static class ScreenSpaceTypeExtensions
    {
        public static string ToValueAsVariable(this ScreenSpaceType screenSpaceType)
        {
            switch (screenSpaceType)
            {
                case ScreenSpaceType.Raw: // for backwards compatibility we return ScreenPosition here
                    return string.Format("IN.{0}", ShaderGeneratorNames.ScreenPosition);
                case ScreenSpaceType.Center:
                    return string.Format("$precision4(IN.{0}.xy * 2 - 1, 0, 0)", ShaderGeneratorNames.NDCPosition);
                case ScreenSpaceType.Tiled:
                    return string.Format("frac($precision4((IN.{0}.x * 2 - 1) * _ScreenParams.x / _ScreenParams.y, IN.{0}.y * 2 - 1, 0, 0))", ShaderGeneratorNames.NDCPosition);
                case ScreenSpaceType.Pixel:
                    return string.Format("$precision4(IN.{0}.xy, 0, 0)", ShaderGeneratorNames.PixelPosition);
                default: // ScreenSpaceType.Default (i.e. Normalized Device Coordinates)
                    return string.Format("$precision4(IN.{0}.xy, 0, 0)", ShaderGeneratorNames.NDCPosition);
            }
        }

        public static bool RequiresScreenPosition(this ScreenSpaceType screenSpaceType)
        {
            return (screenSpaceType == ScreenSpaceType.Raw);
        }

        public static bool RequiresNDCPosition(this ScreenSpaceType screenSpaceType)
        {
            return (screenSpaceType == ScreenSpaceType.Center) || (screenSpaceType == ScreenSpaceType.Tiled) || (screenSpaceType == ScreenSpaceType.Default);
        }

        public static bool RequiresPixelPosition(this ScreenSpaceType screenSpaceType)
        {
            return (screenSpaceType == ScreenSpaceType.Pixel);
        }
    }
}
