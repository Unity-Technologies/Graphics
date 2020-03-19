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
        [GenerateHLSL(PackingRules.Exact, false, false, true, 1500)]
        public struct SurfaceData
        {
            [SurfaceDataAttributes("MaterialFeatures")]
            public uint materialFeatures;

            [MaterialSharedPropertyMapping(MaterialSharedProperty.Normal)]
            [SurfaceDataAttributes(new string[] { "Normal WS", "Normal View Space" }, true)]
            public Vector3 normalWS;
            [SurfaceDataAttributes(new string[] { "Low Frequency Normal WS", "Low Frequency Normal View Space" }, true)]
            public Vector3 lowFrequencyNormalWS;
            [SurfaceDataAttributes(new string[] { "Geometric Normal WS", "Geometric Normal View Space" }, true)]
            public Vector3 geomNormalWS;

            [MaterialSharedPropertyMapping(MaterialSharedProperty.Albedo)]
            [SurfaceDataAttributes("", false, true)]
            public Vector3 diffuseColor;
            public float diffuseWrapAmount;

            [MaterialSharedPropertyMapping(MaterialSharedProperty.Smoothness)]
            [SurfaceDataAttributes("Smoothness")]
            public float perceptualSmoothness;
            public float specularSelfOcclusion;

            public float anisotropy;
            public float anisotropyIOR;
            public float anisotropyOffset;
            public float anisotropyWeight;

            [SurfaceDataAttributes("", false, false)]
            public Vector3 customRefractionColor;

        };

        //-----------------------------------------------------------------------------
        // BSDFData
        //-----------------------------------------------------------------------------

        [GenerateHLSL(PackingRules.Exact, false, false, true, 1550)]
        public struct BSDFData
        {

            public uint materialFeatures;

            [SurfaceDataAttributes(new string[] { "Normal WS", "Normal View Space" }, true)]
            public Vector3 normalWS;
            [SurfaceDataAttributes(new string[] { "Low Frequency Normal WS", "Low Frequency Normal View Space" }, true)]
            public Vector3 lowFrequencyNormalWS;
            [SurfaceDataAttributes(new string[] { "Geometric Normal WS", "Geometric Normal View Space" }, true)]
            public Vector3 geomNormalWS;

            [SurfaceDataAttributes("", false, true)]
            public Vector3 diffuseColor;
            public float diffuseWrapAmount;

            public float roughness;
            public float perceptualRoughness;

            public Vector3 fresnel0;

            public float specularSelfOcclusion;
            
            public float anisotropy;
            public float anisotropyIOR;
            public float anisotropyOffset;
            public float anisotropyWeight;

            [SurfaceDataAttributes("", false, false)]
            public Vector3 customRefractionColor;

            // MaterialFeature dependent attribute
        };

        //-----------------------------------------------------------------------------
        // Init precomputed textures
        //-----------------------------------------------------------------------------

        public Water() {}

        // Reuse GGX textures
    }
}
