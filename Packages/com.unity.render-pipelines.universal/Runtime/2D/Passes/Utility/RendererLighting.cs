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

        // Light Batcher.
        internal static LightBatch lightBatch = new LightBatch();

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

        internal static Material GetLightMaterial(this Renderer2DData rendererData, Light2D light, bool isVolume, bool useShadows)
        {
            var materialIndex = GetLightMaterialIndex(light, isVolume, useShadows);

            if (!rendererData.lightMaterials.TryGetValue(materialIndex, out var material))
            {
                material = CreateLightMaterial(rendererData, light, isVolume, useShadows);
                rendererData.lightMaterials[materialIndex] = material;
            }

            return material;
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
