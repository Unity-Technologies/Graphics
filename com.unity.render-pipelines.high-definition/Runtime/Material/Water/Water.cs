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
            // Standard
            [MaterialSharedPropertyMapping(MaterialSharedProperty.Albedo)]
            [SurfaceDataAttributes("Base Color", false, true)]
            public Vector3 baseColor;

            [MaterialSharedPropertyMapping(MaterialSharedProperty.Normal)]
            [SurfaceDataAttributes(new string[] { "Normal WS", "Normal View Space" }, true, checkIsNormalized: true)]
            public Vector3 normalWS;

            [SurfaceDataAttributes(new string[] { "Low Frequency Normal WS", "Low Frequency Normal View Space" }, true, checkIsNormalized: true)]
            public Vector3 lowFrequencyNormalWS;

            [MaterialSharedPropertyMapping(MaterialSharedProperty.Smoothness)]
            [SurfaceDataAttributes("Smoothness")]
            public float perceptualSmoothness;

            [SurfaceDataAttributes("Foam")]
            public float foam;

            public float tipThickness;

            [SurfaceDataAttributes("Refraction Color")]
            public Vector3 refractionColor;
        }

        //-----------------------------------------------------------------------------
        // BSDFData
        //-----------------------------------------------------------------------------

        [GenerateHLSL(PackingRules.Exact, false, false, true, 1650)]
        public struct BSDFData
        {
            [SurfaceDataAttributes("", false, true)]
            public Vector3 diffuseColor;

            public Vector3 fresnel0;

            [SurfaceDataAttributes(new string[] { "Normal WS", "Normal View Space" }, true, checkIsNormalized: true)]
            public Vector3 normalWS;
            [SurfaceDataAttributes(new string[] { "Low Frequency Normal WS", "Low Frequency Normal View Space" }, true)]
            public Vector3 lowFrequencyNormalWS;

            public float perceptualRoughness;
            public float roughness;

            public Vector3 refractionColor;

            public float foam;

            public float tipThickness;
        }

        public Water() { }
    }
}
