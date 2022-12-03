namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Contains structs used for shader input.
    /// </summary>
    public static partial class ShaderInput
    {
        /// <summary>
        /// Container struct for various data used for lights in URP.
        /// </summary>
        [GenerateHLSL(PackingRules.Exact, false)]
        public struct LightData
        {
            /// <summary>
            /// The position of the light.
            /// </summary>
            public Vector4 position;

            /// <summary>
            /// The color of the light.
            /// </summary>
            public Vector4 color;

            /// <summary>
            /// The attenuation of the light.
            /// </summary>
            public Vector4 attenuation;

            /// <summary>
            /// The direction of the light (Spot light).
            /// </summary>
            public Vector4 spotDirection;

            /// <summary>
            /// The channel for probe occlusion.
            /// </summary>
            public Vector4 occlusionProbeChannels;

            /// <summary>
            /// The layer mask used.
            /// </summary>
            public uint layerMask;
        }
    }
}
