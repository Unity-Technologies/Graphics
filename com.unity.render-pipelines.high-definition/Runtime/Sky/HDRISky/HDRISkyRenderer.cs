namespace UnityEngine.Rendering.HighDefinition
{
    class HDRISkyRenderer : SkyRenderer
    {
        Material m_SkyHDRIMaterial; // Renders a cubemap into a render texture (can be cube or 2D)
        MaterialPropertyBlock m_PropertyBlock;
        HDRISky m_HdriSkyParams;

        private static int m_RenderCubemapID                                = 0;
        private static int m_RenderFullscreenSkyID                          = 1;
        private static int m_RenderCubemapWithBackplateID                   = 2;
        private static int m_RenderFullscreenSkyWithBackplateID             = 3;
        private static int m_RenderDepthOnlyCubemapWithBackplateID          = 4;
        private static int m_RenderDepthOnlyFullscreenSkyWithBackplateID    = 5;

        public HDRISkyRenderer(HDRISky hdriSkyParams)
        {
            m_HdriSkyParams = hdriSkyParams;
            m_PropertyBlock = new MaterialPropertyBlock();
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

        private void GetParameters(out float multiplier, out float exposure, out float phi, out float backplatePhi, BuiltinSkyParameters builtinParams)
        {
            float luxMultiplier = m_HdriSkyParams.desiredLuxValue.value/m_HdriSkyParams.upperHemisphereLuxValue.value;
            multiplier      = (m_HdriSkyParams.skyIntensityMode.value == SkyIntensityMode.Exposure) ? m_HdriSkyParams.multiplier.value : luxMultiplier;
            exposure        = (m_HdriSkyParams.skyIntensityMode.value == SkyIntensityMode.Exposure) ? GetExposure(m_HdriSkyParams, builtinParams.debugSettings) : 1;
            phi             = -Mathf.Deg2Rad*m_HdriSkyParams.rotation.value; // -rotation to match Legacy...
            backplatePhi    = phi - Mathf.Deg2Rad*m_HdriSkyParams.plateRotation.value;
        }

        private Vector4 GetBackplateParameters0()
        {
            // _BackplateParameters0; // xy: scale, z: groundLevel, w: projectionRadius
            return new Vector4(m_HdriSkyParams.scale.value.x, m_HdriSkyParams.scale.value.y, m_HdriSkyParams.groundLevel.value, m_HdriSkyParams.projectionDistance.value);
        }

        private Vector4 GetBackplateParameters1(float backplatePhi)
        {
            // _BackplateParameters1; // x: BackplateType, y: BlendAmount
            float type = 3.0f;
            float blendAmount = m_HdriSkyParams.blendAmount.value/100.0f;
            switch (m_HdriSkyParams.backplateType.value)
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

        public override void PreRenderSky(BuiltinSkyParameters builtinParams, bool renderForCubemap, bool renderSunDisk)
        {
            if (m_HdriSkyParams.enableBackplate.value == false)
            {
                return;
            }

            int passID;
            if (renderForCubemap)
                passID = m_RenderDepthOnlyCubemapWithBackplateID;
            else
                passID = m_RenderDepthOnlyFullscreenSkyWithBackplateID;

            float multiplier, exposure, phi, backplatePhi;
            GetParameters(out multiplier, out exposure, out phi, out backplatePhi, builtinParams);

            using (new ProfilingSample(builtinParams.commandBuffer, "Draw PreSky"))
            {
                m_SkyHDRIMaterial.SetTexture(HDShaderIDs._Cubemap, m_HdriSkyParams.hdriSky.value);
                m_SkyHDRIMaterial.SetVector(HDShaderIDs._SkyParam, new Vector4(exposure, multiplier, Mathf.Cos(phi), Mathf.Sin(phi)));

                m_SkyHDRIMaterial.SetVector(HDShaderIDs._BackplateParameters0, GetBackplateParameters0());

                m_PropertyBlock.SetMatrix(HDShaderIDs._PixelCoordToViewDirWS, builtinParams.pixelCoordToViewDirMatrix);

                CoreUtils.DrawFullScreen(builtinParams.commandBuffer, m_SkyHDRIMaterial, m_PropertyBlock, passID);
            }
        }

        public override void RenderSky(BuiltinSkyParameters builtinParams, bool renderForCubemap, bool renderSunDisk)
        {
            float multiplier, exposure, phi, backplatePhi;
            GetParameters(out multiplier, out exposure, out phi, out backplatePhi, builtinParams);

            int passID;
            if (m_HdriSkyParams.enableBackplate.value == false)
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

            using (new ProfilingSample(builtinParams.commandBuffer, "Draw sky"))
            {
                m_SkyHDRIMaterial.SetTexture(HDShaderIDs._Cubemap, m_HdriSkyParams.hdriSky.value);
                m_SkyHDRIMaterial.SetVector(HDShaderIDs._SkyParam, new Vector4(exposure, multiplier, Mathf.Cos(phi), Mathf.Sin(phi)));
                m_SkyHDRIMaterial.SetVector(HDShaderIDs._BackplateParameters0, GetBackplateParameters0());
                m_SkyHDRIMaterial.SetVector(HDShaderIDs._BackplateParameters1, GetBackplateParameters1(backplatePhi));
                m_SkyHDRIMaterial.SetColor(HDShaderIDs._BackplateShadowTint, m_HdriSkyParams.shadowTint.value);
                uint shadowFilter = 0u;
                if (m_HdriSkyParams.pointLightShadow.value)
                    shadowFilter |= unchecked((uint)LightFeatureFlags.Punctual);
                if (m_HdriSkyParams.dirLightShadow.value)
                    shadowFilter |= unchecked((uint)LightFeatureFlags.Directional);
                if (m_HdriSkyParams.rectLightShadow.value)
                    shadowFilter |= unchecked((uint)LightFeatureFlags.Area);
                m_SkyHDRIMaterial.SetInt(HDShaderIDs._BackplateShadowFilter, unchecked((int)shadowFilter));

                // This matrix needs to be updated at the draw call frequency.
                m_PropertyBlock.SetMatrix(HDShaderIDs._PixelCoordToViewDirWS, builtinParams.pixelCoordToViewDirMatrix);

                CoreUtils.DrawFullScreen(builtinParams.commandBuffer, m_SkyHDRIMaterial, m_PropertyBlock, passID);
            }
        }

        public override bool IsValid()
        {
            return m_HdriSkyParams != null && m_SkyHDRIMaterial != null;
        }
    }
}
