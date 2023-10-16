using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using Unity.Collections;

#if USING_SPRITESHAPE
using UnityEngine.U2D;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.Universal
{
    // TODO: Culling of shadow casters, rotate color channels for shadow casting, check get material functions.


    internal static class ShadowRendering
    {
        private static readonly int k_LightPosID = Shader.PropertyToID("_LightPos");
        private static readonly int k_ShadowRadiusID = Shader.PropertyToID("_ShadowRadius");
        private static readonly int k_ShadowColorMaskID = Shader.PropertyToID("_ShadowColorMask");
        private static readonly int k_ShadowModelMatrixID = Shader.PropertyToID("_ShadowModelMatrix");
        private static readonly int k_ShadowModelInvMatrixID = Shader.PropertyToID("_ShadowModelInvMatrix");
        private static readonly int k_ShadowModelScaleID = Shader.PropertyToID("_ShadowModelScale");
        private static readonly int k_ShadowContractionDistanceID = Shader.PropertyToID("_ShadowContractionDistance");
        private static readonly int k_ShadowAlphaCutoffID = Shader.PropertyToID("_ShadowAlphaCutoff");
        private static readonly int k_SoftShadowAngle = Shader.PropertyToID("_SoftShadowAngle");
        private static readonly int k_ShadowSoftnessFalloffIntensityID = Shader.PropertyToID("_ShadowSoftnessFalloffIntensity");
        private static readonly int k_ShadowShadowColorID = Shader.PropertyToID("_ShadowColor");
        private static readonly int k_ShadowUnshadowColorID = Shader.PropertyToID("_UnshadowColor");

        private static readonly ProfilingSampler m_ProfilingSamplerShadows = new ProfilingSampler("Draw 2D Shadow Texture");
        private static readonly ProfilingSampler m_ProfilingSamplerShadowsA = new ProfilingSampler("Draw 2D Shadows (A)");
        private static readonly ProfilingSampler m_ProfilingSamplerShadowsR = new ProfilingSampler("Draw 2D Shadows (R)");
        private static readonly ProfilingSampler m_ProfilingSamplerShadowsG = new ProfilingSampler("Draw 2D Shadows (G)");
        private static readonly ProfilingSampler m_ProfilingSamplerShadowsB = new ProfilingSampler("Draw 2D Shadows (B)");

        private static readonly float k_MaxShadowSoftnessAngle = 15;
        private static readonly Color k_ShadowColorLookup = new Color(0, 0, 1, 0);
        private static readonly Color k_UnshadowColorLookup = new Color(0, 1, 0, 0);

        private static RTHandle[] m_RenderTargets = null;
        private static int[] m_RenderTargetIds = null;
        private static RenderTargetIdentifier[] m_LightInputTextures = null;
        private static readonly ProfilingSampler[] m_ProfilingSamplerShadowColorsLookup = new ProfilingSampler[4] { m_ProfilingSamplerShadowsA, m_ProfilingSamplerShadowsB, m_ProfilingSamplerShadowsG, m_ProfilingSamplerShadowsR };

        public static uint maxTextureCount { get; private set; }
        public static RenderTargetIdentifier[] lightInputTextures { get { return m_LightInputTextures; } }
        public static void InitializeBudget(uint maxTextureCount)
        {
            if (m_RenderTargets == null || m_RenderTargets.Length != maxTextureCount)
            {
                m_RenderTargets = new RTHandle[maxTextureCount];
                m_RenderTargetIds = new int[maxTextureCount];
                ShadowRendering.maxTextureCount = maxTextureCount;

                for (int i = 0; i < maxTextureCount; i++)
                {
                    m_RenderTargetIds[i] = Shader.PropertyToID($"ShadowTex_{i}");
                    m_RenderTargets[i] = RTHandles.Alloc(m_RenderTargetIds[i], $"ShadowTex_{i}");
                }
            }

            if (m_LightInputTextures == null || m_LightInputTextures.Length != maxTextureCount)
            {
                m_LightInputTextures = new RenderTargetIdentifier[maxTextureCount];
            }
        }

        private static Material CreateMaterial(Shader shader, int offset, int pass)
        {
            Material material;  // pairs of color channels
            material = CoreUtils.CreateEngineMaterial(shader);
            material.SetInt(k_ShadowColorMaskID, 1 << (offset + 1));
            material.SetPass(pass);

            return material;
        }

        private static Material GetProjectedShadowMaterial(this Renderer2DData rendererData)
        {
            //rendererData.projectedShadowMaterial = null;
            if (rendererData.projectedShadowMaterial == null || rendererData.projectedShadowShader != rendererData.projectedShadowMaterial.shader)
            {
                rendererData.projectedShadowMaterial = CreateMaterial(rendererData.projectedShadowShader, 0, 0);
            }

            return rendererData.projectedShadowMaterial;
        }

        private static Material GetProjectedUnshadowMaterial(this Renderer2DData rendererData)
        {
            //rendererData.projectedShadowMaterial = null;
            if (rendererData.projectedUnshadowMaterial == null  || rendererData.projectedShadowShader != rendererData.projectedUnshadowMaterial.shader)
            {
                rendererData.projectedUnshadowMaterial = CreateMaterial(rendererData.projectedShadowShader, 1, 1);
            }

            return rendererData.projectedUnshadowMaterial;
        }

        private static Material GetSpriteShadowMaterial(this Renderer2DData rendererData)
        {
            //rendererData.spriteSelfShadowMaterial = null;
            if (rendererData.spriteSelfShadowMaterial == null || rendererData.spriteShadowShader != rendererData.spriteSelfShadowMaterial.shader)
            {
                rendererData.spriteSelfShadowMaterial = CreateMaterial(rendererData.spriteShadowShader, 0, 0);
            }

            return rendererData.spriteSelfShadowMaterial;
        }

        private static Material GetSpriteUnshadowMaterial(this Renderer2DData rendererData)
        {
            //rendererData.spriteUnshadowMaterial = null;
            if (rendererData.spriteUnshadowMaterial == null ||  rendererData.spriteUnshadowShader != rendererData.spriteUnshadowMaterial.shader)
            {
                rendererData.spriteUnshadowMaterial = CreateMaterial(rendererData.spriteUnshadowShader, 1, 0);
            }

            return rendererData.spriteUnshadowMaterial;
        }

        private static Material GetGeometryShadowMaterial(this Renderer2DData rendererData)
        {
            //rendererData.spriteSelfShadowMaterial = null;
            if (rendererData.geometrySelfShadowMaterial == null || rendererData.geometryShadowShader != rendererData.geometrySelfShadowMaterial.shader)
            {
                rendererData.geometrySelfShadowMaterial = CreateMaterial(rendererData.geometryShadowShader, 0, 0);
            }

            return rendererData.geometrySelfShadowMaterial;
        }

        private static Material GetGeometryUnshadowMaterial(this Renderer2DData rendererData)
        {
            //rendererData.spriteUnshadowMaterial = null;
            if (rendererData.geometryUnshadowMaterial == null || rendererData.geometryUnshadowShader != rendererData.geometryUnshadowMaterial.shader)
            {
                rendererData.geometryUnshadowMaterial = CreateMaterial(rendererData.geometryUnshadowShader, 1, 0);
            }

            return rendererData.geometryUnshadowMaterial;
        }

        private static void CalculateFrustumCornersPerspective(Camera camera, float distance, NativeArray<Vector3> corners)
        {
            float verticalFieldOfView = camera.fieldOfView;  // This will need to be converted if user direction is allowed

            float halfHeight = Mathf.Tan(0.5f * verticalFieldOfView * Mathf.Deg2Rad) * distance;
            float halfWidth = halfHeight * camera.aspect;

            corners[0] = new Vector3(halfWidth, halfHeight, distance);
            corners[1] = new Vector3(halfWidth, -halfHeight, distance);
            corners[2] = new Vector3(-halfWidth, halfHeight, distance);
            corners[3] = new Vector3(-halfWidth, -halfHeight, distance);
        }

        private static void CalculateFrustumCornersOrthographic(Camera camera, float distance, NativeArray<Vector3> corners)
        {
            float halfHeight = camera.orthographicSize;
            float halfWidth = halfHeight * camera.aspect;

            corners[0] = new Vector3(halfWidth, halfHeight, distance);
            corners[1] = new Vector3(halfWidth, -halfHeight, distance);
            corners[2] = new Vector3(-halfWidth, halfHeight, distance);
            corners[3] = new Vector3(-halfWidth, -halfHeight, distance);
        }

        private static Bounds CalculateWorldSpaceBounds(Camera camera, ILight2DCullResult cullResult)
        {
            // TODO: This will need to take into account on screen lights as shadows can be cast from offscreen.

            const int k_Corners = 4;
            NativeArray<Vector3> nearCorners = new NativeArray<Vector3>(k_Corners, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            NativeArray<Vector3> farCorners = new NativeArray<Vector3>(k_Corners, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            if (camera.orthographic)
            {
                CalculateFrustumCornersOrthographic(camera, camera.nearClipPlane, nearCorners);
                CalculateFrustumCornersOrthographic(camera, camera.farClipPlane, farCorners);
            }
            else
            {
                CalculateFrustumCornersPerspective(camera, camera.nearClipPlane, nearCorners);
                CalculateFrustumCornersPerspective(camera, camera.farClipPlane, farCorners);
            }

            Vector3 minCorner = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 maxCorner = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            for (int i = 0; i < k_Corners; i++)
            {
                maxCorner = Vector3.Max(maxCorner, nearCorners[i]);
                maxCorner = Vector3.Max(maxCorner, farCorners[i]);
                minCorner = Vector3.Min(minCorner, nearCorners[i]);
                minCorner = Vector3.Min(minCorner, farCorners[i]);
            }

            nearCorners.Dispose();
            farCorners.Dispose();

            // Transform the point from camera space to world space
            maxCorner = camera.transform.TransformPoint(maxCorner);
            minCorner = camera.transform.TransformPoint(minCorner);

            // TODO: Iterate through the lights
            for (int i = 0; i < cullResult.visibleLights.Count; i++)
            {
                Vector3 lightPos = cullResult.visibleLights[i].transform.position;
                maxCorner = Vector3.Max(maxCorner, lightPos);
                minCorner = Vector3.Min(minCorner, lightPos);
            }

            Vector3 center = 0.5f * (minCorner + maxCorner);
            Vector3 size = maxCorner - minCorner;

            return new Bounds(center, size); ;
        }

        public static void CallOnBeforeRender(Camera camera, ILight2DCullResult cullResult)
        {
            if (ShadowCasterGroup2DManager.shadowCasterGroups != null)
            {
                Bounds bounds = CalculateWorldSpaceBounds(camera, cullResult);

                List<ShadowCasterGroup2D> groups = ShadowCasterGroup2DManager.shadowCasterGroups;
                for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
                {
                    ShadowCasterGroup2D group = groups[groupIndex];

                    List<ShadowCaster2D> shadowCasters = group.GetShadowCasters();
                    if (shadowCasters != null)
                    {
                        for (int shadowCasterIndex = 0; shadowCasterIndex < shadowCasters.Count; shadowCasterIndex++)
                        {
                            ShadowCaster2D shadowCaster = shadowCasters[shadowCasterIndex];
                            if (shadowCaster != null && shadowCaster.shadowCastingSource == ShadowCaster2D.ShadowCastingSources.ShapeProvider)
                            {
                                ShapeProviderUtility.CallOnBeforeRender(shadowCaster.shadowShape2DProvider, shadowCaster.shadowShape2DComponent, shadowCaster.m_ShadowMesh, bounds);
                            }
                        }
                    }
                }
            }
        }

        public static void CreateShadowRenderTexture(IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmdBuffer, int shadowIndex)
        {
            CreateShadowRenderTexture(pass, m_RenderTargetIds[shadowIndex], renderingData, cmdBuffer);
        }

        public static void PrerenderShadows(RasterCommandBuffer cmdBuffer, Renderer2DData rendererData, ref LayerBatch layer, Light2D light, int shadowIndex, float shadowIntensity)
        {
            RenderShadows(cmdBuffer, rendererData, ref layer, light);
        }

        public static bool PrerenderShadows(this IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmdBuffer, ref LayerBatch layer, Light2D light, int shadowIndex, float shadowIntensity)
        {
            ShadowRendering.CreateShadowRenderTexture(pass, renderingData, cmdBuffer, shadowIndex);

            bool hadShadowsToRender = layer.shadowCasters.Count != 0;

            if (hadShadowsToRender)
            {
                cmdBuffer.SetRenderTarget(m_RenderTargets[shadowIndex].nameID, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
                cmdBuffer.ClearRenderTarget(RTClearFlags.All, Color.clear, 1, 0);
                RenderShadows(CommandBufferHelpers.GetRasterCommandBuffer(cmdBuffer), pass.rendererData, ref layer, light);
            }

            m_LightInputTextures[shadowIndex] = m_RenderTargets[shadowIndex].nameID;

            return hadShadowsToRender;
        }

        private static void CreateShadowRenderTexture(IRenderPass2D pass, int handleId, RenderingData renderingData, CommandBuffer cmdBuffer)
        {
            var renderTextureScale = Mathf.Clamp(pass.rendererData.lightRenderTextureScale, 0.01f, 1.0f);
            var width = (int)(renderingData.cameraData.cameraTargetDescriptor.width * renderTextureScale);
            var height = (int)(renderingData.cameraData.cameraTargetDescriptor.height * renderTextureScale);

            var descriptor = new RenderTextureDescriptor(width, height);
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            descriptor.depthBufferBits = 24;
            descriptor.graphicsFormat = GraphicsFormat.B10G11R11_UFloatPack32;
            descriptor.msaaSamples = 1;
            descriptor.dimension = TextureDimension.Tex2D;

            cmdBuffer.GetTemporaryRT(handleId, descriptor, FilterMode.Bilinear);
        }

        public static void ReleaseShadowRenderTexture(CommandBuffer cmdBuffer, int shadowIndex)
        {
            cmdBuffer.ReleaseTemporaryRT(m_RenderTargetIds[shadowIndex]);
        }

        public static void SetShadowProjectionGlobals(RasterCommandBuffer cmdBuffer, ShadowCaster2D shadowCaster, Light2D light)
        {
            cmdBuffer.SetGlobalVector(k_ShadowModelScaleID, shadowCaster.m_CachedLossyScale);
            cmdBuffer.SetGlobalMatrix(k_ShadowModelMatrixID, shadowCaster.m_CachedShadowMatrix);
            cmdBuffer.SetGlobalMatrix(k_ShadowModelInvMatrixID, shadowCaster.m_CachedInverseShadowMatrix);
            cmdBuffer.SetGlobalFloat(k_ShadowSoftnessFalloffIntensityID, light.shadowSoftnessFalloffIntensity);

            if (shadowCaster.edgeProcessing == ShadowCaster2D.EdgeProcessing.None)
                cmdBuffer.SetGlobalFloat(k_ShadowContractionDistanceID, shadowCaster.trimEdge);
            else
                cmdBuffer.SetGlobalFloat(k_ShadowContractionDistanceID, 0f);
        }

        public static void SetGlobalShadowTexture(CommandBuffer cmdBuffer, Light2D light, int shadowIndex)
        {
            var textureIndex = shadowIndex;

            cmdBuffer.SetGlobalTexture("_ShadowTex", m_LightInputTextures[textureIndex]);
            cmdBuffer.SetGlobalColor(k_ShadowShadowColorID, k_ShadowColorLookup);
            cmdBuffer.SetGlobalColor(k_ShadowUnshadowColorID, k_UnshadowColorLookup);
        }

        public static void SetGlobalShadowProp(RasterCommandBuffer cmdBuffer)
        {
            cmdBuffer.SetGlobalColor(k_ShadowShadowColorID, k_ShadowColorLookup);
            cmdBuffer.SetGlobalColor(k_ShadowUnshadowColorID, k_UnshadowColorLookup);
        }

        static bool ShadowCasterIsVisible(ShadowCaster2D shadowCaster)
        {
            #if UNITY_EDITOR
                return SceneVisibilityManager.instance == null ? true : !SceneVisibilityManager.instance.IsHidden(shadowCaster.gameObject);
            #else
                return true;
            #endif
        }

        static Renderer GetRendererFromCaster(ShadowCaster2D shadowCaster, Light2D light, int layerToRender)
        {
            Renderer renderer = null;

            if (shadowCaster.IsLit(light))
            {
                if (shadowCaster != null && shadowCaster.IsShadowedLayer(layerToRender))
                {
                    shadowCaster.TryGetComponent<Renderer>(out renderer);
                }
            }

            return renderer;
        }

        public static void RenderProjectedShadows(RasterCommandBuffer cmdBuffer, int layerToRender, Light2D light, List<ShadowCaster2D> shadowCasters, Material projectedShadowsMaterial, int pass)
        {
            // Draw the projected shadows for the shadow caster group. Writing into the group stencil buffer bit
            for (var i = 0; i < shadowCasters.Count; i++)
            {
                var shadowCaster = shadowCasters[i];

                if (ShadowCasterIsVisible(shadowCaster) && shadowCaster.castsShadows  && shadowCaster.IsLit(light))
                {
                    if (shadowCaster != null && projectedShadowsMaterial != null && shadowCaster.IsShadowedLayer(layerToRender))
                    {
                        if (shadowCaster.shadowCastingSource != ShadowCaster2D.ShadowCastingSources.None && shadowCaster.mesh != null)
                        {
                            SetShadowProjectionGlobals(cmdBuffer, shadowCaster, light);
                            cmdBuffer.DrawMesh(shadowCaster.mesh, shadowCaster.transform.localToWorldMatrix, projectedShadowsMaterial, 0, pass);
                        }
                    }
                }
            }
        }

        static int GetRendererSubmeshes(Renderer renderer, ShadowCaster2D shadowCaster2D)
        {
            int numberOfSubmeshes;

            #if USING_SPRITESHAPE
                if (renderer is SpriteShapeRenderer)
                {
                    SpriteShapeRenderer spriteShapeRenderer = (SpriteShapeRenderer)renderer;
                    numberOfSubmeshes = spriteShapeRenderer.GetSplineMeshCount();
                }
                else
                {
                    numberOfSubmeshes = shadowCaster2D.spriteMaterialCount;
                }
            #else
                numberOfSubmeshes = shadowCaster2D.spriteMaterialCount;
            #endif

            return numberOfSubmeshes;
        }

        public static void RenderSelfShadowOption(RasterCommandBuffer cmdBuffer, int layerToRender, Light2D light, List<ShadowCaster2D> shadowCasters, Material projectedUnshadowMaterial, Material spriteShadowMaterial, Material spriteUnshadowMaterial, Material geometryShadowMaterial, Material geometryUnshadowMaterial)
        {
            // Draw the sprites, either as self shadowing or unshadowing
            for (var i = 0; i < shadowCasters.Count; i++)
            {
                ShadowCaster2D shadowCaster = shadowCasters[i];
                Renderer renderer = GetRendererFromCaster(shadowCaster, light, layerToRender);

                cmdBuffer.SetGlobalFloat(k_ShadowAlphaCutoffID, shadowCaster.alphaCutoff);

                if (renderer != null)
                {
                    if (ShadowCasterIsVisible(shadowCaster) && shadowCaster.selfShadows)
                    {
                        int numberOfSubmeshes = GetRendererSubmeshes(renderer, shadowCaster);
                        for (int submeshIndex = 0; submeshIndex < numberOfSubmeshes; submeshIndex++)
                            cmdBuffer.DrawRenderer(renderer, spriteShadowMaterial, submeshIndex, 0);
                    }
                    else
                    {
                        int numberOfSubmeshes = GetRendererSubmeshes(renderer, shadowCaster);
                        for (int submeshIndex = 0; submeshIndex < numberOfSubmeshes; submeshIndex++)
                        {
                            cmdBuffer.DrawRenderer(renderer, spriteUnshadowMaterial, submeshIndex, 0);

                        }
                    }   
                }
                else
                {
                    if (shadowCaster.mesh != null)
                    {
                        if (ShadowCasterIsVisible(shadowCaster) && shadowCaster.selfShadows)
                            cmdBuffer.DrawMesh(shadowCaster.mesh, shadowCaster.transform.localToWorldMatrix, geometryShadowMaterial, 0, 0);
                        else
                            cmdBuffer.DrawMesh(shadowCaster.mesh, shadowCaster.transform.localToWorldMatrix, geometryUnshadowMaterial, 0, 0);
                    }
                }
            }

            // Draw a masked projected shadow that is inside the sprite to remove the shadow (on different channel)
            for (var i = 0; i < shadowCasters.Count; i++)
            {
                ShadowCaster2D shadowCaster = shadowCasters[i];
                if (ShadowCasterIsVisible(shadowCaster) && shadowCaster.castingOption == ShadowCaster2D.ShadowCastingOptions.CastShadow && shadowCaster.mesh != null)
                {
                    Renderer renderer = GetRendererFromCaster(shadowCaster, light, layerToRender);
                    SetShadowProjectionGlobals(cmdBuffer, shadowCaster, light);
                    cmdBuffer.DrawMesh(shadowCaster.mesh, shadowCaster.transform.localToWorldMatrix, projectedUnshadowMaterial, 0, 1);
                }
            }

            // Fix up shadow removal with transparency
            for (var i = 0; i < shadowCasters.Count; i++)
            {
                ShadowCaster2D shadowCaster = shadowCasters[i];
                if (ShadowCasterIsVisible(shadowCaster) && !shadowCaster.selfShadows)
                {
                    Renderer renderer = GetRendererFromCaster(shadowCaster, light, layerToRender);
                    if (renderer != null)
                    {
                        int numberOfSubmeshes = GetRendererSubmeshes(renderer, shadowCaster);
                        for (int submeshIndex = 0; submeshIndex < numberOfSubmeshes; submeshIndex++)
                        {
                            cmdBuffer.DrawRenderer(renderer, spriteUnshadowMaterial, submeshIndex, 1);
                        }
                    }
                    else
                    {
                        if(shadowCaster.mesh != null)
                            cmdBuffer.DrawMesh(shadowCaster.mesh, shadowCaster.transform.localToWorldMatrix, geometryUnshadowMaterial, 0, 1);
                    }
                }
            }
        }

        public static void RenderShadows(RasterCommandBuffer cmdBuffer, Renderer2DData rendererData, ref LayerBatch layer, Light2D light)
        {
            using (new ProfilingScope(cmdBuffer, m_ProfilingSamplerShadows))
            {
                var shadowRadius = light.boundingSphere.radius + (light.transform.position - light.boundingSphere.position).magnitude;

                cmdBuffer.SetGlobalVector(k_LightPosID, light.transform.position);
                cmdBuffer.SetGlobalFloat(k_ShadowRadiusID, shadowRadius);
                cmdBuffer.SetGlobalFloat(k_SoftShadowAngle, Mathf.Deg2Rad * light.shadowSoftness * k_MaxShadowSoftnessAngle);

                var projectedShadowMaterial = rendererData.GetProjectedShadowMaterial();
                var projectedUnshadowMaterial = rendererData.GetProjectedUnshadowMaterial();
                var spriteShadowMaterial = rendererData.GetSpriteShadowMaterial();
                var spriteUnshadowMaterial = rendererData.GetSpriteUnshadowMaterial();
                var geometryShadowMaterial = rendererData.GetGeometryShadowMaterial();
                var geometryUnshadowMaterial = rendererData.GetGeometryUnshadowMaterial();

                for (var group = 0; group < layer.shadowCasters.Count; group++)
                {
                    var shadowCasters = layer.shadowCasters[group].GetShadowCasters();

                    // Draw the projected shadows for the shadow caster group. Only writes the composite stencil bit
                    RenderProjectedShadows(cmdBuffer, layer.startLayerID, light, shadowCasters, projectedShadowMaterial, 0);

                    // Render self shadowing or non self shadowing
                    RenderSelfShadowOption(cmdBuffer, layer.startLayerID, light, shadowCasters, projectedUnshadowMaterial, spriteShadowMaterial, spriteUnshadowMaterial, geometryShadowMaterial, geometryUnshadowMaterial);
                }
            }
        }
    }
}
