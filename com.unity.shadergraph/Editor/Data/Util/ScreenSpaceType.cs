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
                case ScreenSpaceType.Raw:
                    return string.Format("IN.{0}", ShaderGeneratorNames.ScreenPosition);
                case ScreenSpaceType.Center:
                    return string.Format("$precision4(IN.{0}.xy / IN.{0}.w * 2 - 1, 0, 0)", ShaderGeneratorNames.ScreenPosition);
                case ScreenSpaceType.Tiled:
                    return string.Format("frac($precision4((IN.{0}.x / IN.{0}.w * 2 - 1) * _ScreenParams.x / _ScreenParams.y, IN.{0}.y / IN.{0}.w * 2 - 1, 0, 0))", ShaderGeneratorNames.ScreenPosition);
                case ScreenSpaceType.Pixel:
                    return string.Format("$precision4(IN.{0}.xy * _ScreenParams.xy / IN.{0}.w, 0, 0)", ShaderGeneratorNames.ScreenPosition);
                default:
                    return string.Format("$precision4(IN.{0}.xy / IN.{0}.w, 0, 0)", ShaderGeneratorNames.ScreenPosition);
            }
        }
    }
}
