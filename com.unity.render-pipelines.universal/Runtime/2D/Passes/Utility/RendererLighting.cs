using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal static class RendererLighting
    {
        static readonly ShaderTagId k_NormalsRenderingPassName = new ShaderTagId("NormalsRendering");
        static readonly Color k_NormalClearColor = new Color(0.5f, 0.5f, 1.0f, 1.0f);
        static readonly string k_SpriteLightKeyword = "SPRITE_LIGHT";
        static readonly string k_UsePointLightCookiesKeyword = "USE_POINT_LIGHT_COOKIES";
        static readonly string k_LightQualityFastKeyword = "LIGHT_QUALITY_FAST";
        static readonly string k_UseNormalMap = "USE_NORMAL_MAP";
        static readonly string k_UseAdditiveBlendingKeyword = "USE_ADDITIVE_BLENDING";
        const int k_NumberOfLightMaterials = 1 << 5 + 3;  // 5 keywords +  volume bit, shape bit

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

        static Renderer2DData s_Renderer2DData;
        static RenderingData  s_RenderingData;
        static Light2DBlendStyle[] s_BlendStyles;
        static RenderTargetHandle[] s_LightRenderTargets;
        static bool[] s_LightRenderTargetsDirty;
        static RenderTargetHandle s_ShadowsRenderTarget;
        static RenderTargetHandle s_NormalsTarget;
        static Texture s_LightLookupTexture;
        static Texture s_FalloffLookupTexture;
        static Material[] s_LightMaterials;
        static Material[] s_ShadowMaterials;
        static Material[] s_RemoveSelfShadowMaterials;

        static RenderTextureFormat s_RenderTextureFormatToUse = RenderTextureFormat.ARGB32;
        static bool s_HasSetupRenderTextureFormatToUse;

        static public void Setup(RenderingData renderingData, Renderer2DData renderer2DData)
        {
            s_Renderer2DData = renderer2DData;
            s_BlendStyles = renderer2DData.lightBlendStyles;
            s_RenderingData = renderingData;

            if (s_LightRenderTargets == null)
            {
                s_LightRenderTargets = new RenderTargetHandle[s_BlendStyles.Length];
                s_LightRenderTargets[0].Init("_ShapeLightTexture0");
                s_LightRenderTargets[1].Init("_ShapeLightTexture1");
                s_LightRenderTargets[2].Init("_ShapeLightTexture2");
                s_LightRenderTargets[3].Init("_ShapeLightTexture3");

                s_LightRenderTargetsDirty = new bool[s_BlendStyles.Length];
            }

            if (s_NormalsTarget.id == 0)
                s_NormalsTarget.Init("_NormalMap");

            if (s_ShadowsRenderTarget.id == 0)
                s_ShadowsRenderTarget.Init("_ShadowTex");

            // The array size should be determined by the number of 'feature bit' the material index has. See GetLightMaterialIndex().
            // Not all slots must be filled because certain combinations of the feature bits don't make sense (e.g. sprite bit on + shape bit off).
            if (s_LightMaterials == null)
                s_LightMaterials = new Material[k_NumberOfLightMaterials];

            // This really needs to be deleted and replaced with a material block
            const int totalMaterials = 256;
            if (s_ShadowMaterials == null)
                s_ShadowMaterials = new Material[totalMaterials];

            if (s_RemoveSelfShadowMaterials == null)
                s_RemoveSelfShadowMaterials = new Material[totalMaterials];
        }


        static public void CreateNormalMapRenderTexture(CommandBuffer cmd)
        {
            if (!s_HasSetupRenderTextureFormatToUse)
            {
                if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB111110Float))
                    s_RenderTextureFormatToUse = RenderTextureFormat.RGB111110Float;
                else if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
                    s_RenderTextureFormatToUse = RenderTextureFormat.ARGBHalf;

                s_HasSetupRenderTextureFormatToUse = true;
            }

            RenderTextureDescriptor descriptor = new RenderTextureDescriptor(s_RenderingData.cameraData.cameraTargetDescriptor.width, s_RenderingData.cameraData.cameraTargetDescriptor.height);
            descriptor.colorFormat = s_RenderTextureFormatToUse;
            descriptor.sRGB = false;
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;
            descriptor.dimension = TextureDimension.Tex2D;

            cmd.GetTemporaryRT(s_NormalsTarget.id, descriptor, FilterMode.Bilinear);
        }

        static public void CreateBlendStyleRenderTexture(CommandBuffer cmd, int blendStyleIndex)
        {
            if (!s_HasSetupRenderTextureFormatToUse)
            {
                if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB111110Float))
                    s_RenderTextureFormatToUse = RenderTextureFormat.RGB111110Float;
                else if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
                    s_RenderTextureFormatToUse = RenderTextureFormat.ARGBHalf;

                s_HasSetupRenderTextureFormatToUse = true;
            }

            float renderTextureScale = Mathf.Clamp(s_BlendStyles[blendStyleIndex].renderTextureScale, 0.01f, 1.0f);
            int width = (int)(s_RenderingData.cameraData.cameraTargetDescriptor.width * renderTextureScale);
            int height = (int)(s_RenderingData.cameraData.cameraTargetDescriptor.height * renderTextureScale);

            RenderTextureDescriptor descriptor = new RenderTextureDescriptor(width, height);
            descriptor.colorFormat = s_RenderTextureFormatToUse;
            descriptor.sRGB = false;
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;
            descriptor.dimension = TextureDimension.Tex2D;

            cmd.GetTemporaryRT(s_LightRenderTargets[blendStyleIndex].id, descriptor, FilterMode.Bilinear);

            cmd.EnableShaderKeyword(k_UseBlendStyleKeywords[blendStyleIndex]);
            s_LightRenderTargetsDirty[blendStyleIndex] = true;
        }

        static public void CreateShadowRenderTexture(CommandBuffer cmd, int blendStyleIndex)
        {
            float renderTextureScale = Mathf.Clamp(s_BlendStyles[blendStyleIndex].renderTextureScale, 0.01f, 1.0f);
            int width = (int)(s_RenderingData.cameraData.cameraTargetDescriptor.width * renderTextureScale);
            int height = (int)(s_RenderingData.cameraData.cameraTargetDescriptor.height * renderTextureScale);

            RenderTextureDescriptor descriptor = new RenderTextureDescriptor(width, height);
            descriptor.colorFormat = RenderTextureFormat.ARGB32;
            descriptor.sRGB = false;
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            descriptor.depthBufferBits = 24;
            descriptor.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
            descriptor.msaaSamples = 1;
            descriptor.dimension = TextureDimension.Tex2D;

            cmd.GetTemporaryRT(s_ShadowsRenderTarget.id, descriptor, FilterMode.Bilinear);
        }

        static public void ReleaseShadowRenderTexture(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(s_ShadowsRenderTarget.id);
        }

        static public void ReleaseRenderTextures(CommandBuffer cmd)
        {
            for (int i = 0; i < s_BlendStyles.Length; ++i)
            {
                cmd.ReleaseTemporaryRT(s_LightRenderTargets[i].id);
            }

            cmd.ReleaseTemporaryRT(s_NormalsTarget.id);
            cmd.ReleaseTemporaryRT(s_ShadowsRenderTarget.id);
        }


        static private void RenderShadows(CommandBuffer cmdBuffer, int layerToRender, Light2D light, float shadowIntensity, RenderTargetIdentifier renderTexture)
        {
            cmdBuffer.SetGlobalFloat("_ShadowIntensity", 1 - light.shadowIntensity);
            cmdBuffer.SetGlobalFloat("_ShadowVolumeIntensity", 1 - light.shadowVolumeIntensity);

            if (shadowIntensity > 0)
            {
                CreateShadowRenderTexture(cmdBuffer, light.blendStyleIndex);

                cmdBuffer.SetRenderTarget(s_ShadowsRenderTarget.Identifier()); // This isn't efficient if this light doesn't cast shadow.
                cmdBuffer.ClearRenderTarget(true, true, Color.black);

                BoundingSphere lightBounds = light.GetBoundingSphere(); // Gets the local bounding sphere...

                cmdBuffer.SetGlobalVector("_LightPos", light.transform.position);
                cmdBuffer.SetGlobalFloat("_LightRadius", lightBounds.radius);

                Material shadowMaterial = GetShadowMaterial(1);
                Material removeSelfShadowMaterial = GetRemoveSelfShadowMaterial(1);
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
                            shadowMaterial = GetShadowMaterial(incrementingGroupIndex);
                            removeSelfShadowMaterial = GetRemoveSelfShadowMaterial(incrementingGroupIndex);
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

                ReleaseShadowRenderTexture(cmdBuffer);
                cmdBuffer.SetRenderTarget(renderTexture);
            }
        }

        static private bool RenderLightSet(Camera camera, int blendStyleIndex, CommandBuffer cmdBuffer, int layerToRender, RenderTargetIdentifier renderTexture, List<Light2D> lights)
        {
            bool renderedAnyLight = false;
            
            foreach (var light in lights)
            {
                if (light != null && light.lightType != Light2D.LightType.Global && light.blendStyleIndex == blendStyleIndex && light.IsLitLayer(layerToRender) && light.IsLightVisible(camera))
                {
                    // Render light
                    Material lightMaterial = GetLightMaterial(light, false);
                    if (lightMaterial != null)
                    {
                        Mesh lightMesh = light.GetMesh();
                        if (lightMesh != null)
                        {
                            RenderShadows(cmdBuffer, layerToRender, light, light.shadowIntensity, renderTexture);

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

            return renderedAnyLight;
        }

        static private void RenderLightVolumeSet(Camera camera, int blendStyleIndex, CommandBuffer cmdBuffer, int layerToRender, RenderTargetIdentifier renderTexture, List<Light2D> lights)
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
                            Material lightVolumeMaterial = GetLightMaterial(light, true);
                            if (lightVolumeMaterial != null)
                            {
                                Mesh lightMesh = light.GetMesh();
                                if (lightMesh != null)
                                {
                                    RenderShadows(cmdBuffer, layerToRender, light, light.shadowVolumeIntensity, renderTexture);

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

        static public void SetShapeLightShaderGlobals(CommandBuffer cmdBuffer)
        {
            for (int i = 0; i < s_BlendStyles.Length; ++i)
            {

                if (i >= k_UseBlendStyleKeywords.Length)
                    break;

                string keyword = k_UseBlendStyleKeywords[i];
                cmdBuffer.DisableShaderKeyword(keyword);
                cmdBuffer.SetGlobalVector(k_BlendFactorsPropNames[i], s_BlendStyles[i].blendFactors);
                cmdBuffer.SetGlobalVector(k_MaskFilterPropNames[i], s_BlendStyles[i].maskTextureChannelFilter.mask);
                cmdBuffer.SetGlobalVector(k_InvertedFilterPropNames[i], s_BlendStyles[i].maskTextureChannelFilter.inverted);
            }

            cmdBuffer.SetGlobalTexture("_FalloffLookup", GetFalloffLookupTexture());
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

        static public void ClearDirtyLighting(CommandBuffer cmdBuffer)
        {
            for (int i = 0; i < s_BlendStyles.Length; ++i)
            {
                if (s_LightRenderTargetsDirty[i])
                {
                    cmdBuffer.SetRenderTarget(s_LightRenderTargets[i].Identifier());
                    cmdBuffer.ClearRenderTarget(false, true, Color.black);
                    s_LightRenderTargetsDirty[i] = false;
                }
            }
        }

        static public void RenderNormals(ScriptableRenderContext renderContext, CullingResults cullResults, DrawingSettings drawSettings, FilteringSettings filterSettings)
        {
            var cmd = CommandBufferPool.Get("Clear Normals");
            cmd.SetRenderTarget(s_NormalsTarget.Identifier());
            cmd.ClearRenderTarget(true, true, k_NormalClearColor);
            renderContext.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            drawSettings.SetShaderPassName(0, k_NormalsRenderingPassName);
            renderContext.DrawRenderers(cullResults, ref drawSettings, ref filterSettings);
        }

        static public void RenderLights(Camera camera, CommandBuffer cmdBuffer, int layerToRender, uint blendStylesUsed)
        {
            for (int i = 0; i < s_BlendStyles.Length; ++i)
            {
                if ((blendStylesUsed & (uint)(1<<i)) == 0)
                    continue;

                string sampleName = s_BlendStyles[i].name;
                cmdBuffer.BeginSample(sampleName);

                cmdBuffer.SetRenderTarget(s_LightRenderTargets[i].Identifier());

                bool rtDirty = false;
                Color clearColor;
                if (!Light2DManager.GetGlobalColor(layerToRender, i, out clearColor))
                    clearColor = Color.black;
                else
                    rtDirty = true;

                if (s_LightRenderTargetsDirty[i] || rtDirty)
                    cmdBuffer.ClearRenderTarget(false, true, clearColor);

                rtDirty |= RenderLightSet(
                    camera,
                    i,
                    cmdBuffer,
                    layerToRender,
                    s_LightRenderTargets[i].Identifier(),
                    Light2D.GetLightsByBlendStyle(i)
                );

                s_LightRenderTargetsDirty[i] = rtDirty;

                cmdBuffer.EndSample(sampleName);
            }
        }

        static public void RenderLightVolumes(Camera camera, CommandBuffer cmdBuffer, int layerToRender, RenderTargetIdentifier renderTarget, uint blendStylesUsed)
        {
            for (int i = 0; i < s_BlendStyles.Length; ++i)
            {
                if ((blendStylesUsed & (uint)(1 << i)) == 0)
                    continue;

                string sampleName = s_BlendStyles[i].name;
                cmdBuffer.BeginSample(sampleName);

                RenderLightVolumeSet(
                    camera,
                    i,
                    cmdBuffer,
                    layerToRender,
                    renderTarget,
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

        static Material CreateLightMaterial(Light2D light, bool isVolume)
        {
            bool isShape = light.IsShapeLight();
            Material material;

            if (isVolume)
                material = CoreUtils.CreateEngineMaterial(isShape ? s_Renderer2DData.shapeLightVolumeShader : s_Renderer2DData.pointLightVolumeShader);
            else
            {
                material = CoreUtils.CreateEngineMaterial(isShape ? s_Renderer2DData.shapeLightShader : s_Renderer2DData.pointLightShader);

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

        static Material GetLightMaterial(Light2D light, bool isVolume)
        {
            uint materialIndex = GetLightMaterialIndex(light, isVolume);

            if (s_LightMaterials[materialIndex] == null)
                s_LightMaterials[materialIndex] = CreateLightMaterial(light, isVolume);

            return s_LightMaterials[materialIndex];
        }

        static Material GetShadowMaterial(int index)
        {
            int shadowMaterialIndex = index % 255;
            if(s_ShadowMaterials[shadowMaterialIndex] == null)
            {
                s_ShadowMaterials[shadowMaterialIndex] = CoreUtils.CreateEngineMaterial(s_Renderer2DData.shadowGroupShader);
                s_ShadowMaterials[shadowMaterialIndex].SetFloat("_ShadowStencilGroup", index);
            }

            return s_ShadowMaterials[shadowMaterialIndex];
        }

        static Material GetRemoveSelfShadowMaterial(int index)
        {
            int shadowMaterialIndex = index % 255;
            if (s_RemoveSelfShadowMaterials[shadowMaterialIndex] == null)
            {
                s_RemoveSelfShadowMaterials[shadowMaterialIndex] = CoreUtils.CreateEngineMaterial(s_Renderer2DData.removeSelfShadowShader);
                s_RemoveSelfShadowMaterials[shadowMaterialIndex].SetFloat("_ShadowStencilGroup", index);
            }

            return s_RemoveSelfShadowMaterials[shadowMaterialIndex];
        }
    }
}
