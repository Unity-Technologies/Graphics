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
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(m_GradientSkyMaterial);
        }

        public override void RenderSky(ref CameraData cameraData, CommandBuffer cmd)
        {
            Camera camera = cameraData.camera;

            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);

            var gradientSky = (GradientSky)cameraData.visualSky.skySettings;
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
            var gradientSky = (GradientSky)cameraData.visualSky.skySettings;

            // TODO: Support sky intensity.

            var ambientProbe = new SphericalHarmonicsL2();
            var gradientDiffusion = gradientSky.gradientDiffusion.value;
            if (gradientDiffusion != 0f)
            {
                var isFlipped = gradientDiffusion < 0f;
                if (isFlipped)
                    gradientDiffusion *= -1f;

                // TODO: Find a physically correct way to calculate irradiance for any gradientDiffusion.
                // Magic numbers to get similar SH values for gradientDiffusion == 1 as in the built-in tri-light mode.
                var middleColorWeightOnEquator = 0.833f / gradientDiffusion;
                var middleColorWeightOnPoles = 0.656f / gradientDiffusion;

                var topColor = gradientSky.top.value;
                var middleColor = gradientSky.middle.value;
                var bottomColor = gradientSky.bottom.value;

                var equatorIrradiance = Color.Lerp((topColor + bottomColor) * 0.5f, middleColor, middleColorWeightOnEquator);
                ambientProbe.AddAmbientLight(equatorIrradiance);
                ambientProbe.AddDirectionalLight(Vector3.up, Color.Lerp(isFlipped ? bottomColor : topColor, middleColor, middleColorWeightOnPoles) - equatorIrradiance, 1f);
                ambientProbe.AddDirectionalLight(Vector3.down, Color.Lerp(isFlipped ? topColor : bottomColor, middleColor, middleColorWeightOnPoles) - equatorIrradiance, 1f);
            }
            else
            {
                ambientProbe.AddAmbientLight(gradientSky.middle.value);
            }

            return ambientProbe;
        }
    }
}
