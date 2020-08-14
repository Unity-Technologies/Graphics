namespace UnityEngine.Rendering.Universal
{
    public static class ShaderInput
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

        //[GenerateHLSL(PackingRules.Exact, false)]
        // Currently we do not use this struct in the hlsl side, because worldToShadowMatrix and shadowParams must be stored in arrays of different sizes
        public struct ShadowData // ShadowSliceData
        {
            public Matrix4x4 worldToShadowMatrix; // per-shadow-slice

            // x: shadow strength
            // y: 1 if soft shadows, 0 otherwise
            public Vector4 shadowParams;          // per-casting-light
        }
    }
}
