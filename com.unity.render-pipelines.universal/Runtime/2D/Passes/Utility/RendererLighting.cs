using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal static class RendererLighting
    {
        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Clear Normals");
        static readonly ShaderTagId k_NormalsRenderingPassName = new ShaderTagId("NormalsRendering");
        static readonly Color k_NormalClearColor = new Color(0.5f, 0.5f, 1.0f, 1.0f);
        static readonly string k_SpriteLightKeyword = "SPRITE_LIGHT";
        static readonly string k_UsePointLightCookiesKeyword = "USE_POINT_LIGHT_COOKIES";
        static readonly string k_LightQualityFastKeyword = "LIGHT_QUALITY_FAST";
        static readonly string k_UseNormalMap = "USE_NORMAL_MAP";
        static readonly string k_UseAdditiveBlendingKeyword = "USE_ADDITIVE_BLENDING";

        static readonly string[] k_UseBlendStyleKeywords =
        {
            "USE_SHAPE_LIGHT_TYPE_0", "USE_SHAPE_LIGHT_TYPE_1", "USE_SHAPE_LIGHT_TYPE_2", "USE_SHAPE_LIGHT_TYPE_3"
        };

        static readonly string[] k_BlendFactorsPropNames =
        {
            "_ShapeLightBlendFactors0", "_ShapeLightBlendFactors1", "_ShapeLightBlendFactors2", "_ShapeLightBlendFactors3"
        };

        static readonly string[] k_MaskFilterPropNames =
        {
            "_ShapeLightMaskFilter0", "_ShapeLightMaskFilter1", "_ShapeLightMaskFilter2", "_ShapeLightMaskFilter3"
        };

        static readonly string[] k_InvertedFilterPropNames =
        {
            "_ShapeLightInvertedFilter0", "_ShapeLightInvertedFilter1", "_ShapeLightInvertedFilter2", "_ShapeLightInvertedFilter3"
        };

        static Texture s_LightLookupTexture;
        static Texture s_FalloffLookupTexture;

        static GraphicsFormat s_RenderTextureFormatToUse = GraphicsFormat.R8G8B8A8_UNorm;
        static bool s_HasSetupRenderTextureFormatToUse;

        private static GraphicsFormat GetRenderTextureFormat()
        {
            if (!s_HasSetupRenderTextureFormatToUse)
            {
                if (SystemInfo.IsFormatSupported(GraphicsFormat.B10G11R11_UFloatPack32, FormatUsage.Linear | FormatUsage.Render))
                    s_RenderTextureFormatToUse = GraphicsFormat.B10G11R11_UFloatPack32;
                else if (SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SFloat, FormatUsage.Linear | FormatUsage.Render))
                    s_RenderTextureFormatToUse = GraphicsFormat.R16G16B16A16_SFloat;

                s_HasSetupRenderTextureFormatToUse = true;
            }

            return s_RenderTextureFormatToUse;
        }

        public static void CreateNormalMapRenderTexture(this IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmd)
        {
            var descriptor = new RenderTextureDescriptor(renderingData.cameraData.cameraTargetDescriptor.width, renderingData.cameraData.cameraTargetDescriptor.height);
            descriptor.graphicsFormat = GetRenderTextureFormat();
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = renderingData.cameraData.cameraTargetDescriptor.msaaSamples;
            descriptor.dimension = TextureDimension.Tex2D;

            cmd.GetTemporaryRT(pass.rendererData.normalsRenderTarget.id, descriptor, FilterMode.Bilinear);
        }

        public static void CreateBlendStyleRenderTexture(this IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmd, int blendStyleIndex)
        {
            var renderTextureScale = Mathf.Clamp(pass.rendererData.lightBlendStyles[blendStyleIndex].renderTextureScale, 0.01f, 1.0f);
            var width = (int)(renderingData.cameraData.cameraTargetDescriptor.width * renderTextureScale);
            var height = (int)(renderingData.cameraData.cameraTargetDescriptor.height * renderTextureScale);

            var descriptor = new RenderTextureDescriptor(width, height);
            descriptor.graphicsFormat = GetRenderTextureFormat();
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;
            descriptor.dimension = TextureDimension.Tex2D;

            ref var blendStyle = ref pass.rendererData.lightBlendStyles[blendStyleIndex];
            cmd.GetTemporaryRT(blendStyle.renderTargetHandle.id, descriptor, FilterMode.Bilinear);
            blendStyle.hasRenderTarget = true;
            blendStyle.isDirty = true;
        }

        static public void EnableBlendStyle(CommandBuffer cmd, int blendStyleIndex, bool enabled)
        {
            string keyword = k_UseBlendStyleKeywords[blendStyleIndex];

            if (enabled)
                cmd.EnableShaderKeyword(keyword);
            else
                cmd.DisableShaderKeyword(keyword);
        }

        private static void CreateShadowRenderTexture(IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmd, int blendStyleIndex)
        {
            var renderTextureScale = Mathf.Clamp(pass.rendererData.lightBlendStyles[blendStyleIndex].renderTextureScale, 0.01f, 1.0f);
            var width = (int)(renderingData.cameraData.cameraTargetDescriptor.width * renderTextureScale);
            var height = (int)(renderingData.cameraData.cameraTargetDescriptor.height * renderTextureScale);

            RenderTextureDescriptor descriptor = new RenderTextureDescriptor(width, height);
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            descriptor.depthBufferBits = 24;
            descriptor.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
            descriptor.msaaSamples = 1;
            descriptor.dimension = TextureDimension.Tex2D;

            cmd.GetTemporaryRT(pass.rendererData.shadowsRenderTarget.id, descriptor, FilterMode.Bilinear);
        }

        public static void ReleaseRenderTextures(this IRenderPass2D pass, CommandBuffer cmd)
        {
            for (var i = 0; i < pass.rendererData.lightBlendStyles.Length; i++)
            {
                if(pass.rendererData.lightBlendStyles[i].hasRenderTarget)
                {
                    pass.rendererData.lightBlendStyles[i].hasRenderTarget = false;
                    cmd.ReleaseTemporaryRT(pass.rendererData.lightBlendStyles[i].renderTargetHandle.id);
                }
            }

            cmd.ReleaseTemporaryRT(pass.rendererData.normalsRenderTarget.id);
            cmd.ReleaseTemporaryRT(pass.rendererData.shadowsRenderTarget.id);
        }


        private static void RenderShadows(IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmdBuffer, int layerToRender, Light2D light, float shadowIntensity, RenderTargetIdentifier renderTexture, RenderTargetIdentifier depthTexture)
        {
            cmdBuffer.SetGlobalFloat("_ShadowIntensity", 1 - light.shadowIntensity);
            cmdBuffer.SetGlobalFloat("_ShadowVolumeIntensity", 1 - light.shadowVolumeIntensity);

            if (shadowIntensity > 0)
            {
                CreateShadowRenderTexture(pass, renderingData, cmdBuffer, light.blendStyleIndex);

                cmdBuffer.SetRenderTarget(pass.rendererData.shadowsRenderTarget.Identifier(), RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
                cmdBuffer.ClearRenderTarget(true, true, Color.black);

                BoundingSphere lightBounds = light.GetBoundingSphere(); // Gets the local bounding sphere...
                float shadowRadius = 1.42f * lightBounds.radius;

                cmdBuffer.SetGlobalVector("_LightPos", light.transform.position);
                cmdBuffer.SetGlobalFloat("_ShadowRadius", shadowRadius);

                Material shadowMaterial = pass.rendererData.GetShadowMaterial(1);
                Material removeSelfShadowMaterial = pass.rendererData.GetRemoveSelfShadowMaterial(1);
                List<ShadowCasterGroup2D> shadowCasterGroups = ShadowCasterGroup2DManager.shadowCasterGroups;
                if (shadowCasterGroups != null && shadowCasterGroups.Count > 0)
                {
                    int previousShadowGroupIndex = -1;
                    int incrementingGroupIndex = 0;
                    for (int group = 0; group < shadowCasterGroups.Count; group++)
                    {
                        ShadowCasterGroup2D shadowCasterGroup = shadowCasterGroups[group];

                        List<ShadowCaster2D> shadowCasters = shadowCasterGroup.GetShadowCasters();

                        int shadowGroupIndex = shadowCasterGroup.GetShadowGroup();
                        if (LightUtility.CheckForChange(shadowGroupIndex, ref previousShadowGroupIndex) || shadowGroupIndex == 0)
                        {
                            incrementingGroupIndex++;
                            shadowMaterial = pass.rendererData.GetShadowMaterial(incrementingGroupIndex);
                            removeSelfShadowMaterial = pass.rendererData.GetRemoveSelfShadowMaterial(incrementingGroupIndex);
                        }

                        if (shadowCasters != null)
                        {
                            // Draw the shadow casting group first, then draw the silhouttes..
                            for (int i = 0; i < shadowCasters.Count; i++)
                            {
                                ShadowCaster2D shadowCaster = (ShadowCaster2D)shadowCasters[i];

                                if (shadowCaster != null && shadowMaterial != null && shadowCaster.IsShadowedLayer(layerToRender))
                                {
                                    if (shadowCaster.castsShadows)
                                        cmdBuffer.DrawMesh(shadowCaster.mesh, shadowCaster.transform.localToWorldMatrix, shadowMaterial);
                                }
                            }

                            for (int i = 0; i < shadowCasters.Count; i++)
                            {
                                ShadowCaster2D shadowCaster = (ShadowCaster2D)shadowCasters[i];

                                if (shadowCaster != null && shadowMaterial != null && shadowCaster.IsShadowedLayer(layerToRender))
                                {
                                    if (shadowCaster.useRendererSilhouette)
                                    {
                                        Renderer renderer = shadowCaster.GetComponent<Renderer>();
                                        if (renderer != null)
                                        {
                                            if (!shadowCaster.selfShadows)
                                                cmdBuffer.DrawRenderer(renderer, removeSelfShadowMaterial);
                                            else
                                                cmdBuffer.DrawRenderer(renderer, shadowMaterial, 0, 1);
                                        }
                                    }
                                    else
                                    {
                                        if (!shadowCaster.selfShadows)
                                        {
                                            Matrix4x4 meshMat = shadowCaster.transform.localToWorldMatrix;
                                            cmdBuffer.DrawMesh(shadowCaster.mesh, meshMat, removeSelfShadowMaterial);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                cmdBuffer.ReleaseTemporaryRT(pass.rendererData.shadowsRenderTarget.id);
                cmdBuffer.SetRenderTarget(renderTexture, depthTexture);
            }
        }

        private static bool RenderLightSet(IRenderPass2D pass, RenderingData renderingData, Camera camera, int blendStyleIndex, CommandBuffer cmdBuffer, int layerToRender, RenderTargetIdentifier renderTexture, bool rtNeedsClear, Color clearColor, List<Light2D> lights)
        {
            bool renderedAnyLight = false;

            foreach (var light in lights)
            {
                if (light != null && light.lightType != Light2D.LightType.Global && light.blendStyleIndex == blendStyleIndex && light.IsLitLayer(layerToRender) && light.IsLightVisible(camera))
                {
                    // Render light
                    Material lightMaterial = pass.rendererData.GetLightMaterial(light, false);
                    if (lightMaterial != null)
                    {
                        Mesh lightMesh = light.GetMesh();
                        if (lightMesh != null)
                        {
                            RenderShadows(pass, renderingData, cmdBuffer, layerToRender, light, light.shadowIntensity, renderTexture, renderTexture);

                            if (!renderedAnyLight && rtNeedsClear)
                            {
                                cmdBuffer.ClearRenderTarget(false, true, clearColor);
                            }

                            renderedAnyLight = true;

                            if (light.lightType == Light2D.LightType.Sprite && light.lightCookieSprite != null && light.lightCookieSprite.texture != null)
                                cmdBuffer.SetGlobalTexture("_CookieTex", light.lightCookieSprite.texture);

                            cmdBuffer.SetGlobalFloat("_FalloffIntensity", light.falloffIntensity);
                            cmdBuffer.SetGlobalFloat("_FalloffDistance", light.shapeLightFalloffSize);
                            cmdBuffer.SetGlobalVector("_FalloffOffset", light.shapeLightFalloffOffset);
                            cmdBuffer.SetGlobalColor("_LightColor", light.intensity * light.color);
                            cmdBuffer.SetGlobalFloat("_VolumeOpacity", light.volumeOpacity);

                            if(light.useNormalMap || light.lightType == Light2D.LightType.Point)
                                RendererLighting.SetPointLightShaderGlobals(cmdBuffer, light);

                            // Light code could be combined...
                            if (light.lightType == Light2D.LightType.Parametric || light.lightType == Light2D.LightType.Freeform || light.lightType == Light2D.LightType.Sprite)
                            {
                                cmdBuffer.DrawMesh(lightMesh, light.transform.localToWorldMatrix, lightMaterial);
                            }
                            else if(light.lightType == Light2D.LightType.Point)
                            {
                                Vector3 scale = new Vector3(light.pointLightOuterRadius, light.pointLightOuterRadius, light.pointLightOuterRadius);
                                Matrix4x4 matrix = Matrix4x4.TRS(light.transform.position, Quaternion.identity, scale);
                                cmdBuffer.DrawMesh(lightMesh, matrix, lightMaterial);
                            }
                        }
                    }
                }
            }

            // If no lights were rendered, just clear the RenderTarget if needed
            if (!renderedAnyLight && rtNeedsClear)
            {
                cmdBuffer.ClearRenderTarget(false, true, clearColor);
            }

            return renderedAnyLight;
        }

        private static void RenderLightVolumeSet(IRenderPass2D pass, RenderingData renderingData, Camera camera, int blendStyleIndex, CommandBuffer cmdBuffer, int layerToRender, RenderTargetIdentifier renderTexture, RenderTargetIdentifier depthTexture, List<Light2D> lights)
        {
            if (lights.Count > 0)
            {
                for (int i = 0; i < lights.Count; i++)
                {
                    Light2D light = lights[i];

                    int topMostLayer = light.GetTopMostLitLayer();
                    if (layerToRender == topMostLayer)
                    {
                        if (light != null && light.lightType != Light2D.LightType.Global && light.volumeOpacity > 0.0f && light.blendStyleIndex == blendStyleIndex && light.IsLitLayer(layerToRender) && light.IsLightVisible(camera))
                        {
                            Material lightVolumeMaterial = pass.rendererData.GetLightMaterial(light, true);
                            if (lightVolumeMaterial != null)
                            {
                                Mesh lightMesh = light.GetMesh();
                                if (lightMesh != null)
                                {
                                    RenderShadows(pass, renderingData, cmdBuffer, layerToRender, light, light.shadowVolumeIntensity, renderTexture, depthTexture);

                                    if (light.lightType == Light2D.LightType.Sprite && light.lightCookieSprite != null && light.lightCookieSprite.texture != null)
                                        cmdBuffer.SetGlobalTexture("_CookieTex", light.lightCookieSprite.texture);

                                    cmdBuffer.SetGlobalFloat("_FalloffIntensity", light.falloffIntensity);
                                    cmdBuffer.SetGlobalFloat("_FalloffDistance", light.shapeLightFalloffSize);
                                    cmdBuffer.SetGlobalVector("_FalloffOffset", light.shapeLightFalloffOffset);
                                    cmdBuffer.SetGlobalColor("_LightColor", light.intensity * light.color);
                                    cmdBuffer.SetGlobalFloat("_VolumeOpacity", light.volumeOpacity);

                                    // Is this needed
                                    if (light.useNormalMap || light.lightType == Light2D.LightType.Point)
                                        RendererLighting.SetPointLightShaderGlobals(cmdBuffer, light);

                                    // Could be combined...
                                    if (light.lightType == Light2D.LightType.Parametric || light.lightType == Light2D.LightType.Freeform || light.lightType == Light2D.LightType.Sprite)
                                    {
                                        cmdBuffer.DrawMesh(lightMesh, light.transform.localToWorldMatrix, lightVolumeMaterial);
                                    }
                                    else if (light.lightType == Light2D.LightType.Point)
                                    {
                                        Vector3 scale = new Vector3(light.pointLightOuterRadius, light.pointLightOuterRadius, light.pointLightOuterRadius);
                                        Matrix4x4 matrix = Matrix4x4.TRS(light.transform.position, Quaternion.identity, scale);
                                        cmdBuffer.DrawMesh(lightMesh, matrix, lightVolumeMaterial);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void SetShapeLightShaderGlobals(this IRenderPass2D pass, CommandBuffer cmdBuffer)
        {
            for(var i = 0; i < pass.rendererData.lightBlendStyles.Length; i++)
            {
                var blendStyle = pass.rendererData.lightBlendStyles[i];
                if (i >= k_BlendFactorsPropNames.Length)
                    break;

                cmdBuffer.SetGlobalVector(k_BlendFactorsPropNames[i], blendStyle.blendFactors);
                cmdBuffer.SetGlobalVector(k_MaskFilterPropNames[i], blendStyle.maskTextureChannelFilter.mask);
                cmdBuffer.SetGlobalVector(k_InvertedFilterPropNames[i], blendStyle.maskTextureChannelFilter.inverted);
            }

            cmdBuffer.SetGlobalTexture("_FalloffLookup", RendererLighting.GetFalloffLookupTexture());
        }

        static Texture GetLightLookupTexture()
        {
            if (s_LightLookupTexture == null)
                s_LightLookupTexture = Light2DLookupTexture.CreatePointLightLookupTexture();

            return s_LightLookupTexture;
        }

        static Texture GetFalloffLookupTexture()
        {
            if (s_FalloffLookupTexture == null)
                s_FalloffLookupTexture = Light2DLookupTexture.CreateFalloffLookupTexture();

            return s_FalloffLookupTexture;
        }

        static public float GetNormalizedInnerRadius(Light2D light)
        {
            return light.pointLightInnerRadius / light.pointLightOuterRadius;
        }

        static public float GetNormalizedAngle(float angle)
        {
            return (angle / 360.0f);
        }

        static public void GetScaledLightInvMatrix(Light2D light, out Matrix4x4 retMatrix, bool includeRotation)
        {
            float outerRadius = light.pointLightOuterRadius;
            Vector3 lightScale = Vector3.one;
            Vector3 outerRadiusScale = new Vector3(lightScale.x * outerRadius, lightScale.y * outerRadius, lightScale.z * outerRadius);

            Quaternion rotation;
            if (includeRotation)
                rotation = light.transform.rotation;
            else
                rotation = Quaternion.identity;

            Matrix4x4 scaledLightMat = Matrix4x4.TRS(light.transform.position, rotation, outerRadiusScale);
            retMatrix = Matrix4x4.Inverse(scaledLightMat);
        }

        static public void SetPointLightShaderGlobals(CommandBuffer cmdBuffer, Light2D light)
        {
            // This is used for the lookup texture
            Matrix4x4 lightInverseMatrix;
            Matrix4x4 lightNoRotInverseMatrix;
            GetScaledLightInvMatrix(light, out lightInverseMatrix, true);
            GetScaledLightInvMatrix(light, out lightNoRotInverseMatrix, false);

            float innerRadius = GetNormalizedInnerRadius(light);
            float innerAngle = GetNormalizedAngle(light.pointLightInnerAngle);
            float outerAngle = GetNormalizedAngle(light.pointLightOuterAngle);
            float innerRadiusMult = 1 / (1 - innerRadius);

            cmdBuffer.SetGlobalVector("_LightPosition", light.transform.position);
            cmdBuffer.SetGlobalMatrix("_LightInvMatrix", lightInverseMatrix);
            cmdBuffer.SetGlobalMatrix("_LightNoRotInvMatrix", lightNoRotInverseMatrix);
            cmdBuffer.SetGlobalFloat("_InnerRadiusMult", innerRadiusMult);

            cmdBuffer.SetGlobalFloat("_OuterAngle", outerAngle);
            cmdBuffer.SetGlobalFloat("_InnerAngleMult", 1 / (outerAngle - innerAngle));
            cmdBuffer.SetGlobalTexture("_LightLookup", GetLightLookupTexture());
            cmdBuffer.SetGlobalTexture("_FalloffLookup", GetFalloffLookupTexture());
            cmdBuffer.SetGlobalFloat("_FalloffIntensity", light.falloffIntensity);
            cmdBuffer.SetGlobalFloat("_IsFullSpotlight", innerAngle == 1 ? 1.0f : 0.0f);

            cmdBuffer.SetGlobalFloat("_LightZDistance", light.pointLightDistance);

            if (light.lightCookieSprite != null && light.lightCookieSprite.texture != null)
                cmdBuffer.SetGlobalTexture("_PointLightCookieTex", light.lightCookieSprite.texture);
        }

        public static void ClearDirtyLighting(this IRenderPass2D pass, CommandBuffer cmd, uint blendStylesUsed)
        {
            for (var i = 0; i < pass.rendererData.lightBlendStyles.Length; ++i)
            {
                if ((blendStylesUsed & (uint)(1 << i)) == 0)
                    continue;

                if (!pass.rendererData.lightBlendStyles[i].isDirty)
                    continue;

                cmd.SetRenderTarget(pass.rendererData.lightBlendStyles[i].renderTargetHandle.Identifier());
                cmd.ClearRenderTarget(false, true, Color.black);
                pass.rendererData.lightBlendStyles[i].isDirty = false;
            }
        }

        public static void RenderNormals(this IRenderPass2D pass, ScriptableRenderContext context, CullingResults cullResults, DrawingSettings drawSettings, FilteringSettings filterSettings, RenderTargetIdentifier depthTarget)
        {
            var cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                cmd.SetRenderTarget(pass.rendererData.normalsRenderTarget.Identifier(), depthTarget);
                cmd.ClearRenderTarget(true, true, k_NormalClearColor);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            drawSettings.SetShaderPassName(0, k_NormalsRenderingPassName);
            context.DrawRenderers(cullResults, ref drawSettings, ref filterSettings);
        }

        public static void RenderLights(this IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmdBuffer, int layerToRender, uint blendStylesUsed)
        {
            var blendStyles = pass.rendererData.lightBlendStyles;
            var camera = renderingData.cameraData.camera;

            for (var i = 0; i < blendStyles.Length; ++i)
            {
                if ((blendStylesUsed & (uint)(1<<i)) == 0)
                    continue;

                var sampleName = blendStyles[i].name;
                cmdBuffer.BeginSample(sampleName);

                var rtID = pass.rendererData.lightBlendStyles[i].renderTargetHandle.Identifier();
                cmdBuffer.SetRenderTarget(rtID);

                var rtDirty = false;
                if (!Light2DManager.GetGlobalColor(layerToRender, i, out var clearColor))
                    clearColor = Color.black;
                else
                    rtDirty = true;

                rtDirty |= RenderLightSet(
                    pass, renderingData,
                    camera,
                    i,
                    cmdBuffer,
                    layerToRender,
                    rtID,
                    (pass.rendererData.lightBlendStyles[i].isDirty || rtDirty),
                    clearColor,
                    Light2D.GetLightsByBlendStyle(i)
                );

                pass.rendererData.lightBlendStyles[i].isDirty = rtDirty;

                cmdBuffer.EndSample(sampleName);
            }
        }

        public static void RenderLightVolumes(this IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmdBuffer, int layerToRender, RenderTargetIdentifier renderTarget, RenderTargetIdentifier depthTarget, uint blendStylesUsed)
        {
            var blendStyles = pass.rendererData.lightBlendStyles;
            var camera = renderingData.cameraData.camera;

            for (var i = 0; i < blendStyles.Length; ++i)
            {
                if ((blendStylesUsed & (uint)(1 << i)) == 0)
                    continue;

                string sampleName = blendStyles[i].name;
                cmdBuffer.BeginSample(sampleName);

                RenderLightVolumeSet(
                    pass, renderingData,
                    camera,
                    i,
                    cmdBuffer,
                    layerToRender,
                    renderTarget,
                    depthTarget,
                    Light2D.GetLightsByBlendStyle(i)
                );

                cmdBuffer.EndSample(sampleName);
            }
        }

        static void SetBlendModes(Material material, BlendMode src, BlendMode dst)
        {
            material.SetFloat("_SrcBlend", (float)src);
            material.SetFloat("_DstBlend", (float)dst);
        }

        static uint GetLightMaterialIndex(Light2D light, bool isVolume)
        {
            int bitIndex = 0;
            uint volumeBit = isVolume ? 1u << bitIndex : 0u;
            bitIndex++;
            uint shapeBit = light.IsShapeLight() ? 1u << bitIndex : 0u;
            bitIndex++;
            uint additiveBit = light.alphaBlendOnOverlap ? 0u : 1u << bitIndex;
            bitIndex++;
            uint spriteBit = light.lightType == Light2D.LightType.Sprite ? 1u << bitIndex : 0u;
            bitIndex++;
            uint pointCookieBit = (!light.IsShapeLight() && light.lightCookieSprite != null && light.lightCookieSprite.texture != null) ? 1u << bitIndex : 0u;
            bitIndex++;
            uint pointFastQualityBit = (!light.IsShapeLight() && light.pointLightQuality == Light2D.PointLightQuality.Fast) ? 1u << bitIndex : 0u;
            bitIndex++;
            uint useNormalMap = light.useNormalMap ? 1u << bitIndex : 0u;

            return pointFastQualityBit | pointCookieBit | spriteBit | additiveBit | shapeBit | volumeBit | useNormalMap;
        }

        private static Material CreateLightMaterial(Renderer2DData rendererData, Light2D light, bool isVolume)
        {
            bool isShape = light.IsShapeLight();
            Material material;

            if (isVolume)
                material = CoreUtils.CreateEngineMaterial(isShape ? rendererData.shapeLightVolumeShader : rendererData.pointLightVolumeShader);
            else
            {
                material = CoreUtils.CreateEngineMaterial(isShape ? rendererData.shapeLightShader : rendererData.pointLightShader);

                if (!light.alphaBlendOnOverlap)
                {
                    SetBlendModes(material, BlendMode.One, BlendMode.One);
                    material.EnableKeyword(k_UseAdditiveBlendingKeyword);
                }
                else
                    SetBlendModes(material, BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha);
            }

            if (light.lightType == Light2D.LightType.Sprite)
                material.EnableKeyword(k_SpriteLightKeyword);

            if (!isShape && light.lightCookieSprite != null && light.lightCookieSprite.texture != null)
                material.EnableKeyword(k_UsePointLightCookiesKeyword);

            if (!isShape && light.pointLightQuality == Light2D.PointLightQuality.Fast)
                material.EnableKeyword(k_LightQualityFastKeyword);

            if (light.useNormalMap)
                material.EnableKeyword(k_UseNormalMap);

            return material;
        }

        private static Material GetLightMaterial(this Renderer2DData rendererData, Light2D light, bool isVolume)
        {
            uint materialIndex = GetLightMaterialIndex(light, isVolume);

            if (rendererData.lightMaterials[materialIndex] == null)
                rendererData.lightMaterials[materialIndex] = CreateLightMaterial(rendererData, light, isVolume);

            return rendererData.lightMaterials[materialIndex];
        }

        private static Material GetShadowMaterial(this Renderer2DData rendererData, int index)
        {
            int shadowMaterialIndex = index % 255;
            if(rendererData.shadowMaterials[shadowMaterialIndex] == null)
            {
                rendererData.shadowMaterials[shadowMaterialIndex] = CoreUtils.CreateEngineMaterial(rendererData.shadowGroupShader);
                rendererData.shadowMaterials[shadowMaterialIndex].SetFloat("_ShadowStencilGroup", index);
            }

            return rendererData.shadowMaterials[shadowMaterialIndex];
        }

        private static Material GetRemoveSelfShadowMaterial(this Renderer2DData rendererData, int index)
        {
            int shadowMaterialIndex = index % 255;
            if (rendererData.removeSelfShadowMaterials[shadowMaterialIndex] == null)
            {
                rendererData.removeSelfShadowMaterials[shadowMaterialIndex] = CoreUtils.CreateEngineMaterial(rendererData.removeSelfShadowShader);
                rendererData.removeSelfShadowMaterials[shadowMaterialIndex].SetFloat("_ShadowStencilGroup", index);
            }

            return rendererData.removeSelfShadowMaterials[shadowMaterialIndex];
        }
    }
}
