namespace UnityEngine.Rendering.HighDefinition
{
    class HDRISkyRenderer : SkyRenderer
    {
        Material m_SkyHDRIMaterial; // Renders a cubemap into a render texture (can be cube or 2D)
        MaterialPropertyBlock m_PropertyBlock = new MaterialPropertyBlock();

        public HDRISkyRenderer()
        {
        }

        public override void Build()
        {
            var hdrp = HDRenderPipeline.defaultAsset;
            m_SkyHDRIMaterial = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.shaders.hdriSkyPS);
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(m_SkyHDRIMaterial);
        }

        public override void RenderSky(BuiltinSkyParameters builtinParams, bool renderForCubemap, bool renderSunDisk)
        {
            var hdriSky = builtinParams.skySettings as HDRISky;
            float luxMultiplier = hdriSky.desiredLuxValue.value / hdriSky.upperHemisphereLuxValue.value;
            float multiplier = (hdriSky.skyIntensityMode.value == SkyIntensityMode.Exposure) ? hdriSky.multiplier.value : luxMultiplier;
            float exposure = (hdriSky.skyIntensityMode.value == SkyIntensityMode.Exposure) ? GetExposure(hdriSky, builtinParams.debugSettings) : 1;
            float phi = Mathf.Deg2Rad * -hdriSky.rotation.value; // -rotation to match Legacy...

            m_SkyHDRIMaterial.SetTexture(HDShaderIDs._Cubemap, hdriSky.hdriSky.value);
            m_SkyHDRIMaterial.SetVector(HDShaderIDs._SkyParam, new Vector4(exposure, multiplier, Mathf.Cos(phi), Mathf.Sin(phi)));

            using (new ProfilingSample(builtinParams.commandBuffer, "Draw sky"))
            {
                // This matrix needs to be updated at the draw call frequency.
                m_PropertyBlock.SetMatrix(HDShaderIDs._PixelCoordToViewDirWS, builtinParams.pixelCoordToViewDirMatrix);

                CoreUtils.DrawFullScreen(builtinParams.commandBuffer, m_SkyHDRIMaterial, m_PropertyBlock, renderForCubemap ? 0 : 1);
            }
        }
    }
}
