namespace UnityEditor.ShaderGraph
{
    enum ScreenSpaceType
    {
        Default,
        Raw,
        Center,
        Tiled
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
                default:
                    return string.Format("$precision4(IN.{0}.xy / IN.{0}.w, 0, 0)", ShaderGeneratorNames.ScreenPosition);
            }
        }
    }
}
