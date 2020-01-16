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

        //RTHandle m_OctMap;
        RTHandle m_LatLongMap;
        //Material m_CubeToOct;
        Material m_CubeToLatLong;
        bool preValue = false;

        public ImportantSampler2D m_ImportanceSampler = null;

        Vector4     m_SkyIntensity;

        public HDRISkyRenderer()
        {
            m_ImportanceSampler = null;
        }

        public override void Build()
        {
            var hdrp = HDRenderPipeline.defaultAsset;
            m_SkyHDRIMaterial = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.shaders.hdriSkyPS);
            //m_CubeToOct = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.shaders.cubeToOctahedral);
            m_CubeToLatLong = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.shaders.cubeToPanoPS);
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(m_SkyHDRIMaterial);
        }

        private void GetParameters(out float intensity, out float phi, out float backplatePhi, BuiltinSkyParameters builtinParams, HDRISky hdriSky)
        {
            intensity       = GetSkyIntensity(hdriSky, builtinParams.debugSettings);
            phi             = -Mathf.Deg2Rad*hdriSky.rotation.value; // -rotation to match Legacy...
            backplatePhi    = phi - Mathf.Deg2Rad*hdriSky.plateRotation.value;
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

        public Vector4 GetSphereSkyIntegral()
        {
            return m_SkyIntensity;
        }

        public override void PreRenderSky(BuiltinSkyParameters builtinParams, bool renderForCubemap, bool renderSunDisk)
        {
            var hdriSky = builtinParams.skySettings as HDRISky;

            if (hdriSky.rectLightShadow.value != preValue)
            {
                preValue = hdriSky.rectLightShadow.value;
                int size = 1024;
                //m_OctMap = RTHandles.Alloc(size, size, slices:(int)Mathf.Log((float)size, 2.0f), useMipMap:true, autoGenerateMips: false,
                //                           colorFormat: Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat,
                //                           enableRandomWrite: true);
                //m_OctMap = RTHandles.Alloc(size, size,
                //                           colorFormat: Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat,
                //                           enableRandomWrite: true);
                m_LatLongMap = RTHandles.Alloc(size, size/2,
                                               colorFormat: Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat,
                                               enableRandomWrite: true);
                //m_CubeToOct.SetTexture(HDShaderIDs._Cubemap, hdriSky.hdriSky.value);
                m_CubeToLatLong.SetTexture("_srcCubeTexture", hdriSky.hdriSky.value);
                //Graphics.Blit(Texture2D.whiteTexture, m_OctMap, m_CubeToOct);
                //builtinParams.commandBuffer.Blit(Texture2D.whiteTexture, m_OctMap, m_CubeToOct, 0);
                builtinParams.commandBuffer.Blit(Texture2D.whiteTexture, m_LatLongMap, m_CubeToLatLong, 0);

                var hdrp = HDRenderPipeline.defaultAsset;
                Material integrator = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.shaders.integrateHdriSkyPS);
                integrator.SetFloat("_CoefForIntegration", 1.0f/(4096.0f*size*size));
                integrator.SetTexture(HDShaderIDs._Cubemap, hdriSky.hdriSky.value);
                RTHandle intensitySphereTexture = RTHandles.Alloc(1, 1, colorFormat: Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);
                Texture2D readBackTexture = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, false);
                Graphics.Blit(Texture2D.whiteTexture, intensitySphereTexture.rt, integrator, 1);
                RenderTexture.active = intensitySphereTexture.rt;
                readBackTexture.ReadPixels(new Rect(0.0f, 0.0f, 1, 1), 0, 0);
                RenderTexture.active = null;
                Color skyColor = readBackTexture.GetPixel(0, 0);
                m_SkyIntensity = skyColor;

                if (m_ImportanceSampler == null)
                    m_ImportanceSampler = new ImportantSampler2D();

                m_ImportanceSampler.Init(m_LatLongMap, m_SkyIntensity, builtinParams.commandBuffer);
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

            float intensity, phi, backplatePhi;
            GetParameters(out intensity, out phi, out backplatePhi, builtinParams, hdriSky);

            using (new ProfilingScope(builtinParams.commandBuffer, ProfilingSampler.Get(HDProfileId.PreRenderSky)))
            {
                m_SkyHDRIMaterial.SetTexture(HDShaderIDs._Cubemap, hdriSky.hdriSky.value);
                m_SkyHDRIMaterial.SetVector(HDShaderIDs._SkyParam, new Vector4(intensity, 0.0f, Mathf.Cos(phi), Mathf.Sin(phi)));
                m_SkyHDRIMaterial.SetVector(HDShaderIDs._BackplateParameters0, GetBackplateParameters0(hdriSky));

                m_PropertyBlock.SetMatrix(HDShaderIDs._PixelCoordToViewDirWS, builtinParams.pixelCoordToViewDirMatrix);

                CoreUtils.DrawFullScreen(builtinParams.commandBuffer, m_SkyHDRIMaterial, m_PropertyBlock, passID);
            }
        }

        public override void RenderSky(BuiltinSkyParameters builtinParams, bool renderForCubemap, bool renderSunDisk)
        {
            var hdriSky = builtinParams.skySettings as HDRISky;
            float intensity, phi, backplatePhi;
            GetParameters(out intensity, out phi, out backplatePhi, builtinParams, hdriSky);
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
                m_SkyHDRIMaterial.SetVector(HDShaderIDs._SkyParam, new Vector4(intensity, 0.0f, Mathf.Cos(phi), Mathf.Sin(phi)));
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
