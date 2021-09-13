using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Mathematics;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal static class ShadowRendering
    {
        private static readonly int k_LightPosID = Shader.PropertyToID("_LightPos");
        private static readonly int k_ShadowStencilGroupID = Shader.PropertyToID("_ShadowStencilGroup");
        private static readonly int k_ShadowIntensityID = Shader.PropertyToID("_ShadowIntensity");
        private static readonly int k_ShadowVolumeIntensityID = Shader.PropertyToID("_ShadowVolumeIntensity");
        private static readonly int k_ShadowRadiusID = Shader.PropertyToID("_ShadowRadius");

        private static RenderTargetHandle[] m_RenderTargets = null;
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
        }

        public static void CreateShadowRenderTexture(IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmdBuffer, int shadowIndex)
        {
            CreateShadowRenderTexture(pass, m_RenderTargets[shadowIndex], renderingData, cmdBuffer);
        }

        public static void PrerenderShadows(IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmdBuffer, int layerToRender, Light2D light, int shadowIndex, float shadowIntensity)
        {
            // Render the shadows for this light
            RenderShadows(pass, renderingData, cmdBuffer, layerToRender, light, shadowIntensity, m_RenderTargets[shadowIndex].Identifier());
        }

        public static void SetGlobalShadowTexture(CommandBuffer cmdBuffer, Light2D light, int shadowIndex)
        {
            cmdBuffer.SetGlobalTexture("_ShadowTex", m_RenderTargets[shadowIndex].Identifier());
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
            cmdBuffer.ReleaseTemporaryRT(m_RenderTargets[shadowIndex].id);
        }

        private static Material GetShadowMaterial(this Renderer2DData rendererData, int index)
        {
            var shadowMaterialIndex = index % 255;
            if (rendererData.shadowMaterials[shadowMaterialIndex] == null)
            {
                rendererData.shadowMaterials[shadowMaterialIndex] = CoreUtils.CreateEngineMaterial(rendererData.shadowGroupShader);
                rendererData.shadowMaterials[shadowMaterialIndex].SetFloat(k_ShadowStencilGroupID, index);
            }

            return rendererData.shadowMaterials[shadowMaterialIndex];
        }

        private static Material GetRemoveSelfShadowMaterial(this Renderer2DData rendererData, int index)
        {
            var shadowMaterialIndex = index % 255;
            if (rendererData.removeSelfShadowMaterials[shadowMaterialIndex] == null)
            {
                rendererData.removeSelfShadowMaterials[shadowMaterialIndex] = CoreUtils.CreateEngineMaterial(rendererData.removeSelfShadowShader);
                rendererData.removeSelfShadowMaterials[shadowMaterialIndex].SetFloat(k_ShadowStencilGroupID, index);
            }

            return rendererData.removeSelfShadowMaterials[shadowMaterialIndex];
        }

        public static void RenderShadows(IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmdBuffer, int layerToRender, Light2D light, float shadowIntensity, RenderTargetIdentifier renderTexture)
        {
            cmdBuffer.SetRenderTarget(renderTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
            cmdBuffer.ClearRenderTarget(true, true, Color.black);  // clear stencil

            var shadowRadius = 1.42f * light.boundingSphere.radius;

            cmdBuffer.SetGlobalVector(k_LightPosID, light.transform.position);
            cmdBuffer.SetGlobalFloat(k_ShadowRadiusID, shadowRadius);

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
                        // Draw the shadow casting group first, then draw the silhouettes..
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
        }
    }
}
