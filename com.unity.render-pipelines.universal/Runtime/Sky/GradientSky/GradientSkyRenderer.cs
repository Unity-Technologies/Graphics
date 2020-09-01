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
            if (urpRendererData is ForwardRendererData forwardRendererData)
            {
                m_GradientSkyMaterial = CoreUtils.CreateEngineMaterial(forwardRendererData.shaders.skyGradientSkyPS);
            }
            else if (urpRendererData is DeferredRendererData deferredRendererData)
            {
                m_GradientSkyMaterial = CoreUtils.CreateEngineMaterial(deferredRendererData.shaders.skyGradientSkyPS);
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

        public override SphericalHarmonicsL2 GetAmbientProbe(ref CameraData cameraData)
        {
            var gradientSky = (cameraData.visualSky.skySettings as GradientSky);

            // TODO: Figure out a proper way to calculate the SH. Support gradientDiffusion and sky intensity.
            var ambientProbe = new SphericalHarmonicsL2();
            var middleColor = gradientSky.middle.value;
            ambientProbe.AddAmbientLight(middleColor);
            ambientProbe.AddDirectionalLight(Vector3.up, middleColor, -1f);
            ambientProbe.AddDirectionalLight(Vector3.down, middleColor, -1f);
            ambientProbe.AddDirectionalLight(Vector3.up, gradientSky.top.value, 1f);
            ambientProbe.AddDirectionalLight(Vector3.down, gradientSky.bottom.value, 1f);

            return ambientProbe;
        }
    }
}
