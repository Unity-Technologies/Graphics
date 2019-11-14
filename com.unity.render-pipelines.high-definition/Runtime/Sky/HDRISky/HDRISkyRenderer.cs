namespace UnityEngine.Rendering.HighDefinition
{
    class HDRISkyRenderer : SkyRenderer
    {
        Material m_SkyHDRIMaterial; // Renders a cubemap into a render texture (can be cube or 2D)
        MaterialPropertyBlock m_PropertyBlock = new MaterialPropertyBlock();

        private static int m_RenderCubemapID                                = 0; // FragBaking
        private static int m_RenderFullscreenSkyID                          = 1; // FragRender
        private static int m_RenderCubemapWithBackplateID                   = 2; // FragBakingBackplate
        private static int m_RenderFullscreenSkyWithBackplateID             = 3; // FragRenderBackplate
        private static int m_RenderDepthOnlyCubemapWithBackplateID          = 4; // FragBakingBackplateDepth
        private static int m_RenderDepthOnlyFullscreenSkyWithBackplateID    = 5; // FragRenderBackplateDepth

        //private RenderTexture m_OctahedralMap;
        //private RenderTexture m_PDFHorizontal;
        private RTHandle m_OctahedralMap;
        private RTHandle m_PDFHorizontal;

        private bool m_HDRISkyHash = false;

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

        private void GetParameters(out float multiplier, out float exposure, out float phi, out float backplatePhi, BuiltinSkyParameters builtinParams, HDRISky hdriSky)
        {
            float luxMultiplier = hdriSky.desiredLuxValue.value/hdriSky.upperHemisphereLuxValue.value;
            multiplier      = (hdriSky.skyIntensityMode.value == SkyIntensityMode.Exposure) ? hdriSky.multiplier.value : luxMultiplier;
            exposure        = (hdriSky.skyIntensityMode.value == SkyIntensityMode.Exposure) ? GetExposure(hdriSky, builtinParams.debugSettings) : 1;
            phi             = -Mathf.Deg2Rad*hdriSky.rotation.value; // -rotation to match Legacy...
            backplatePhi    = phi - Mathf.Deg2Rad*hdriSky.plateRotation.value;
        }

        private Vector4 GetBackplateParameters0(HDRISky hdriSky)
        {
            // xy: scale, z: groundLevel, w: projectionDistance
            return new Vector4(hdriSky.scale.value.x, hdriSky.scale.value.y, hdriSky.groundLevel.value, hdriSky.projectionDistance.value);
        }

        private Vector4 GetBackplateParameters1(float backplatePhi, HDRISky hdriSky)
        {
            // x: BackplateType, y: BlendAmount, zw: backplate rotation (cosPhi_plate, sinPhi_plate)
            float type = 3.0f;
            float blendAmount = hdriSky.blendAmount.value/100.0f;
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
            float localPhi = -Mathf.Deg2Rad*hdriSky.plateTexRotation.value;
            return new Vector4(Mathf.Cos(localPhi), Mathf.Sin(localPhi), hdriSky.plateTexOffset.value.x, hdriSky.plateTexOffset.value.y);
        }

        public void SaveOcta(AsyncGPUReadbackRequest request)
        {
            if (!request.hasError)
            {
                Unity.Collections.NativeArray<float> result = request.GetData<float>();
                float[] copy = new float[result.Length];
                result.CopyTo(copy);
                byte[] bytes0 = ImageConversion.EncodeArrayToEXR(copy, Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat, (uint)request.width, (uint)request.height, 0, Texture2D.EXRFlags.CompressZIP);
                string path = @"C:\UProjects\OctSkyExport.exr";
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.SetAttributes(path, System.IO.FileAttributes.Normal);
                    System.IO.File.Delete(path);
                }
                System.IO.File.WriteAllBytes(path, bytes0);
            }
        }

        public override void PreRenderSky(BuiltinSkyParameters builtinParams, bool renderForCubemap, bool renderSunDisk)
        {
            var hdriSky = builtinParams.skySettings as HDRISky;

            bool currentHash = hdriSky.rectLightShadow.value;
            if (m_HDRISkyHash != currentHash)
            {
                m_HDRISkyHash = currentHash;

                const int size = 1024;

                RenderTextureDescriptor desc0 = new RenderTextureDescriptor(size, size, Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat, 0);
                desc0.enableRandomWrite = true;
                RenderTextureDescriptor desc1 = new RenderTextureDescriptor(size, 32,   Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat, 0);
                desc1.enableRandomWrite = true;
                m_OctahedralMap = RTHandles.Alloc(size, size, colorFormat: Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat, enableRandomWrite: true, name: "OctahedralMap");

                Cubemap cubemap = hdriSky.hdriSky.value;

                var hdrp = HDRenderPipeline.defaultAsset;
                Material cubeToOctahedral = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.shaders.cubeToOctahedral);

                cubeToOctahedral.SetTexture(HDShaderIDs._Cubemap, cubemap);
                builtinParams.commandBuffer.SetRenderTarget(m_OctahedralMap);
                builtinParams.commandBuffer.SetViewport(new Rect(0.0f, 0.0f, (float)size, (float)size));
                CoreUtils.DrawFullScreen(builtinParams.commandBuffer, cubeToOctahedral);
                builtinParams.commandBuffer.RequestAsyncReadback(m_OctahedralMap, SaveOcta);

                ImportantSampler2D is2d = new ImportantSampler2D();

                is2d.Init(m_OctahedralMap, builtinParams.commandBuffer);
            }

            if (hdriSky.enableBackplate.value == false)
            {
                return;
            }

            int passID;
            if (renderForCubemap)
                passID = m_RenderDepthOnlyCubemapWithBackplateID;
            else
                passID = m_RenderDepthOnlyFullscreenSkyWithBackplateID;

            float multiplier, exposure, phi, backplatePhi;
            GetParameters(out multiplier, out exposure, out phi, out backplatePhi, builtinParams, hdriSky);

            using (new ProfilingSample(builtinParams.commandBuffer, "Draw PreSky"))
            {
                m_SkyHDRIMaterial.SetTexture(HDShaderIDs._Cubemap, hdriSky.hdriSky.value);
                m_SkyHDRIMaterial.SetVector(HDShaderIDs._SkyParam, new Vector4(exposure, multiplier, Mathf.Cos(phi), Mathf.Sin(phi)));
                m_SkyHDRIMaterial.SetVector(HDShaderIDs._BackplateParameters0, GetBackplateParameters0(hdriSky));

                m_PropertyBlock.SetMatrix(HDShaderIDs._PixelCoordToViewDirWS, builtinParams.pixelCoordToViewDirMatrix);

                CoreUtils.DrawFullScreen(builtinParams.commandBuffer, m_SkyHDRIMaterial, m_PropertyBlock, passID);
            }
        }

        public override void RenderSky(BuiltinSkyParameters builtinParams, bool renderForCubemap, bool renderSunDisk)
        {
            var hdriSky = builtinParams.skySettings as HDRISky;
            float multiplier, exposure, phi, backplatePhi;
            GetParameters(out multiplier, out exposure, out phi, out backplatePhi, builtinParams, hdriSky);
            m_SkyHDRIMaterial.SetTexture(HDShaderIDs._Cubemap, hdriSky.hdriSky.value);
            m_SkyHDRIMaterial.SetVector(HDShaderIDs._SkyParam, new Vector4(exposure, multiplier, Mathf.Cos(phi), Mathf.Sin(phi)));
            int passID;
            if (hdriSky.enableBackplate.value == false)
            {
                if (renderForCubemap)
                    passID = m_RenderCubemapID;
                else
                    passID = m_RenderFullscreenSkyID;
            }
            else
            {
                if (renderForCubemap)
                    passID = m_RenderCubemapWithBackplateID;
                else
                    passID = m_RenderFullscreenSkyWithBackplateID;
            }
            m_SkyHDRIMaterial.SetTexture(HDShaderIDs._Cubemap, hdriSky.hdriSky.value);
            m_SkyHDRIMaterial.SetVector(HDShaderIDs._SkyParam, new Vector4(exposure, multiplier, Mathf.Cos(phi), Mathf.Sin(phi)));

            using (new ProfilingSample(builtinParams.commandBuffer, "Draw sky"))
            {
                m_SkyHDRIMaterial.SetTexture(HDShaderIDs._Cubemap, hdriSky.hdriSky.value);
                m_SkyHDRIMaterial.SetVector(HDShaderIDs._SkyParam, new Vector4(exposure, multiplier, Mathf.Cos(phi), Mathf.Sin(phi)));
                m_SkyHDRIMaterial.SetVector(HDShaderIDs._BackplateParameters0, GetBackplateParameters0(hdriSky));
                m_SkyHDRIMaterial.SetVector(HDShaderIDs._BackplateParameters1, GetBackplateParameters1(backplatePhi, hdriSky));
                m_SkyHDRIMaterial.SetVector(HDShaderIDs._BackplateParameters2, GetBackplateParameters2(hdriSky));
                m_SkyHDRIMaterial.SetColor(HDShaderIDs._BackplateShadowTint, hdriSky.shadowTint.value);
                uint shadowFilter = 0u;
                if (hdriSky.pointLightShadow.value)
                    shadowFilter |= unchecked((uint)LightFeatureFlags.Punctual);
                if (hdriSky.dirLightShadow.value)
                    shadowFilter |= unchecked((uint)LightFeatureFlags.Directional);
                if (hdriSky.rectLightShadow.value)
                    shadowFilter |= unchecked((uint)LightFeatureFlags.Area);
                m_SkyHDRIMaterial.SetInt(HDShaderIDs._BackplateShadowFilter, unchecked((int)shadowFilter));

                // This matrix needs to be updated at the draw call frequency.
                m_PropertyBlock.SetMatrix(HDShaderIDs._PixelCoordToViewDirWS, builtinParams.pixelCoordToViewDirMatrix);

                CoreUtils.DrawFullScreen(builtinParams.commandBuffer, m_SkyHDRIMaterial, m_PropertyBlock, passID);
            }
        }
    }
}
