namespace UnityEngine.Rendering.Universal
{
    [GenerateHLSL(PackingRules.Exact, false, false, false, 1, true)]
    public struct ShaderOptionsHigh
    {
        public static int reflectionProbe = 1;
        public static int blendReflectionProbe = 1;
        public static int bumpScale = 1;
    }
}
