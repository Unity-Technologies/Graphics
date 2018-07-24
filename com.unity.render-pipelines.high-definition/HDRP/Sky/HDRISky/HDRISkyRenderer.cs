using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class HDRISkyRenderer : SkyRenderer
    {
        Material m_SkyHDRIMaterial; // Renders a cubemap into a render texture (can be cube or 2D)
        Material m_IntegrateHDRISkyMaterial; // Compute the HDRI sky intensity in lux for the skybox
        MaterialPropertyBlock m_PropertyBlock;
        HDRISky m_HdriSkyParams;
        RTHandleSystem.RTHandle m_IntensityTexture;

        public HDRISkyRenderer(HDRISky hdriSkyParams)
        {
            m_HdriSkyParams = hdriSkyParams;
            m_PropertyBlock = new MaterialPropertyBlock();
            m_IntensityTexture = RTHandles.Alloc(1, 1, colorFormat: RenderTextureFormat.RFloat, sRGB: false);
        }

        public override void Build()
        {
            var hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            m_SkyHDRIMaterial = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.hdriSky);
            m_IntegrateHDRISkyMaterial = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.integrateHdriSky);
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(m_SkyHDRIMaterial);
        }

        public override void SetRenderTargets(BuiltinSkyParameters builtinParams)
        {
            if (builtinParams.depthBuffer == BuiltinSkyParameters.nullRT)
            {
                HDUtils.SetRenderTarget(builtinParams.commandBuffer, builtinParams.hdCamera, builtinParams.colorBuffer);
            }
            else
            {
                HDUtils.SetRenderTarget(builtinParams.commandBuffer, builtinParams.hdCamera, builtinParams.colorBuffer, builtinParams.depthBuffer);
            }
        }
        
        public override void UpdateSky(BuiltinSkyParameters builtinParams)
        {
            using (new ProfilingSample(builtinParams.commandBuffer, "Get hdri skybox intensity"))
            {
                // if (m_HdriSkyParams.updateHDRISkyIntensity)
                {
                    float omegaP = (Mathf.PI * 4) / (6.0f * m_HdriSkyParams.hdriSky.value.width * m_HdriSkyParams.hdriSky.value.width);
                    m_IntegrateHDRISkyMaterial.SetTexture(HDShaderIDs._Cubemap, m_HdriSkyParams.hdriSky);
                    m_IntegrateHDRISkyMaterial.SetFloat(HDShaderIDs._InvOmegaP, 1.0f / omegaP);

                    CoreUtils.SetRenderTarget(builtinParams.commandBuffer, m_IntensityTexture, ClearFlag.None);
                    CoreUtils.DrawFullScreen(builtinParams.commandBuffer, m_IntegrateHDRISkyMaterial);
                    m_HdriSkyParams.updateHDRISkyIntensity.value = false;
                }
            }
        }

        public override void RenderSky(BuiltinSkyParameters builtinParams, bool renderForCubemap)
        {
            float lux = (m_HdriSkyParams.skyIntensityMode == SkyIntensityMode.Lux) ? m_HdriSkyParams.lux.value : 1;
            float multiplier = (m_HdriSkyParams.skyIntensityMode == SkyIntensityMode.Exposure) ? m_HdriSkyParams.multiplier.value : 1;
            float exposure = (m_HdriSkyParams.skyIntensityMode == SkyIntensityMode.Exposure) ? GetExposure(m_HdriSkyParams, builtinParams.debugSettings) : 0;

            m_SkyHDRIMaterial.SetTexture(HDShaderIDs._Cubemap, m_HdriSkyParams.hdriSky);
            m_SkyHDRIMaterial.SetVector(HDShaderIDs._SkyParam, new Vector4(exposure, multiplier, -m_HdriSkyParams.rotation, lux)); // -rotation to match Legacy...

            using (new ProfilingSample(builtinParams.commandBuffer, "Draw sky"))
            {
                // Bind a white texture if the intensity mode isn't lux so we sample an intensity of 1
                if (m_HdriSkyParams.skyIntensityMode == SkyIntensityMode.Lux)
                    m_SkyHDRIMaterial.SetTexture(HDShaderIDs._SkyIntensity, m_IntensityTexture);
                else
                    m_SkyHDRIMaterial.SetTexture(HDShaderIDs._SkyIntensity, Texture2D.whiteTexture);
    
                // This matrix needs to be updated at the draw call frequency.
                m_PropertyBlock.SetMatrix(HDShaderIDs._PixelCoordToViewDirWS, builtinParams.pixelCoordToViewDirMatrix);
    
                CoreUtils.DrawFullScreen(builtinParams.commandBuffer, m_SkyHDRIMaterial, m_PropertyBlock, renderForCubemap ? 0 : 1);
            }
        }

        public override bool IsValid()
        {
            return m_HdriSkyParams != null && m_SkyHDRIMaterial != null;
        }
    }
}