namespace UnityEngine.Rendering.Universal
{
    public static partial class ShaderInput
    {
        [GenerateHLSL(PackingRules.Exact, false)]
        public struct LightData
        {
            public Vector4 position;
            public Vector4 color;
            public Vector4 attenuation;
            public Vector4 spotDirection;
            public Vector4 occlusionProbeChannels;
        }

        [GenerateHLSL(PackingRules.Exact, false)]
        public struct ReflectionProbeData
        {
            public Vector4 position;
            public Vector4 boxMin;
            public Vector4 boxMax;
            public Vector4 hdr;
        }
    }
}
