#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
    class HDRISkyRenderer : SkyRenderer
    {
        Material m_SkyHDRIMaterial; // Renders a cubemap into a render texture (can be cube or 2D)
        MaterialPropertyBlock m_PropertyBlock = new MaterialPropertyBlock();

        float scrollFactor = 0.0f, lastTime = 0.0f;

        private static int m_RenderCubemapID = 0; // FragBaking
        private static int m_RenderFullscreenSkyID = 1; // FragRender
        private static int m_RenderFullscreenSkyWithBackplateID = 2; // FragRenderBackplate
        private static int m_RenderDepthOnlyFullscreenSkyWithBackplateID = 3; // FragRenderBackplateDepth

        public HDRISkyRenderer()
        {
            SupportDynamicSunLight = false;
        }

        public override void Build()
        {
            m_SkyHDRIMaterial = CoreUtils.CreateEngineMaterial(HDRenderPipelineGlobalSettings.instance.renderPipelineResources.shaders.hdriSkyPS);
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(m_SkyHDRIMaterial);
        }

        private void GetParameters(out float intensity, out float phi, out float backplatePhi, BuiltinSkyParameters builtinParams, HDRISky hdriSky)
        {
            intensity = GetSkyIntensity(hdriSky, builtinParams.debugSettings);
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

        public override bool RequiresPreRenderSky(BuiltinSkyParameters builtinParams)
        {
            var hdriSky = builtinParams.skySettings as HDRISky;
            return hdriSky.enableBackplate.value;
        }

        public override void PreRenderSky(BuiltinSkyParameters builtinParams)
        {
            var hdriSky = builtinParams.skySettings as HDRISky;

            float intensity, phi, backplatePhi;
            GetParameters(out intensity, out phi, out backplatePhi, builtinParams, hdriSky);

            using (new ProfilingScope(builtinParams.commandBuffer, ProfilingSampler.Get(HDProfileId.PreRenderSky)))
            {
                m_SkyHDRIMaterial.SetTexture(HDShaderIDs._Cubemap, hdriSky.hdriSky.value);
                m_SkyHDRIMaterial.SetVector(HDShaderIDs._SkyParam, new Vector4(intensity, 0.0f, Mathf.Cos(phi), Mathf.Sin(phi)));
                m_SkyHDRIMaterial.SetVector(HDShaderIDs._BackplateParameters0, GetBackplateParameters0(hdriSky));

                m_PropertyBlock.SetMatrix(HDShaderIDs._PixelCoordToViewDirWS, builtinParams.pixelCoordToViewDirMatrix);

                CoreUtils.DrawFullScreen(builtinParams.commandBuffer, m_SkyHDRIMaterial, m_PropertyBlock, m_RenderDepthOnlyFullscreenSkyWithBackplateID);
            }
        }

        public override void RenderSky(BuiltinSkyParameters builtinParams, bool renderForCubemap, bool renderSunDisk)
        {
            var hdriSky = builtinParams.skySettings as HDRISky;
            float intensity, phi, backplatePhi;
            GetParameters(out intensity, out phi, out backplatePhi, builtinParams, hdriSky);
            int passID;
            if (renderForCubemap)
            {
                passID = m_RenderCubemapID;
            }
            else
            {
                if (hdriSky.enableBackplate.value == false)
                {
                    passID = m_RenderFullscreenSkyID;
                }
                else
                {
                    passID = m_RenderFullscreenSkyWithBackplateID;
                }
            }

            if (hdriSky.distortionMode.value != HDRISky.DistortionMode.None)
            {
                m_SkyHDRIMaterial.EnableKeyword("SKY_MOTION");
                if (hdriSky.distortionMode.value == HDRISky.DistortionMode.Flowmap)
                {
                    m_SkyHDRIMaterial.EnableKeyword("USE_FLOWMAP");
                    m_SkyHDRIMaterial.SetTexture(HDShaderIDs._Flowmap, hdriSky.flowmap.value);
                }
                else
                    m_SkyHDRIMaterial.DisableKeyword("USE_FLOWMAP");

                var hdCamera = builtinParams.hdCamera;
                float rot = Mathf.Deg2Rad * (hdriSky.scrollOrientation.GetValue(hdCamera) - hdriSky.rotation.value);
                bool upperHemisphereOnly = hdriSky.upperHemisphereOnly.value || (hdriSky.distortionMode.value == HDRISky.DistortionMode.Procedural);
                Vector4 flowmapParam = new Vector4(upperHemisphereOnly ? 1.0f : 0.0f, scrollFactor / 200.0f, -Mathf.Cos(rot), -Mathf.Sin(rot));

                m_SkyHDRIMaterial.SetVector(HDShaderIDs._FlowmapParam, flowmapParam);

                scrollFactor += hdCamera.animateMaterials ? hdriSky.scrollSpeed.GetValue(hdCamera) * (hdCamera.time - lastTime) * 0.01f : 0.0f;
                lastTime = hdCamera.time;
            }
            else
                m_SkyHDRIMaterial.DisableKeyword("SKY_MOTION");

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
