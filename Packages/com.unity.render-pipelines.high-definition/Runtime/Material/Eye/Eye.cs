using UnityEngine.Rendering.HighDefinition.Attributes;

//-----------------------------------------------------------------------------
// structure definition
//-----------------------------------------------------------------------------
namespace UnityEngine.Rendering.HighDefinition
{
    class Eye : RenderPipelineMaterial
    {
        [GenerateHLSL(PackingRules.Exact)]
        public enum MaterialFeatureFlags
        {
            EyeCinematic = 1 << 0,
            EyeSubsurfaceScattering = 1 << 1,
            EyeCausticFromLUT = 1 << 2
        };

        //-----------------------------------------------------------------------------
        // SurfaceData
        //-----------------------------------------------------------------------------

        // Main structure that store the user data (i.e user input of master node in material graph)
        [GenerateHLSL(PackingRules.Exact, false, false, true, 1500)]
        public struct SurfaceData
        {
            [SurfaceDataAttributes("Material Features")]
            public uint materialFeatures;

            // Standard
            [MaterialSharedPropertyMapping(MaterialSharedProperty.Albedo)]
            [SurfaceDataAttributes("Base Color", false, true)]
            public Vector3 baseColor;

            [MaterialSharedPropertyMapping(MaterialSharedProperty.Normal)]
            [SurfaceDataAttributes(new string[] { "Normal", "Normal View Space" }, true, checkIsNormalized = true)]
            public Vector3 normalWS;

            [SurfaceDataAttributes(new string[] { "Iris Normal", "Iris Normal View Space" }, true, checkIsNormalized = true)]
            public Vector3 irisNormalWS;

            [SurfaceDataAttributes(new string[] { "Geometric Normal", "Geometric Normal View Space" }, true, checkIsNormalized = true)]
            public Vector3 geomNormalWS;

            [MaterialSharedPropertyMapping(MaterialSharedProperty.Smoothness)]
            [SurfaceDataAttributes("Smoothness")]
            public float perceptualSmoothness;

            [MaterialSharedPropertyMapping(MaterialSharedProperty.AmbientOcclusion)]
            [SurfaceDataAttributes("Ambient Occlusion")]
            public float ambientOcclusion;

            [SurfaceDataAttributes("Specular Occlusion")]
            public float specularOcclusion;

            [MaterialSharedPropertyMapping(MaterialSharedProperty.Specular)]
            [SurfaceDataAttributes("IOR", false, true)]
            public float IOR;

            [SurfaceDataAttributes("Mask", false, true)]
            public Vector2 mask; // cornea, Pupil

            // SSS
            [SurfaceDataAttributes("Diffusion Profile Hash")]
            public uint diffusionProfileHash;

            [SurfaceDataAttributes("Subsurface Mask")]
            public float subsurfaceMask;

            [SurfaceDataAttributes("Iris Plane Offset")]
            public float irisPlaneOffset;

            [SurfaceDataAttributes("Iris Radius")]
            public float irisRadius;

            [SurfaceDataAttributes("Caustic intensity multiplier")]
            public float causticIntensity;

            [SurfaceDataAttributes("Blending factor between caustic and cinematic diffuse")]
            public float causticBlend;
        };

        //-----------------------------------------------------------------------------
        // BSDFData
        //-----------------------------------------------------------------------------

        [GenerateHLSL(PackingRules.Exact, false, false, true, 1550)]
        public struct BSDFData
        {
            public uint materialFeatures;

            [SurfaceDataAttributes("", false, true)]
            public Vector3 diffuseColor;
            public Vector3 fresnel0;
            public float IOR; // Keep IOR value

            public float ambientOcclusion;
            public float specularOcclusion;

            [SurfaceDataAttributes(new string[] { "Normal WS", "Normal View Space" }, true, checkIsNormalized = true)]
            public Vector3 normalWS; // Specular normal

            [SurfaceDataAttributes(new string[] { "Diffuse Normal WS", "Diffuse Normal View Space" }, true, checkIsNormalized = true)]
            public Vector3 diffuseNormalWS;

            [SurfaceDataAttributes(new string[] { "Geometric Normal", "Geometric Normal View Space" }, true, checkIsNormalized = true)]
            public Vector3 geomNormalWS;

            public float perceptualRoughness;

            public Vector2 mask; // cornea, pupil

            // MaterialFeature dependent attribute
            public float irisPlaneOffset;
            public float irisRadius;
            public float causticIntensity;
            public float causticBlend;

            // SSS
            public uint diffusionProfileIndex;
            public float subsurfaceMask;

            public float roughness;
        };

        //-----------------------------------------------------------------------------
        // Init precomputed textures
        //-----------------------------------------------------------------------------

        private Texture3D m_EyeCausticLUT;

        public static readonly int _PreIntegratedEyeCaustic = Shader.PropertyToID("_PreIntegratedEyeCaustic");

        public Eye() { }

        public override void Build(HDRenderPipelineAsset hdAsset, HDRenderPipelineRuntimeResources defaultResources)
        {
            m_EyeCausticLUT = defaultResources.textures.eyeCausticLUT;
        }

        public override void Cleanup()
        {
            m_EyeCausticLUT = null;
        }

        public override void RenderInit(CommandBuffer cmd)
        {

        }

        public override void Bind(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(_PreIntegratedEyeCaustic, m_EyeCausticLUT);
        }

        // Reuse GGX textures
    }
}
