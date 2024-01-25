using System;
using System.Collections.Generic;
using UnityEditor;
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

        public static readonly int _HairAttenuation               = Shader.PropertyToID("_HairAttenuation");
        public static readonly int _HairAzimuthalScattering       = Shader.PropertyToID("_HairAzimuthalScattering");
        public static readonly int _HairLongitudinalScattering    = Shader.PropertyToID("_HairLongitudinalScattering");

        private Texture3D m_HairAttenuationLUT;
        private Texture3D m_HairAzimuthalScatteringLUT;
        private Texture3D m_HairLongitudinalScatteringLUT;

        public Hair() { }

        public override void Build(HDRenderPipeline renderPipeline)
        {
            PreIntegratedFGD.instance.Build(PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse);
            LTCAreaLight.instance.Build();

            m_HairAttenuationLUT            = renderPipeline.runtimeTextures.hairAttenuationLUT;
            m_HairAzimuthalScatteringLUT    = renderPipeline.runtimeTextures.hairAzimuthalScatteringLUT;
            m_HairLongitudinalScatteringLUT = renderPipeline.runtimeTextures.hairLongitudinalScatteringLUT;
        }

        public override void BuildOffline(ref List<RenderTexture> generatedResourceList)
        {
            var lookupDescriptor = new RenderTextureDescriptor
            {
                // All hair lookups are three-dimensional.
                dimension         = TextureDimension.Tex3D,
                width             = kDim,
                height            = kDim,
                volumeDepth       = kDim,
                enableRandomWrite = true,
                autoGenerateMips  = false,
                msaaSamples       = 1,
                depthBufferBits   = 0,
            };

            RenderTexture GetTemporaryHairMaterialRT(RenderTextureDescriptor baseDescriptor, string name, GraphicsFormat format)
            {
                baseDescriptor.graphicsFormat = format;

                var rt = RenderTexture.GetTemporary(baseDescriptor);
                {
                    // Can't specify a name in a descriptor...
                    // We want to label it since it governs the asset name.
                    rt.name = name;

                    // Async-readback will fail unless this is called.
                    rt.Create();
                }
                return rt;
            }

            var lutAttenuation            = GetTemporaryHairMaterialRT(lookupDescriptor, "_HairAttenuationUAV",            GraphicsFormat.R16G16B16A16_SFloat); // ~1mb
            var lutAzimuthalScattering    = GetTemporaryHairMaterialRT(lookupDescriptor, "_HairAzimuthalScatteringUAV",    GraphicsFormat.R16G16B16A16_SFloat); // ~2mb
            var lutLongitudinalScattering = GetTemporaryHairMaterialRT(lookupDescriptor, "_HairLongitudinalScatteringUAV", GraphicsFormat.R16G16B16A16_SFloat); // ~2mb

            var shaders = GraphicsSettings.GetRenderPipelineSettings<HDRenderPipelineRuntimeShaders>();
            var kernels = shaders.preIntegratedFiberScatteringCS;

            void ComputeHairLookUpTable(string kernelName, RenderTexture resource)
            {
                var cmd = CommandBufferPool.Get();

                var kernelID = kernels.FindKernel(kernelName);

                cmd.SetComputeTextureParam(kernels, kernelID, resource.name, resource);

                var dispatchSize = new[]
                {
                    // All the LUT kernels work in 8x8x8 work-groups.
                    HDUtils.DivRoundUp(kDim, 8),
                    HDUtils.DivRoundUp(kDim, 8),
                    HDUtils.DivRoundUp(kDim, 8)
                };
                cmd.DispatchCompute(kernels, kernelID, dispatchSize[0], dispatchSize[1], dispatchSize[2]);

                Graphics.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            ComputeHairLookUpTable("ComputeAttenuationForward",     lutAttenuation);
            ComputeHairLookUpTable("ComputeAttenuationBackward",    lutAttenuation);
            ComputeHairLookUpTable("ComputeAzimuthalScattering",    lutAzimuthalScattering);
            ComputeHairLookUpTable("ComputeLongitudinalScattering", lutLongitudinalScattering);

            // Write these look-up tables to disk.
            generatedResourceList.Add(lutAttenuation);
            generatedResourceList.Add(lutAzimuthalScattering);
            generatedResourceList.Add(lutLongitudinalScattering);
        }

        public override void Cleanup()
        {
            PreIntegratedFGD.instance.Cleanup(PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse);
            LTCAreaLight.instance.Cleanup();
        }

        public override void RenderInit(CommandBuffer cmd)
        {
            PreIntegratedFGD.instance.RenderInit(PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse, cmd);
        }

        public override void Bind(CommandBuffer cmd)
        {
            PreIntegratedFGD.instance.Bind(cmd, PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse);
            LTCAreaLight.instance.Bind(cmd);

            cmd.SetGlobalTexture(_HairAttenuation, m_HairAttenuationLUT);
            cmd.SetGlobalTexture(_HairAzimuthalScattering, m_HairAzimuthalScatteringLUT);
            cmd.SetGlobalTexture(_HairLongitudinalScattering, m_HairLongitudinalScatteringLUT);
        }
    }
}
