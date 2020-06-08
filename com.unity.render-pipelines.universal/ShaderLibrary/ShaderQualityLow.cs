namespace UnityEngine.Rendering.Universal
{
    [GenerateHLSL(PackingRules.Exact, false, false, false, 1, true)]
    public struct ShaderOptionsLow
    {
        public static int reflectionProbe = 1;
        public static int blendReflectionProbe = 0;
        public static int boxProjection = 0;
        public static int bumpScale = 0;
        public static int fadeShadows = 0;
    }
}
