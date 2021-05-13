using UnityEngine.Rendering.HighDefinition.Attributes;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class Hair : RenderPipelineMaterial
    {
        [GenerateHLSL(PackingRules.Exact)]
        public enum MaterialFeatureFlags
        {
            HairKajiyaKay = 1 << 0,
            HairMarschner = 1 << 1
        };

        //-----------------------------------------------------------------------------
        // SurfaceData
        //-----------------------------------------------------------------------------

        // Main structure that store the user data (i.e user input of master node in material graph)
        [GenerateHLSL(PackingRules.Exact, false, false, true, 1400)]
        public struct SurfaceData
        {
            [SurfaceDataAttributes("Material Features")]
            public uint materialFeatures;

            [MaterialSharedPropertyMapping(MaterialSharedProperty.AmbientOcclusion)]
            [SurfaceDataAttributes("Ambient Occlusion")]
            public float ambientOcclusion;

            // Standard
            [MaterialSharedPropertyMapping(MaterialSharedProperty.Albedo)]
            [SurfaceDataAttributes("Diffuse", false, true)]
            public Vector3 diffuseColor;
            [SurfaceDataAttributes("Specular Occlusion")]
            public float specularOcclusion;

            [MaterialSharedPropertyMapping(MaterialSharedProperty.Normal)]
            [SurfaceDataAttributes(new string[] {"Normal", "Normal View Space"}, true, checkIsNormalized = true)]
            public Vector3 normalWS;

            [SurfaceDataAttributes(new string[] { "Geometric Normal", "Geometric Normal View Space" }, true, checkIsNormalized = true)]
            public Vector3 geomNormalWS;

            [MaterialSharedPropertyMapping(MaterialSharedProperty.Smoothness)]
            [SurfaceDataAttributes("Smoothness")]
            public float perceptualSmoothness;

            [SurfaceDataAttributes("Transmittance")]
            public Vector3 transmittance;

            [SurfaceDataAttributes("Rim Transmission Intensity")]
            public float rimTransmissionIntensity;

            // Anisotropic
            [SurfaceDataAttributes("Hair Strand Direction", true)]
            public Vector3 hairStrandDirectionWS;

            // Kajiya kay
            [SurfaceDataAttributes("Secondary Smoothness")]
            public float secondaryPerceptualSmoothness;

            // Specular Color
            [MaterialSharedPropertyMapping(MaterialSharedProperty.Specular)]
            [SurfaceDataAttributes("Specular Tint", false, true)]
            public Vector3 specularTint;

            [SurfaceDataAttributes("Secondary Specular Tint", false, true)]
            public Vector3 secondarySpecularTint;

            [SurfaceDataAttributes("Specular Shift")]
            public float specularShift;

            [SurfaceDataAttributes("Secondary Specular Shift")]
            public float secondarySpecularShift;

            // Marschner
            [SurfaceDataAttributes("Longitudinal Roughness")]
            public float roughnessLongitudinal;
            [SurfaceDataAttributes("Azimuthal Roughness")]
            public float roughnessAzimuthal;
            [SurfaceDataAttributes("Primary Reflection Roughness")]
            public float roughnessPrimaryReflection;
            [SurfaceDataAttributes("Refraction Index")]
            public float ior;
            [SurfaceDataAttributes("Cuticle Angle")]
            public float cuticleAngle;
        };

        //-----------------------------------------------------------------------------
        // BSDFData
        //-----------------------------------------------------------------------------

        [GenerateHLSL(PackingRules.Exact, false, false, true, 1450)]
        public struct BSDFData
        {
            public uint materialFeatures;

            public float ambientOcclusion;
            public float specularOcclusion;

            [SurfaceDataAttributes("", false, true)]
            public Vector3 diffuseColor;
            public Vector3 fresnel0;

            public Vector3 specularTint;

            [SurfaceDataAttributes(new string[] { "Normal WS", "Normal View Space" }, true, checkIsNormalized = true)]
            public Vector3 normalWS;

            [SurfaceDataAttributes(new string[] { "Geometric Normal", "Geometric Normal View Space" }, true, checkIsNormalized = true)]
            public Vector3 geomNormalWS;

            public float perceptualRoughness;

            public Vector3 transmittance;
            public float   rimTransmissionIntensity;

            // Anisotropic
            [SurfaceDataAttributes("", true)]
            public Vector3 hairStrandDirectionWS;
            public float anisotropy;

            // TEMP: Pathtracer Compatibility.
            // Path tracer assumes this anisotropic fields generally exist (even though we don't use them).
            public Vector3 tangentWS;
            public Vector3 bitangentWS;
            public float   roughnessT;
            public float   roughnessB;

            // Kajiya kay
            public float secondaryPerceptualRoughness;
            public Vector3 secondarySpecularTint;
            public float specularExponent;
            public float secondarySpecularExponent;
            public float specularShift;
            public float secondarySpecularShift;

            // Marschner
            public float lightPathLength;

            public float cuticleAngleR;
            public float cuticleAngleTT;
            public float cuticleAngleTRT;

            public float roughnessLR;
            public float roughnessLTT;
            public float roughnessLTRT;

            public float roughnessAR;
            public float roughnessATT;
            public float roughnessATRT;

            public float ior;
        };


        //-----------------------------------------------------------------------------
        // Init precomputed texture
        //-----------------------------------------------------------------------------

        public Hair() {}

        public override void Build(HDRenderPipelineAsset hdAsset, HDRenderPipelineRuntimeResources defaultResources)
        {
            PreIntegratedFGD.instance.Build(PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse);
            LTCAreaLight.instance.Build();
            PreIntegratedAzimuthalScattering.instance.Build();
        }

        public override void Cleanup()
        {
            PreIntegratedFGD.instance.Cleanup(PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse);
            LTCAreaLight.instance.Cleanup();
            PreIntegratedAzimuthalScattering.instance.Cleanup();
        }

        public override void RenderInit(CommandBuffer cmd)
        {
            PreIntegratedFGD.instance.RenderInit(PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse, cmd);
            PreIntegratedAzimuthalScattering.instance.RenderInit(cmd);
        }

        public override void Bind(CommandBuffer cmd)
        {
            PreIntegratedFGD.instance.Bind(cmd, PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse);
            LTCAreaLight.instance.Bind(cmd);
            PreIntegratedAzimuthalScattering.instance.Bind(cmd);
        }
    }
}
