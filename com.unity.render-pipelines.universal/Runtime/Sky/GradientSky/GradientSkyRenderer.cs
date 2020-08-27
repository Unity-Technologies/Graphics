namespace UnityEngine.Rendering.Universal
{
    class GradientSkyRenderer : SkyRenderer
    {
        Material m_GradientSkyMaterial;
        MaterialPropertyBlock m_PropertyBlock = new MaterialPropertyBlock();

        readonly int _GradientBottom = Shader.PropertyToID("_GradientBottom");
        readonly int _GradientMiddle = Shader.PropertyToID("_GradientMiddle");
        readonly int _GradientTop = Shader.PropertyToID("_GradientTop");
        readonly int _GradientDiffusion = Shader.PropertyToID("_GradientDiffusion");

        public override void Build()
        {
            var urpRendererData = UniversalRenderPipeline.asset.scriptableRendererData;
            if (urpRendererData is ForwardRendererData)
            {
                m_GradientSkyMaterial = CoreUtils.CreateEngineMaterial((urpRendererData as ForwardRendererData).shaders.skyGradientSkyPS);
            }
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(m_GradientSkyMaterial);
        }

        public override void RenderSky(ref CameraData cameraData, CommandBuffer cmd)
        {
            Camera camera = cameraData.camera;

            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);

            var gradientSky = (cameraData.visualSky.skySettings as GradientSky);
            m_GradientSkyMaterial.SetColor(_GradientBottom, gradientSky.bottom.value);
            m_GradientSkyMaterial.SetColor(_GradientMiddle, gradientSky.middle.value);
            m_GradientSkyMaterial.SetColor(_GradientTop, gradientSky.top.value);
            m_GradientSkyMaterial.SetFloat(_GradientDiffusion, gradientSky.gradientDiffusion.value);
            //m_GradientSkyMaterial.SetFloat(SkyShaderConstants._SkyIntensity, GetSkyIntensity(gradientSky)); // TODO Enable again
            m_GradientSkyMaterial.SetFloat(SkyShaderConstants._SkyIntensity, 1.0f);

            m_PropertyBlock.SetMatrix(SkyShaderConstants._PixelCoordToViewDirWS, cameraData.pixelCoordToViewDirMatrix);

            CoreUtils.DrawFullScreen(cmd, m_GradientSkyMaterial, m_PropertyBlock, 0);

            cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
        }

    }
}
