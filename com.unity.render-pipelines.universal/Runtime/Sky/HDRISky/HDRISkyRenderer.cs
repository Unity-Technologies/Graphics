namespace UnityEngine.Rendering.Universal
{
    class HDRISkyRenderer : SkyRenderer
    {
        Material m_HDRISkyMaterial;
        MaterialPropertyBlock m_PropertyBlock = new MaterialPropertyBlock();

        static readonly int SHADER_PASS_SKY = 0;
        static readonly int SHADER_PASS_SKY_WITH_BACKPLATE = 1;
        static readonly int SHADER_PASS_SKY_BACKPLATE_PRERENDER = 2;

        static readonly int _Cubemap = Shader.PropertyToID("_Cubemap");
        static readonly int _SkyParam = Shader.PropertyToID("_SkyParam");
        static readonly int _BackplateParameters0 = Shader.PropertyToID("_BackplateParameters0");
        static readonly int _BackplateParameters1 = Shader.PropertyToID("_BackplateParameters1");
        static readonly int _BackplateParameters2 = Shader.PropertyToID("_BackplateParameters2");

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

        private void GetParameters(out float intensity, out float phi, out float backplatePhi, HDRISky hdriSky)
        {
            //intensity = GetSkyIntensity(hdriSky); // TODO Enable again
            intensity = 1.0f;
            phi = -Mathf.Deg2Rad * hdriSky.rotation.value; // -rotation to match Legacy...
            backplatePhi = phi - Mathf.Deg2Rad * hdriSky.plateRotation.value;
        }

        private Vector4 GetBackplateParameters0(HDRISky hdriSky)
        {
            // xy: scale, z: groundLevel, w: projectionDistance
            float scaleX = Mathf.Abs(hdriSky.scale.value.x);
            float scaleY = Mathf.Abs(hdriSky.scale.value.y);

            if (hdriSky.backplateType.value == BackplateType.Disc)
            {
                scaleY = scaleX;
            }

            return new Vector4(scaleX, scaleY, hdriSky.groundLevel.value, hdriSky.projectionDistance.value);
        }

        private Vector4 GetBackplateParameters1(float backplatePhi, HDRISky hdriSky)
        {
            // x: BackplateType, y: BlendAmount, zw: backplate rotation (cosPhi_plate, sinPhi_plate)
            float type = 3.0f;
            float blendAmount = hdriSky.blendAmount.value / 100.0f;
            switch (hdriSky.backplateType.value)
            {
                case BackplateType.Disc:
                    type = 0.0f;
                    break;
                case BackplateType.Rectangle:
                    type = 1.0f;
                    break;
                case BackplateType.Ellipse:
                    type = 2.0f;
                    break;
                case BackplateType.Infinite:
                    type = 3.0f;
                    blendAmount = 0.0f;
                    break;
            }
            return new Vector4(type, blendAmount, Mathf.Cos(backplatePhi), Mathf.Sin(backplatePhi));
        }

        private Vector4 GetBackplateParameters2(HDRISky hdriSky)
        {
            // xy: BackplateTextureRotation (cos/sin), zw: Backplate Texture Offset
            float localPhi = -Mathf.Deg2Rad * hdriSky.plateTexRotation.value;
            return new Vector4(Mathf.Cos(localPhi), Mathf.Sin(localPhi), hdriSky.plateTexOffset.value.x, hdriSky.plateTexOffset.value.y);
        }

        public override void PrerenderSky(ref CameraData cameraData, CommandBuffer cmd)
        {
            var hdriSky = (HDRISky)cameraData.visualSky.skySettings;
            if (!hdriSky.enableBackplate.value)
                return;

            float intensity, phi, backplatePhi;
            GetParameters(out intensity, out phi, out backplatePhi, hdriSky);

            m_HDRISkyMaterial.SetVector(_BackplateParameters0, GetBackplateParameters0(hdriSky));
            m_HDRISkyMaterial.SetVector(_BackplateParameters1, GetBackplateParameters1(backplatePhi, hdriSky));

            m_PropertyBlock.SetMatrix(SkyShaderConstants._PixelCoordToViewDirWS, cameraData.pixelCoordToViewDirMatrix);

            CoreUtils.DrawFullScreen(cmd, m_HDRISkyMaterial, m_PropertyBlock, SHADER_PASS_SKY_BACKPLATE_PRERENDER);
        }

        public override void RenderSky(ref CameraData cameraData, CommandBuffer cmd)
        {
            var hdriSky = (HDRISky)cameraData.visualSky.skySettings;
            int shaderPass = !hdriSky.enableBackplate.value ? SHADER_PASS_SKY : SHADER_PASS_SKY_WITH_BACKPLATE;

            float intensity, phi, backplatePhi;
            GetParameters(out intensity, out phi, out backplatePhi, hdriSky);

            m_HDRISkyMaterial.SetTexture(_Cubemap, hdriSky.hdriSky.value);
            m_HDRISkyMaterial.SetVector(_SkyParam, new Vector4(intensity, 0.0f, Mathf.Cos(phi), Mathf.Sin(phi)));

            m_HDRISkyMaterial.SetVector(_BackplateParameters0, GetBackplateParameters0(hdriSky));
            m_HDRISkyMaterial.SetVector(_BackplateParameters1, GetBackplateParameters1(backplatePhi, hdriSky));
            m_HDRISkyMaterial.SetVector(_BackplateParameters2, GetBackplateParameters2(hdriSky));

            m_PropertyBlock.SetMatrix(SkyShaderConstants._PixelCoordToViewDirWS, cameraData.pixelCoordToViewDirMatrix);

            CoreUtils.DrawFullScreen(cmd, m_HDRISkyMaterial, m_PropertyBlock, shaderPass);
        }

        public override SphericalHarmonicsL2 GetAmbientProbe(ref CameraData cameraData)
        {
            // TODO
            var ambientProbe = new SphericalHarmonicsL2();
            return ambientProbe;
        }
    }
}
