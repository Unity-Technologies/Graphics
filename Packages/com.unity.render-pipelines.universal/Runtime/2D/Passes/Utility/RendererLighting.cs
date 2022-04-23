using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using Unity.Collections;

namespace UnityEngine.Rendering.Universal
{
    internal static class RendererLighting
    {
        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Draw Normals");
        private static readonly ShaderTagId k_NormalsRenderingPassName = new ShaderTagId("NormalsRendering");
        private static readonly Color k_NormalClearColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
        private static readonly string k_SpriteLightKeyword = "SPRITE_LIGHT";
        private static readonly string k_UsePointLightCookiesKeyword = "USE_POINT_LIGHT_COOKIES";
        private static readonly string k_LightQualityFastKeyword = "LIGHT_QUALITY_FAST";
        private static readonly string k_UseNormalMap = "USE_NORMAL_MAP";
        private static readonly string k_UseAdditiveBlendingKeyword = "USE_ADDITIVE_BLENDING";

        private static readonly string[] k_UseBlendStyleKeywords =
        {
            "USE_SHAPE_LIGHT_TYPE_0", "USE_SHAPE_LIGHT_TYPE_1", "USE_SHAPE_LIGHT_TYPE_2", "USE_SHAPE_LIGHT_TYPE_3"
        };

        private static readonly int[] k_BlendFactorsPropIDs =
        {
            Shader.PropertyToID("_ShapeLightBlendFactors0"),
            Shader.PropertyToID("_ShapeLightBlendFactors1"),
            Shader.PropertyToID("_ShapeLightBlendFactors2"),
            Shader.PropertyToID("_ShapeLightBlendFactors3")
        };

        private static readonly int[] k_MaskFilterPropIDs =
        {
            Shader.PropertyToID("_ShapeLightMaskFilter0"),
            Shader.PropertyToID("_ShapeLightMaskFilter1"),
            Shader.PropertyToID("_ShapeLightMaskFilter2"),
            Shader.PropertyToID("_ShapeLightMaskFilter3")
        };

        private static readonly int[] k_InvertedFilterPropIDs =
        {
            Shader.PropertyToID("_ShapeLightInvertedFilter0"),
            Shader.PropertyToID("_ShapeLightInvertedFilter1"),
            Shader.PropertyToID("_ShapeLightInvertedFilter2"),
            Shader.PropertyToID("_ShapeLightInvertedFilter3")
        };

        private static GraphicsFormat s_RenderTextureFormatToUse = GraphicsFormat.R8G8B8A8_UNorm;
        private static bool s_HasSetupRenderTextureFormatToUse;

        private static readonly int k_SrcBlendID = Shader.PropertyToID("_SrcBlend");
        private static readonly int k_DstBlendID = Shader.PropertyToID("_DstBlend");
        private static readonly int k_FalloffIntensityID = Shader.PropertyToID("_FalloffIntensity");
        private static readonly int k_FalloffDistanceID = Shader.PropertyToID("_FalloffDistance");
        private static readonly int k_LightColorID = Shader.PropertyToID("_LightColor");
        private static readonly int k_VolumeOpacityID = Shader.PropertyToID("_VolumeOpacity");
        private static readonly int k_CookieTexID = Shader.PropertyToID("_CookieTex");
        private static readonly int k_FalloffLookupID = Shader.PropertyToID("_FalloffLookup");
        private static readonly int k_LightPositionID = Shader.PropertyToID("_LightPosition");
        private static readonly int k_LightInvMatrixID = Shader.PropertyToID("_LightInvMatrix");
        private static readonly int k_InnerRadiusMultID = Shader.PropertyToID("_InnerRadiusMult");
        private static readonly int k_OuterAngleID = Shader.PropertyToID("_OuterAngle");
        private static readonly int k_InnerAngleMultID = Shader.PropertyToID("_InnerAngleMult");
        private static readonly int k_LightLookupID = Shader.PropertyToID("_LightLookup");
        private static readonly int k_IsFullSpotlightID = Shader.PropertyToID("_IsFullSpotlight");
        private static readonly int k_LightZDistanceID = Shader.PropertyToID("_LightZDistance");
        private static readonly int k_PointLightCookieTexID = Shader.PropertyToID("_PointLightCookieTex");

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

        public static void CreateNormalMapRenderTexture(this IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmd, float renderScale)
        {
            if (renderScale != pass.rendererData.normalsRenderTargetScale)
            {
                if (pass.rendererData.isNormalsRenderTargetValid)
                    cmd.ReleaseTemporaryRT(pass.rendererData.normalsRenderTargetId);

                pass.rendererData.isNormalsRenderTargetValid = true;
                pass.rendererData.normalsRenderTargetScale = renderScale;

                var descriptor = new RenderTextureDescriptor(
                    (int)(renderingData.cameraData.cameraTargetDescriptor.width * renderScale),
                    (int)(renderingData.cameraData.cameraTargetDescriptor.height * renderScale));

                descriptor.graphicsFormat = GetRenderTextureFormat();
                descriptor.useMipMap = false;
                descriptor.autoGenerateMips = false;
                descriptor.depthBufferBits = pass.rendererData.useDepthStencilBuffer ? Renderer2D.k_DepthBufferBits : 0;
                descriptor.msaaSamples = renderingData.cameraData.cameraTargetDescriptor.msaaSamples;
                descriptor.dimension = TextureDimension.Tex2D;

                cmd.GetTemporaryRT(pass.rendererData.normalsRenderTargetId, descriptor, FilterMode.Bilinear);
            }
        }

        public static RenderTextureDescriptor GetBlendStyleRenderTextureDesc(this IRenderPass2D pass, RenderingData renderingData)
        {
            var renderTextureScale = Mathf.Clamp(pass.rendererData.lightRenderTextureScale, 0.01f, 1.0f);
            var width = (int)(renderingData.cameraData.cameraTargetDescriptor.width * renderTextureScale);
            var height = (int)(renderingData.cameraData.cameraTargetDescriptor.height * renderTextureScale);

            var descriptor = new RenderTextureDescriptor(width, height);
            descriptor.graphicsFormat = GetRenderTextureFormat();
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;
            descriptor.dimension = TextureDimension.Tex2D;

            return descriptor;
        }

        public static void CreateCameraSortingLayerRenderTexture(this IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmd, Downsampling downsamplingMethod)
        {
            var renderTextureScale = 1.0f;
            if (downsamplingMethod == Downsampling._2xBilinear)
                renderTextureScale = 0.5f;
            else if (downsamplingMethod == Downsampling._4xBox || downsamplingMethod == Downsampling._4xBilinear)
                renderTextureScale = 0.25f;

            var width = (int)(renderingData.cameraData.cameraTargetDescriptor.width * renderTextureScale);
            var height = (int)(renderingData.cameraData.cameraTargetDescriptor.height * renderTextureScale);

            var descriptor = new RenderTextureDescriptor(width, height);
            descriptor.graphicsFormat = renderingData.cameraData.cameraTargetDescriptor.graphicsFormat;
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;
            descriptor.dimension = TextureDimension.Tex2D;

            cmd.GetTemporaryRT(pass.rendererData.cameraSortingLayerRenderTargetId, descriptor, FilterMode.Bilinear);
        }

        public static void EnableBlendStyle(CommandBuffer cmd, int blendStyleIndex, bool enabled)
        {
            var keyword = k_UseBlendStyleKeywords[blendStyleIndex];

            if (enabled)
                cmd.EnableShaderKeyword(keyword);
            else
                cmd.DisableShaderKeyword(keyword);
        }

        public static void DisableAllKeywords(this IRenderPass2D pass, CommandBuffer cmd)
        {
            foreach (var keyword in k_UseBlendStyleKeywords)
            {
                cmd.DisableShaderKeyword(keyword);
            }
        }

        public static void ReleaseRenderTextures(this IRenderPass2D pass, CommandBuffer cmd)
        {
            pass.rendererData.isNormalsRenderTargetValid = false;
            pass.rendererData.normalsRenderTargetScale = 0.0f;
            cmd.ReleaseTemporaryRT(pass.rendererData.normalsRenderTargetId);
            cmd.ReleaseTemporaryRT(pass.rendererData.shadowsRenderTargetId);
            cmd.ReleaseTemporaryRT(pass.rendererData.cameraSortingLayerRenderTargetId);
        }

        public static void DrawPointLight(CommandBuffer cmd, Light2D light, Mesh lightMesh, Material material)
        {
            var scale = new Vector3(light.pointLightOuterRadius, light.pointLightOuterRadius, light.pointLightOuterRadius);
            var matrix = Matrix4x4.TRS(light.transform.position, light.transform.rotation, scale);
            cmd.DrawMesh(lightMesh, matrix, material);
        }


        private static bool CanCastShadows(Light2D light, int layerToRender)
        {
            return light.shadowsEnabled && light.shadowIntensity > 0 && light.IsLitLayer(layerToRender);
        }

        private static bool CanCastVolumetricShadows(Light2D light, int endLayerValue)
        {
            var topMostLayerValue = light.GetTopMostLitLayer();
            return light.volumetricShadowsEnabled && light.shadowVolumeIntensity > 0 && topMostLayerValue == endLayerValue;
        }


        private static void RenderLightSet(IRenderPass2D pass, RenderingData renderingData, int blendStyleIndex, CommandBuffer cmd, int layerToRender, RenderTargetIdentifier renderTexture, List<Light2D> lights)
        {
            var maxShadowLightCount = ShadowRendering.maxTextureCount * 4;
            var requiresRTInit = true;

            // This case should never happen, but if it does it may cause an infinite loop later.
            if (maxShadowLightCount < 1)
            {
                Debug.LogError("maxShadowTextureCount cannot be less than 1");
                return;
            }


            NativeArray<bool> doesLightAtIndexHaveShadows = new NativeArray<bool>(lights.Count, Allocator.Temp);

            // Break up light rendering into batches for the purpose of shadow casting
            var lightIndex = 0;
            while (lightIndex < lights.Count)
            {
                var remainingLights = (uint)lights.Count - lightIndex;
                var batchedLights = 0;

                // Add lights to our batch until the number of shadow textures reach the maxShadowTextureCount
                int shadowLightCount = 0;
                while (batchedLights < remainingLights && shadowLightCount < maxShadowLightCount)
                {
                    int curLightIndex = lightIndex + batchedLights;
                    var light = lights[curLightIndex];
                    if (CanCastShadows(light, layerToRender))
                    {
                        doesLightAtIndexHaveShadows[curLightIndex] = false;
                        if (ShadowRendering.PrerenderShadows(pass, renderingData, cmd, layerToRender, light, shadowLightCount, light.shadowIntensity))
                        {
                            doesLightAtIndexHaveShadows[curLightIndex] = true;
                            shadowLightCount++;
                        }
                    }
                    batchedLights++;
                }


                // Set the current RT to the light RT
                if (shadowLightCount > 0 || requiresRTInit)
                {
                    cmd.SetRenderTarget(renderTexture, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
                    requiresRTInit = false;
                }

                // Render all the lights.
                shadowLightCount = 0;
                for (var lightIndexOffset = 0; lightIndexOffset < batchedLights; lightIndexOffset++)
                {
                    var light = lights[(int)(lightIndex + lightIndexOffset)];

                    if (light != null &&
                        light.lightType != Light2D.LightType.Global &&
                        light.blendStyleIndex == blendStyleIndex &&
                        light.IsLitLayer(layerToRender))
                    {
                        // Render light
                        var lightMaterial = pass.rendererData.GetLightMaterial(light, false);
                        if (lightMaterial == null)
                            continue;

                        var lightMesh = light.lightMesh;
                        if (lightMesh == null)
                            continue;

                        // Set the shadow texture to read from
                        if (doesLightAtIndexHaveShadows[lightIndex + lightIndexOffset])
                            ShadowRendering.SetGlobalShadowTexture(cmd, light, shadowLightCount++);
                        else
                            ShadowRendering.DisableGlobalShadowTexture(cmd);


                        if (light.lightType == Light2D.LightType.Sprite && light.lightCookieSprite != null && light.lightCookieSprite.texture != null)
                            cmd.SetGlobalTexture(k_CookieTexID, light.lightCookieSprite.texture);

                        SetGeneralLightShaderGlobals(pass, cmd, light);

                        if (light.normalMapQuality != Light2D.NormalMapQuality.Disabled || light.lightType == Light2D.LightType.Point)
                            SetPointLightShaderGlobals(pass, cmd, light);

                        // Light code could be combined...
                        if (light.lightType == (Light2D.LightType)Light2D.DeprecatedLightType.Parametric || light.lightType == Light2D.LightType.Freeform || light.lightType == Light2D.LightType.Sprite)
                        {
                            cmd.DrawMesh(lightMesh, light.transform.localToWorldMatrix, lightMaterial);
                        }
                        else if (light.lightType == Light2D.LightType.Point)
                        {
                            DrawPointLight(cmd, light, lightMesh, lightMaterial);
                        }
                    }
                }

                // Release all of the temporary shadow textures
                for (var releaseIndex = shadowLightCount - 1; releaseIndex >= 0; releaseIndex--)
                    ShadowRendering.ReleaseShadowRenderTexture(cmd, releaseIndex);

                lightIndex += batchedLights;
            }

            doesLightAtIndexHaveShadows.Dispose();
        }

        public static void RenderLightVolumes(this IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmd, int layerToRender, int endLayerValue,
            RenderTargetIdentifier renderTexture, RenderTargetIdentifier depthTexture, RenderBufferStoreAction intermediateStoreAction,
            RenderBufferStoreAction finalStoreAction, bool requiresRTInit, List<Light2D> lights)
        {
            var maxShadowLightCount = ShadowRendering.maxTextureCount * 4;  // Now encodes shadows into RGBA as well as seperate textures

            NativeArray<bool> doesLightAtIndexHaveShadows = new NativeArray<bool>(lights.Count, Allocator.Temp);

            // This case should never happen, but if it does it may cause an infinite loop later.
            if (maxShadowLightCount < 1)
            {
                Debug.LogError("maxShadowLightCount cannot be less than 1");
                return;
            }

            // Determine last light with volumetric shadows to be rendered if we want to use a different store action after using rendering its volumetric shadows
            int useFinalStoreActionAfter = lights.Count;
            if (intermediateStoreAction != finalStoreAction)
            {
                for (int i = lights.Count - 1; i >= 0; i--)
                {
                    if (lights[i].renderVolumetricShadows)
                    {
                        useFinalStoreActionAfter = i;
                        break;
                    }
                }
            }

            // Break up light rendering into batches for the purpose of shadow casting
            var lightIndex = 0;
            while (lightIndex < lights.Count)
            {
                var remainingLights = (uint)lights.Count - lightIndex;
                var batchedLights = 0;

                // Add lights to our batch until the number of shadow textures reach the maxShadowTextureCount
                var shadowLightCount = 0;
                while (batchedLights < remainingLights && shadowLightCount < maxShadowLightCount)
                {
                    int curLightIndex = lightIndex + batchedLights;
                    var light = lights[curLightIndex];

                    if (CanCastVolumetricShadows(light, endLayerValue))
                    {
                        doesLightAtIndexHaveShadows[curLightIndex] = false;
                        if (ShadowRendering.PrerenderShadows(pass, renderingData, cmd, layerToRender, light, shadowLightCount, light.shadowVolumeIntensity))
                        {
                            doesLightAtIndexHaveShadows[curLightIndex] = true;
                            shadowLightCount++;
                        }
                    }
                    batchedLights++;
                }

                // Set the current RT to the light RT
                if (shadowLightCount > 0 || requiresRTInit)
                {
                    var storeAction = lightIndex + batchedLights >= useFinalStoreActionAfter ? finalStoreAction : intermediateStoreAction;
                    cmd.SetRenderTarget(renderTexture, RenderBufferLoadAction.Load, storeAction, depthTexture, RenderBufferLoadAction.Load, storeAction);
                    requiresRTInit = false;
                }

                // Render all the lights.
                shadowLightCount = 0;
                for (var lightIndexOffset = 0; lightIndexOffset < batchedLights; lightIndexOffset++)
                {
                    var light = lights[(int)(lightIndex + lightIndexOffset)];

                    if (light.lightType == Light2D.LightType.Global)
                        continue;

                    if (light.volumeIntensity <= 0.0f || !light.volumeIntensityEnabled)
                        continue;

                    var topMostLayerValue = light.GetTopMostLitLayer();
                    if (endLayerValue == topMostLayerValue) // this implies the layer is correct
                    {
                        var lightVolumeMaterial = pass.rendererData.GetLightMaterial(light, true);
                        var lightMesh = light.lightMesh;

                        // Set the shadow texture to read from.
                        if (doesLightAtIndexHaveShadows[lightIndex + lightIndexOffset])
                            ShadowRendering.SetGlobalShadowTexture(cmd, light, shadowLightCount++);
                        else
                            ShadowRendering.DisableGlobalShadowTexture(cmd);

                        if (light.lightType == Light2D.LightType.Sprite && light.lightCookieSprite != null && light.lightCookieSprite.texture != null)
                            cmd.SetGlobalTexture(k_CookieTexID, light.lightCookieSprite.texture);

                        SetGeneralLightShaderGlobals(pass, cmd, light);

                        // Is this needed
                        if (light.normalMapQuality != Light2D.NormalMapQuality.Disabled || light.lightType == Light2D.LightType.Point)
                            SetPointLightShaderGlobals(pass, cmd, light);

                        // Could be combined...
                        if (light.lightType == Light2D.LightType.Parametric || light.lightType == Light2D.LightType.Freeform || light.lightType == Light2D.LightType.Sprite)
                        {
                            cmd.DrawMesh(lightMesh, light.transform.localToWorldMatrix, lightVolumeMaterial);
                        }
                        else if (light.lightType == Light2D.LightType.Point)
                        {
                            DrawPointLight(cmd, light, lightMesh, lightVolumeMaterial);
                        }
                    }
                }


                // Release all of the temporary shadow textures
                for (var releaseIndex = shadowLightCount - 1; releaseIndex >= 0; releaseIndex--)
                    ShadowRendering.ReleaseShadowRenderTexture(cmd, releaseIndex);

                lightIndex += batchedLights;
            }

            doesLightAtIndexHaveShadows.Dispose();
        }

        public static void SetShapeLightShaderGlobals(this IRenderPass2D pass, CommandBuffer cmd)
        {
            for (var i = 0; i < pass.rendererData.lightBlendStyles.Length; i++)
            {
                var blendStyle = pass.rendererData.lightBlendStyles[i];
                if (i >= k_BlendFactorsPropIDs.Length)
                    break;

                cmd.SetGlobalVector(k_BlendFactorsPropIDs[i], blendStyle.blendFactors);
                cmd.SetGlobalVector(k_MaskFilterPropIDs[i], blendStyle.maskTextureChannelFilter.mask);
                cmd.SetGlobalVector(k_InvertedFilterPropIDs[i], blendStyle.maskTextureChannelFilter.inverted);
            }

            cmd.SetGlobalTexture(k_FalloffLookupID, pass.rendererData.fallOffLookup);
        }

        private static float GetNormalizedInnerRadius(Light2D light)
        {
            return light.pointLightInnerRadius / light.pointLightOuterRadius;
        }

        private static float GetNormalizedAngle(float angle)
        {
            return (angle / 360.0f);
        }

        private static void GetScaledLightInvMatrix(Light2D light, out Matrix4x4 retMatrix)
        {
            var outerRadius = light.pointLightOuterRadius;
            var lightScale = Vector3.one;
            var outerRadiusScale = new Vector3(lightScale.x * outerRadius, lightScale.y * outerRadius, lightScale.z * outerRadius);

            var transform = light.transform;

            var scaledLightMat = Matrix4x4.TRS(transform.position, transform.rotation, outerRadiusScale);
            retMatrix = Matrix4x4.Inverse(scaledLightMat);
        }

        private static void SetGeneralLightShaderGlobals(IRenderPass2D pass, CommandBuffer cmd, Light2D light)
        {
            float intensity = light.intensity * light.color.a;
            Color color = intensity * light.color;
            color.a = 1.0f;

            float volumeIntensity = light.volumeIntensity;

            cmd.SetGlobalFloat(k_FalloffIntensityID, light.falloffIntensity);
            cmd.SetGlobalFloat(k_FalloffDistanceID, light.shapeLightFalloffSize);
            cmd.SetGlobalColor(k_LightColorID, color);
            cmd.SetGlobalFloat(k_VolumeOpacityID, volumeIntensity);
        }

        private static void SetPointLightShaderGlobals(IRenderPass2D pass, CommandBuffer cmd, Light2D light)
        {
            // This is used for the lookup texture
            GetScaledLightInvMatrix(light, out var lightInverseMatrix);

            var innerRadius = GetNormalizedInnerRadius(light);
            var innerAngle = GetNormalizedAngle(light.pointLightInnerAngle);
            var outerAngle = GetNormalizedAngle(light.pointLightOuterAngle);
            var innerRadiusMult = 1 / (1 - innerRadius);

            cmd.SetGlobalVector(k_LightPositionID, light.transform.position);
            cmd.SetGlobalMatrix(k_LightInvMatrixID, lightInverseMatrix);
            cmd.SetGlobalFloat(k_InnerRadiusMultID, innerRadiusMult);
            cmd.SetGlobalFloat(k_OuterAngleID, outerAngle);
            cmd.SetGlobalFloat(k_InnerAngleMultID, 1 / (outerAngle - innerAngle));
            cmd.SetGlobalTexture(k_LightLookupID, Light2DLookupTexture.GetLightLookupTexture());
            cmd.SetGlobalTexture(k_FalloffLookupID, pass.rendererData.fallOffLookup);
            cmd.SetGlobalFloat(k_FalloffIntensityID, light.falloffIntensity);
            cmd.SetGlobalFloat(k_IsFullSpotlightID, innerAngle == 1 ? 1.0f : 0.0f);

            cmd.SetGlobalFloat(k_LightZDistanceID, light.normalMapDistance);

            if (light.lightCookieSprite != null && light.lightCookieSprite.texture != null)
                cmd.SetGlobalTexture(k_PointLightCookieTexID, light.lightCookieSprite.texture);
        }

        public static void ClearDirtyLighting(this IRenderPass2D pass, CommandBuffer cmd, uint blendStylesUsed)
        {
            for (var i = 0; i < pass.rendererData.lightBlendStyles.Length; ++i)
            {
                if ((blendStylesUsed & (uint)(1 << i)) == 0)
                    continue;

                if (!pass.rendererData.lightBlendStyles[i].isDirty)
                    continue;

                CoreUtils.SetRenderTarget(cmd, pass.rendererData.lightBlendStyles[i].renderTargetHandle, ClearFlag.Color, Color.black);
                pass.rendererData.lightBlendStyles[i].isDirty = false;
            }
        }

        public static void RenderNormals(this IRenderPass2D pass, ScriptableRenderContext context, RenderingData renderingData, DrawingSettings drawSettings, FilteringSettings filterSettings, RenderTargetIdentifier depthTarget, CommandBuffer cmd, LightStats lightStats)
        {
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                // figure out the scale
                var normalRTScale = 0.0f;

                if (depthTarget != BuiltinRenderTextureType.None)
                    normalRTScale = 1.0f;
                else
                    normalRTScale = Mathf.Clamp(pass.rendererData.lightRenderTextureScale, 0.01f, 1.0f);

                pass.CreateNormalMapRenderTexture(renderingData, cmd, normalRTScale);


                var msaaEnabled = renderingData.cameraData.cameraTargetDescriptor.msaaSamples > 1;
                var storeAction = msaaEnabled ? RenderBufferStoreAction.Resolve : RenderBufferStoreAction.Store;
                var clearFlag = pass.rendererData.useDepthStencilBuffer ? ClearFlag.All : ClearFlag.Color;
                if (depthTarget != BuiltinRenderTextureType.None)
                {
                    CoreUtils.SetRenderTarget(cmd,
                        pass.rendererData.normalsRenderTarget, RenderBufferLoadAction.DontCare, storeAction,
                        depthTarget, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,
                        clearFlag, k_NormalClearColor);
                }
                else
                    CoreUtils.SetRenderTarget(cmd, pass.rendererData.normalsRenderTarget, RenderBufferLoadAction.DontCare, storeAction, clearFlag, k_NormalClearColor);

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                drawSettings.SetShaderPassName(0, k_NormalsRenderingPassName);
                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filterSettings);
            }
        }

        public static void RenderLights(this IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmd, int layerToRender, ref LayerBatch layerBatch, ref RenderTextureDescriptor rtDesc)
        {
            // Before rendering the lights cache some values that are expensive to get/calculate
            var culledLights = pass.rendererData.lightCullResult.visibleLights;
            for (var i = 0; i < culledLights.Count; i++)
            {
                culledLights[i].CacheValues();
            }

            ShadowCasterGroup2DManager.CacheValues();


            var blendStyles = pass.rendererData.lightBlendStyles;

            for (var i = 0; i < blendStyles.Length; ++i)
            {
                if ((layerBatch.lightStats.blendStylesUsed & (uint)(1 << i)) == 0)
                    continue;

                var sampleName = blendStyles[i].name;
                cmd.BeginSample(sampleName);

                if (!Light2DManager.GetGlobalColor(layerToRender, i, out var clearColor))
                    clearColor = Color.black;

                var anyLights = (layerBatch.lightStats.blendStylesWithLights & (uint)(1 << i)) != 0;

                var desc = rtDesc;
                if (!anyLights) // No lights -- create tiny texture
                    desc.width = desc.height = 4;
                var identifier = layerBatch.GetRTId(cmd, desc, i);

                cmd.SetRenderTarget(identifier,
                    RenderBufferLoadAction.DontCare,
                    RenderBufferStoreAction.Store,
                    RenderBufferLoadAction.DontCare,
                    RenderBufferStoreAction.DontCare);
                cmd.ClearRenderTarget(false, true, clearColor);

                if (anyLights)
                {
                    RenderLightSet(
                        pass, renderingData,
                        i,
                        cmd,
                        layerToRender,
                        identifier,
                        pass.rendererData.lightCullResult.visibleLights
                    );
                }

                cmd.EndSample(sampleName);
            }
        }

        private static void SetBlendModes(Material material, BlendMode src, BlendMode dst)
        {
            material.SetFloat(k_SrcBlendID, (float)src);
            material.SetFloat(k_DstBlendID, (float)dst);
        }

        private static uint GetLightMaterialIndex(Light2D light, bool isVolume)
        {
            var isPoint = light.isPointLight;
            var bitIndex = 0;
            var volumeBit = isVolume ? 1u << bitIndex : 0u;
            bitIndex++;
            var shapeBit = !isPoint ? 1u << bitIndex : 0u;
            bitIndex++;
            var additiveBit = light.overlapOperation == Light2D.OverlapOperation.AlphaBlend ? 0u : 1u << bitIndex;
            bitIndex++;
            var spriteBit = light.lightType == Light2D.LightType.Sprite ? 1u << bitIndex : 0u;
            bitIndex++;
            var pointCookieBit = (isPoint && light.lightCookieSprite != null && light.lightCookieSprite.texture != null) ? 1u << bitIndex : 0u;
            bitIndex++;
            var pointFastQualityBit = (isPoint && light.normalMapQuality == Light2D.NormalMapQuality.Fast) ? 1u << bitIndex : 0u;
            bitIndex++;
            var useNormalMap = light.normalMapQuality != Light2D.NormalMapQuality.Disabled ? 1u << bitIndex : 0u;

            return pointFastQualityBit | pointCookieBit | spriteBit | additiveBit | shapeBit | volumeBit | useNormalMap;
        }

        private static Material CreateLightMaterial(Renderer2DData rendererData, Light2D light, bool isVolume)
        {
            var isPoint = light.isPointLight;
            Material material;

            if (isVolume)
                material = CoreUtils.CreateEngineMaterial(isPoint ? rendererData.pointLightVolumeShader : rendererData.shapeLightVolumeShader);
            else
            {
                material = CoreUtils.CreateEngineMaterial(isPoint ? rendererData.pointLightShader : rendererData.shapeLightShader);

                if (light.overlapOperation == Light2D.OverlapOperation.Additive)
                {
                    SetBlendModes(material, BlendMode.One, BlendMode.One);
                    material.EnableKeyword(k_UseAdditiveBlendingKeyword);
                }
                else
                    SetBlendModes(material, BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha);
            }

            if (light.lightType == Light2D.LightType.Sprite)
                material.EnableKeyword(k_SpriteLightKeyword);

            if (isPoint && light.lightCookieSprite != null && light.lightCookieSprite.texture != null)
                material.EnableKeyword(k_UsePointLightCookiesKeyword);

            if (isPoint && light.normalMapQuality == Light2D.NormalMapQuality.Fast)
                material.EnableKeyword(k_LightQualityFastKeyword);

            if (light.normalMapQuality != Light2D.NormalMapQuality.Disabled)
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

            return material;
        }
    }
}
