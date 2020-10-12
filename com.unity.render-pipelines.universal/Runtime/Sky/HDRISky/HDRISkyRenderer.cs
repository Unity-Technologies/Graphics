namespace UnityEngine.Rendering.Universal
{
    class HDRISkyRenderer : SkyRenderer
    {
        Material m_HDRISkyMaterial;
        MaterialPropertyBlock m_PropertyBlock = new MaterialPropertyBlock();

        readonly int _Cubemap = Shader.PropertyToID("_Cubemap");
        readonly int _SkyParam = Shader.PropertyToID("_SkyParam");

        public override void Build()
        {
            var urpRendererData = UniversalRenderPipeline.asset.scriptableRendererData;
            if (urpRendererData is ForwardRendererData forwardRendererData)
            {
                m_HDRISkyMaterial = CoreUtils.CreateEngineMaterial(forwardRendererData.shaders.skyHdriSkyPS);
            }
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(m_HDRISkyMaterial);
        }

        private void GetParameters(out float intensity, out float phi, HDRISky hdriSky)
        {
            //intensity = GetSkyIntensity(hdriSky); // TODO Enable again
            intensity = 1.0f;
            phi = -Mathf.Deg2Rad * hdriSky.rotation.value; // -rotation to match Legacy...
        }

        public override void RenderSky(ref CameraData cameraData, CommandBuffer cmd)
        {
            var hdriSky = (HDRISky)cameraData.visualSky.skySettings;
            float intensity, phi;
            GetParameters(out intensity, out phi, hdriSky);

            Camera camera = cameraData.camera;

            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);

            m_HDRISkyMaterial.SetTexture(_Cubemap, hdriSky.hdriSky.value);
            m_HDRISkyMaterial.SetVector(_SkyParam, new Vector4(intensity, 0.0f, Mathf.Cos(phi), Mathf.Sin(phi)));

            m_PropertyBlock.SetMatrix(SkyShaderConstants._PixelCoordToViewDirWS, cameraData.pixelCoordToViewDirMatrix);

            CoreUtils.DrawFullScreen(cmd, m_HDRISkyMaterial, m_PropertyBlock, 0);

            cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
        }

        public override SphericalHarmonicsL2 GetAmbientProbe(ref CameraData cameraData)
        {
            // TODO
            var ambientProbe = new SphericalHarmonicsL2();
            return ambientProbe;
        }
    }
}
