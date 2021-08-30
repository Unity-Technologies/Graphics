namespace UnityEngine.Rendering.HighDefinition
{
    class GradientSkyRenderer : SkyRenderer
    {
        Material m_GradientSkyMaterial; // Renders a cubemap into a render texture (can be cube or 2D)
        MaterialPropertyBlock m_PropertyBlock = new MaterialPropertyBlock();

        readonly int _GradientBottom = Shader.PropertyToID("_GradientBottom");
        readonly int _GradientMiddle = Shader.PropertyToID("_GradientMiddle");
        readonly int _GradientTop = Shader.PropertyToID("_GradientTop");
        readonly int _GradientDiffusion = Shader.PropertyToID("_GradientDiffusion");

        public GradientSkyRenderer()
        {
            SupportDynamicSunLight = false;
        }

        public override void Build()
        {
            m_GradientSkyMaterial = CoreUtils.CreateEngineMaterial(HDRenderPipelineGlobalSettings.instance.renderPipelineResources.shaders.gradientSkyPS);
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(m_GradientSkyMaterial);
        }

        public override void RenderSky(BuiltinSkyParameters builtinParams, bool renderForCubemap, bool renderSunDisk)
        {
            var gradientSky = builtinParams.skySettings as GradientSky;
            m_GradientSkyMaterial.SetColor(_GradientBottom, gradientSky.bottom.value);
            m_GradientSkyMaterial.SetColor(_GradientMiddle, gradientSky.middle.value);
            m_GradientSkyMaterial.SetColor(_GradientTop, gradientSky.top.value);
            m_GradientSkyMaterial.SetFloat(_GradientDiffusion, gradientSky.gradientDiffusion.value);
            m_GradientSkyMaterial.SetFloat(HDShaderIDs._SkyIntensity, GetSkyIntensity(gradientSky, builtinParams.debugSettings));

            // This matrix needs to be updated at the draw call frequency.
            m_PropertyBlock.SetMatrix(HDShaderIDs._PixelCoordToViewDirWS, builtinParams.pixelCoordToViewDirMatrix);

            CoreUtils.DrawFullScreen(builtinParams.commandBuffer, m_GradientSkyMaterial, m_PropertyBlock, renderForCubemap ? 0 : 1);
        }
    }
}
