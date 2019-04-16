using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class HDRISkyRenderer : SkyRenderer
    {
        Material m_SkyHDRIMaterial; // Renders a cubemap into a render texture (can be cube or 2D)
        MaterialPropertyBlock m_PropertyBlock;
        HDRISky m_HdriSkyParams;

        public HDRISkyRenderer(HDRISky hdriSkyParams)
        {
            m_HdriSkyParams = hdriSkyParams;
            m_PropertyBlock = new MaterialPropertyBlock();
        }

        public override void Build()
        {
            var hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            m_SkyHDRIMaterial = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.shaders.hdriSkyPS);
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(m_SkyHDRIMaterial);
        }

        public override void SetRenderTargets(BuiltinSkyParameters builtinParams)
        {
            if (builtinParams.depthBuffer == BuiltinSkyParameters.nullRT)
            {
                HDUtils.SetRenderTarget(builtinParams.commandBuffer, builtinParams.colorBuffer);
            }
            else
            {
                HDUtils.SetRenderTarget(builtinParams.commandBuffer, builtinParams.colorBuffer, builtinParams.depthBuffer);
            }
        }

        public override void RenderSky(BuiltinSkyParameters builtinParams, bool renderForCubemap, bool renderSunDisk)
        {
            float luxMultiplier = m_HdriSkyParams.desiredLuxValue.value / m_HdriSkyParams.upperHemisphereLuxValue.value;
            float multiplier = (m_HdriSkyParams.skyIntensityMode == SkyIntensityMode.Exposure) ? m_HdriSkyParams.multiplier.value : luxMultiplier;
            float exposure = (m_HdriSkyParams.skyIntensityMode == SkyIntensityMode.Exposure) ? GetExposure(m_HdriSkyParams, builtinParams.debugSettings) : 1;
            float phi = Mathf.Deg2Rad * -m_HdriSkyParams.rotation.value; // -rotation to match Legacy...

            m_SkyHDRIMaterial.SetTexture(HDShaderIDs._Cubemap, m_HdriSkyParams.hdriSky.value);
            m_SkyHDRIMaterial.SetVector(HDShaderIDs._SkyParam, new Vector4(exposure, multiplier, Mathf.Cos(phi), Mathf.Sin(phi)));

            using (new ProfilingSample(builtinParams.commandBuffer, "Draw sky"))
            {
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
