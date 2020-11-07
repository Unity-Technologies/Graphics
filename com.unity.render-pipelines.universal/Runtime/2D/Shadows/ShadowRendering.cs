using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal static class ShadowRendering
    {
        private static readonly int k_LightPosID = Shader.PropertyToID("_LightPos");
        private static readonly int k_SelfShadowingID = Shader.PropertyToID("_SelfShadowing");
        private static readonly int k_ShadowStencilGroupID = Shader.PropertyToID("_ShadowStencilGroup");
        private static readonly int k_ShadowIntensityID = Shader.PropertyToID("_ShadowIntensity");
        private static readonly int k_ShadowVolumeIntensityID = Shader.PropertyToID("_ShadowVolumeIntensity");
        private static readonly int k_ShadowRadiusID = Shader.PropertyToID("_ShadowRadius");
        private static readonly int k_ShadowColorMaskID = Shader.PropertyToID("_ShadowColorMask");


        private static Material GetProjectedShadowMaterial(this Renderer2DData rendererData, int colorMask)
        {
            //if (rendererData.projectedShadowMaterial == null)
            {
                var material = CoreUtils.CreateEngineMaterial(rendererData.projectedShadowShader);
                material.SetInt(k_ShadowColorMaskID, colorMask);
                rendererData.projectedShadowMaterial = material;
            }

            return rendererData.projectedShadowMaterial;
        }

        private static Material GetStencilOnlyShadowMaterial(this Renderer2DData rendererData, int colorMask)
        {
            //if (rendererData.stencilOnlyShadowMaterial == null)
            {
                var material = CoreUtils.CreateEngineMaterial(rendererData.projectedShadowShader);
                material.SetInt(k_ShadowColorMaskID, colorMask);
                rendererData.stencilOnlyShadowMaterial = material;
            }

            return rendererData.stencilOnlyShadowMaterial;
        }

        private static Material GetSpriteSelfShadowMaterial(this Renderer2DData rendererData, int colorMask)
        {
            //if (rendererData.spriteSelfShadowMaterial == null)
            {
                Material material = CoreUtils.CreateEngineMaterial(rendererData.spriteShadowShader);
                material.SetInt(k_ShadowColorMaskID, colorMask);
                rendererData.spriteSelfShadowMaterial = material;
            }
                
            return rendererData.spriteSelfShadowMaterial;
        }

        private static Material GetSpriteUnshadowMaterial(this Renderer2DData rendererData, int colorMask)
        {
            //if (rendererData.spriteUnshadowMaterial == null)
            {
                Material material = CoreUtils.CreateEngineMaterial(rendererData.spriteUnshadowShader);
                material.SetInt(k_ShadowColorMaskID, colorMask);
                rendererData.spriteUnshadowMaterial = material;
            }

            return rendererData.spriteUnshadowMaterial;
        }


        private static void CreateShadowRenderTexture(IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmd)
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

            cmd.GetTemporaryRT(pass.rendererData.shadowsRenderTarget.id, descriptor, FilterMode.Bilinear);
        }

        public static void RenderShadows(IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmdBuffer, int layerToRender, Light2D light, float shadowIntensity, RenderTargetIdentifier renderTexture, RenderTargetIdentifier depthTexture)
        {
            cmdBuffer.SetGlobalFloat(k_ShadowIntensityID, 1 - light.shadowIntensity);
            cmdBuffer.SetGlobalFloat(k_ShadowVolumeIntensityID, 1 - light.shadowVolumeIntensity);

            if (shadowIntensity > 0)
            {
                CreateShadowRenderTexture(pass, renderingData, cmdBuffer);

                cmdBuffer.SetRenderTarget(pass.rendererData.shadowsRenderTarget.Identifier(), RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
                cmdBuffer.ClearRenderTarget(true, true, Color.black);

                var shadowRadius = 1.42f * light.boundingSphere.radius;

                cmdBuffer.SetGlobalVector(k_LightPosID, light.transform.position);
                cmdBuffer.SetGlobalFloat(k_ShadowRadiusID, shadowRadius);
                cmdBuffer.SetGlobalColor(k_ShadowColorMaskID, new Color(0,0,1,0));

                int colorMask = 2;
                var spriteSelfShadowMaterial = pass.rendererData.GetSpriteSelfShadowMaterial(colorMask);
                var spriteUnshadowMaterial = pass.rendererData.GetSpriteUnshadowMaterial(colorMask);
                var projectedShadowMaterial = pass.rendererData.GetProjectedShadowMaterial(colorMask);
                var stencilOnlyShadowMaterial = pass.rendererData.GetStencilOnlyShadowMaterial(colorMask);

                var shadowCasterGroups = ShadowCasterGroup2DManager.shadowCasterGroups;
                if (shadowCasterGroups != null && shadowCasterGroups.Count > 0)
                {
                    for (var group = 0; group < shadowCasterGroups.Count; group++)
                    {
                        var shadowCasterGroup = shadowCasterGroups[group];
                        var shadowCasters = shadowCasterGroup.GetShadowCasters();

                        if (shadowCasters != null)
                        {
                            // Draw the projected shadows and write a composite shadow bit into the stencil buffer
                            for(int shadowCasterIndex=0; shadowCasterIndex < shadowCasters.Count; shadowCasterIndex++)
                            {
                                var shadowCaster = shadowCasters[shadowCasterIndex];
                                if(shadowCaster.castsShadows)
                                {
                                    // If we cast shadows draw the projected shadows
                                    cmdBuffer.DrawMesh(shadowCaster.mesh, shadowCaster.transform.localToWorldMatrix, projectedShadowMaterial, 0, 0);
                                }
                            }

                            for (int shadowCasterIndex = 0; shadowCasterIndex < shadowCasters.Count; shadowCasterIndex++)
                            {
                                var shadowCaster = shadowCasters[shadowCasterIndex];
                                if (shadowCaster.useRendererSilhouette)
                                {
                                    // Draw the sprites
                                    var renderer = shadowCaster.GetComponent<Renderer>();
                                    if (renderer != null)
                                    {
                                        if (shadowCaster.selfShadows)
                                            cmdBuffer.DrawRenderer(renderer, spriteSelfShadowMaterial);
                                        else
                                            cmdBuffer.DrawRenderer(renderer, spriteUnshadowMaterial);
                                        
                                    }
                                }
                                else
                                {
                                    // Draw the caster shape
                                    if (shadowCaster.selfShadows)
                                        cmdBuffer.DrawMesh(shadowCaster.mesh, shadowCaster.transform.localToWorldMatrix, spriteSelfShadowMaterial);
                                    else
                                        cmdBuffer.DrawMesh(shadowCaster.mesh, shadowCaster.transform.localToWorldMatrix, spriteUnshadowMaterial);
                                }
                            }

                            // Update the stencil buffer. If a composite shadow value was written convert it into global shadow bit
                            for (int shadowCasterIndex = 0; shadowCasterIndex < shadowCasters.Count; shadowCasterIndex++)
                            {
                                var shadowCaster = shadowCasters[shadowCasterIndex];
                                if (shadowCaster.castsShadows)
                                {
                                    // If we cast shadows draw update the stencil buffer. Use stencil only pass
                                    cmdBuffer.DrawMesh(shadowCaster.mesh, shadowCaster.transform.localToWorldMatrix, stencilOnlyShadowMaterial, 0, 1);
                                }
                            }
                        }
                    }
                }

                cmdBuffer.ReleaseTemporaryRT(pass.rendererData.shadowsRenderTarget.id);
                cmdBuffer.SetRenderTarget(renderTexture, depthTexture);
            }
        }
    }
}
