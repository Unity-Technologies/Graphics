namespace UnityEngine.Rendering.Universal
{
    class GradientSkyRenderer : SkyRenderer
    {
        Material m_GradientSkyMaterial;
        MaterialPropertyBlock m_PropertyBlock = new MaterialPropertyBlock();

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

            m_PropertyBlock.SetMatrix(SkyShaderConstants._PixelCoordToViewDirWS, cameraData.pixelCoordToViewDirMatrix);

            CoreUtils.DrawFullScreen(cmd, m_GradientSkyMaterial, m_PropertyBlock, 1);

            cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
        }

    }
}
