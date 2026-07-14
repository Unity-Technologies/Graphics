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
    // TODO: Culling of shadow casters, rotate color channels for shadow casting.
    internal static class ShadowRendering
    {
        internal enum ShadowTestType
        {
            Always,
            Unshadow,
        }

        private static readonly int k_LightPosID = Shader.PropertyToID("_LightPos");
        private static readonly int k_ShadowRadiusID = Shader.PropertyToID("_ShadowRadius");
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

        static readonly string k_NotTransformable = "NOT_TRANSFORMABLE";

        // Logical pass roles that URP looks up by name on shadow materials.
        // The integer values index into CachedShadowMaterial.passIndices.
        // Custom user shaders (and the future ShaderGraph 2D shadow master node)
        // declare passes by name; URP resolves name -> shader pass index once
        // at material creation, then per-frame uses passIndices[(int)role].
        internal enum ShadowPassRole
        {
            Self,
            UnshadowMark,
            UnshadowUnmark,
            ProjectedSelf,
            ProjectedUnshadow,
            Count,
        }

        // Bit-set of pass roles a given shader is expected to declare. Used by
        // BuildPassIndices to warn when an expected pass is missing.
        [Flags]
        internal enum ShadowPassRoles : uint
        {
            None              = 0,
            Self              = 1u << (int)ShadowPassRole.Self,
            UnshadowMark      = 1u << (int)ShadowPassRole.UnshadowMark,
            UnshadowUnmark    = 1u << (int)ShadowPassRole.UnshadowUnmark,
            ProjectedSelf     = 1u << (int)ShadowPassRole.ProjectedSelf,
            ProjectedUnshadow = 1u << (int)ShadowPassRole.ProjectedUnshadow,

            // Sprite and geometry caster shaders implement these three roles.
            Caster    = Self | UnshadowMark | UnshadowUnmark,
            // Projected shadow shaders implement these two roles.
            Projected = ProjectedSelf | ProjectedUnshadow,
            // All known roles (used for validating against an arbitrary shader).
            All       = Caster | Projected,
        }

        // Pass names on the consolidated shadow shaders. Looked up via Material.FindPass
        // exactly once per material, at cache-creation time.
        internal const string k_SelfPassName = "Self";
        internal const string k_UnshadowMarkPassName = "UnshadowMark";
        internal const string k_UnshadowUnmarkPassName = "UnshadowUnmark";
        internal const string k_ProjectedSelfPassName = "ProjectedSelf";
        internal const string k_ProjectedUnshadowPassName = "ProjectedUnshadow";

        // A cached shadow material plus its resolved pass indices, keyed in the
        // Renderer2DData dictionaries by a bitmask of variant bits (e.g. SKINNED_SPRITE).
        // passIndices is length ShadowPassRole.Count; slots with -1 indicate the shader
        // does not declare that role. Invariant: if material is non-null, passIndices is non-null.
        internal readonly struct CachedShadowMaterial
        {
            public readonly Material material;
            public readonly int[] passIndices;

            public CachedShadowMaterial(Material material, int[] passIndices)
            {
                this.material = material;
                this.passIndices = passIndices;
            }
        }

        // Bitmask key for the sprite shadow material cache.
        private const uint k_SpriteShadowMaterialSkinnedBit = 1u;
        private const string k_SkinnedSpriteKeyword = "SKINNED_SPRITE";

        private static string GetPassName(ShadowPassRole role)
        {
            switch (role)
            {
                case ShadowPassRole.Self:              return k_SelfPassName;
                case ShadowPassRole.UnshadowMark:      return k_UnshadowMarkPassName;
                case ShadowPassRole.UnshadowUnmark:    return k_UnshadowUnmarkPassName;
                case ShadowPassRole.ProjectedSelf:     return k_ProjectedSelfPassName;
                case ShadowPassRole.ProjectedUnshadow: return k_ProjectedUnshadowPassName;
                default:
                    // If a new ShadowPassRole is added without updating this switch, fail loudly
                    // during development rather than silently producing empty pass names.
                    throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown ShadowPassRole; update GetPassName when adding new roles.");
            }
        }

        private static int[] BuildPassIndices(Material material, ShadowPassRoles expected)
        {
            var indices = new int[(int)ShadowPassRole.Count];
            for (int i = 0; i < (int)ShadowPassRole.Count; i++)
            {
                var passName = GetPassName((ShadowPassRole)i);
                indices[i] = material.FindPass(passName);

                var roleBit = (ShadowPassRoles)(1u << i);
                if ((expected & roleBit) != 0 && indices[i] < 0)
                {
                    // Diagnostic for custom user shaders / future ShaderGraph master nodes
                    // that forget to declare a pass URP is going to ask for at draw time.
                    // Fires once per material (cache miss), not per frame.
                    var shaderName = material.shader != null ? material.shader.name : "<null>";
                    Debug.LogWarning($"[Shadow2D] Material '{material.name}' (shader '{shaderName}') is missing expected pass '{passName}'. Add `Pass {{ Name \"{passName}\" ... }}` to the shader; casters using this role will not render until then.");
                }
            }
            return indices;
        }

        private static CachedShadowMaterial GetOrCreateCachedShadowMaterial(Dictionary<uint, CachedShadowMaterial> cache, uint key, Shader shader, bool enableSkinnedKeyword, ShadowPassRoles expectedRoles)
        {
            if (shader == null)
                return default;

#if UNITY_EDITOR
            // In editor the resource shader can be re-assigned; ensure the cached material still matches.
            if (cache.TryGetValue(key, out var existing))
            {
                if (existing.material != null && existing.material.shader == shader)
                    return existing;
                if (existing.material != null)
                    CoreUtils.Destroy(existing.material);
                cache.Remove(key);
            }
#else
            if (cache.TryGetValue(key, out var cached) && cached.material != null)
                return cached;
#endif

            var material = CoreUtils.CreateEngineMaterial(shader);
            if (enableSkinnedKeyword)
                material.EnableKeyword(k_SkinnedSpriteKeyword);

            var entry = new CachedShadowMaterial(material, BuildPassIndices(material, expectedRoles));
            cache[key] = entry;
            return entry;
        }

        internal static CachedShadowMaterial GetSpriteShadowMaterial(this Renderer2DData rendererData, Renderer2DResources resources, bool skinned)
        {
            uint key = skinned ? k_SpriteShadowMaterialSkinnedBit : 0u;
            return GetOrCreateCachedShadowMaterial(rendererData.spriteShadowMaterials, key, resources.spriteShadowShader, skinned, ShadowPassRoles.Caster);
        }

        internal static CachedShadowMaterial GetGeometryShadowMaterial(this Renderer2DData rendererData, Renderer2DResources resources)
        {
            return GetOrCreateCachedShadowMaterial(rendererData.geometryShadowMaterials, 0u, resources.geometryShadowShader, false, ShadowPassRoles.Caster);
        }

        internal static CachedShadowMaterial GetProjectedShadowMaterial(this Renderer2DData rendererData, Renderer2DResources resources)
        {
            return GetOrCreateCachedShadowMaterial(rendererData.projectedShadowMaterials, 0u, resources.projectedShadowShader, false, ShadowPassRoles.Projected);
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
                                ShapeProviderUtility.CallOnBeforeRender(shadowCaster.shadowShape2DProvider, shadowCaster.shadowShape2DComponent, shadowCaster.m_ShadowMesh, bounds, camera);
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

            if (!shadowCaster.isTransformable)
                cmdBuffer.EnableShaderKeyword(k_NotTransformable);
            else
                cmdBuffer.DisableShaderKeyword(k_NotTransformable);

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

        private static void RenderProjectedShadows(UnsafeCommandBuffer cmdBuffer, int layerToRender, Light2D light, List<ShadowCaster2D> shadowCasters, CachedShadowMaterial projected, ShadowPassRole passRole, ShadowTestType shadowTestType)
        {
            if (projected.material == null)
                return;

            int pass = projected.passIndices[(int)passRole];
            if (pass < 0)
                return;

            // Draw the projected shadows for the shadow caster group. Writing into the group stencil buffer bit
            for (var i = 0; i < shadowCasters.Count; i++)
            {
                var shadowCaster = shadowCasters[i];
                if (ShadowTest(shadowTestType, shadowCaster))
                {
                    if (ShadowCasterIsVisible(shadowCaster) && shadowCaster.castsShadows && shadowCaster.IsLit(light))
                    {
                        if (shadowCaster != null && shadowCaster.IsShadowedLayer(layerToRender))
                        {
                            if (shadowCaster.shadowCastingSource != ShadowCaster2D.ShadowCastingSources.None && shadowCaster.mesh != null)
                            {
                                SetShadowProjectionGlobals(cmdBuffer, shadowCaster, light);
                                cmdBuffer.DrawMesh(shadowCaster.mesh, shadowCaster.transform.localToWorldMatrix, projected.material, 0, pass);
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

        private static void RenderSpriteShadow(UnsafeCommandBuffer cmdBuffer, int layerToRender, Light2D light, List<ShadowCaster2D> shadowCasters, CachedShadowMaterial sprite, CachedShadowMaterial spriteSkinned, CachedShadowMaterial geometry, ShadowPassRole unshadowRole, ShadowTestType shadowTestType)
        {
            // Self-shadow casters always use ShadowPassRole.Self; only the unshadow role varies
            // between phases (UnshadowMark in phase 1, UnshadowUnmark in phase 4). Resolve once
            // from the pre-built per-material arrays.
            const int selfRoleIdx = (int)ShadowPassRole.Self;
            int unshadowRoleIdx = (int)unshadowRole;

            int spriteSelfPass            = (sprite.material         != null) ? sprite.passIndices[selfRoleIdx]            : -1;
            int spriteUnshadowPass        = (sprite.material         != null) ? sprite.passIndices[unshadowRoleIdx]        : -1;
            int spriteSelfPassSkinned     = (spriteSkinned.material  != null) ? spriteSkinned.passIndices[selfRoleIdx]     : -1;
            int spriteUnshadowPassSkinned = (spriteSkinned.material  != null) ? spriteSkinned.passIndices[unshadowRoleIdx] : -1;
            int geometrySelfPass          = (geometry.material       != null) ? geometry.passIndices[selfRoleIdx]          : -1;
            int geometryUnshadowPass      = (geometry.material       != null) ? geometry.passIndices[unshadowRoleIdx]      : -1;

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
                        var spriteMat    = useSkinnedMaterials ? spriteSkinned.material      : sprite.material;
                        int selfPass     = useSkinnedMaterials ? spriteSelfPassSkinned       : spriteSelfPass;
                        int unshadowPass = useSkinnedMaterials ? spriteUnshadowPassSkinned   : spriteUnshadowPass;

                        if (spriteMat == null)
                            continue;

                        if (ShadowCasterIsVisible(shadowCaster) && shadowCaster.selfShadows)
                        {
                            if (selfPass < 0)
                                continue;
                            int numberOfSubmeshes = GetRendererSubmeshes(renderer, shadowCaster);
                            for (int submeshIndex = 0; submeshIndex < numberOfSubmeshes; submeshIndex++)
                                cmdBuffer.DrawRenderer(renderer, spriteMat, submeshIndex, selfPass);
                        }
                        else
                        {
                            if (unshadowPass < 0)
                                continue;
                            int numberOfSubmeshes = GetRendererSubmeshes(renderer, shadowCaster);
                            for (int submeshIndex = 0; submeshIndex < numberOfSubmeshes; submeshIndex++)
                            {
                                cmdBuffer.DrawRenderer(renderer, spriteMat, submeshIndex, unshadowPass);
                            }
                        }
                    }
                    else
                    {
                        if (geometry.material == null || shadowCaster.mesh == null)
                            continue;

                        if (ShadowCasterIsVisible(shadowCaster) && shadowCaster.selfShadows)
                        {
                            if (geometrySelfPass < 0)
                                continue;
                            cmdBuffer.DrawMesh(shadowCaster.mesh, shadowCaster.transform.localToWorldMatrix, geometry.material, 0, geometrySelfPass);
                        }
                        else
                        {
                            if (geometryUnshadowPass < 0)
                                continue;
                            cmdBuffer.DrawMesh(shadowCaster.mesh, shadowCaster.transform.localToWorldMatrix, geometry.material, 0, geometryUnshadowPass);
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
            // Resolve the resources lookup once per call; pass down so the four Get* calls below
            // don't each hit GraphicsSettings.TryGetRenderPipelineSettings.
            if (!GraphicsSettings.TryGetRenderPipelineSettings<Renderer2DResources>(out var resources))
                return;

            using (new ProfilingScope(cmdBuffer, ProfilerMarkers.s_ProfilingSamplerShadows))
            {
                var shadowRadius = light.boundingSphere.radius + (light.transform.position - light.boundingSphere.position).magnitude;

                cmdBuffer.SetGlobalVector(k_LightPosID, light.transform.position);
                cmdBuffer.SetGlobalFloat(k_ShadowRadiusID, shadowRadius);
                cmdBuffer.SetGlobalFloat(k_SoftShadowAngle, Mathf.Deg2Rad * light.shadowSoftness * k_MaxShadowSoftnessAngle);

                var projectedShadowMaterial = rendererData.GetProjectedShadowMaterial(resources);
                var spriteShadowMaterial = rendererData.GetSpriteShadowMaterial(resources, skinned: false);
#if USING_2DANIMATION
                var spriteShadowMaterialSkinned = rendererData.GetSpriteShadowMaterial(resources, skinned: true);
#else
                CachedShadowMaterial spriteShadowMaterialSkinned = default;
#endif
                var geometryShadowMaterial = rendererData.GetGeometryShadowMaterial(resources);


                for (var group = 0; group < layer.shadowCasters.Count; group++)
                {
                    var shadowCasters = layer.shadowCasters[group].GetShadowCasters();

                    // Phase 1: self-shadow casters draw their "Self" pass; non-self casters draw their "UnshadowMark" pass (sets stencil ref 1).
                    RenderSpriteShadow(cmdBuffer, layer.startLayerID, light, shadowCasters, spriteShadowMaterial, spriteShadowMaterialSkinned, geometryShadowMaterial, ShadowPassRole.UnshadowMark, ShadowTestType.Always);
                    // Phase 2: projected shadows are written for all casters (stencil NotEqual gates the contribution).
                    RenderProjectedShadows(cmdBuffer, layer.startLayerID, light, shadowCasters, projectedShadowMaterial, ShadowPassRole.ProjectedSelf, ShadowTestType.Always);
                    // Phase 3: projected unshadow re-writes the projected channel for non-self casters where stencil == 1.
                    RenderProjectedShadows(cmdBuffer, layer.startLayerID, light, shadowCasters, projectedShadowMaterial, ShadowPassRole.ProjectedUnshadow, ShadowTestType.Unshadow);
                    // Phase 4: non-self casters draw their "UnshadowUnmark" pass to clear stencil ref 0.
                    RenderSpriteShadow(cmdBuffer, layer.startLayerID, light, shadowCasters, spriteShadowMaterial, spriteShadowMaterialSkinned, geometryShadowMaterial, ShadowPassRole.UnshadowUnmark, ShadowTestType.Unshadow);

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
