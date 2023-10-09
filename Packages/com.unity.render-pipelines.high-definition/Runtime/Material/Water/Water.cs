using UnityEngine.Rendering.HighDefinition.Attributes;

//-----------------------------------------------------------------------------
// structure definition
//-----------------------------------------------------------------------------
namespace UnityEngine.Rendering.HighDefinition
{
    class Water : RenderPipelineMaterial
    {
        [GenerateHLSL(PackingRules.Exact)]
        public enum MaterialFeatureFlags
        {
            WaterStandard = 1 << 0,
            WaterCinematic = 1 << 1
        };

        //-----------------------------------------------------------------------------
        // SurfaceData
        //-----------------------------------------------------------------------------

        // Main structure that store the user data (i.e user input of master node in material graph)
        [GenerateHLSL(PackingRules.Exact, false, false, true, 1600)]
        public struct SurfaceData
        {
            // Base color on the water
            [MaterialSharedPropertyMapping(MaterialSharedProperty.Albedo)]
            [SurfaceDataAttributes("Base Color", false, true)]
            public Vector3 baseColor;

            // Complete normal signal
            [MaterialSharedPropertyMapping(MaterialSharedProperty.Normal)]
            [SurfaceDataAttributes(new string[] { "Normal WS", "Normal View Space" }, true, checkIsNormalized: true)]
            public Vector3 normalWS;

            // Low frequency normal signal
            [SurfaceDataAttributes(new string[] { "Low Frequency Normal WS", "Low Frequency Normal View Space" }, true, checkIsNormalized: true)]
            public Vector3 lowFrequencyNormalWS;

            // Perceptual smoothness on the surface of the water
            [MaterialSharedPropertyMapping(MaterialSharedProperty.Smoothness)]
            [SurfaceDataAttributes("Smoothness")]
            public float perceptualSmoothness;

            // Foam value on the surface of the water
            [SurfaceDataAttributes("Foam")]
            public float foam;

            // Thickness of the waves
            public float tipThickness;

            // Underwater caustics
            [SurfaceDataAttributes("Caustics")]
            public float caustics;

            [SurfaceDataAttributes("Refracted Position WS")]
            public Vector3 refractedPositionWS;
        }

        //-----------------------------------------------------------------------------
        // BSDFData
        //-----------------------------------------------------------------------------

        [GenerateHLSL(PackingRules.Exact, false, false, true, 1650)]
        public struct BSDFData
        {
            // Base color on the water
            [SurfaceDataAttributes("", false, true)]
            public Vector3 diffuseColor;

            // Fresnel0 of the water surface
            public Vector3 fresnel0;

            // Complete normal signal
            [SurfaceDataAttributes(new string[] { "Normal WS", "Normal View Space" }, true, checkIsNormalized: true)]
            public Vector3 normalWS;

            // Low frequency normal signal
            [SurfaceDataAttributes(new string[] { "Low Frequency Normal WS", "Low Frequency Normal View Space" }, true)]
            public Vector3 lowFrequencyNormalWS;

            // Perceptual smoothness on the surface of the water
            public float perceptualRoughness;

            // Linear smoothness on the surface of the water
            public float roughness;

            // Underwater caustics
            public float caustics;

            // Foam Value
            public float foam;

            // Foam Value
            public Vector3 foamColor;

            // Thickness of the waves
            public float tipThickness;

            public uint frontFace;

            // Integer that allows us to track which water surface this is
            public uint surfaceIndex;
        }

        public Water() { }
    }
}
