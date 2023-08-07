using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using Unity.Collections;
using System;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    internal static class RendererLighting
    {
        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Draw Normals");
        private static readonly ShaderTagId k_NormalsRenderingPassName = new ShaderTagId("NormalsRendering");
        public static readonly Color k_NormalClearColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
        private static readonly string k_UsePointLightCookiesKeyword = "USE_POINT_LIGHT_COOKIES";
        private static readonly string k_LightQualityFastKeyword = "LIGHT_QUALITY_FAST";
        private static readonly string k_UseNormalMap = "USE_NORMAL_MAP";
        private static readonly string k_UseAdditiveBlendingKeyword = "USE_ADDITIVE_BLENDING";
        private static readonly string k_UseVolumetric = "USE_VOLUMETRIC";

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

        public static readonly string[] k_ShapeLightTextureIDs =
        {
            "_ShapeLightTexture0",
            "_ShapeLightTexture1",
            "_ShapeLightTexture2",
            "_ShapeLightTexture3"
        };

        private static GraphicsFormat s_RenderTextureFormatToUse = GraphicsFormat.R8G8B8A8_UNorm;
        private static bool s_HasSetupRenderTextureFormatToUse;

        private static readonly int k_SrcBlendID = Shader.PropertyToID("_SrcBlend");
        private static readonly int k_DstBlendID = Shader.PropertyToID("_DstBlend");
        private static readonly int k_CookieTexID = Shader.PropertyToID("_CookieTex");
        private static readonly int k_LightLookupID = Shader.PropertyToID("_LightLookup");
        private static readonly int k_FalloffLookupID = Shader.PropertyToID("_FalloffLookup");
        private static readonly int k_PointLightCookieTexID = Shader.PropertyToID("_PointLightCookieTex");

        private static readonly int k_L2DInvMatrix = Shader.PropertyToID("L2DInvMatrix");
        private static readonly int k_L2DColor = Shader.PropertyToID("L2DColor");
        private static readonly int k_L2DPosition = Shader.PropertyToID("L2DPosition");
        private static readonly int k_L2DFalloffIntensity = Shader.PropertyToID("L2DFalloffIntensity");
        private static readonly int k_L2DFalloffDistance = Shader.PropertyToID("L2DFalloffDistance");
        private static readonly int k_L2DOuterAngle = Shader.PropertyToID("L2DOuterAngle");
        private static readonly int k_L2DInnerAngle = Shader.PropertyToID("L2DInnerAngle");
        private static readonly int k_L2DInnerRadiusMult = Shader.PropertyToID("L2DInnerRadiusMult");
        private static readonly int k_L2DVolumeOpacity = Shader.PropertyToID("L2DVolumeOpacity");
        private static readonly int k_L2DShadowIntensity = Shader.PropertyToID("L2DShadowIntensity");
        private static readonly int k_L2DLightType = Shader.PropertyToID("L2DLightType");

        // Light Batcher.
        internal static LightBatch lightBatch = new LightBatch();

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
            var descriptor = new RenderTextureDescriptor(
                (int)(renderingData.cameraData.cameraTargetDescriptor.width * renderScale),
                (int)(renderingData.cameraData.cameraTargetDescriptor.height * renderScale));

            descriptor.graphicsFormat = GetRenderTextureFormat();
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = renderingData.cameraData.cameraTargetDescriptor.msaaSamples;
            descriptor.dimension = TextureDimension.Tex2D;

            RenderingUtils.ReAllocateIfNeeded(ref pass.rendererData.normalsRenderTarget, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_NormalMap");
            cmd.SetGlobalTexture(pass.rendererData.normalsRenderTarget.name, pass.rendererData.normalsRenderTarget.nameID);
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
            descriptor.graphicsFormat = GraphicsFormat.B10G11R11_UFloatPack32;
            descriptor.dimension = TextureDimension.Tex2D;

            RenderingUtils.ReAllocateIfNeeded(ref pass.rendererData.cameraSortingLayerRenderTarget, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_CameraSortingLayerTexture");
            cmd.SetGlobalTexture(pass.rendererData.cameraSortingLayerRenderTarget.name, pass.rendererData.cameraSortingLayerRenderTarget.nameID);
        }

        public static void EnableBlendStyle(CommandBuffer cmd, int blendStyleIndex, bool enabled)
        {
            var keyword = k_UseBlendStyleKeywords[blendStyleIndex];

            if (enabled)
                cmd.EnableShaderKeyword(keyword);
            else
                cmd.DisableShaderKeyword(keyword);
        }

        public static void DisableAllKeywords(CommandBuffer cmd)
        {
            foreach (var keyword in k_UseBlendStyleKeywords)
            {
                cmd.DisableShaderKeyword(keyword);
            }
        }

        public static void GetTransparencySortingMode(Renderer2DData rendererData, Camera camera, ref SortingSettings sortingSettings)
        {
            var mode = rendererData.transparencySortMode;

            if (mode == TransparencySortMode.Default)
            {
                mode = camera.orthographic ? TransparencySortMode.Orthographic : TransparencySortMode.Perspective;
            }

            switch (mode)
            {
                case TransparencySortMode.Perspective:
                    sortingSettings.distanceMetric = DistanceMetric.Perspective;
                    break;
                case TransparencySortMode.Orthographic:
                    sortingSettings.distanceMetric = DistanceMetric.Orthographic;
                    break;
                default:
                    sortingSettings.distanceMetric = DistanceMetric.CustomAxis;
                    sortingSettings.customAxis = rendererData.transparencySortAxis;
                    break;
            }
        }

        private static bool CanRenderLight(IRenderPass2D pass, Light2D light, int blendStyleIndex, int layerToRender, bool isVolume, ref Mesh lightMesh, ref Material lightMaterial)
        {
            if (light != null && light.lightType != Light2D.LightType.Global && light.blendStyleIndex == blendStyleIndex && light.IsLitLayer(layerToRender))
            {
                lightMesh = light.lightMesh;
                if (lightMesh == null)
                    return false;
                lightMaterial = pass.rendererData.GetLightMaterial(light, isVolume);
                if (lightMaterial == null)
                    return false;
                return true;
            }
            return false;
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

        internal static void RenderLight(IRenderPass2D pass, CommandBuffer cmd, Light2D light, bool isVolume, int blendStyleIndex, int layerToRender, bool hasShadows, bool batchingSupported, ref int shadowLightCount)
        {
            Mesh lightMesh = null;
            Material lightMaterial = null;
            if (!CanRenderLight(pass, light, blendStyleIndex, layerToRender, isVolume, ref lightMesh, ref lightMaterial))
                return;

            // For Batching.
            bool canBatch = lightBatch.CanBatch(light, lightMaterial, light.batchSlotIndex, out int lightHash);
            bool hasCookies = SetCookieShaderGlobals(cmd, light);

            // Flush on Break.
            bool breakBatch = hasShadows || hasCookies || !canBatch;
            if (breakBatch && batchingSupported)
                lightBatch.Flush(cmd);

            // Set the shadow texture to read from
            if (hasShadows)
                ShadowRendering.SetGlobalShadowTexture(cmd, light, shadowLightCount++);

            var slotIndex = lightBatch.SlotIndex(light.batchSlotIndex);
            SetPerLightShaderGlobals(cmd, light, slotIndex, isVolume, hasShadows, batchingSupported);
            if (light.lightType == Light2D.LightType.Point)
                SetPerPointLightShaderGlobals(pass.rendererData, cmd, light, slotIndex, batchingSupported);

            // Check if StructuredBuffer is supported, if not fallback.
            if (batchingSupported)
            {
                lightBatch.AddBatch(light, lightMaterial, light.GetMatrix(), lightMesh, 0, lightHash, light.batchSlotIndex);
            }
            else
            {
                cmd.DrawMesh(lightMesh, light.GetMatrix(), lightMaterial);
            }
        }

        private static void RenderLightSet(IRenderPass2D pass, RenderingData renderingData, int blendStyleIndex, CommandBuffer cmd, int layerToRender, RenderTargetIdentifier renderTexture, List<Light2D> lights)
        {
            var maxShadowLightCount = ShadowRendering.maxTextureCount;
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
                    var arrayIndex = (int)(lightIndex + lightIndexOffset);
                    RenderLight(pass, cmd, lights[arrayIndex], false, blendStyleIndex, layerToRender, doesLightAtIndexHaveShadows[arrayIndex], LightBatch.isBatchingSupported, ref shadowLightCount);
                }
                lightBatch.Flush(cmd);

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
            var maxShadowLightCount = ShadowRendering.maxTextureCount;  // Now encodes shadows into RG,BA as well as seperate textures

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
                    var arrayIndex = (int)(lightIndex + lightIndexOffset);
                    var light = lights[arrayIndex];

                    if (light.volumeIntensity <= 0.0f || !light.volumetricEnabled)
                        continue;

                    if (endLayerValue == light.GetTopMostLitLayer()) // this implies the layer is correct
                        RenderLight(pass, cmd, light, true, light.blendStyleIndex, layerToRender, doesLightAtIndexHaveShadows[arrayIndex], LightBatch.isBatchingSupported, ref shadowLightCount);
                }
                lightBatch.Flush(cmd);

                // Release all of the temporary shadow textures
                for (var releaseIndex = shadowLightCount - 1; releaseIndex >= 0; releaseIndex--)
                    ShadowRendering.ReleaseShadowRenderTexture(cmd, releaseIndex);

                lightIndex += batchedLights;
            }

            doesLightAtIndexHaveShadows.Dispose();
        }

        public static void SetLightShaderGlobals(Renderer2DData rendererData, CommandBuffer cmd)
        {
            for (var i = 0; i < rendererData.lightBlendStyles.Length; i++)
            {
                var blendStyle = rendererData.lightBlendStyles[i];
                if (i >= k_BlendFactorsPropIDs.Length)
                    break;

                cmd.SetGlobalVector(k_BlendFactorsPropIDs[i], blendStyle.blendFactors);
                cmd.SetGlobalVector(k_MaskFilterPropIDs[i], blendStyle.maskTextureChannelFilter.mask);
                cmd.SetGlobalVector(k_InvertedFilterPropIDs[i], blendStyle.maskTextureChannelFilter.inverted);
            }

            cmd.SetGlobalTexture(k_FalloffLookupID, rendererData.fallOffLookup);
            cmd.SetGlobalTexture(k_LightLookupID, Light2DLookupTexture.GetLightLookupTexture());
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

        public static void SetPerLightShaderGlobals(CommandBuffer cmd, Light2D light, int slot, bool isVolumetric, bool hasShadows, bool batchingSupported)
        {
            float intensity = light.intensity * light.color.a;
            Color color = intensity * light.color;
            color.a = 1.0f;

            float volumeIntensity = light.volumetricEnabled ? light.volumeIntensity : 1.0f;

            if (batchingSupported)
            {
                // Batched Params.
                PerLight2D perLight = lightBatch.GetLight(slot);
                perLight.Position = new float4(light.transform.position, light.normalMapDistance);
                perLight.FalloffIntensity = light.falloffIntensity;
                perLight.FalloffDistance = light.shapeLightFalloffSize;
                perLight.Color = new float4(color.r, color.g, color.b, color.a);
                perLight.VolumeOpacity = volumeIntensity;
                perLight.LightType = (int)light.lightType;
                perLight.ShadowIntensity = 1.0f;
                if (hasShadows)
                    perLight.ShadowIntensity = isVolumetric ? (1 - light.shadowVolumeIntensity) : (1 - light.shadowIntensity);
                lightBatch.SetLight(slot, perLight);
            }
            else
            {
                cmd.SetGlobalVector(k_L2DPosition, new float4(light.transform.position, light.normalMapDistance));
                cmd.SetGlobalFloat(k_L2DFalloffIntensity, light.falloffIntensity);
                cmd.SetGlobalFloat(k_L2DFalloffDistance, light.shapeLightFalloffSize);
                cmd.SetGlobalColor(k_L2DColor, color);
                cmd.SetGlobalFloat(k_L2DVolumeOpacity, volumeIntensity);
                cmd.SetGlobalInt(k_L2DLightType, (int)light.lightType);
                cmd.SetGlobalFloat(k_L2DShadowIntensity, hasShadows ? (isVolumetric ? (1 - light.shadowVolumeIntensity) : (1 - light.shadowIntensity)) : 1);
            }
        }

        public static void SetPerPointLightShaderGlobals(Renderer2DData rendererData, CommandBuffer cmd, Light2D light, int slot, bool batchingSupported)
        {
            // This is used for the lookup texture
            GetScaledLightInvMatrix(light, out var lightInverseMatrix);

            var innerRadius = GetNormalizedInnerRadius(light);
            var innerAngle = GetNormalizedAngle(light.pointLightInnerAngle);
            var outerAngle = GetNormalizedAngle(light.pointLightOuterAngle);
            var innerRadiusMult = 1 / (1 - innerRadius);

            if (batchingSupported)
            {
                // Batched Params.
                PerLight2D perLight = lightBatch.GetLight(slot);
                perLight.InvMatrix = new float4x4(lightInverseMatrix.GetColumn(0), lightInverseMatrix.GetColumn(1), lightInverseMatrix.GetColumn(2), lightInverseMatrix.GetColumn(3));
                perLight.InnerRadiusMult = innerRadiusMult;
                perLight.InnerAngle = innerAngle;
                perLight.OuterAngle = outerAngle;
                lightBatch.SetLight(slot, perLight);
            }
            else
            {
                cmd.SetGlobalMatrix(k_L2DInvMatrix, lightInverseMatrix);
                cmd.SetGlobalFloat(k_L2DInnerRadiusMult, innerRadiusMult);
                cmd.SetGlobalFloat(k_L2DInnerAngle, innerAngle);
                cmd.SetGlobalFloat(k_L2DOuterAngle, outerAngle);
            }
        }

        public static bool SetCookieShaderGlobals(CommandBuffer cmd, Light2D light)
        {
            bool hasCookies = (light.lightType == Light2D.LightType.Point || light.lightType == Light2D.LightType.Sprite) && (light.lightCookieSprite != null && light.lightCookieSprite.texture != null);

            if (hasCookies)
                cmd.SetGlobalTexture(light.lightType == Light2D.LightType.Sprite ? k_CookieTexID : k_PointLightCookieTexID, light.lightCookieSprite.texture);

            return hasCookies;
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

        public static void RenderNormals(this IRenderPass2D pass, ScriptableRenderContext context, RenderingData renderingData, DrawingSettings drawSettings, FilteringSettings filterSettings, RTHandle depthTarget, LightStats lightStats)
        {
            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                // figure out the scale
                var normalRTScale = 0.0f;

                if (depthTarget != null)
                    normalRTScale = 1.0f;
                else
                    normalRTScale = Mathf.Clamp(pass.rendererData.lightRenderTextureScale, 0.01f, 1.0f);

                pass.CreateNormalMapRenderTexture(renderingData, cmd, normalRTScale);


                var msaaEnabled = renderingData.cameraData.cameraTargetDescriptor.msaaSamples > 1;
                var storeAction = msaaEnabled ? RenderBufferStoreAction.Resolve : RenderBufferStoreAction.Store;
                var clearFlag = pass.rendererData.useDepthStencilBuffer ? ClearFlag.All : ClearFlag.Color;
                if (depthTarget != null)
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

                var param = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);
                var rl = context.CreateRendererList(ref param);
                cmd.DrawRendererList(rl);
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
            var shapeBit = (isVolume && !isPoint) ? 1u << bitIndex : 0u;
            bitIndex++;
            var additiveBit = light.overlapOperation == Light2D.OverlapOperation.AlphaBlend ? 0u : 1u << bitIndex;
            bitIndex++;
            var pointCookieBit = (isPoint && light.lightCookieSprite != null && light.lightCookieSprite.texture != null) ? 1u << bitIndex : 0u;
            bitIndex++;
            var fastQualityBit = (light.normalMapQuality == Light2D.NormalMapQuality.Fast) ? 1u << bitIndex : 0u;
            bitIndex++;
            var useNormalMap = light.normalMapQuality != Light2D.NormalMapQuality.Disabled ? 1u << bitIndex : 0u;

            return fastQualityBit | pointCookieBit | additiveBit | shapeBit | volumeBit | useNormalMap;
        }

        private static Material CreateLightMaterial(Renderer2DData rendererData, Light2D light, bool isVolume)
        {
            var isPoint = light.isPointLight;

            Material material = CoreUtils.CreateEngineMaterial(rendererData.lightShader);

            if (!isVolume)
            {
                if (light.overlapOperation == Light2D.OverlapOperation.Additive)
                {
                    SetBlendModes(material, BlendMode.One, BlendMode.One);
                    material.EnableKeyword(k_UseAdditiveBlendingKeyword);
                }
                else
                    SetBlendModes(material, BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha);
            }
            else
            {
                material.EnableKeyword(k_UseVolumetric);

                if (light.lightType == Light2D.LightType.Point)
                    SetBlendModes(material, BlendMode.One, BlendMode.One);
                else
                {
                    material.SetInt("_HandleZTest", (int)CompareFunction.Disabled);
                    SetBlendModes(material, BlendMode.SrcAlpha, BlendMode.One);
                }
            }


            if (isPoint && light.lightCookieSprite != null && light.lightCookieSprite.texture != null)
                material.EnableKeyword(k_UsePointLightCookiesKeyword);

            if (light.normalMapQuality == Light2D.NormalMapQuality.Fast)
                material.EnableKeyword(k_LightQualityFastKeyword);

            if (light.normalMapQuality != Light2D.NormalMapQuality.Disabled)
                material.EnableKeyword(k_UseNormalMap);

            return material;
        }

        public static Material GetLightMaterial(this Renderer2DData rendererData, Light2D light, bool isVolume)
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
