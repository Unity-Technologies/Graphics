using UnityEngine.Experimental.Rendering;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    internal static class RendererLighting
    {
        // The passes a custom Light2D material is expected to declare, matching the names the
        // ShaderGraph Light2D SubTarget emits. DrawLight2DPass draws pass 0 for the light and pass 1
        // for its volumetric contribution, so a material for this field carries both.
        internal const string k_LightPassName = "Light2D";
        internal const string k_LightVolumetricPassName = "Light2DVolumetric";

        static readonly string[] k_LightPassNames = { k_LightPassName, k_LightVolumetricPassName };

        // Which of the expected light passes `material` does not declare, as a display string, or an
        // empty string when it declares them all. The counterpart of
        // ShadowRendering.GetMissingShadowPasses for the light material.
        internal static string GetMissingLightPasses(Material material)
        {
            if (material == null)
                return string.Empty;

            // A material with no shader can declare nothing, so every pass is missing.
            if (material.shader == null)
                return string.Join(", ", k_LightPassNames);

            var builder = new System.Text.StringBuilder();
            for (int i = 0; i < k_LightPassNames.Length; i++)
            {
                if (material.FindPass(k_LightPassNames[i]) >= 0)
                    continue;

                if (builder.Length > 0)
                    builder.Append(", ");
                builder.Append(k_LightPassNames[i]);
            }

            return builder.ToString();
        }

        // Whether `material` can be assigned to Light2D.material without a pass the renderer draws
        // being absent. The same predicate the inspector error uses, as a bool for callers that only
        // need a yes/no -- notably the material picker's filter.
        internal static bool IsCompatibleLightMaterial(Material material)
        {
            return material != null && GetMissingLightPasses(material).Length == 0;
        }

        // Shader-level counterpart, for callers sweeping the whole project where many materials share
        // one shader. Compatibility depends only on the pass names the shader declares, so the verdict
        // is a property of the Shader, not the Material.
        //
        // Allocates a throwaway Material because pass names are only reachable through Material.
        // Callers testing many shaders should memoize per Shader -- see Light2DMaterialSearchProvider.
        internal static bool IsCompatibleLightShader(Shader shader)
        {
            if (shader == null)
                return false;

            var probe = new Material(shader);
            try
            {
                return GetMissingLightPasses(probe).Length == 0;
            }
            finally
            {
                // DestroyImmediate rather than CoreUtils.Destroy, for the reason given on
                // ShadowRendering.IsCompatibleShadowShader: callers sweep every shader in the project
                // in one synchronous pass, and deferring would hold one live Material per shader.
                Object.DestroyImmediate(probe);
            }
        }

        public static readonly Color k_NormalClearColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
        private static readonly string k_UsePointLightCookiesKeyword = "USE_POINT_LIGHT_COOKIES";
        private static readonly string k_LightQualityFastKeyword = "LIGHT_QUALITY_FAST";
        private static readonly string k_UseNormalMap = "USE_NORMAL_MAP";
        private static readonly string k_UseShadowMap = "USE_SHADOW_MAP";
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
        private static readonly int k_VolSrcBlendID = Shader.PropertyToID("_VolSrcBlend");
        private static readonly int k_VolDstBlendID = Shader.PropertyToID("_VolDstBlend");
        private static readonly int k_CookieTexID = Shader.PropertyToID("_CookieTex");
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
        private static readonly int k_L2DLightRenderingLayer = Shader.PropertyToID("L2DLightRenderingLayer");

        // Light Batcher.
        internal static LightBatch lightBatch = new LightBatch();

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod]
        static void ResetStaticsOnLoad()
        {
            s_HasSetupRenderTextureFormatToUse = false;
            s_RenderTextureFormatToUse = GraphicsFormat.R8G8B8A8_UNorm;
            lightBatch.Release();
            lightBatch = new LightBatch();
        }
#endif

        internal static GraphicsFormat GetRenderTextureFormat()
        {
            if (!s_HasSetupRenderTextureFormatToUse)
            {
                // UUM-41070: We require `Linear | Render` but with the deprecated FormatUsage this was checking `Blend`
                // For now, we keep checking for `Blend` until the performance hit of doing the correct checks is evaluated
                if (SystemInfo.IsFormatSupported(GraphicsFormat.B10G11R11_UFloatPack32, GraphicsFormatUsage.Blend))
                    s_RenderTextureFormatToUse = GraphicsFormat.B10G11R11_UFloatPack32;
                else if (SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SFloat, GraphicsFormatUsage.Blend))
                    s_RenderTextureFormatToUse = GraphicsFormat.R16G16B16A16_SFloat;

                s_HasSetupRenderTextureFormatToUse = true;
            }

            return s_RenderTextureFormatToUse;
        }

        internal static void EnableBlendStyle(IRasterCommandBuffer cmd, int blendStyleIndex, bool enabled)
        {
            var keyword = k_UseBlendStyleKeywords[blendStyleIndex];

            if (enabled)
                cmd.EnableShaderKeyword(keyword);
            else
                cmd.DisableShaderKeyword(keyword);
        }

        internal static void DisableAllKeywords(IRasterCommandBuffer cmd)
        {
            foreach (var keyword in k_UseBlendStyleKeywords)
            {
                cmd.DisableShaderKeyword(keyword);
            }
        }

        internal static void GetTransparencySortingMode(Renderer2DData rendererData, Camera camera, ref SortingSettings sortingSettings)
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

        internal static bool CanCastShadows(Light2D light, int layerToRender)
        {
            return light.shadowsEnabled && light.shadowIntensity > 0 && light.IsLitLayer(layerToRender);
        }

        internal static bool CanCastVolumetricShadows(Light2D light, int endLayerValue)
        {
            return light.volumeIntensity > 0 && light.volumetricEnabled && light.renderVolumetricShadows && light.GetTopMostLitLayer() == endLayerValue;
        }

        internal static void SetLightShaderGlobals(IRasterCommandBuffer cmd, Light2DBlendStyle[] lightBlendStyles, int[] blendStyleIndices)
        {
            for (var i = 0; i < blendStyleIndices.Length; i++)
            {
                var blendStyleIndex = blendStyleIndices[i];
                if (blendStyleIndex >= k_BlendFactorsPropIDs.Length)
                    break;

                var blendStyle = lightBlendStyles[blendStyleIndex];
                cmd.SetGlobalVector(k_BlendFactorsPropIDs[blendStyleIndex], blendStyle.blendFactors);
                cmd.SetGlobalVector(k_MaskFilterPropIDs[blendStyleIndex], blendStyle.maskTextureChannelFilter.mask);
                cmd.SetGlobalVector(k_InvertedFilterPropIDs[blendStyleIndex], blendStyle.maskTextureChannelFilter.inverted);
            }
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

        internal static void SetPerLightShaderGlobals(IRasterCommandBuffer cmd, Light2D light, int slot, bool isVolumetric, bool hasShadows, bool batchingSupported)
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
                cmd.SetGlobalInt(k_L2DLightRenderingLayer, (int)light.renderingLayerMask.value);
                cmd.SetGlobalFloat(k_L2DShadowIntensity, hasShadows ? (isVolumetric ? (1 - light.shadowVolumeIntensity) : (1 - light.shadowIntensity)) : 1);
            }

            if (hasShadows)
                ShadowRendering.SetGlobalShadowProp(cmd);
        }

        internal static void SetPerPointLightShaderGlobals(IRasterCommandBuffer cmd, Light2D light, int slot, bool batchingSupported)
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

        internal static void SetCookieShaderProperties(Light2D light, MaterialPropertyBlock properties)
        {
            if (light.useCookieSprite && light.m_CookieSpriteTextureHandle.IsValid())
                properties.SetTexture(light.lightType == Light2D.LightType.Sprite ? k_CookieTexID : k_PointLightCookieTexID, light.m_CookieSpriteTextureHandle);
        }

        private static void SetBlendModes(Material material, BlendMode src, BlendMode dst)
        {
            material.SetFloat(k_SrcBlendID, (float)src);
            material.SetFloat(k_DstBlendID, (float)dst);
        }

        private static uint GetLightMaterialIndex(Light2D light, bool isVolume, bool useShadows)
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
            bitIndex++;
            var useShadowMap = useShadows ? 1u << bitIndex : 0u;

            return fastQualityBit | pointCookieBit | additiveBit | shapeBit | volumeBit | useNormalMap | useShadowMap;
        }

        private static Material CreateLightMaterial(Renderer2DData rendererData, Light2D light, bool isVolume, bool useShadows)
        {
            if (!GraphicsSettings.TryGetRenderPipelineSettings<Renderer2DResources>(out var resources))
                return null;

            var isPoint = light.isPointLight;

            Material material = CoreUtils.CreateEngineMaterial(resources.lightShader);

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
                    SetBlendModes(material, BlendMode.SrcAlpha, BlendMode.One);
            }

            if (isPoint && light.lightCookieSprite != null && light.lightCookieSprite.texture != null)
                material.EnableKeyword(k_UsePointLightCookiesKeyword);

            if (light.normalMapQuality == Light2D.NormalMapQuality.Fast)
                material.EnableKeyword(k_LightQualityFastKeyword);

            if (light.normalMapQuality != Light2D.NormalMapQuality.Disabled)
                material.EnableKeyword(k_UseNormalMap);

            if (useShadows)
                material.EnableKeyword(k_UseShadowMap);

            return material;
        }

        // Applies blend modes for a provider (ShaderGraph) material clone that bypasses the per-config
        // material cache. Sets BOTH the non-volumetric properties (_SrcBlend/_DstBlend, pass 0) and
        // the volumetric properties (_VolSrcBlend/_VolDstBlend, pass 1). Both depend only on
        // light.overlapOperation and light.lightType, which are baked into the customLightMaterials
        // cache key, so calling this once per cache-fill is sufficient — no per-frame mutation.
        // Using separate properties per-pass avoids intra-material conflict where pass 0 and pass 1
        // would otherwise share `_SrcBlend/_DstBlend` and the later-set value would win.
        private static void ApplyBlendModesForLight(Material material, Light2D light)
        {
            // Pass 0 (Light2D): non-volumetric blend
            if (light.overlapOperation == Light2D.OverlapOperation.Additive)
                SetBlendModes(material, BlendMode.One, BlendMode.One);
            else
                SetBlendModes(material, BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha);

            // Pass 1 (Light2DVolumetric): volumetric blend via dedicated properties
            if (light.lightType == Light2D.LightType.Point)
            {
                material.SetFloat(k_VolSrcBlendID, (float)BlendMode.One);
                material.SetFloat(k_VolDstBlendID, (float)BlendMode.One);
            }
            else
            {
                material.SetFloat(k_VolSrcBlendID, (float)BlendMode.SrcAlpha);
                material.SetFloat(k_VolDstBlendID, (float)BlendMode.One);
            }
        }

        // Packs (source-material EntityId, overlapOperation, lightType) into a value-tuple key for
        // the customLightMaterials cache. Two Light2Ds sharing the same source material but
        // differing in overlapOperation or lightType hash to different keys and therefore get
        // independent Material clones — which is what lets each clone hold its own blend state
        // without the cross-light stomp described on the customLightMaterials field.
        //
        // Uses EntityId.ToULong instead of the obsolete GetInstanceID. OverlapOperation is a
        // 2-value enum (1 bit) and LightType is a small enum (< 16 values, 4 bits); packing them
        // into the low bits of a single int is enough to distinguish every valid combination.
        private static (ulong id, int state) CustomLightMaterialKey(Material src, Light2D light)
        {
            int state = ((int)light.overlapOperation << 4) | ((int)light.lightType & 0xF);
            return (EntityId.ToULong(src.GetEntityId()), state);
        }

        internal static Material GetLightMaterial(this Renderer2DData rendererData, Light2D light, bool isVolume, bool useShadows)
        {
            // Light2D.material override: a non-null custom material bypasses URP's variant-bit cache.
            // Keywords are driven by cmd-recorded SetVolumetricKeyword / SetAdditiveBlendingKeyword /
            // SetPointLightCookiesKeyword. Blend state is baked into a per-(source, overlapOperation,
            // lightType) clone so multiple Light2Ds sharing the same source material don't stomp
            // each other's _SrcBlend/_DstBlend at frame recording. Clones live on Renderer2DData and
            // are destroyed alongside `lightMaterials` in Dispose / ClearLightMaterialCache.
            //
            // Trade-off: property changes on the source material at runtime (e.g. user-tweaked
            // color) don't propagate to the clones. Users needing dynamic property updates should
            // re-assign `light.material` (invalidates the cache) or drive the property via a shader
            // global. This mirrors the existing built-in `lightMaterials` cache contract.
            var custom = light.material;
            if (custom != null)
            {
                var key = CustomLightMaterialKey(custom, light);
                if (!rendererData.customLightMaterials.TryGetValue(key, out var instance) || instance == null)
                {
                    instance = new Material(custom);
                    ApplyBlendModesForLight(instance, light);
                    rendererData.customLightMaterials[key] = instance;
                }
                return instance;
            }

            var materialIndex = GetLightMaterialIndex(light, isVolume, useShadows);

            if (!rendererData.lightMaterials.TryGetValue(materialIndex, out var material))
            {
                material = CreateLightMaterial(rendererData, light, isVolume, useShadows);
                rendererData.lightMaterials[materialIndex] = material;
            }

            return material;
        }

        // Records USE_VOLUMETRIC keyword state into the command buffer so it executes in draw-call order.
        // Must be called via cmd (not material.EnableKeyword) because material keyword mutations are
        // immediate — they don't respect command buffer ordering, which breaks custom-material paths
        // where the same material instance is used for both the non-volumetric and volumetric passes.
        internal static void SetVolumetricKeyword(RasterCommandBuffer cmd, Material material, bool isVolumetric)
        {
            var keyword = new LocalKeyword(material.shader, k_UseVolumetric);
            if (isVolumetric)
                cmd.EnableKeyword(material, keyword);
            else
                cmd.DisableKeyword(material, keyword);
        }

        // Records USE_ADDITIVE_BLENDING keyword state into the command buffer for the same reason
        // as SetVolumetricKeyword. Built-in Light2D materials get this keyword set once at cache
        // creation via CreateLightMaterial's material.EnableKeyword call, but custom (ShaderGraph)
        // materials assigned via Light2D.material bypass that cache — without a cmd-level setter,
        // the keyword stays off, the shader's #if USE_ADDITIVE_BLENDING branch never fires, and an
        // additive light's fragment writes falloff into lightColor.a which the runtime One,One
        // blend ignores. The result is a solid-color light with no visible falloff on the main pass
        // (volumetric works because that path is gated on USE_VOLUMETRIC, which IS being set).
        internal static void SetAdditiveBlendingKeyword(RasterCommandBuffer cmd, Material material, bool isAdditive)
        {
            var keyword = new LocalKeyword(material.shader, k_UseAdditiveBlendingKeyword);
            if (isAdditive)
                cmd.EnableKeyword(material, keyword);
            else
                cmd.DisableKeyword(material, keyword);
        }

        // Records USE_POINT_LIGHT_COOKIES keyword state into the command buffer so custom
        // (ShaderGraph) Light2D materials pick up the cookie branch in the Point path.
        // Built-in cached materials bake this keyword in at cache-creation time (see
        // CreateLightMaterial), but custom materials bypass that cache — without a
        // cmd-level setter, a Point light with a cookie sprite renders as if no cookie
        // were assigned even though _PointLightCookieTex is bound on the property block.
        internal static void SetPointLightCookiesKeyword(RasterCommandBuffer cmd, Material material, bool useCookie)
        {
            var keyword = new LocalKeyword(material.shader, k_UsePointLightCookiesKeyword);
            if (useCookie)
                cmd.EnableKeyword(material, keyword);
            else
                cmd.DisableKeyword(material, keyword);
        }

        // Records USE_NORMAL_MAP keyword state into the command buffer so custom
        // (ShaderGraph) Light2D materials pick up the normal-map branch in Light2DPass.hlsl.
        // Built-in cached materials bake this keyword in at cache-creation time (see
        // CreateLightMaterial's `material.EnableKeyword(k_UseNormalMap)` call), but custom
        // materials bypass that cache — without a cmd-level setter, USE_NORMAL_MAP stays
        // off on the ShaderGraph variant, the `#if USE_NORMAL_MAP && !USE_VOLUMETRIC`
        // block never fires, and _NormalMap/sampler_NormalMap aren't even declared
        // (NORMALS_LIGHTING_VARIABLES in LightingUtility.hlsl expands to empty without
        // the keyword). The per-light normal texture set on the property block at
        // DrawLight2DPass line 127-128 is therefore invisible to the shader — a
        // Light2D with normalMapQuality != Disabled renders as if normal mapping were off.
        //
        // Same cmd-vs-material rationale as SetVolumetricKeyword: a single custom material
        // instance is reused across lights that may differ in normalMapQuality, so the
        // keyword must be recorded per-draw in command order rather than mutated on the
        // material immediately.
        internal static void SetNormalMapKeyword(RasterCommandBuffer cmd, Material material, bool useNormalMap)
        {
            var keyword = new LocalKeyword(material.shader, k_UseNormalMap);
            if (useNormalMap)
                cmd.EnableKeyword(material, keyword);
            else
                cmd.DisableKeyword(material, keyword);
        }

        internal static short GetCameraSortingLayerBoundsIndex(this Renderer2DData rendererData)
        {
            SortingLayer[] sortingLayers = Light2DManager.GetCachedSortingLayer();
            for (short i = 0; i < sortingLayers.Length; i++)
            {
                if (sortingLayers[i].id == rendererData.cameraSortingLayerTextureBound)
                    return (short)sortingLayers[i].value;
            }

            return short.MinValue;
        }
    }
}
