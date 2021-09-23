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
            [SurfaceDataAttributes("Azimuthal Roughness")]
            public float perceptualRadialSmoothness;
            [SurfaceDataAttributes("Cuticle Angle")]
            public float cuticleAngle;

            // Global Scattering
            [SurfaceDataAttributes("Strand Count Probe")]
            public Vector4 strandCountProbe;
            [SurfaceDataAttributes("Strand Shadow Bias")]
            public float strandShadowBias;
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

            public float cuticleAngleR;
            public float cuticleAngleTT;
            public float cuticleAngleTRT;

            public float roughnessR;
            public float roughnessTT;
            public float roughnessTRT;

            public float roughnessRadial;

            // Global Scattering
            public Vector4 strandCountProbe;
            public float strandShadowBias;
            public float splineVisibility;
        };


        //-----------------------------------------------------------------------------
        // Init precomputed texture
        //-----------------------------------------------------------------------------

        private Texture2D m_PreIntegratedAzimuthalScatteringLUT;

        // X - Roughness
        // Y - Theta
        // Z - Absorption
        private ComputeShader m_PreIntegratedFiberScatteringCS;
        private RenderTexture m_PreIntegratedFiberScatteringLUT;
        private const int m_DimTheta = 64;
        private const int m_DimBeta = 64;
        private const int m_DimAbsorption = 64;
        private bool m_PreIntegratedFiberScatteringIsInit;

        // X - Theta
        // Y - Absorption
        private RenderTexture m_PreIntegratedFiberAverageScatteringLUT;
        private bool m_PreIntegratedFiberAverageScatteringIsInit;

        // NOTE: Since we re-use Hair.hlsl for both the BSDF pre-integration and at runtime, we need to maintain these two different binding
        // names to avoid compiler complaining.
        public static readonly int _PreIntegratedHairFiberScatteringUAV = Shader.PropertyToID("_PreIntegratedHairFiberScatteringUAV");
        public static readonly int _PreIntegratedHairFiberScattering = Shader.PropertyToID("_PreIntegratedHairFiberScattering");

        public static readonly int _PreIntegratedAverageHairFiberScatteringUAV = Shader.PropertyToID("_PreIntegratedAverageHairFiberScatteringUAV");
        public static readonly int _PreIntegratedAverageHairFiberScattering = Shader.PropertyToID("_PreIntegratedAverageHairFiberScattering");

        public Hair() { }

        public override void Build(HDRenderPipelineAsset hdAsset, HDRenderPipelineRuntimeResources defaultResources)
        {
            PreIntegratedFGD.instance.Build(PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse);
            LTCAreaLight.instance.Build();

            m_PreIntegratedAzimuthalScatteringLUT = defaultResources.textures.preintegratedAzimuthalScattering;

            // Initialize the dual scattering LUT.
            m_PreIntegratedFiberScatteringLUT = new RenderTexture(m_DimTheta, m_DimBeta, 0, GraphicsFormat.R16G16_SFloat)
            {
                dimension = TextureDimension.Tex3D,
                volumeDepth = m_DimAbsorption,
                enableRandomWrite = true,
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = CoreUtils.GetRenderTargetAutoName(m_DimTheta, m_DimBeta, 0, GraphicsFormat.R16G16_SFloat, "PreIntegratedFiberScattering")
            };
            m_PreIntegratedFiberScatteringLUT.Create();

            m_PreIntegratedFiberAverageScatteringLUT = new RenderTexture(m_DimTheta, m_DimAbsorption, 0, GraphicsFormat.R16G16B16A16_SFloat)
            {
                dimension = TextureDimension.Tex2D,
                enableRandomWrite = true,
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = CoreUtils.GetRenderTargetAutoName(m_DimTheta, m_DimAbsorption, 0, GraphicsFormat.R16G16B16A16_SFloat, "PreIntegratedAverageFiberScattering")
            };
            m_PreIntegratedFiberAverageScatteringLUT.Create();

            m_PreIntegratedFiberScatteringCS = defaultResources.shaders.preIntegratedFiberScatteringCS;
        }

        public override void Cleanup()
        {
            PreIntegratedFGD.instance.Cleanup(PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse);
            LTCAreaLight.instance.Cleanup();

            CoreUtils.Destroy(m_PreIntegratedFiberScatteringLUT);
            m_PreIntegratedFiberScatteringLUT = null;

            CoreUtils.Destroy(m_PreIntegratedFiberAverageScatteringLUT);
            m_PreIntegratedFiberAverageScatteringLUT = null;
        }

        public override void RenderInit(CommandBuffer cmd)
        {
            PreIntegratedFGD.instance.RenderInit(PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse, cmd);

            if (m_PreIntegratedFiberScatteringCS == null)
                return;

            // Preintegration of the dual scattering LUT.
            if (!m_PreIntegratedFiberScatteringIsInit)
            {
                cmd.SetComputeTextureParam(m_PreIntegratedFiberScatteringCS, 0, _PreIntegratedHairFiberScatteringUAV, m_PreIntegratedFiberScatteringLUT);
                cmd.DispatchCompute(m_PreIntegratedFiberScatteringCS, 0, HDUtils.DivRoundUp(m_DimTheta, 8), HDUtils.DivRoundUp(m_DimBeta, 8), HDUtils.DivRoundUp(m_DimAbsorption, 8));

                m_PreIntegratedFiberScatteringIsInit = true;
            }

            if (!m_PreIntegratedFiberAverageScatteringIsInit)
            {
                cmd.SetComputeTextureParam(m_PreIntegratedFiberScatteringCS, 1, _PreIntegratedAverageHairFiberScatteringUAV, m_PreIntegratedFiberAverageScatteringLUT);
                cmd.DispatchCompute(m_PreIntegratedFiberScatteringCS, 1, HDUtils.DivRoundUp(m_DimTheta, 8), HDUtils.DivRoundUp(m_DimBeta, 8), 1);

                m_PreIntegratedFiberAverageScatteringIsInit = true;
            }
        }

        public override void Bind(CommandBuffer cmd)
        {
            PreIntegratedFGD.instance.Bind(cmd, PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse);
            LTCAreaLight.instance.Bind(cmd);

            if (m_PreIntegratedAzimuthalScatteringLUT != null)
                cmd.SetGlobalTexture(HDShaderIDs._PreIntegratedAzimuthalScattering, m_PreIntegratedAzimuthalScatteringLUT);

            if (m_PreIntegratedFiberScatteringLUT == null)
            {
                throw new Exception("Pre-Integrated Hair Fiber LUT not available!");
            }
            cmd.SetGlobalTexture(_PreIntegratedHairFiberScattering, m_PreIntegratedFiberScatteringLUT);

            if (m_PreIntegratedFiberAverageScatteringLUT == null)
            {
                throw new Exception("Pre-Integrated Hair Fiber LUT not available!");
            }
            cmd.SetGlobalTexture(_PreIntegratedAverageHairFiberScattering, m_PreIntegratedFiberAverageScatteringLUT);
        }
    }
}
