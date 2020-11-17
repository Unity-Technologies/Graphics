using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    unsafe struct ShaderVariablesClouds
    {
        public float _CloudDomeSize;
        public float _HighestCloudAltitude;
        public float _LowestCloudAltitude;
        public int _NumPrimarySteps;

        public int _NumLightSteps;
        public Vector3 _ScatteringTint;

        public float _Eccentricity;
        public float _SilverIntensity;
        public float _SilverSpread;
        public float _Padding0;

        public int _ExposureSunColor;
        public Vector3 _SunLightColor;
        public Vector3 _SunDirection;
        public int _AccumulationFrameIndex;

        [HLSLArray(7, typeof(Vector4))]
        public fixed float _AmbientProbeCoeffs[7 * 4];  // 3 bands of SH, packed, rescaled and convolved with the phase function
    }

    public partial class HDRenderPipeline
    {
        ShaderVariablesClouds m_ShaderVariablesCloudsCB = new ShaderVariablesClouds();
        Vector4[] m_PackedCoeffsClouds;
        ZonalHarmonicsL2 m_PhaseZHClouds;
        int m_CloudRenderKernel;
        int m_CloudCombineKernel;

        void InitializeVolumetricClouds()
        {
            m_PackedCoeffsClouds = new Vector4[7];
            m_PhaseZHClouds = new ZonalHarmonicsL2();
            m_PhaseZHClouds.coeffs = new float[3];

            // Grab the kernels we need
            ComputeShader volumetricCloudsCS = m_Asset.renderPipelineResources.shaders.volumetricCloudsCS;
            m_CloudRenderKernel = volumetricCloudsCS.FindKernel("RenderClouds");
            m_CloudCombineKernel = volumetricCloudsCS.FindKernel("CombineClouds");
        }

        unsafe void SetPreconvolvedAmbientLightProbe(ref ShaderVariablesClouds cb, HDCamera hdCamera, VolumetricClouds settings)
        {
            SphericalHarmonicsL2 probeSH = SphericalHarmonicMath.UndoCosineRescaling(m_SkyManager.GetAmbientProbe(hdCamera));
            probeSH = SphericalHarmonicMath.RescaleCoefficients(probeSH, settings.globalLightProbeDimmer.value);
            ZonalHarmonicsL2.GetCornetteShanksPhaseFunction(m_PhaseZHClouds, 0.0f);
            SphericalHarmonicsL2 finalSH = SphericalHarmonicMath.PremultiplyCoefficients(SphericalHarmonicMath.Convolve(probeSH, m_PhaseZHClouds));

            SphericalHarmonicMath.PackCoefficients(m_PackedCoeffsClouds, finalSH);
            for (int i = 0; i < 7; i++)
                for (int j = 0; j < 4; ++j)
                    cb._AmbientProbeCoeffs[i * 4 + j] = m_PackedCoeffsClouds[i][j];
        }

        void UpdateShaderVariableslClouds(ref ShaderVariablesClouds cb, HDCamera hdCamera, VolumetricClouds settings)
        {
            // Convert to kilometers
            cb._CloudDomeSize = settings.cloudDomeSize.value * 1000.0f;
            cb._LowestCloudAltitude = settings.lowestCloudAltitude.value;
            cb._HighestCloudAltitude = settings.highestCloudAltitude.value;
            cb._NumPrimarySteps = settings.numPrimarySteps.value;
            cb._NumLightSteps = settings.numLightSteps.value;
            cb._ScatteringTint = new Vector3(settings.scatteringTint.value.r, settings.scatteringTint.value.g, settings.scatteringTint.value.b);
            cb._Eccentricity = settings.eccentricity.value;
            cb._SilverIntensity = settings.silverIntensity.value;
            cb._SilverSpread = settings.silverSpread.value;
            cb._AccumulationFrameIndex = RayTracingFrameIndex(hdCamera);
            if (m_lightList.directionalLights.Count != 0)
            {
                cb._SunDirection = -m_lightList.directionalLights[0].forward;
                cb._SunLightColor = m_lightList.directionalLights[0].color;
                cb._ExposureSunColor = 1;
            }   
            else
            {
                cb._SunDirection = Vector3.up;
                cb._SunLightColor = Vector3.one;
                cb._ExposureSunColor = 0;
            }
            cb._AccumulationFrameIndex = RayTracingFrameIndex(hdCamera);

            // Evaluate the ambient probe data
            SetPreconvolvedAmbientLightProbe(ref cb, hdCamera, settings);
        }

        struct VolumetricCloudsParameters
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Other data
            public Texture3D worley128RGBA;
            public Texture3D worley32RGB;
            public Texture cloudMapTexture;
            public Texture cloudLutTexture;
            public ComputeShader volumetricCloudsCS;
            public int renderKernel;
            public int combineKernel;
            public BlueNoise.DitheredTextureSet ditheredTextureSet;
            public ShaderVariablesClouds cloudsCB;
        }

        VolumetricCloudsParameters PrepareVolumetricCloudsParameters(HDCamera hdCamera)
        {
            VolumetricCloudsParameters parameters = new VolumetricCloudsParameters();
            // Camera parameters
            parameters.texWidth = hdCamera.actualWidth;
            parameters.texHeight = hdCamera.actualHeight;
            parameters.viewCount = hdCamera.viewCount;

            // Grab the volume component
            VolumetricClouds settings = hdCamera.volumeStack.GetComponent<VolumetricClouds>();

            // Update the constant buffer
            UpdateShaderVariableslClouds(ref m_ShaderVariablesCloudsCB, hdCamera, settings);
            parameters.cloudsCB = m_ShaderVariablesCloudsCB;

            parameters.volumetricCloudsCS = m_Asset.renderPipelineResources.shaders.volumetricCloudsCS;
            parameters.renderKernel = m_CloudRenderKernel;
            parameters.combineKernel = m_CloudCombineKernel;
            parameters.cloudMapTexture = settings.cloudMap.value;
            parameters.cloudLutTexture = settings.cloudLut.value;
            parameters.worley128RGBA = m_Asset.renderPipelineResources.textures.worleyNoise128RGBA;
            parameters.worley32RGB = m_Asset.renderPipelineResources.textures.worleyNoise32RGB;
            BlueNoise blueNoise = GetBlueNoiseManager();
            parameters.ditheredTextureSet = blueNoise.DitheredTextureSet8SPP();

            return parameters;
        }

        static void TraceVolumetricClouds(CommandBuffer cmd, VolumetricCloudsParameters parameters, RTHandle colorBuffer, RTHandle depthBuffer, RTHandle intermediateBuffer)
        {
            BlueNoise.BindDitheredTextureSet(cmd, parameters.ditheredTextureSet);

            // Bind all the input data
            ConstantBuffer.Push(cmd, parameters.cloudsCB, parameters.volumetricCloudsCS, HDShaderIDs._ShaderVariablesClouds);

            cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._CameraColorTexture, colorBuffer);
            cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._DepthTexture, depthBuffer);
            cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._Worley128RGBA, parameters.worley128RGBA);
            cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._Worley32RGB, parameters.worley32RGB);
            cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._CloudMapTexture, parameters.cloudMapTexture);
            cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._CloudLutTexture, parameters.cloudLutTexture);

            // Bind the output buffers
            cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._VolumetricCloudsTextureRW, intermediateBuffer);

            // Evaluate the dispatch parameters
            int numTilesXHR = (parameters.texWidth + (8 - 1)) / 8;
            int numTilesYHR = (parameters.texHeight + (8 - 1)) / 8;

            // Compute the directions
            cmd.DispatchCompute(parameters.volumetricCloudsCS, parameters.renderKernel, numTilesXHR, numTilesYHR, parameters.viewCount);

            // Compute the directions
            cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.combineKernel, HDShaderIDs._VolumetricCloudsTexture, intermediateBuffer);
            cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.combineKernel, HDShaderIDs._CameraColorTextureRW, colorBuffer);
            cmd.DispatchCompute(parameters.volumetricCloudsCS, parameters.combineKernel, numTilesXHR, numTilesYHR, parameters.viewCount);
        }

        class VolumetricCloudsData
        {
            public VolumetricCloudsParameters parameters;
            public TextureHandle colorBuffer;
            public TextureHandle depthPyramid;
            public TextureHandle intermediateBuffer;
        }

        TextureHandle TraceVolumetricClouds(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle depthPyramid)
        {
            using (var builder = renderGraph.AddRenderPass<VolumetricCloudsData>("Generating the rays for RTR", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingIndirectDiffuseDirectionGeneration)))
            {
                builder.EnableAsyncCompute(false);

                passData.parameters = PrepareVolumetricCloudsParameters(hdCamera);
                passData.colorBuffer = builder.ReadTexture(builder.WriteTexture(colorBuffer));
                passData.depthPyramid = builder.ReadTexture(depthPyramid);
                passData.intermediateBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Temporary Clouds Buffer" });

                builder.SetRenderFunc(
                (VolumetricCloudsData data, RenderGraphContext ctx) =>
                {
                    TraceVolumetricClouds(ctx.cmd, data.parameters, data.colorBuffer, data.depthPyramid, data.intermediateBuffer);
                });

                return passData.colorBuffer;
            }
        }

        void RenderVolumetricClouds(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle depthPyramid)
        {
            VolumetricClouds settings = hdCamera.volumeStack.GetComponent<VolumetricClouds>();
            // If the current volume does not enable the feature, quit right away.
            if (!settings.enable.value)
                return;

            TraceVolumetricClouds(renderGraph, hdCamera, colorBuffer, depthPyramid);
        }
    }
}
