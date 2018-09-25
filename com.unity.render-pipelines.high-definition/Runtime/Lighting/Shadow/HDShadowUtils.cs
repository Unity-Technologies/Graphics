using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // TODO remove every occurrence of ShadowSplitData in function parameters when we'll have scriptable culling
    public static class HDShadowUtils
    {
        static float GetPunctualFilterWidthInTexels(LightType lightType)
        {
            var hdAsset = (GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset);

            if (hdAsset == null)
                return 1;

            switch (hdAsset.renderPipelineSettings.hdShadowInitParams.punctualShadowQuality)
            {
                // Warning: these values have to match the algorithms used for shadow filtering (in HDShadowAlgorithm.hlsl)
                case HDShadowQuality.Low:
                    return 3; // PCF 3x3
                case HDShadowQuality.Medium:
                    return 5; // PCF 5x5
                default:
                    return 1; // Any non PCF algorithms
            }
        }

        public static void ExtractPointLightData(LightType lightType, VisibleLight visibleLight, Vector2 viewportSize, float normalBiasMax, uint faceIndex, out Matrix4x4 view, out Matrix4x4 invViewProjection, out Matrix4x4 projection, out Matrix4x4 deviceProjection, out ShadowSplitData splitData)
        {
            Vector4 lightDir;

            float guardAngle = ShadowUtils.CalcGuardAnglePerspective(90.0f, viewportSize.x, GetPunctualFilterWidthInTexels(lightType), normalBiasMax, 79.0f);
            ShadowUtils.ExtractPointLightMatrix(visibleLight, faceIndex, guardAngle, out view, out projection, out deviceProjection, out invViewProjection, out lightDir, out splitData);
        }

        // TODO: box spot and pyramid spots with non 1 aspect ratios shadow are incorrectly culled, see when scriptable culling will be here
        public static void ExtractSpotLightData(LightType lightType, SpotLightShape shape, float aspectRatio, float shapeWidth, float shapeHeight, VisibleLight visibleLight, Vector2 viewportSize, float normalBiasMax, out Matrix4x4 view, out Matrix4x4 invViewProjection, out Matrix4x4 projection, out Matrix4x4 deviceProjection, out ShadowSplitData splitData)
        {
            Vector4 lightDir;

            // There is no aspect ratio for non pyramid spot lights
            if (shape != SpotLightShape.Pyramid)
                aspectRatio = 1.0f;

            float guardAngle = ShadowUtils.CalcGuardAnglePerspective(visibleLight.light.spotAngle, viewportSize.x, GetPunctualFilterWidthInTexels(lightType), normalBiasMax, 180.0f - visibleLight.light.spotAngle);
            ShadowUtils.ExtractSpotLightMatrix(visibleLight, guardAngle, aspectRatio, out view, out projection, out deviceProjection, out invViewProjection, out lightDir, out splitData);

            if (shape == SpotLightShape.Box)
            {
                float nearMin = 0.1f;
                float nearZ = visibleLight.light.shadowNearPlane >= nearMin ? visibleLight.light.shadowNearPlane : nearMin;
                projection = Matrix4x4.Ortho(-shapeWidth / 2, shapeWidth / 2, -shapeHeight / 2, shapeHeight / 2, nearZ, visibleLight.range);
                deviceProjection = GL.GetGPUProjectionMatrix(projection, false);
                ShadowUtils.InvertOrthographic(ref projection, ref view, out invViewProjection);
            }
        }

        public static void ExtractDirectionalLightData(VisibleLight visibleLight, Vector2 viewportSize, uint cascadeIndex, int cascadeCount, float[] cascadeRatios, float nearPlaneOffset, CullResults cullResults, int lightIndex, out Matrix4x4 view, out Matrix4x4 invViewProjection, out Matrix4x4 projection, out Matrix4x4 deviceProjection, out ShadowSplitData splitData)
        {
            Vector4     lightDir;

            ShadowUtils.ExtractDirectionalLightMatrix(visibleLight, cascadeIndex, cascadeCount, cascadeRatios, nearPlaneOffset, (uint)viewportSize.x, (uint)viewportSize.y, out view, out projection, out deviceProjection, out invViewProjection, out lightDir, out splitData, cullResults, lightIndex);
        }

        // Currently area light shadows are not supported
        public static void ExtractAreaLightData(VisibleLight visibleLight, LightTypeExtent lightTypeExtent, out Matrix4x4 view, out Matrix4x4 invViewProjection, out Matrix4x4 projection, out Matrix4x4 deviceProjection, out ShadowSplitData splitData)
        {
            view = Matrix4x4.identity;
            invViewProjection = Matrix4x4.identity;
            deviceProjection = Matrix4x4.identity;
            projection = Matrix4x4.identity;
            splitData = default(ShadowSplitData);
        }
    }
}