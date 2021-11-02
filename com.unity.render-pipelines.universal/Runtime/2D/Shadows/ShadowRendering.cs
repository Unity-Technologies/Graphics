using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    // TODO: Culling of shadow casters, rotate color channels for shadow casting, check get material functions.


    internal static class ShadowRendering
    {
        private static readonly int k_LightPosID = Shader.PropertyToID("_LightPos");
        private static readonly int k_SelfShadowingID = Shader.PropertyToID("_SelfShadowing");
        private static readonly int k_ShadowStencilGroupID = Shader.PropertyToID("_ShadowStencilGroup");
        private static readonly int k_ShadowIntensityID = Shader.PropertyToID("_ShadowIntensity");
        private static readonly int k_ShadowVolumeIntensityID = Shader.PropertyToID("_ShadowVolumeIntensity");
        private static readonly int k_ShadowRadiusID = Shader.PropertyToID("_ShadowRadius");
        private static readonly int k_ShadowColorMaskID = Shader.PropertyToID("_ShadowColorMask");
        private static readonly int k_ShadowModelMatrixID = Shader.PropertyToID("_ShadowModelMatrix");
        private static readonly int k_ShadowModelInvMatrixID = Shader.PropertyToID("_ShadowModelInvMatrix");
        private static readonly int k_ShadowModelScaleID = Shader.PropertyToID("_ShadowModelScale");

        private static readonly ProfilingSampler m_ProfilingSamplerShadows = new ProfilingSampler("Draw 2D Shadow Texture");
        private static readonly ProfilingSampler m_ProfilingSamplerShadowsA = new ProfilingSampler("Draw 2D Shadows (A)");
        private static readonly ProfilingSampler m_ProfilingSamplerShadowsR = new ProfilingSampler("Draw 2D Shadows (R)");
        private static readonly ProfilingSampler m_ProfilingSamplerShadowsG = new ProfilingSampler("Draw 2D Shadows (G)");
        private static readonly ProfilingSampler m_ProfilingSamplerShadowsB = new ProfilingSampler("Draw 2D Shadows (B)");

        private static RenderTargetHandle[] m_RenderTargets = null;
        private static RenderTargetIdentifier[] m_LightInputTextures = null;
        private static readonly Color[] k_ColorLookup = new Color[4] { new Color(0, 0, 0, 1), new Color(0, 0, 1, 0), new Color(0, 1, 0, 0), new Color(1, 0, 0, 0) };
        private static readonly ProfilingSampler[] m_ProfilingSamplerShadowColorsLookup = new ProfilingSampler[4] { m_ProfilingSamplerShadowsA, m_ProfilingSamplerShadowsB, m_ProfilingSamplerShadowsG, m_ProfilingSamplerShadowsR };

        public static uint maxTextureCount { get; private set; }

        public static void InitializeBudget(uint maxTextureCount)
        {
            if (m_RenderTargets == null || m_RenderTargets.Length != maxTextureCount)
            {
                m_RenderTargets = new RenderTargetHandle[maxTextureCount];
                ShadowRendering.maxTextureCount = maxTextureCount;

                for (int i = 0; i < maxTextureCount; i++)
                {
                    unsafe
                    {
                        m_RenderTargets[i].id = Shader.PropertyToID($"ShadowTex_{i}");
                    }
                }
            }

            if (m_LightInputTextures == null || m_LightInputTextures.Length != maxTextureCount)
            {
                m_LightInputTextures = new RenderTargetIdentifier[maxTextureCount];
            }
        }

        private static Material[] CreateMaterials(Shader shader, int pass = 0)
        {
            const int k_ColorChannels = 4;
            Material[] materials = new Material[k_ColorChannels];

            for (int i = 0; i < k_ColorChannels; i++)
            {
                materials[i] = CoreUtils.CreateEngineMaterial(shader);
                materials[i].SetInt(k_ShadowColorMaskID, 1 << i);
                materials[i].SetPass(pass);
            }

            return materials;
        }

        private static Material GetProjectedShadowMaterial(this Renderer2DData rendererData, int colorIndex)
        {
            //rendererData.projectedShadowMaterial = null;
            if (rendererData.projectedShadowMaterial == null || rendererData.projectedShadowMaterial.Length == 0 || rendererData.projectedShadowShader != rendererData.projectedShadowMaterial[0].shader)
            {
                rendererData.projectedShadowMaterial = CreateMaterials(rendererData.projectedShadowShader);
            }

            return rendererData.projectedShadowMaterial[colorIndex];
        }

        private static Material GetStencilOnlyShadowMaterial(this Renderer2DData rendererData, int colorIndex)
        {
            //rendererData.stencilOnlyShadowMaterial = null;
            if (rendererData.stencilOnlyShadowMaterial == null || rendererData.stencilOnlyShadowMaterial.Length == 0 || rendererData.projectedShadowShader != rendererData.stencilOnlyShadowMaterial[0].shader)
            {
                rendererData.stencilOnlyShadowMaterial = CreateMaterials(rendererData.projectedShadowShader, 1);
            }

            return rendererData.stencilOnlyShadowMaterial[colorIndex];
        }

        private static Material GetSpriteSelfShadowMaterial(this Renderer2DData rendererData, int colorIndex)
        {
            //rendererData.spriteSelfShadowMaterial = null;
            if (rendererData.spriteSelfShadowMaterial == null || rendererData.spriteSelfShadowMaterial.Length == 0 || rendererData.spriteShadowShader != rendererData.spriteSelfShadowMaterial[0].shader)
            {
                rendererData.spriteSelfShadowMaterial = CreateMaterials(rendererData.spriteShadowShader);
            }

            return rendererData.spriteSelfShadowMaterial[colorIndex];
        }

        private static Material GetSpriteUnshadowMaterial(this Renderer2DData rendererData, int colorIndex)
        {
            //rendererData.spriteUnshadowMaterial = null;
            if (rendererData.spriteUnshadowMaterial == null || rendererData.spriteUnshadowMaterial.Length == 0 || rendererData.spriteUnshadowShader != rendererData.spriteUnshadowMaterial[0].shader)
            {
                rendererData.spriteUnshadowMaterial = CreateMaterials(rendererData.spriteUnshadowShader);
            }

            return rendererData.spriteUnshadowMaterial[colorIndex];
        }

        private static Material GetGeometryUnshadowMaterial(this Renderer2DData rendererData, int colorIndex)
        {
            //rendererData.geometryUnshadowMaterial = null;
            if (rendererData.geometryUnshadowMaterial == null || rendererData.geometryUnshadowMaterial.Length == 0 || rendererData.geometryUnshadowShader != rendererData.geometryUnshadowMaterial[0].shader)
            {
                rendererData.geometryUnshadowMaterial = CreateMaterials(rendererData.geometryUnshadowShader);
            }

            return rendererData.geometryUnshadowMaterial[colorIndex];
        }

        public static void CreateShadowRenderTexture(IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmdBuffer, int shadowIndex)
        {
            CreateShadowRenderTexture(pass, m_RenderTargets[shadowIndex], renderingData, cmdBuffer);
        }

        public static void PrerenderShadows(IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmdBuffer, int layerToRender, Light2D light, int shadowIndex, float shadowIntensity)
        {
            var colorChannel = shadowIndex % 4;
            var textureIndex = shadowIndex / 4;

            if (colorChannel == 0)
                ShadowRendering.CreateShadowRenderTexture(pass, renderingData, cmdBuffer, textureIndex);

            //RenderShadows(pass, renderingData, cmdBuffer, layerToRender, light, shadowIntensity, m_RenderTargets[textureIndex].Identifier(), colorChannel);
            //m_LightInputTextures[textureIndex] = m_RenderTargets[textureIndex].Identifier();


            // Render the shadows for this light
            if (RenderShadows(pass, renderingData, cmdBuffer, layerToRender, light, shadowIntensity, m_RenderTargets[textureIndex].Identifier(), colorChannel))
                m_LightInputTextures[textureIndex] = m_RenderTargets[textureIndex].Identifier();
            else
                m_LightInputTextures[textureIndex] = Texture2D.blackTexture;
        }

        public static void SetGlobalShadowTexture(CommandBuffer cmdBuffer, Light2D light, int shadowIndex)
        {
            var colorChannel = shadowIndex % 4;
            var textureIndex = shadowIndex / 4;

            cmdBuffer.SetGlobalTexture("_ShadowTex", m_LightInputTextures[textureIndex]);
            cmdBuffer.SetGlobalColor(k_ShadowColorMaskID, k_ColorLookup[colorChannel]);
            cmdBuffer.SetGlobalFloat(k_ShadowIntensityID, 1 - light.shadowIntensity);
            cmdBuffer.SetGlobalFloat(k_ShadowVolumeIntensityID, 1 - light.shadowVolumeIntensity);
        }

        public static void DisableGlobalShadowTexture(CommandBuffer cmdBuffer)
        {
            cmdBuffer.SetGlobalFloat(k_ShadowIntensityID, 1);
            cmdBuffer.SetGlobalFloat(k_ShadowVolumeIntensityID, 1);
        }

        private static void CreateShadowRenderTexture(IRenderPass2D pass, RenderTargetHandle rtHandle, RenderingData renderingData, CommandBuffer cmdBuffer)
        {
            var renderTextureScale = Mathf.Clamp(pass.rendererData.lightRenderTextureScale, 0.01f, 1.0f);
            var width = (int)(renderingData.cameraData.cameraTargetDescriptor.width * renderTextureScale);
            var height = (int)(renderingData.cameraData.cameraTargetDescriptor.height * renderTextureScale);

            var descriptor = new RenderTextureDescriptor(width, height);
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            descriptor.depthBufferBits = 24;
            descriptor.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
            descriptor.msaaSamples = 1;
            descriptor.dimension = TextureDimension.Tex2D;

            cmdBuffer.GetTemporaryRT(rtHandle.id, descriptor, FilterMode.Bilinear);
        }

        public static void ReleaseShadowRenderTexture(CommandBuffer cmdBuffer, int shadowIndex)
        {
            var colorChannel = shadowIndex % 4;
            var textureIndex = shadowIndex / 4;

            if (colorChannel == 0)
                cmdBuffer.ReleaseTemporaryRT(m_RenderTargets[textureIndex].id);
        }

        public static void SetShadowProjectionGlobals(CommandBuffer cmdBuffer, ShadowCaster2D shadowCaster)
        {
            cmdBuffer.SetGlobalVector(k_ShadowModelScaleID, shadowCaster.m_CachedLossyScale);
            cmdBuffer.SetGlobalMatrix(k_ShadowModelMatrixID, shadowCaster.m_CachedShadowMatrix);
            cmdBuffer.SetGlobalMatrix(k_ShadowModelInvMatrixID, shadowCaster.m_CachedInverseShadowMatrix);
        }

        public static bool RenderShadows(IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmdBuffer, int layerToRender, Light2D light, float shadowIntensity, RenderTargetIdentifier renderTexture, int colorBit)
        {
            using (new ProfilingScope(cmdBuffer, m_ProfilingSamplerShadows))
            {
                bool hasShadow = false;
                var shadowCasterGroups = ShadowCasterGroup2DManager.shadowCasterGroups;
                if (shadowCasterGroups != null && shadowCasterGroups.Count > 0)
                {
                    // Before doing anything check to see if any of the shadow casters are visible to this light
                    for (var group = 0; group < shadowCasterGroups.Count; group++)
                    {
                        var shadowCasterGroup = shadowCasterGroups[group];
                        var shadowCasters = shadowCasterGroup.GetShadowCasters();

                        if (shadowCasters != null)
                        {
                            // Draw the projected shadows for the shadow caster group. Writing into the group stencil buffer bit
                            for (var i = 0; i < shadowCasters.Count; i++)
                            {
                                var shadowCaster = shadowCasters[i];
                                if (shadowCaster != null && shadowCaster.IsLit(light) && shadowCaster.IsShadowedLayer(layerToRender))
                                {
                                    hasShadow = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (hasShadow)
                    {
                        cmdBuffer.SetRenderTarget(renderTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);

                        using (new ProfilingScope(cmdBuffer, m_ProfilingSamplerShadowColorsLookup[colorBit]))
                        {
                            if (colorBit == 0)
                                cmdBuffer.ClearRenderTarget(true, true, Color.clear);
                            else
                                cmdBuffer.ClearRenderTarget(true, false, Color.clear);

                            var shadowRadius = light.boundingSphere.radius;

                            cmdBuffer.SetGlobalVector(k_LightPosID, light.transform.position);
                            cmdBuffer.SetGlobalFloat(k_ShadowRadiusID, shadowRadius);


                            cmdBuffer.SetGlobalColor(k_ShadowColorMaskID, k_ColorLookup[colorBit]);
                            var unshadowGeometryMaterial = pass.rendererData.GetGeometryUnshadowMaterial(colorBit);
                            var projectedShadowsMaterial = pass.rendererData.GetProjectedShadowMaterial(colorBit);
                            var selfShadowMaterial = pass.rendererData.GetSpriteSelfShadowMaterial(colorBit);
                            var unshadowMaterial = pass.rendererData.GetSpriteUnshadowMaterial(colorBit);
                            var setGlobalStencilMaterial = pass.rendererData.GetStencilOnlyShadowMaterial(colorBit);

                            for (var group = 0; group < shadowCasterGroups.Count; group++)
                            {
                                var shadowCasterGroup = shadowCasterGroups[group];
                                var shadowCasters = shadowCasterGroup.GetShadowCasters();

                                if (shadowCasters != null)
                                {
                                    for (var i = 0; i < shadowCasters.Count; i++)
                                    {
                                        var shadowCaster = shadowCasters[i];

                                        if (shadowCaster.IsLit(light))
                                        {
                                            if (shadowCaster != null && projectedShadowsMaterial != null && shadowCaster.IsShadowedLayer(layerToRender))
                                            {
                                                if (shadowCaster.castsShadows)
                                                {
                                                    SetShadowProjectionGlobals(cmdBuffer, shadowCaster);

                                                    cmdBuffer.DrawMesh(shadowCaster.mesh, shadowCaster.m_CachedLocalToWorldMatrix, unshadowGeometryMaterial, 0, 0);
                                                    cmdBuffer.DrawMesh(shadowCaster.mesh, shadowCaster.m_CachedLocalToWorldMatrix, projectedShadowsMaterial, 0, 0);
                                                    cmdBuffer.DrawMesh(shadowCaster.mesh, shadowCaster.m_CachedLocalToWorldMatrix, unshadowGeometryMaterial, 0, 1);
                                                }
                                            }
                                        }
                                    }

                                    // Draw the sprites, either as self shadowing or unshadowing
                                    for (var i = 0; i < shadowCasters.Count; i++)
                                    {
                                        var shadowCaster = shadowCasters[i];

                                        if (shadowCaster.IsLit(light))
                                        {
                                            if (shadowCaster != null && shadowCaster.IsShadowedLayer(layerToRender))
                                            {
                                                if (shadowCaster.useRendererSilhouette)
                                                {
                                                    // Draw using the sprite renderer
                                                    var renderer = (Renderer)null;
                                                    shadowCaster.TryGetComponent<Renderer>(out renderer);

                                                    if (renderer != null)
                                                    {
                                                        var material = shadowCaster.selfShadows ? selfShadowMaterial : unshadowMaterial;
                                                        if (material != null)
                                                            cmdBuffer.DrawRenderer(renderer, material);
                                                    }
                                                }
                                                else
                                                {
                                                    var meshMat = shadowCaster.m_CachedLocalToWorldMatrix;
                                                    var material = shadowCaster.selfShadows ? selfShadowMaterial : unshadowMaterial;

                                                    // Draw using the shadow mesh
                                                    if (material != null)
                                                        cmdBuffer.DrawMesh(shadowCaster.mesh, meshMat, material);
                                                }
                                            }
                                        }
                                    }

                                    // Draw the projected shadows for the shadow caster group. Writing clearing the group stencil bit, and setting the global bit
                                    for (var i = 0; i < shadowCasters.Count; i++)
                                    {
                                        var shadowCaster = shadowCasters[i];

                                        if (shadowCaster.IsLit(light))
                                        {
                                            if (shadowCaster != null && projectedShadowsMaterial != null && shadowCaster.IsShadowedLayer(layerToRender))
                                            {
                                                if (shadowCaster.castsShadows)
                                                {
                                                    SetShadowProjectionGlobals(cmdBuffer, shadowCaster);
                                                    cmdBuffer.DrawMesh(shadowCaster.mesh, shadowCaster.m_CachedLocalToWorldMatrix, projectedShadowsMaterial, 0, 1);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return hasShadow;
            }
        }
    }
}
