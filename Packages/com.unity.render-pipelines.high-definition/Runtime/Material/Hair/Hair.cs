using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition.Attributes;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class Hair : RenderPipelineMaterial
    {
        [GenerateHLSL(PackingRules.Exact)]
        public enum MaterialFeatureFlags
        {
            HairKajiyaKay          = 1 << 0,
            HairMarschner          = 1 << 1,
            HairMarschnerCinematic = 1 << 2
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
            [SurfaceDataAttributes(new string[] { "Normal", "Normal View Space" }, true, checkIsNormalized = true)]
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
            [SurfaceDataAttributes("Absorption Coefficient")]
            public Vector3 absorption;
            [SurfaceDataAttributes("Eumelanin")]
            public float eumelanin;
            [SurfaceDataAttributes("Pheomelanin")]
            public float pheomelanin;

            [SurfaceDataAttributes("Azimuthal Roughness")]
            public float perceptualRadialSmoothness;
            [SurfaceDataAttributes("Cuticle Angle")]
            public float cuticleAngle;

            // Global Scattering
            [SurfaceDataAttributes("Strand Count Probe")]
            public Vector4 strandCountProbe;
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
            public float rimTransmissionIntensity;

            // Anisotropic
            [SurfaceDataAttributes("", true)]
            public Vector3 hairStrandDirectionWS;
            public float anisotropy;

            // TEMP: Pathtracer Compatibility.
            // Path tracer assumes this anisotropic fields generally exist (even though we don't use them).
            public Vector3 tangentWS;
            public Vector3 bitangentWS;
            public float roughnessT;
            public float roughnessB;

            public float h;

            // Kajiya kay
            public float secondaryPerceptualRoughness;
            public Vector3 secondarySpecularTint;
            public float specularExponent;
            public float secondarySpecularExponent;
            public float specularShift;
            public float secondarySpecularShift;

            // Marschner
            public Vector3 absorption;

            public float lightPathLength;

            public float cuticleAngle;
            public float cuticleAngleR;
            public float cuticleAngleTT;
            public float cuticleAngleTRT;

            public float roughnessR;
            public float roughnessTT;
            public float roughnessTRT;

            public float perceptualRoughnessRadial;

            // Normalization factor for area lights.
            public Vector3 distributionNormalizationFactor;

            // Global Scattering
            public Vector4 strandCountProbe;
            public float   visibility;
        };


        //-----------------------------------------------------------------------------
        // Init precomputed texture
        //-----------------------------------------------------------------------------

        private const int kDim = 64;

        private ComputeShader m_PreIntegrationKernels;
        private int m_ComputeForwardAttenuationKernel;
        private int m_ComputeBackwardAttenuationKernel;
        private int m_ComputeAzimuthalScatteringKernel;
        private int m_ComputeLongitudinalScatteringKernel;

        // X - Roughness
        // Y - Theta
        // Z - Absorption
        private RenderTexture m_AttenuationLUT;
        private bool m_AttenuationIsInit;

        // X: Phi
        // Y: Theta
        // Z: Azimuthal Roughness
        private RenderTexture m_AzimuthalScatteringLUT;
        private bool m_AzimuthalScatteringIsInit;

        // X:
        // Y:
        // Z:
        private RenderTexture m_LongitudinalScatteringLUT;
        private bool m_LongitudinalScatteringIsInit;

        // NOTE: Since we re-use Hair.hlsl for both the BSDF pre-integration and at runtime, we need to maintain these two different binding
        public static readonly int _HairAttenuationUAV            = Shader.PropertyToID("_HairAttenuationUAV");
        public static readonly int _HairAttenuation               = Shader.PropertyToID("_HairAttenuation");
        public static readonly int _HairAzimuthalScatteringUAV    = Shader.PropertyToID("_HairAzimuthalScatteringUAV");
        public static readonly int _HairAzimuthalScattering       = Shader.PropertyToID("_HairAzimuthalScattering");
        public static readonly int _HairLongitudinalScatteringUAV = Shader.PropertyToID("_HairLongitudinalScatteringUAV");
        public static readonly int _HairLongitudinalScattering    = Shader.PropertyToID("_HairLongitudinalScattering");

        public Hair() { }

        public override void Build(HDRenderPipelineAsset hdAsset, HDRenderPipelineRuntimeResources defaultResources)
        {
            PreIntegratedFGD.instance.Build(PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse);
            LTCAreaLight.instance.Build();

            // 4b * kDim^3 = ~1mb
            m_AttenuationLUT = new RenderTexture(kDim, kDim, 0, GraphicsFormat.R16G16_SFloat)
            {
                dimension = TextureDimension.Tex3D,
                volumeDepth = kDim,
                enableRandomWrite = true,
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = CoreUtils.GetRenderTargetAutoName(kDim, kDim, 0, GraphicsFormat.R16G16_SFloat, "HairAttenuationLUT")
            };
            m_AttenuationLUT.Create();

            // 8b * kDim^3 = ~2mb
            m_AzimuthalScatteringLUT = new RenderTexture(kDim, kDim, 0, GraphicsFormat.R16G16B16A16_SFloat)
            {
                dimension = TextureDimension.Tex3D,
                volumeDepth = kDim,
                enableRandomWrite = true,
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = CoreUtils.GetRenderTargetAutoName(kDim, kDim, 0, GraphicsFormat.R16G16B16A16_SFloat, "HairAzimuthalScatteringLUT")
            };
            m_AzimuthalScatteringLUT.Create();

            // 8b * kDim^3 = ~2mb
            m_LongitudinalScatteringLUT = new RenderTexture(kDim, kDim, 0, GraphicsFormat.R16G16B16A16_SFloat)
            {
                dimension = TextureDimension.Tex3D,
                volumeDepth = kDim,
                enableRandomWrite = true,
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = CoreUtils.GetRenderTargetAutoName(kDim, kDim, 0, GraphicsFormat.R16G16B16A16_SFloat, "HairLongitudinalScatteringUAV")
            };
            m_LongitudinalScatteringLUT.Create();

            m_PreIntegrationKernels = defaultResources.shaders.preIntegratedFiberScatteringCS;
            {
                m_ComputeForwardAttenuationKernel     = m_PreIntegrationKernels.FindKernel("ComputeAttenuationForward");
                m_ComputeBackwardAttenuationKernel    = m_PreIntegrationKernels.FindKernel("ComputeAttenuationBackward");
                m_ComputeAzimuthalScatteringKernel    = m_PreIntegrationKernels.FindKernel("ComputeAzimuthalScattering");
                m_ComputeLongitudinalScatteringKernel = m_PreIntegrationKernels.FindKernel("ComputeLongitudinalScattering");
            }
        }

        public override void Cleanup()
        {
            PreIntegratedFGD.instance.Cleanup(PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse);
            LTCAreaLight.instance.Cleanup();

            CoreUtils.Destroy(m_AttenuationLUT);
            m_AttenuationLUT = null;

            CoreUtils.Destroy(m_AzimuthalScatteringLUT);
            m_AzimuthalScatteringLUT = null;

            CoreUtils.Destroy(m_LongitudinalScatteringLUT);
            m_LongitudinalScatteringLUT = null;
        }

        public override void RenderInit(CommandBuffer cmd)
        {
            PreIntegratedFGD.instance.RenderInit(PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse, cmd);

            if (m_PreIntegrationKernels == null)
                return;

            if (!m_AttenuationIsInit)
            {
                // Forward attenuation
                cmd.SetComputeTextureParam(m_PreIntegrationKernels, m_ComputeForwardAttenuationKernel, _HairAttenuationUAV, m_AttenuationLUT);
                cmd.DispatchCompute(m_PreIntegrationKernels, m_ComputeForwardAttenuationKernel, HDUtils.DivRoundUp(kDim, 8), HDUtils.DivRoundUp(kDim, 8), HDUtils.DivRoundUp(kDim, 8));

                // Backward attenuation
                cmd.SetComputeTextureParam(m_PreIntegrationKernels, m_ComputeBackwardAttenuationKernel, _HairAttenuationUAV, m_AttenuationLUT);
                cmd.DispatchCompute(m_PreIntegrationKernels, m_ComputeBackwardAttenuationKernel, HDUtils.DivRoundUp(kDim, 8), HDUtils.DivRoundUp(kDim, 8), HDUtils.DivRoundUp(kDim, 8));

                m_AttenuationIsInit = true;
            }

            if (!m_AzimuthalScatteringIsInit)
            {
                cmd.SetComputeTextureParam(m_PreIntegrationKernels, m_ComputeAzimuthalScatteringKernel, _HairAzimuthalScatteringUAV, m_AzimuthalScatteringLUT);
                cmd.DispatchCompute(m_PreIntegrationKernels, m_ComputeAzimuthalScatteringKernel, HDUtils.DivRoundUp(kDim, 8), HDUtils.DivRoundUp(kDim, 8), HDUtils.DivRoundUp(kDim, 8));

                m_AzimuthalScatteringIsInit = true;
            }

            if (!m_LongitudinalScatteringIsInit)
            {
                cmd.SetComputeTextureParam(m_PreIntegrationKernels, m_ComputeLongitudinalScatteringKernel, _HairLongitudinalScatteringUAV, m_LongitudinalScatteringLUT);
                cmd.DispatchCompute(m_PreIntegrationKernels, m_ComputeLongitudinalScatteringKernel, HDUtils.DivRoundUp(kDim, 8), HDUtils.DivRoundUp(kDim, 8), HDUtils.DivRoundUp(kDim, 8));

                m_LongitudinalScatteringIsInit = true;
            }
        }

        public override void Bind(CommandBuffer cmd)
        {
            PreIntegratedFGD.instance.Bind(cmd, PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse);
            LTCAreaLight.instance.Bind(cmd);

            if (m_AttenuationLUT == null)
            {
                throw new Exception("Pre-Integrated Hair Fiber LUT not available!");
            }
            cmd.SetGlobalTexture(_HairAttenuation, m_AttenuationLUT);

            if (m_AzimuthalScatteringLUT == null)
            {
                throw new Exception("Pre-Integrated Hair Fiber LUT not available!");
            }
            cmd.SetGlobalTexture(_HairAzimuthalScattering, m_AzimuthalScatteringLUT);

            if (m_LongitudinalScatteringLUT == null)
            {
                throw new Exception("Pre-Integrated Hair Fiber LUT not available!");
            }
            cmd.SetGlobalTexture(_HairLongitudinalScattering, m_LongitudinalScatteringLUT);
        }
    }
}
