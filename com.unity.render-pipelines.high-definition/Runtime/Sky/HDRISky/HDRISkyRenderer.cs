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

        internal Texture    m_CurrentCubemap = null;

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

        public override void PreRenderSky(BuiltinSkyParameters builtinParams, bool renderForCubemap, bool renderSunDisk)
        {
            var hdriSky = builtinParams.skySettings as HDRISky;

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

        static private void Default(AsyncGPUReadbackRequest request, string name)
        {
            if (!request.hasError)
            {
                Unity.Collections.NativeArray<float> result = request.GetData<float>();
                float[] copy = new float[result.Length];
                result.CopyTo(copy);
                byte[] bytes0 = ImageConversion.EncodeArrayToEXR(copy, Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat, (uint)request.width, (uint)request.height, 0, Texture2D.EXRFlags.CompressZIP);
                string path = @"C:\UProjects\" + name + ".exr";
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.SetAttributes(path, System.IO.FileAttributes.Normal);
                    System.IO.File.Delete(path);
                }
                System.IO.File.WriteAllBytes(path, bytes0);
            }
        }

        static bool notDone = true;
        public override void RenderSky(BuiltinSkyParameters builtinParams, bool renderForCubemap, bool renderSunDisk)
        {
            var hdriSky = builtinParams.skySettings as HDRISky;
            m_CurrentCubemap = hdriSky.hdriSky.value;

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

            m_SkyHDRIMaterial.SetTexture(HDShaderIDs._Cubemap, m_CurrentCubemap);
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

            if (hdriSky.hdriSky.value != null && notDone)
            {
                return;
                RTHandle marg;
                RTHandle condMarg;
                bool done = GenerateMarginalTexture(out marg, out condMarg, builtinParams.commandBuffer);
                if (marg != null && condMarg != null)
                {
                    RTHandleDeleter.ScheduleRelease(marg);
                    RTHandleDeleter.ScheduleRelease(condMarg);
                }
                if (done)
                    notDone = false;
            }
        }


        public bool GenerateMarginalTexture(out RTHandle marginal, out RTHandle conditionalMarginal, CommandBuffer cmd)
        {
            marginal            = null;
            conditionalMarginal = null;

            if (m_CurrentCubemap == null)
                return false;

            int width   = 4*m_CurrentCubemap.width;
            int height  = 2*m_CurrentCubemap.width;

            Experimental.Rendering.GraphicsFormat gfxFormat = Experimental.Rendering.GraphicsFormat.R32_SFloat;

            RTHandle latLongMap = RTHandles.Alloc(  width, height,
                                                    colorFormat: gfxFormat,
                                                    enableRandomWrite: true);
            RTHandleDeleter.ScheduleRelease(latLongMap);

            var hdrp = HDRenderPipeline.defaultAsset;
            Material cubeToLatLong = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.shaders.cubeToPanoPS);
            cubeToLatLong.SetTexture("_srcCubeTexture", m_CurrentCubemap);
            cubeToLatLong.SetInt    ("_cubeMipLvl",                 0);
            cubeToLatLong.SetInt    ("_cubeArrayIndex",             0);
            cubeToLatLong.SetInt    ("_buildPDF",                   1);
            cubeToLatLong.SetInt    ("_preMultiplyByJacobian",      1);
            cubeToLatLong.SetInt    ("_preMultiplyBySolidAngle",    0);
            cubeToLatLong.SetVector (HDShaderIDs._Sizes, new Vector4(      (float)width,        (float)height,
                                                                     1.0f/((float)width), 1.0f/((float)height)));
            cmd.Blit(m_CurrentCubemap, latLongMap, cubeToLatLong, 0);

            void DefaultDumper(AsyncGPUReadbackRequest request, string name, Experimental.Rendering.GraphicsFormat format)
            {
                if (!request.hasError)
                {
                    Unity.Collections.NativeArray<float> result = request.GetData<float>();
                    float[] copy = new float[result.Length];
                    result.CopyTo(copy);
                    byte[] bytes0 = ImageConversion.EncodeArrayToEXR(
                                                        copy,
                                                        format,
                                                        (uint)request.width, (uint)request.height, 0,
                                                        Texture2D.EXRFlags.CompressZIP);
                    string path = @"C:\UProjects\" + name + ".exr";
                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.SetAttributes(path, System.IO.FileAttributes.Normal);
                        System.IO.File.Delete(path);
                    }
                    System.IO.File.WriteAllBytes(path, bytes0);
                }
            }

            cmd.RequestAsyncReadback(latLongMap, delegate (AsyncGPUReadbackRequest request)
            {
                DefaultDumper(request, "___LatLongPDFJacobian", latLongMap.rt.graphicsFormat);
            });

            // Begin: Integrate Equirectangular Map
            RTHandle cdf = GPUScan.ComputeOperation(latLongMap,
                                                    cmd,
                                                    GPUScan.Operation.Add,
                                                    GPUScan.Direction.Horizontal,
                                                    gfxFormat);
            RTHandleDeleter.ScheduleRelease(cdf);
            RTHandle lastCol = RTHandles.Alloc(1, cdf.rt.height, enableRandomWrite: true, colorFormat: gfxFormat);
            RTHandleDeleter.ScheduleRelease(lastCol);
            cmd.CopyTexture(cdf, 0, 0, cdf.rt.width - 1, 0, 1, cdf.rt.height, lastCol, 0, 0, 0, 0);
            RTHandle integral = GPUScan.ComputeOperation(lastCol,
                                                         cmd,
                                                         GPUScan.Operation.Add,
                                                         GPUScan.Direction.Vertical,
                                                         gfxFormat);
            RTHandleDeleter.ScheduleRelease(integral);
            // Normalize the LatLong to have integral over sphere to be == 1
            GPUArithmetic.ComputeOperation(latLongMap, latLongMap, integral, cmd, GPUArithmetic.Operation.Div);
            // End: Integrate Equirectangular Map

            cmd.RequestAsyncReadback(latLongMap, delegate (AsyncGPUReadbackRequest request)
            {
                DefaultDumper(request, "___LatLongIntegrated", latLongMap.rt.graphicsFormat);
            });

            ImportanceSampler2D.GenerateMarginals(out marginal, out conditionalMarginal, latLongMap, cmd, true, 0);

            return true;
        }
    }
}
