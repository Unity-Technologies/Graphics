using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using Unity.Collections;
using UnityEngine.Rendering.Universal.U2D.Profiler;

#if USING_SPRITESHAPE
using UnityEngine.U2D;
#endif

#if USING_2DANIMATION
using UnityEngine.U2D.Animation;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.Universal
{
    // TODO: Culling of shadow casters, rotate color channels for shadow casting, check get material functions.
    internal static class ShadowRendering
    {
        internal enum ShadowTestType
        {
            Always,
            Unshadow,
        }

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


        private static readonly float k_MaxShadowSoftnessAngle = 15;
        private static readonly Color k_ShadowColorLookup = new Color(0, 0, 1, 0);
        private static readonly Color k_UnshadowColorLookup = new Color(0, 1, 0, 0);

        private static Material CreateMaterial(Shader shader, int offset, int pass)
        {
            Material material = CoreUtils.CreateEngineMaterial(shader);
            material.SetInt(k_ShadowColorMaskID, 1 << (offset + 1));
            material.SetPass(pass);

            return material;
        }

        private static Material GetProjectedShadowMaterial(
            Material material,
            Func<Renderer2DResources, Shader> shaderFunc,
            int offset, int pass)
        {

#if !UNITY_EDITOR // In standalone builds, shaders are never changed. We can early exit
            if (material != null)
                return material;
#endif

            if (!GraphicsSettings.TryGetRenderPipelineSettings<Renderer2DResources>(out var renderer2DResources))
                return null;

            var shader = shaderFunc(renderer2DResources);

            if (material != null)
            {
                if (material.shader != shader)
                    material = null;
            }

            if (material == null)
            {
                material = CoreUtils.CreateEngineMaterial(shader);
                material.SetInt(k_ShadowColorMaskID, 1 << (offset + 1));
                material.SetPass(pass);
            }

            return material;
        }

        internal static Material GetProjectedShadowMaterial(this Renderer2DData rendererData)
        {
            rendererData.projectedShadowMaterial = GetProjectedShadowMaterial(
                rendererData.projectedShadowMaterial,
                r => r.projectedShadowShader,
                0, 0);

            return rendererData.projectedShadowMaterial;
        }

        internal static Material GetProjectedUnshadowMaterial(this Renderer2DData rendererData)
        {
            rendererData.projectedUnshadowMaterial = GetProjectedShadowMaterial(
                rendererData.projectedUnshadowMaterial,
                r => r.projectedShadowShader,
                1, 1);

            return rendererData.projectedUnshadowMaterial;
        }

        private static Material GetSpriteShadowMaterial(this Renderer2DData rendererData)
        {
            rendererData.spriteSelfShadowMaterial = GetProjectedShadowMaterial(
                rendererData.spriteSelfShadowMaterial,
                r => r.spriteShadowShader,
                0, 0);

            return rendererData.spriteSelfShadowMaterial;
        }

        private static Material GetSpriteUnshadowMaterial(this Renderer2DData rendererData)
        {
            rendererData.spriteUnshadowMaterial = GetProjectedShadowMaterial(
                rendererData.spriteUnshadowMaterial,
                r => r.spriteUnshadowShader,
                1, 0);

            return rendererData.spriteUnshadowMaterial;
        }

#if USING_2DANIMATION
        private const string k_SkinnedSpriteKeyword = "SKINNED_SPRITE";

        private static Material GetSpriteShadowMaterialSkinned(this Renderer2DData rendererData)
        {
            if (rendererData.spriteSelfShadowMaterialSkinned != null)
                return rendererData.spriteSelfShadowMaterialSkinned;

            var baseMaterial = rendererData.GetSpriteShadowMaterial();
            if (baseMaterial == null || !baseMaterial.shader.isSupported)
                return null;

            rendererData.spriteSelfShadowMaterialSkinned = new Material(baseMaterial);
            rendererData.spriteSelfShadowMaterialSkinned.EnableKeyword(k_SkinnedSpriteKeyword);
            return rendererData.spriteSelfShadowMaterialSkinned;
        }

        private static Material GetSpriteUnshadowMaterialSkinned(this Renderer2DData rendererData)
        {
            if (rendererData.spriteUnshadowMaterialSkinned != null)
                return rendererData.spriteUnshadowMaterialSkinned;

            var baseMaterial = rendererData.GetSpriteUnshadowMaterial();
            if (baseMaterial == null || !baseMaterial.shader.isSupported)
                return null;

            rendererData.spriteUnshadowMaterialSkinned = new Material(baseMaterial);
            rendererData.spriteUnshadowMaterialSkinned.EnableKeyword(k_SkinnedSpriteKeyword);
            return rendererData.spriteUnshadowMaterialSkinned;
        }
#endif

        private static Material GetGeometryShadowMaterial(this Renderer2DData rendererData)
        {
            rendererData.geometrySelfShadowMaterial = GetProjectedShadowMaterial(
                rendererData.geometrySelfShadowMaterial,
                r => r.geometryShadowShader,
                0, 0);

            return rendererData.geometrySelfShadowMaterial;
        }

        private static Material GetGeometryUnshadowMaterial(this Renderer2DData rendererData)
        {
            rendererData.geometryUnshadowMaterial = GetProjectedShadowMaterial(
                rendererData.geometryUnshadowMaterial,
                r => r.geometryUnshadowShader,
                1, 0);

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
                maxCorner = Vector3.Max(maxCorner, camera.transform.TransformPoint(nearCorners[i]));
                maxCorner = Vector3.Max(maxCorner, camera.transform.TransformPoint(farCorners[i]));
                minCorner = Vector3.Min(minCorner, camera.transform.TransformPoint(nearCorners[i]));
                minCorner = Vector3.Min(minCorner, camera.transform.TransformPoint(farCorners[i]));
            }

            nearCorners.Dispose();
            farCorners.Dispose();

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

        internal static void CallOnBeforeRender(Camera camera, ILight2DCullResult cullResult)
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

        internal static void PrerenderShadows(UnsafeCommandBuffer cmdBuffer, Renderer2DData rendererData, ref LayerBatch layer, Light2D light, int shadowIndex, float shadowIntensity)
        {
            RenderShadows(cmdBuffer, rendererData, ref layer, light);
        }

        private static void SetShadowProjectionGlobals(UnsafeCommandBuffer cmdBuffer, ShadowCaster2D shadowCaster, Light2D light)
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

        internal static void SetGlobalShadowProp(IRasterCommandBuffer cmdBuffer)
        {
            cmdBuffer.SetGlobalColor(k_ShadowShadowColorID, k_ShadowColorLookup);
            cmdBuffer.SetGlobalColor(k_ShadowUnshadowColorID, k_UnshadowColorLookup);
        }

        static bool ShadowCasterIsVisible(ShadowCaster2D shadowCaster)
        {
#if UNITY_EDITOR
            return SceneVisibilityManager.instance == null || !SceneVisibilityManager.instance.IsHidden(shadowCaster.gameObject);
#else
            return true;
#endif
        }

        /// <summary>
        /// Use skinned sprite shadow materials only when this caster uses the SpriteSkin shape provider and
        /// GPU deformation is active for that sprite (per <see cref="UnityEngine.U2D.Animation.SpriteSkinUtility.IsGpuDeformationActive"/>).
        /// Materials are created like non-skinned variants; no null check here.
        /// </summary>
        static bool ShouldUseSkinnedSpriteShadowMaterials(ShadowCaster2D shadowCaster, Renderer renderer)
        {
#if USING_2DANIMATION
            if (shadowCaster.shadowShape2DProvider is not ShadowShape2DProvider_SpriteSkin)
                return false;
            return renderer is SpriteRenderer spriteRenderer && SpriteSkinUtility.IsGpuDeformationActive(spriteRenderer);
#else
            return false;
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

        private static void RenderProjectedShadows(UnsafeCommandBuffer cmdBuffer, int layerToRender, Light2D light, List<ShadowCaster2D> shadowCasters, Material projectedShadowsMaterial, int pass, ShadowTestType shadowTestType)
        {
            // Draw the projected shadows for the shadow caster group. Writing into the group stencil buffer bit
            for (var i = 0; i < shadowCasters.Count; i++)
            {
                var shadowCaster = shadowCasters[i];
                if (ShadowTest(shadowTestType, shadowCaster))
                {
                    if (ShadowCasterIsVisible(shadowCaster) && shadowCaster.castsShadows && shadowCaster.IsLit(light))
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

        private static void RenderSpriteShadow(UnsafeCommandBuffer cmdBuffer, int layerToRender, Light2D light, List<ShadowCaster2D> shadowCasters, Material spriteShadowMaterial, Material spriteUnshadowMaterial, Material geometryShadowMaterial, Material geometryUnshadowMaterial, Material spriteShadowMaterialSkinned, Material spriteUnshadowMaterialSkinned, int pass, ShadowTestType shadowTestType)
        {
            //Draw the sprites, either as self shadowing or unshadowing
            for (var i = 0; i < shadowCasters.Count; i++)
            {
                ShadowCaster2D shadowCaster = shadowCasters[i];
                if (ShadowTest(shadowTestType, shadowCaster))
                {
                    if (!shadowCaster.IsLit(light))
                        continue;

                    Renderer renderer = GetRendererFromCaster(shadowCaster, light, layerToRender);

                    cmdBuffer.SetGlobalFloat(k_ShadowAlphaCutoffID, shadowCaster.alphaCutoff);

                    if (renderer != null)
                    {
                        bool useSkinnedMaterials = ShouldUseSkinnedSpriteShadowMaterials(shadowCaster, renderer);
                        var shadowMat = useSkinnedMaterials ? spriteShadowMaterialSkinned : spriteShadowMaterial;
                        var unshadowMat = useSkinnedMaterials ? spriteUnshadowMaterialSkinned : spriteUnshadowMaterial;

                        if (ShadowCasterIsVisible(shadowCaster) && shadowCaster.selfShadows)
                        {
                            int numberOfSubmeshes = GetRendererSubmeshes(renderer, shadowCaster);
                            for (int submeshIndex = 0; submeshIndex < numberOfSubmeshes; submeshIndex++)
                                cmdBuffer.DrawRenderer(renderer, shadowMat, submeshIndex, pass);
                        }
                        else
                        {
                            int numberOfSubmeshes = GetRendererSubmeshes(renderer, shadowCaster);
                            for (int submeshIndex = 0; submeshIndex < numberOfSubmeshes; submeshIndex++)
                            {
                                cmdBuffer.DrawRenderer(renderer, unshadowMat, submeshIndex, pass);

                            }
                        }
                    }
                    else
                    {
                        if (shadowCaster.mesh != null)
                        {
                            if (ShadowCasterIsVisible(shadowCaster) && shadowCaster.selfShadows)
                                cmdBuffer.DrawMesh(shadowCaster.mesh, shadowCaster.transform.localToWorldMatrix, geometryShadowMaterial, 0, pass);
                            else
                                cmdBuffer.DrawMesh(shadowCaster.mesh, shadowCaster.transform.localToWorldMatrix, geometryUnshadowMaterial, 0, pass);
                        }
                    }
                }
            }
        }

        internal static bool ShadowTest(ShadowTestType shadowTestType, ShadowCaster2D shadowCaster)
        {
            // This is just being done because using delegates are creating garbage and my tests are failing
            if(shadowTestType == ShadowTestType.Always)
                return true;
            else if(shadowTestType == ShadowTestType.Unshadow)
                return !shadowCaster.selfShadows;

            return false;
        }


        private static void RenderShadows(UnsafeCommandBuffer cmdBuffer, Renderer2DData rendererData, ref LayerBatch layer, Light2D light)
        {
            using (new ProfilingScope(cmdBuffer, ProfilerMarkers.s_ProfilingSamplerShadows))
            {
                var shadowRadius = light.boundingSphere.radius + (light.transform.position - light.boundingSphere.position).magnitude;

                cmdBuffer.SetGlobalVector(k_LightPosID, light.transform.position);
                cmdBuffer.SetGlobalFloat(k_ShadowRadiusID, shadowRadius);
                cmdBuffer.SetGlobalFloat(k_SoftShadowAngle, Mathf.Deg2Rad * light.shadowSoftness * k_MaxShadowSoftnessAngle);

                var projectedShadowMaterial = rendererData.GetProjectedShadowMaterial();
                var projectedUnshadowMaterial = rendererData.GetProjectedUnshadowMaterial();
                var spriteShadowMaterial = rendererData.GetSpriteShadowMaterial();
                var spriteUnshadowMaterial = rendererData.GetSpriteUnshadowMaterial();
#if USING_2DANIMATION
                var spriteShadowMaterialSkinned = rendererData.GetSpriteShadowMaterialSkinned();
                var spriteUnshadowMaterialSkinned = rendererData.GetSpriteUnshadowMaterialSkinned();
#else
                Material spriteShadowMaterialSkinned = null;
                Material spriteUnshadowMaterialSkinned = null;
#endif
                var geometryShadowMaterial = rendererData.GetGeometryShadowMaterial();
                var geometryUnshadowMaterial = rendererData.GetGeometryUnshadowMaterial();


                for (var group = 0; group < layer.shadowCasters.Count; group++)
                {
                    var shadowCasters = layer.shadowCasters[group].GetShadowCasters();

                    // Render self shadowing or non self shadowing
                    RenderSpriteShadow(cmdBuffer, layer.startLayerID, light, shadowCasters, spriteShadowMaterial, spriteUnshadowMaterial, geometryShadowMaterial, geometryUnshadowMaterial, spriteShadowMaterialSkinned, spriteUnshadowMaterialSkinned, 0, ShadowTestType.Always);
                    // Draw the projected shadows for the shadow caster group. Only writes the composite stencil bit
                    RenderProjectedShadows(cmdBuffer, layer.startLayerID, light, shadowCasters, projectedShadowMaterial, 0, ShadowTestType.Always);
                    // Draw the projected shadows for the shadow caster group. Only writes the composite stencil bit
                    RenderProjectedShadows(cmdBuffer, layer.startLayerID, light, shadowCasters, projectedShadowMaterial, 1, ShadowTestType.Unshadow);
                    //Render self shadowing or non self shadowing
                    RenderSpriteShadow(cmdBuffer, layer.startLayerID, light, shadowCasters, spriteShadowMaterial, spriteUnshadowMaterial, geometryShadowMaterial, geometryUnshadowMaterial, spriteShadowMaterialSkinned, spriteUnshadowMaterialSkinned, 1, ShadowTestType.Unshadow);

#if ENABLE_PROFILER && PROFILER_INSTALLED
                    if (Renderer2D.canProfilerCapture)
                    {
                        for (var i = 0; i < shadowCasters.Count; i++)
                        {
                            var shadowCaster = shadowCasters[i];
                            if (!shadowCaster.IsLit(light))
                                continue;                            
                            ProfilerMarkers.s_U2DShadowCasterCounterValue.Value++;
                            ProfilerMarkers.s_ShadowRenderFrameData.Capture(shadowCaster.gameObject.GetEntityId());
                            ProfilerMarkers.s_ShadowMeshFrameData.Capture(shadowCaster.gameObject, shadowCaster.mesh);
                        }
                    }
#endif
                }
            }
        }
    }
}
