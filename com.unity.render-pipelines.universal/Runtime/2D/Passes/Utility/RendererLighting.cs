using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal static class RendererLighting
    {
        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Clear Normals");
        private static readonly ShaderTagId k_NormalsRenderingPassName = new ShaderTagId("NormalsRendering");
        private static readonly Color k_NormalClearColor = new Color(0.5f, 0.5f, 1.0f, 1.0f);
        private static readonly string k_SpriteLightKeyword = "SPRITE_LIGHT";
        private static readonly string k_UsePointLightCookiesKeyword = "USE_POINT_LIGHT_COOKIES";
        private static readonly string k_LightQualityFastKeyword = "LIGHT_QUALITY_FAST";
        private static readonly string k_UseNormalMap = "USE_NORMAL_MAP";
        private static readonly string k_UseAdditiveBlendingKeyword = "USE_ADDITIVE_BLENDING";

        private static readonly string[] k_UseBlendStyleKeywords =
        {
            "USE_SHAPE_LIGHT_TYPE_0", "USE_SHAPE_LIGHT_TYPE_1", "USE_SHAPE_LIGHT_TYPE_2", "USE_SHAPE_LIGHT_TYPE_3"
        };

        private static readonly string[] k_BlendFactorsPropNames =
        {
            "_ShapeLightBlendFactors0", "_ShapeLightBlendFactors1", "_ShapeLightBlendFactors2", "_ShapeLightBlendFactors3"
        };

        private static readonly string[] k_MaskFilterPropNames =
        {
            "_ShapeLightMaskFilter0", "_ShapeLightMaskFilter1", "_ShapeLightMaskFilter2", "_ShapeLightMaskFilter3"
        };

        private static readonly string[] k_InvertedFilterPropNames =
        {
            "_ShapeLightInvertedFilter0", "_ShapeLightInvertedFilter1", "_ShapeLightInvertedFilter2", "_ShapeLightInvertedFilter3"
        };

        private static GraphicsFormat s_RenderTextureFormatToUse = GraphicsFormat.R8G8B8A8_UNorm;
        private static bool s_HasSetupRenderTextureFormatToUse;

        private static readonly int SrcBlendID = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendID = Shader.PropertyToID("_DstBlend");
        private static readonly int ShadowStencilGroupID = Shader.PropertyToID("_ShadowStencilGroup");
        private static readonly int ShadowIntensityID = Shader.PropertyToID("_ShadowIntensity");
        private static readonly int ShadowVolumeIntensityID = Shader.PropertyToID("_ShadowVolumeIntensity");
        private static readonly int LightPosID = Shader.PropertyToID("_LightPos");
        private static readonly int ShadowRadiusID = Shader.PropertyToID("_ShadowRadius");
        private static readonly int FalloffIntensityID = Shader.PropertyToID("_FalloffIntensity");
        private static readonly int FalloffDistanceID = Shader.PropertyToID("_FalloffDistance");
        private static readonly int FalloffOffsetID = Shader.PropertyToID("_FalloffOffset");
        private static readonly int LightColorID = Shader.PropertyToID("_LightColor");
        private static readonly int VolumeOpacityID = Shader.PropertyToID("_VolumeOpacity");
        private static readonly int CookieTexID = Shader.PropertyToID("_CookieTex");
        private static readonly int FalloffLookupID = Shader.PropertyToID("_FalloffLookup");
        private static readonly int LightPositionID = Shader.PropertyToID("_LightPosition");
        private static readonly int LightInvMatrixID = Shader.PropertyToID("_LightInvMatrix");
        private static readonly int LightNoRotInvMatrixID = Shader.PropertyToID("_LightNoRotInvMatrix");
        private static readonly int InnerRadiusMultID = Shader.PropertyToID("_InnerRadiusMult");
        private static readonly int OuterAngleID = Shader.PropertyToID("_OuterAngle");
        private static readonly int InnerAngleMultID = Shader.PropertyToID("_InnerAngleMult");
        private static readonly int LightLookupID = Shader.PropertyToID("_LightLookup");
        private static readonly int IsFullSpotlightID = Shader.PropertyToID("_IsFullSpotlight");
        private static readonly int LightZDistanceID = Shader.PropertyToID("_LightZDistance");
        private static readonly int PointLightCookieTexID = Shader.PropertyToID("_PointLightCookieTex");

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

        public static void EnableBlendStyle(CommandBuffer cmd, int blendStyleIndex, bool enabled)
        {
            var keyword = k_UseBlendStyleKeywords[blendStyleIndex];

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

            var descriptor = new RenderTextureDescriptor(width, height);
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
                if (!pass.rendererData.lightBlendStyles[i].hasRenderTarget)
                    continue;

                pass.rendererData.lightBlendStyles[i].hasRenderTarget = false;
                cmd.ReleaseTemporaryRT(pass.rendererData.lightBlendStyles[i].renderTargetHandle.id);
            }

            cmd.ReleaseTemporaryRT(pass.rendererData.normalsRenderTarget.id);
            cmd.ReleaseTemporaryRT(pass.rendererData.shadowsRenderTarget.id);
        }


        private static void RenderShadows(IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmdBuffer, int layerToRender, Light2D light, float shadowIntensity, RenderTargetIdentifier renderTexture, RenderTargetIdentifier depthTexture)
        {
            cmdBuffer.SetGlobalFloat(ShadowIntensityID, 1 - light.shadowIntensity);
            cmdBuffer.SetGlobalFloat(ShadowVolumeIntensityID, 1 - light.shadowVolumeIntensity);

            if (shadowIntensity > 0)
            {
                CreateShadowRenderTexture(pass, renderingData, cmdBuffer, light.blendStyleIndex);

                cmdBuffer.SetRenderTarget(pass.rendererData.shadowsRenderTarget.Identifier(), RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
                cmdBuffer.ClearRenderTarget(true, true, Color.black);

                var lightBounds = light.GetBoundingSphere(); // Gets the local bounding sphere...
                var shadowRadius = 1.42f * lightBounds.radius;

                cmdBuffer.SetGlobalVector(LightPosID, light.transform.position);
                cmdBuffer.SetGlobalFloat(ShadowRadiusID, shadowRadius);

                var shadowMaterial = pass.rendererData.GetShadowMaterial(1);
                var removeSelfShadowMaterial = pass.rendererData.GetRemoveSelfShadowMaterial(1);
                var shadowCasterGroups = ShadowCasterGroup2DManager.shadowCasterGroups;
                if (shadowCasterGroups != null && shadowCasterGroups.Count > 0)
                {
                    var previousShadowGroupIndex = -1;
                    var incrementingGroupIndex = 0;
                    for (var group = 0; group < shadowCasterGroups.Count; group++)
                    {
                        var shadowCasterGroup = shadowCasterGroups[group];

                        var shadowCasters = shadowCasterGroup.GetShadowCasters();

                        var shadowGroupIndex = shadowCasterGroup.GetShadowGroup();
                        if (LightUtility.CheckForChange(shadowGroupIndex, ref previousShadowGroupIndex) || shadowGroupIndex == 0)
                        {
                            incrementingGroupIndex++;
                            shadowMaterial = pass.rendererData.GetShadowMaterial(incrementingGroupIndex);
                            removeSelfShadowMaterial = pass.rendererData.GetRemoveSelfShadowMaterial(incrementingGroupIndex);
                        }

                        if (shadowCasters != null)
                        {
                            // Draw the shadow casting group first, then draw the silhouttes..
                            for (var i = 0; i < shadowCasters.Count; i++)
                            {
                                var shadowCaster = shadowCasters[i];

                                if (shadowCaster != null && shadowMaterial != null && shadowCaster.IsShadowedLayer(layerToRender))
                                {
                                    if (shadowCaster.castsShadows)
                                        cmdBuffer.DrawMesh(shadowCaster.mesh, shadowCaster.transform.localToWorldMatrix, shadowMaterial);
                                }
                            }

                            for (var i = 0; i < shadowCasters.Count; i++)
                            {
                                var shadowCaster = shadowCasters[i];

                                if (shadowCaster != null && shadowMaterial != null && shadowCaster.IsShadowedLayer(layerToRender))
                                {
                                    if (shadowCaster.useRendererSilhouette)
                                    {
                                        var renderer = shadowCaster.GetComponent<Renderer>();
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
                                            var meshMat = shadowCaster.transform.localToWorldMatrix;
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

        private static bool RenderLightSet(IRenderPass2D pass, RenderingData renderingData, int blendStyleIndex, CommandBuffer cmd, int layerToRender, RenderTargetIdentifier renderTexture, bool rtNeedsClear, Color clearColor, List<Light2D> lights)
        {
            var renderedAnyLight = false;

            foreach (var light in lights)
            {
                if (light != null &&
                    light.lightType != Light2D.LightType.Global &&
                    light.blendStyleIndex == blendStyleIndex &&
                    light.IsLitLayer(layerToRender))
                {
                    // Render light
                    var lightMaterial = pass.rendererData.GetLightMaterial(light, false);
                    if (lightMaterial == null)
                        continue;

                    var lightMesh = light.GetMesh();
                    if (lightMesh == null)
                        continue;

                    RenderShadows(pass, renderingData, cmd, layerToRender, light, light.shadowIntensity, renderTexture, renderTexture);

                    if (!renderedAnyLight && rtNeedsClear)
                    {
                        cmd.ClearRenderTarget(false, true, clearColor);
                    }

                    renderedAnyLight = true;

                    if (light.lightType == Light2D.LightType.Sprite && light.lightCookieSprite != null && light.lightCookieSprite.texture != null)
                        cmd.SetGlobalTexture(CookieTexID, light.lightCookieSprite.texture);

                    cmd.SetGlobalFloat(FalloffIntensityID, light.falloffIntensity);
                    cmd.SetGlobalFloat(FalloffDistanceID, light.shapeLightFalloffSize);
                    cmd.SetGlobalVector(FalloffOffsetID, light.shapeLightFalloffOffset);
                    cmd.SetGlobalColor(LightColorID, light.intensity * light.color);
                    cmd.SetGlobalFloat(VolumeOpacityID, light.volumeOpacity);

                    if(light.useNormalMap || light.lightType == Light2D.LightType.Point)
                        SetPointLightShaderGlobals(cmd, light);

                    // Light code could be combined...
                    if (light.lightType == Light2D.LightType.Parametric || light.lightType == Light2D.LightType.Freeform || light.lightType == Light2D.LightType.Sprite)
                    {
                        cmd.DrawMesh(lightMesh, light.transform.localToWorldMatrix, lightMaterial);
                    }
                    else if(light.lightType == Light2D.LightType.Point)
                    {
                        var scale = new Vector3(light.pointLightOuterRadius, light.pointLightOuterRadius, light.pointLightOuterRadius);
                        var matrix = Matrix4x4.TRS(light.transform.position, Quaternion.identity, scale);
                        cmd.DrawMesh(lightMesh, matrix, lightMaterial);
                    }
                }
            }

            // If no lights were rendered, just clear the RenderTarget if needed
            if (!renderedAnyLight && rtNeedsClear)
            {
                cmd.ClearRenderTarget(false, true, clearColor);
            }

            return renderedAnyLight;
        }

        private static void RenderLightVolumeSet(IRenderPass2D pass, RenderingData renderingData, int blendStyleIndex, CommandBuffer cmd, int layerToRender, RenderTargetIdentifier renderTexture, RenderTargetIdentifier depthTexture, List<Light2D> lights)
        {
            if (lights.Count > 0)
            {
                for (var i = 0; i < lights.Count; i++)
                {
                    var light = lights[i];

                    var topMostLayer = light.GetTopMostLitLayer();
                    if (layerToRender == topMostLayer)
                    {
                        if (light != null && light.lightType != Light2D.LightType.Global && light.volumeOpacity > 0.0f && light.blendStyleIndex == blendStyleIndex && light.IsLitLayer(layerToRender))
                        {
                            var lightVolumeMaterial = pass.rendererData.GetLightMaterial(light, true);
                            if (lightVolumeMaterial != null)
                            {
                                var lightMesh = light.GetMesh();
                                if (lightMesh != null)
                                {
                                    RenderShadows(pass, renderingData, cmd, layerToRender, light, light.shadowVolumeIntensity, renderTexture, depthTexture);

                                    if (light.lightType == Light2D.LightType.Sprite && light.lightCookieSprite != null && light.lightCookieSprite.texture != null)
                                        cmd.SetGlobalTexture(CookieTexID, light.lightCookieSprite.texture);

                                    cmd.SetGlobalFloat(FalloffIntensityID, light.falloffIntensity);
                                    cmd.SetGlobalFloat(FalloffDistanceID, light.shapeLightFalloffSize);
                                    cmd.SetGlobalVector(FalloffOffsetID, light.shapeLightFalloffOffset);
                                    cmd.SetGlobalColor(LightColorID, light.intensity * light.color);
                                    cmd.SetGlobalFloat(VolumeOpacityID, light.volumeOpacity);

                                    // Is this needed
                                    if (light.useNormalMap || light.lightType == Light2D.LightType.Point)
                                        SetPointLightShaderGlobals(cmd, light);

                                    // Could be combined...
                                    if (light.lightType == Light2D.LightType.Parametric || light.lightType == Light2D.LightType.Freeform || light.lightType == Light2D.LightType.Sprite)
                                    {
                                        cmd.DrawMesh(lightMesh, light.transform.localToWorldMatrix, lightVolumeMaterial);
                                    }
                                    else if (light.lightType == Light2D.LightType.Point)
                                    {
                                        var scale = new Vector3(light.pointLightOuterRadius, light.pointLightOuterRadius, light.pointLightOuterRadius);
                                        var matrix = Matrix4x4.TRS(light.transform.position, Quaternion.identity, scale);
                                        cmd.DrawMesh(lightMesh, matrix, lightVolumeMaterial);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void SetShapeLightShaderGlobals(this IRenderPass2D pass, CommandBuffer cmd)
        {
            for(var i = 0; i < pass.rendererData.lightBlendStyles.Length; i++)
            {
                var blendStyle = pass.rendererData.lightBlendStyles[i];
                if (i >= k_BlendFactorsPropNames.Length)
                    break;

                cmd.SetGlobalVector(k_BlendFactorsPropNames[i], blendStyle.blendFactors);
                cmd.SetGlobalVector(k_MaskFilterPropNames[i], blendStyle.maskTextureChannelFilter.mask);
                cmd.SetGlobalVector(k_InvertedFilterPropNames[i], blendStyle.maskTextureChannelFilter.inverted);
            }

            cmd.SetGlobalTexture(FalloffLookupID, Light2DLookupTexture.GetFalloffLookupTexture());
        }

        private static float GetNormalizedInnerRadius(Light2D light)
        {
            return light.pointLightInnerRadius / light.pointLightOuterRadius;
        }

        private static float GetNormalizedAngle(float angle)
        {
            return (angle / 360.0f);
        }

        private static void GetScaledLightInvMatrix(Light2D light, out Matrix4x4 retMatrix, bool includeRotation)
        {
            var outerRadius = light.pointLightOuterRadius;
            var lightScale = Vector3.one;
            var outerRadiusScale = new Vector3(lightScale.x * outerRadius, lightScale.y * outerRadius, lightScale.z * outerRadius);

            var transform = light.transform;
            var rotation = includeRotation ? transform.rotation : Quaternion.identity;

            var scaledLightMat = Matrix4x4.TRS(transform.position, rotation, outerRadiusScale);
            retMatrix = Matrix4x4.Inverse(scaledLightMat);
        }

        private static void SetPointLightShaderGlobals(CommandBuffer cmd, Light2D light)
        {
            // This is used for the lookup texture
            GetScaledLightInvMatrix(light, out var lightInverseMatrix, true);
            GetScaledLightInvMatrix(light, out var lightNoRotInverseMatrix, false);

            var innerRadius = GetNormalizedInnerRadius(light);
            var innerAngle = GetNormalizedAngle(light.pointLightInnerAngle);
            var outerAngle = GetNormalizedAngle(light.pointLightOuterAngle);
            var innerRadiusMult = 1 / (1 - innerRadius);

            cmd.SetGlobalVector(LightPositionID, light.transform.position);
            cmd.SetGlobalMatrix(LightInvMatrixID, lightInverseMatrix);
            cmd.SetGlobalMatrix(LightNoRotInvMatrixID, lightNoRotInverseMatrix);
            cmd.SetGlobalFloat(InnerRadiusMultID, innerRadiusMult);
            cmd.SetGlobalFloat(OuterAngleID, outerAngle);
            cmd.SetGlobalFloat(InnerAngleMultID, 1 / (outerAngle - innerAngle));
            cmd.SetGlobalTexture(LightLookupID, Light2DLookupTexture.GetLightLookupTexture());
            cmd.SetGlobalTexture(FalloffLookupID, Light2DLookupTexture.GetFalloffLookupTexture());
            cmd.SetGlobalFloat(FalloffIntensityID, light.falloffIntensity);
            cmd.SetGlobalFloat(IsFullSpotlightID, innerAngle == 1 ? 1.0f : 0.0f);

            cmd.SetGlobalFloat(LightZDistanceID, light.pointLightDistance);

            if (light.lightCookieSprite != null && light.lightCookieSprite.texture != null)
                cmd.SetGlobalTexture(PointLightCookieTexID, light.lightCookieSprite.texture);
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

        public static void RenderLights(this IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmd, int layerToRender, uint blendStylesUsed)
        {
            var blendStyles = pass.rendererData.lightBlendStyles;

            for (var i = 0; i < blendStyles.Length; ++i)
            {
                if ((blendStylesUsed & (uint)(1<<i)) == 0)
                    continue;

                var sampleName = blendStyles[i].name;
                cmd.BeginSample(sampleName);

                var rtID = pass.rendererData.lightBlendStyles[i].renderTargetHandle.Identifier();
                cmd.SetRenderTarget(rtID);

                var rtDirty = false;
                if (!Light2DManager.GetGlobalColor(layerToRender, i, out var clearColor))
                    clearColor = Color.black;
                else
                    rtDirty = true;

                rtDirty |= RenderLightSet(
                    pass, renderingData,
                    i,
                    cmd,
                    layerToRender,
                    rtID,
                    (pass.rendererData.lightBlendStyles[i].isDirty || rtDirty),
                    clearColor,
                    pass.rendererData.lightCullResult.visibleLights
                );

                pass.rendererData.lightBlendStyles[i].isDirty = rtDirty;

                cmd.EndSample(sampleName);
            }
        }

        public static void RenderLightVolumes(this IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmd, int layerToRender, RenderTargetIdentifier renderTarget, RenderTargetIdentifier depthTarget, uint blendStylesUsed)
        {
            var blendStyles = pass.rendererData.lightBlendStyles;

            for (var i = 0; i < blendStyles.Length; ++i)
            {
                if ((blendStylesUsed & (uint)(1 << i)) == 0)
                    continue;

                string sampleName = blendStyles[i].name;
                cmd.BeginSample(sampleName);

                RenderLightVolumeSet(
                    pass, renderingData,
                    i,
                    cmd,
                    layerToRender,
                    renderTarget,
                    depthTarget,
                    pass.rendererData.lightCullResult.visibleLights
                );

                cmd.EndSample(sampleName);
            }
        }

        private static void SetBlendModes(Material material, BlendMode src, BlendMode dst)
        {
            material.SetFloat(SrcBlendID, (float)src);
            material.SetFloat(DstBlendID, (float)dst);
        }

        private static uint GetLightMaterialIndex(Light2D light, bool isVolume)
        {
            var bitIndex = 0;
            var volumeBit = isVolume ? 1u << bitIndex : 0u;
            bitIndex++;
            var shapeBit = light.IsShapeLight() ? 1u << bitIndex : 0u;
            bitIndex++;
            var additiveBit = light.alphaBlendOnOverlap ? 0u : 1u << bitIndex;
            bitIndex++;
            var spriteBit = light.lightType == Light2D.LightType.Sprite ? 1u << bitIndex : 0u;
            bitIndex++;
            var pointCookieBit = (!light.IsShapeLight() && light.lightCookieSprite != null && light.lightCookieSprite.texture != null) ? 1u << bitIndex : 0u;
            bitIndex++;
            var pointFastQualityBit = (!light.IsShapeLight() && light.pointLightQuality == Light2D.PointLightQuality.Fast) ? 1u << bitIndex : 0u;
            bitIndex++;
            var useNormalMap = light.useNormalMap ? 1u << bitIndex : 0u;

            return pointFastQualityBit | pointCookieBit | spriteBit | additiveBit | shapeBit | volumeBit | useNormalMap;
        }

        private static Material CreateLightMaterial(Renderer2DData rendererData, Light2D light, bool isVolume)
        {
            var isShape = light.IsShapeLight();
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
            var materialIndex = GetLightMaterialIndex(light, isVolume);

            if (!rendererData.lightMaterials.TryGetValue(materialIndex, out var material))
            {
                material = CreateLightMaterial(rendererData, light, isVolume);
                rendererData.lightMaterials[materialIndex] = material;
            }

            return rendererData.lightMaterials[materialIndex];
        }

        private static Material GetShadowMaterial(this Renderer2DData rendererData, int index)
        {
            var shadowMaterialIndex = index % 255;
            if(rendererData.shadowMaterials[shadowMaterialIndex] == null)
            {
                rendererData.shadowMaterials[shadowMaterialIndex] = CoreUtils.CreateEngineMaterial(rendererData.shadowGroupShader);
                rendererData.shadowMaterials[shadowMaterialIndex].SetFloat(ShadowStencilGroupID, index);
            }

            return rendererData.shadowMaterials[shadowMaterialIndex];
        }

        private static Material GetRemoveSelfShadowMaterial(this Renderer2DData rendererData, int index)
        {
            var shadowMaterialIndex = index % 255;
            if (rendererData.removeSelfShadowMaterials[shadowMaterialIndex] == null)
            {
                rendererData.removeSelfShadowMaterials[shadowMaterialIndex] = CoreUtils.CreateEngineMaterial(rendererData.removeSelfShadowShader);
                rendererData.removeSelfShadowMaterials[shadowMaterialIndex].SetFloat(ShadowStencilGroupID, index);
            }

            return rendererData.removeSelfShadowMaterials[shadowMaterialIndex];
        }
    }
}
