using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    // Mode describing how the main/additional shadow caster pass operates
    internal enum ShadowPassMode
    {
        // Renders shadow casters into the atlas, then sets shadow keywords/params.
        DrawGeometry,
        // Reuses the shadow atlas rendered by a previous pass, only keywords/params are set.
        ReuseCachedAtlas,
        // Binds the default shadowmap and only sets keywords and empty params.
        Empty
    }

    /// <summary>
    /// Struct container for shadow slice data.
    /// </summary>
    public struct ShadowSliceData
    {
        /// <summary>
        /// The view matrix.
        /// </summary>
        public Matrix4x4 viewMatrix;

        /// <summary>
        /// The projection matrix.
        /// </summary>
        public Matrix4x4 projectionMatrix;

        /// <summary>
        /// The shadow transform matrix.
        /// </summary>
        public Matrix4x4 shadowTransform;

        /// <summary>
        /// The X offset to the shadow map.
        /// </summary>
        public int offsetX;

        /// <summary>
        /// The Y offset to the shadow map.
        /// </summary>
        public int offsetY;

        /// <summary>
        /// The maximum tile resolution in an Atlas.
        /// </summary>
        public int resolution;

        /// <summary>
        /// The shadow split data containing culling information.
        /// </summary>
        public ShadowSplitData splitData;

        /// <summary>
        /// Clears and resets the data.
        /// </summary>
        public void Clear()
        {
            viewMatrix = Matrix4x4.identity;
            projectionMatrix = Matrix4x4.identity;
            shadowTransform = Matrix4x4.identity;
            offsetX = offsetY = 0;
            resolution = 1024;
        }
    }

    /// <summary>
    /// Various utility functions used for shadows.
    /// </summary>
    public static class ShadowUtils
    {
        // Must match PCSS_DISK_SAMPLE_COUNT in com.unity.render-pipelines.core/ShaderLibrary/Shadow/PCSS.hlsl
        internal const int k_PCSSMaxSampleCount = 64;

        // Must match SOFT_SHADOW_QUALITY_PCSS in Shadows.hlsl. The PCF filters read it as High.
        internal const float k_SoftShadowQualityPCSS = (int)SoftShadowQuality.High + 1;

        internal static readonly bool m_ForceShadowPointSampling;

        static ShadowUtils()
        {
            m_ForceShadowPointSampling = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal &&
                GraphicsSettings.HasShaderDefine(Graphics.activeTier, BuiltinShaderDefine.UNITY_METAL_SHADOWS_USE_POINT_FILTERING);
        }

        /// <summary>
        /// Extracts the directional light matrix.
        /// </summary>
        /// <param name="cullResults">The results of a culling operation.</param>
        /// <param name="shadowData">Data containing shadow settings.</param>
        /// <param name="shadowLightIndex">The visible light index.</param>
        /// <param name="cascadeIndex">The cascade index.</param>
        /// <param name="shadowmapWidth">The shadow map width.</param>
        /// <param name="shadowmapHeight">The shadow map height.</param>
        /// <param name="shadowResolution">The shadow map resolution.</param>
        /// <param name="shadowNearPlane">Near plane value to use for shadow frustums.</param>
        /// <param name="cascadeSplitDistance">The culling sphere for the cascade.</param>
        /// <param name="shadowSliceData">The struct container for shadow slice data.</param>
        /// <param name="viewMatrix">The view matrix to be set.</param>
        /// <param name="projMatrix">The projection matrix to be set.</param>
        /// <returns>True if the matrix was successfully extracted.</returns>
        public static bool ExtractDirectionalLightMatrix(ref CullingResults cullResults, ref ShadowData shadowData, int shadowLightIndex, int cascadeIndex, int shadowmapWidth, int shadowmapHeight, int shadowResolution, float shadowNearPlane, out Vector4 cascadeSplitDistance, out ShadowSliceData shadowSliceData, out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix)
        {
            bool result = ExtractDirectionalLightMatrix(ref cullResults, ref shadowData, shadowLightIndex, cascadeIndex, shadowmapWidth, shadowmapHeight, shadowResolution, shadowNearPlane, out cascadeSplitDistance, out shadowSliceData);
            viewMatrix = shadowSliceData.viewMatrix;
            projMatrix = shadowSliceData.projectionMatrix;
            return result;
        }

        /// <summary>
        /// Extracts the directional light matrix.
        /// </summary>
        /// <param name="cullResults">The results of a culling operation.</param>
        /// <param name="shadowData">Data containing shadow settings.</param>
        /// <param name="shadowLightIndex">The visible light index.</param>
        /// <param name="cascadeIndex">The cascade index.</param>
        /// <param name="shadowmapWidth">The shadow map width.</param>
        /// <param name="shadowmapHeight">The shadow map height.</param>
        /// <param name="shadowResolution">The shadow map resolution.</param>
        /// <param name="shadowNearPlane">Near plane value to use for shadow frustums.</param>
        /// <param name="cascadeSplitDistance">The culling sphere for the cascade.</param>
        /// <param name="shadowSliceData">The struct container for shadow slice data.</param>
        /// <returns>True if the matrix was successfully extracted.</returns>
        public static bool ExtractDirectionalLightMatrix(ref CullingResults cullResults, ref ShadowData shadowData, int shadowLightIndex, int cascadeIndex, int shadowmapWidth, int shadowmapHeight, int shadowResolution, float shadowNearPlane, out Vector4 cascadeSplitDistance, out ShadowSliceData shadowSliceData)
        {
            return ExtractDirectionalLightMatrix(ref cullResults, shadowData.universalShadowData,
                shadowLightIndex, cascadeIndex, shadowmapWidth, shadowmapHeight, shadowResolution,
                shadowNearPlane, out cascadeSplitDistance, out shadowSliceData);
        }

        /// <summary>
        /// Extracts the directional light matrix.
        /// </summary>
        /// <param name="cullResults">The results of a culling operation.</param>
        /// <param name="shadowData">Data containing shadow settings.</param>
        /// <param name="shadowLightIndex">The visible light index.</param>
        /// <param name="cascadeIndex">The cascade index.</param>
        /// <param name="shadowmapWidth">The shadow map width.</param>
        /// <param name="shadowmapHeight">The shadow map height.</param>
        /// <param name="shadowResolution">The shadow map resolution.</param>
        /// <param name="shadowNearPlane">Near plane value to use for shadow frustums.</param>
        /// <param name="cascadeSplitDistance">The culling sphere for the cascade.</param>
        /// <param name="shadowSliceData">The struct container for shadow slice data.</param>
        /// <returns>True if the matrix was successfully extracted.</returns>
        public static bool ExtractDirectionalLightMatrix(ref CullingResults cullResults, UniversalShadowData shadowData, int shadowLightIndex, int cascadeIndex, int shadowmapWidth, int shadowmapHeight, int shadowResolution, float shadowNearPlane, out Vector4 cascadeSplitDistance, out ShadowSliceData shadowSliceData)
        {
            bool success = cullResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(shadowLightIndex,
                cascadeIndex, shadowData.mainLightShadowCascadesCount, shadowData.mainLightShadowCascadesSplit, shadowResolution, shadowNearPlane, out shadowSliceData.viewMatrix, out shadowSliceData.projectionMatrix,
                out shadowSliceData.splitData);

            cascadeSplitDistance = shadowSliceData.splitData.cullingSphere;
            shadowSliceData.offsetX = (cascadeIndex % 2) * shadowResolution;
            shadowSliceData.offsetY = (cascadeIndex / 2) * shadowResolution;
            shadowSliceData.resolution = shadowResolution;
            shadowSliceData.shadowTransform = GetShadowTransform(shadowSliceData.projectionMatrix, shadowSliceData.viewMatrix);

            // It is the culling sphere radius multiplier for shadow cascade blending
            // If this is less than 1.0, then it will begin to cull castors across cascades
            shadowSliceData.splitData.shadowCascadeBlendCullingFactor = 1.0f;

            // If we have shadow cascades baked into the atlas we bake cascade transform
            // in each shadow matrix to save shader ALU and L/S
            if (shadowData.mainLightShadowCascadesCount > 1)
                ApplySliceTransform(ref shadowSliceData, shadowmapWidth, shadowmapHeight);

            return success;
        }

        /// <summary>
        /// Extracts the spot light matrix.
        /// </summary>
        /// <param name="cullResults">The results of a culling operation.</param>
        /// <param name="shadowData">Data containing shadow settings.</param>
        /// <param name="shadowLightIndex">The visible light index.</param>
        /// <param name="shadowMatrix">The shadow matrix to be set.</param>
        /// <param name="viewMatrix">The view matrix to be set.</param>
        /// <param name="projMatrix">The projection matrix to be set.</param>
        /// <param name="splitData">The shadow split data containing culling information.</param>
        /// <returns>True if the matrix was successfully extracted.</returns>
        public static bool ExtractSpotLightMatrix(ref CullingResults cullResults, ref ShadowData shadowData, int shadowLightIndex, out Matrix4x4 shadowMatrix, out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData splitData)
        {
            return ExtractSpotLightMatrix(ref cullResults, shadowData.universalShadowData, shadowLightIndex, out shadowMatrix, out viewMatrix, out projMatrix, out splitData);
        }

        /// <summary>
        /// Extracts the spot light matrix.
        /// </summary>
        /// <param name="cullResults">The results of a culling operation.</param>
        /// <param name="shadowData">Data containing shadow settings.</param>
        /// <param name="shadowLightIndex">The visible light index.</param>
        /// <param name="shadowMatrix">The shadow matrix to be set.</param>
        /// <param name="viewMatrix">The view matrix to be set.</param>
        /// <param name="projMatrix">The projection matrix to be set.</param>
        /// <param name="splitData">The shadow split data containing culling information.</param>
        /// <returns>True if the matrix was successfully extracted.</returns>
        public static bool ExtractSpotLightMatrix(ref CullingResults cullResults, UniversalShadowData shadowData, int shadowLightIndex, out Matrix4x4 shadowMatrix, out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData splitData)
        {
            bool success = cullResults.ComputeSpotShadowMatricesAndCullingPrimitives(shadowLightIndex, out viewMatrix, out projMatrix, out splitData); // returns false if input parameters are incorrect (rare)
            shadowMatrix = GetShadowTransform(projMatrix, viewMatrix);
            return success;
        }

        /// <summary>
        /// Extracts the point light matrix.
        /// </summary>
        /// <param name="cullResults">The results of a culling operation.</param>
        /// <param name="shadowData">Data containing shadow settings.</param>
        /// <param name="shadowLightIndex">The visible light index.</param>
        /// <param name="cubemapFace">The face of the cubemap to use.</param>
        /// <param name="fovBias">The guard angle that must be added to a point light shadow face frustum angle in order to avoid shadows missing at the boundaries between cube faces.</param>
        /// <param name="shadowMatrix">The shadow matrix to be set.</param>
        /// <param name="viewMatrix">The view matrix to be set.</param>
        /// <param name="projMatrix">The projection matrix to be set.</param>
        /// <param name="splitData">The shadow split data containing culling information.</param>
        /// <returns>True if the matrix was successfully extracted.</returns>
        public static bool ExtractPointLightMatrix(ref CullingResults cullResults, ref ShadowData shadowData, int shadowLightIndex, CubemapFace cubemapFace, float fovBias, out Matrix4x4 shadowMatrix, out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData splitData)
        {
            return ExtractPointLightMatrix(ref cullResults, shadowData.universalShadowData, shadowLightIndex, cubemapFace, fovBias, out shadowMatrix, out viewMatrix, out projMatrix, out splitData);
        }

        /// <summary>
        /// Extracts the point light matrix.
        /// </summary>
        /// <param name="cullResults">The results of a culling operation.</param>
        /// <param name="shadowData">Data containing shadow settings.</param>
        /// <param name="shadowLightIndex">The visible light index.</param>
        /// <param name="cubemapFace">The face of the cubemap to use.</param>
        /// <param name="fovBias">The guard angle that must be added to a point light shadow face frustum angle in order to avoid shadows missing at the boundaries between cube faces.</param>
        /// <param name="shadowMatrix">The shadow matrix to be set.</param>
        /// <param name="viewMatrix">The view matrix to be set.</param>
        /// <param name="projMatrix">The projection matrix to be set.</param>
        /// <param name="splitData">The shadow split data containing culling information.</param>
        /// <returns>True if the matrix was successfully extracted.</returns>
        public static bool ExtractPointLightMatrix(ref CullingResults cullResults, UniversalShadowData shadowData, int shadowLightIndex, CubemapFace cubemapFace, float fovBias, out Matrix4x4 shadowMatrix, out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData splitData)
        {
            bool success = cullResults.ComputePointShadowMatricesAndCullingPrimitives(shadowLightIndex, cubemapFace, fovBias, out viewMatrix, out projMatrix, out splitData); // returns false if input parameters are incorrect (rare)

            // In native API CullingResults.ComputeSpotShadowMatricesAndCullingPrimitives there is code that inverts the 3rd component of shadow-casting spot light's "world-to-local" matrix (it was so since its original addition to the code base):
            // https://github.cds.internal.unity3d.com/unity/unity/commit/34813e063526c4be0ef0448dfaae3a911dd8be58#diff-cf0b417fc6bd8ee2356770797e628cd4R331
            // (the same transformation has also always been used in the Built-In Render Pipeline)
            //
            // However native API CullingResults.ComputePointShadowMatricesAndCullingPrimitives does not contain this transformation.
            // As a result, the view matrices returned for a point light shadow face, and for a spot light with same direction as that face, have opposite 3rd component.
            //
            // This causes normalBias to be incorrectly applied to shadow caster vertices during the point light shadow pass.
            // To counter this effect, we invert the point light shadow view matrix component here:
            {
                viewMatrix.m10 = -viewMatrix.m10;
                viewMatrix.m11 = -viewMatrix.m11;
                viewMatrix.m12 = -viewMatrix.m12;
                viewMatrix.m13 = -viewMatrix.m13;
            }

            shadowMatrix = GetShadowTransform(projMatrix, viewMatrix);
            return success;
        }

        // Slope factor applied to every shadow slice in the legacy depth bias mode, and by the deprecated RenderShadowSlice overloads regardless of the mode; matches HDRP defaults.
        const float k_LegacySlopeScaleDepthBias = 2.5f;

        // Slope reached at a normalized bias of 1 for hard shadows; GetSlopeScaleDepthBias scales it by the soft shadow kernel radius.
        const float k_MaxSlopeScale = 5.0f;

        internal static float maxNormalizedSlopeScale => 1.0f;

        // Covers receivers facing the light head-on (N.L near 1) in the slope-scale mode with caster normal bias, where both the slope term and the normal bias vanish.
        const float k_SlopeScaleCasterDepthBias = 0.05f;

        internal static ShadowDepthBiasMode GetConfiguredDepthBiasMode(URPShadowBiasSettings shadowBiasSettings)
        {
            return shadowBiasSettings != null ? shadowBiasSettings.depthBiasMode : ShadowDepthBiasMode.Legacy;
        }

        internal static ShadowNormalBiasMode GetConfiguredNormalBiasMode(URPShadowBiasSettings shadowBiasSettings)
        {
            return shadowBiasSettings != null ? shadowBiasSettings.normalBiasMode : ShadowNormalBiasMode.Caster;
        }

        /// <summary>
        /// Returns the slope-scale depth bias to set while rendering the shadow slices of a visible light, respecting the depth bias mode configured in the URP graphics settings.
        /// In the slope-scale depth bias mode the maximum slope is scaled by the light's soft shadow PCF kernel radius, since wider kernels sample farther from the receiver.
        /// Pair this with the RenderShadowSlice overloads taking a slopeScaleDepthBias when writing a custom shadow pass.
        /// </summary>
        /// <param name="shadowLight">The visible light casting the shadows.</param>
        /// <param name="shadowData">Data containing shadow settings.</param>
        /// <param name="visibleLightIndex">The index of the visible light.</param>
        /// <returns>The slope-scale depth bias to apply while rendering the light's shadow slices.</returns>
        public static float GetSlopeScaleDepthBias(ref VisibleLight shadowLight, UniversalShadowData shadowData, int visibleLightIndex)
        {
            return GetSlopeScaleDepthBias(ref shadowLight, shadowData.bias, shadowData.depthBiasMode, shadowData.supportsSoftShadows, visibleLightIndex, UniversalRenderPipeline.asset);
        }

        static float GetSlopeScaleDepthBias(ref VisibleLight shadowLight, List<Vector4> bias, ShadowDepthBiasMode depthBiasMode, bool supportsSoftShadows, int visibleLightIndex, UniversalRenderPipelineAsset asset)
        {
            float normalizedSlopeScale = 0.0f;
            float softShadowKernelRadius = 1.0f;
            if (depthBiasMode == ShadowDepthBiasMode.SlopeScale)
            {
                Debug.Assert(bias != null && visibleLightIndex >= 0 && visibleLightIndex < bias.Count, "GetSlopeScaleDepthBias expects a visible light index with an entry in the shadow bias data.");
                normalizedSlopeScale = bias[visibleLightIndex].x;
                softShadowKernelRadius = GetSoftShadowKernelRadius(ref shadowLight, supportsSoftShadows, asset);
            }

            return GetSlopeScaleDepthBias(normalizedSlopeScale, depthBiasMode, softShadowKernelRadius);
        }

        internal static float GetSlopeScaleDepthBias(float normalizedSlopeScale, ShadowDepthBiasMode depthBiasMode, float softShadowKernelRadius)
        {
            switch (depthBiasMode)
            {
                case ShadowDepthBiasMode.SlopeScale:
                    return Mathf.Lerp(k_LegacySlopeScaleDepthBias, k_MaxSlopeScale * softShadowKernelRadius, Mathf.Clamp(normalizedSlopeScale, 0.0f, maxNormalizedSlopeScale));
                case ShadowDepthBiasMode.Legacy:
                    return k_LegacySlopeScaleDepthBias;
                default:
                    Debug.Assert(false, "Unexpected depth bias mode.");
                    return k_LegacySlopeScaleDepthBias;
            }
        }

        /// <summary>
        /// Renders shadows to a shadow slice.
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="context"></param>
        /// <param name="shadowSliceData"></param>
        /// <param name="settings"></param>
        /// <param name="proj"></param>
        /// <param name="view"></param>
        [Obsolete("Use the overload taking slopeScaleDepthBias and useDepthClamp so the configured depth bias mode is respected; retrieve the slope with ShadowUtils.GetSlopeScaleDepthBias and pass false for useDepthClamp to keep the previous clipping. #from(6000.7)", false)]
        public static void RenderShadowSlice(CommandBuffer cmd, ref ScriptableRenderContext context,
            ref ShadowSliceData shadowSliceData, ref ShadowDrawingSettings settings,
            Matrix4x4 proj, Matrix4x4 view)
        {
            RenderShadowSlice(cmd, ref context, ref shadowSliceData, ref settings, proj, view, k_LegacySlopeScaleDepthBias, useDepthClamp: false);
        }

        /// <summary>
        /// Renders shadows to a shadow slice.
        /// </summary>
        /// <param name="cmd">The command buffer to record rendering commands on.</param>
        /// <param name="context">The rendering context.</param>
        /// <param name="shadowSliceData">The shadow slice to render.</param>
        /// <param name="settings">The shadow drawing settings.</param>
        /// <param name="proj">The projection matrix used while rendering.</param>
        /// <param name="view">The view matrix used while rendering.</param>
        /// <param name="slopeScaleDepthBias">The slope-scale depth bias to apply while rendering, see <see cref="GetSlopeScaleDepthBias(ref VisibleLight, UniversalShadowData, int)"/>.</param>
        [Obsolete("Use the overload taking useDepthClamp; pass false to keep the previous clipping. #from(7000.0)", false)]
        public static void RenderShadowSlice(CommandBuffer cmd, ref ScriptableRenderContext context,
            ref ShadowSliceData shadowSliceData, ref ShadowDrawingSettings settings,
            Matrix4x4 proj, Matrix4x4 view, float slopeScaleDepthBias)
        {
            RenderShadowSlice(cmd, ref context, ref shadowSliceData, ref settings, proj, view, slopeScaleDepthBias, useDepthClamp: false);
        }

        /// <summary>
        /// Renders shadows to a shadow slice.
        /// </summary>
        /// <param name="cmd">The command buffer to record rendering commands on.</param>
        /// <param name="context">The rendering context.</param>
        /// <param name="shadowSliceData">The shadow slice to render.</param>
        /// <param name="settings">The shadow drawing settings.</param>
        /// <param name="proj">The projection matrix used while rendering.</param>
        /// <param name="view">The view matrix used while rendering.</param>
        /// <param name="slopeScaleDepthBias">The slope-scale depth bias to apply while rendering, see <see cref="GetSlopeScaleDepthBias(ref VisibleLight, UniversalShadowData, int)"/>.</param>
        /// <param name="useDepthClamp">When true, casters render with depth clipping disabled (ZClip Off) so depth is clamped at the near plane instead of clipped. Pass true only for directional lights when <see cref="SystemInfo.supportsDepthClamp"/> is true.</param>
        public static void RenderShadowSlice(CommandBuffer cmd, ref ScriptableRenderContext context,
            ref ShadowSliceData shadowSliceData, ref ShadowDrawingSettings settings,
            Matrix4x4 proj, Matrix4x4 view, float slopeScaleDepthBias, bool useDepthClamp)
        {
            cmd.SetGlobalDepthBias(1.0f, slopeScaleDepthBias);
            cmd.SetGlobalFloat(ShaderPropertyId.zClip, useDepthClamp ? 0.0f : 1.0f);

            cmd.SetViewport(new Rect(shadowSliceData.offsetX, shadowSliceData.offsetY, shadowSliceData.resolution, shadowSliceData.resolution));
            cmd.SetViewProjectionMatrices(view, proj);
            var rl = context.CreateShadowRendererList(ref settings);
            cmd.DrawRendererList(rl);
            cmd.DisableScissorRect();
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            // Restore previous depth bias and ZClip values
            cmd.SetGlobalFloat(ShaderPropertyId.zClip, 1.0f);
            cmd.SetGlobalDepthBias(0.0f, 0.0f);
        }

        internal static void RenderShadowSlice(RasterCommandBuffer cmd,
            ref ShadowSliceData shadowSliceData, ref RendererList shadowRendererList,
            Matrix4x4 proj, Matrix4x4 view, float slopeScaleDepthBias, bool useDepthClamp)
        {
            cmd.SetGlobalDepthBias(1.0f, slopeScaleDepthBias);
            cmd.SetGlobalFloat(ShaderPropertyId.zClip, useDepthClamp ? 0.0f : 1.0f);

            cmd.SetViewport(new Rect(shadowSliceData.offsetX, shadowSliceData.offsetY, shadowSliceData.resolution, shadowSliceData.resolution));
            cmd.SetViewProjectionMatrices(view, proj);
            if(shadowRendererList.isValid)
                cmd.DrawRendererList(shadowRendererList);

            cmd.DisableScissorRect();
            // Restore previous depth bias and ZClip values
            cmd.SetGlobalFloat(ShaderPropertyId.zClip, 1.0f);
            cmd.SetGlobalDepthBias(0.0f, 0.0f);
        }

        /// <summary>
        /// Renders shadows to a shadow slice.
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="context"></param>
        /// <param name="shadowSliceData"></param>
        /// <param name="settings"></param>
        [Obsolete("Use the overload taking slopeScaleDepthBias and useDepthClamp so the configured depth bias mode is respected; retrieve the slope with ShadowUtils.GetSlopeScaleDepthBias and pass false for useDepthClamp to keep the previous clipping. #from(6000.7)", false)]
        public static void RenderShadowSlice(CommandBuffer cmd, ref ScriptableRenderContext context,
            ref ShadowSliceData shadowSliceData, ref ShadowDrawingSettings settings)
        {
            RenderShadowSlice(cmd, ref context, ref shadowSliceData, ref settings,
                shadowSliceData.projectionMatrix, shadowSliceData.viewMatrix, k_LegacySlopeScaleDepthBias, useDepthClamp: false);
        }

        /// <summary>
        /// Renders shadows to a shadow slice.
        /// </summary>
        /// <param name="cmd">The command buffer to record rendering commands on.</param>
        /// <param name="context">The rendering context.</param>
        /// <param name="shadowSliceData">The shadow slice to render.</param>
        /// <param name="settings">The shadow drawing settings.</param>
        /// <param name="slopeScaleDepthBias">The slope-scale depth bias to apply while rendering, see <see cref="GetSlopeScaleDepthBias(ref VisibleLight, UniversalShadowData, int)"/>.</param>
        [Obsolete("Use the overload taking proj, view and useDepthClamp; pass shadowSliceData.projectionMatrix, shadowSliceData.viewMatrix and false to keep the previous clipping. #from(7000.0)", false)]
        public static void RenderShadowSlice(CommandBuffer cmd, ref ScriptableRenderContext context,
            ref ShadowSliceData shadowSliceData, ref ShadowDrawingSettings settings, float slopeScaleDepthBias)
        {
            RenderShadowSlice(cmd, ref context, ref shadowSliceData, ref settings,
                shadowSliceData.projectionMatrix, shadowSliceData.viewMatrix, slopeScaleDepthBias, useDepthClamp: false);
        }

        /// <summary>
        /// Calculates the maximum tile resolution in an Atlas.
        /// </summary>
        /// <param name="atlasWidth"></param>
        /// <param name="atlasHeight"></param>
        /// <param name="tileCount"></param>
        /// <returns>The maximum tile resolution in an Atlas.</returns>
        public static int GetMaxTileResolutionInAtlas(int atlasWidth, int atlasHeight, int tileCount)
        {
            int resolution = Mathf.Min(atlasWidth, atlasHeight);
            int currentTileCount = atlasWidth / resolution * atlasHeight / resolution;
            while (currentTileCount < tileCount)
            {
                resolution = resolution >> 1;
                currentTileCount = atlasWidth / resolution * atlasHeight / resolution;
            }
            return resolution;
        }

        /// <summary>
        /// Used for baking bake cascade transforms in each shadow matrix.
        /// </summary>
        /// <param name="shadowSliceData"></param>
        /// <param name="atlasWidth"></param>
        /// <param name="atlasHeight"></param>
        public static void ApplySliceTransform(ref ShadowSliceData shadowSliceData, int atlasWidth, int atlasHeight)
        {
            Matrix4x4 sliceTransform = Matrix4x4.identity;
            float oneOverAtlasWidth = 1.0f / atlasWidth;
            float oneOverAtlasHeight = 1.0f / atlasHeight;
            sliceTransform.m00 = shadowSliceData.resolution * oneOverAtlasWidth;
            sliceTransform.m11 = shadowSliceData.resolution * oneOverAtlasHeight;
            sliceTransform.m03 = shadowSliceData.offsetX * oneOverAtlasWidth;
            sliceTransform.m13 = shadowSliceData.offsetY * oneOverAtlasHeight;

            // Apply shadow slice scale and offset
            shadowSliceData.shadowTransform = sliceTransform * shadowSliceData.shadowTransform;
        }

        /// <summary>
        /// Calculates the depth and normal bias from a light.
        /// </summary>
        /// <param name="shadowLight"></param>
        /// <param name="shadowLightIndex"></param>
        /// <param name="shadowData"></param>
        /// <param name="lightProjectionMatrix"></param>
        /// <param name="shadowResolution"></param>
        /// <returns>The depth and normal bias from a visible light. In the slope-scale depth bias mode the x component (depth bias) is a small fixed texel-scaled caster offset, or 0 in the shadow-receiver normal bias mode; retrieve the rasterizer bias with GetSlopeScaleDepthBias instead. In the shadow-receiver normal bias mode the y component (normal bias) is 0; retrieve the world-space sampling offset with GetReceiverNormalBias instead.</returns>
        public static Vector4 GetShadowBias(ref VisibleLight shadowLight, int shadowLightIndex, ref ShadowData shadowData, Matrix4x4 lightProjectionMatrix, float shadowResolution)
        {
            return GetShadowBias(ref shadowLight, shadowLightIndex, shadowData.bias, shadowData.supportsSoftShadows, shadowData.depthBiasMode, shadowData.normalBiasMode, lightProjectionMatrix, shadowResolution, UniversalRenderPipeline.asset);
        }

        /// <summary>
        /// Calculates the depth and normal bias from a light.
        /// </summary>
        /// <param name="shadowLight"></param>
        /// <param name="shadowLightIndex"></param>
        /// <param name="shadowData"></param>
        /// <param name="lightProjectionMatrix"></param>
        /// <param name="shadowResolution"></param>
        /// <returns>The depth and normal bias from a visible light. In the slope-scale depth bias mode the x component (depth bias) is a small fixed texel-scaled caster offset, or 0 in the shadow-receiver normal bias mode; retrieve the rasterizer bias with GetSlopeScaleDepthBias instead. In the shadow-receiver normal bias mode the y component (normal bias) is 0; retrieve the world-space sampling offset with GetReceiverNormalBias instead.</returns>
        public static Vector4 GetShadowBias(ref VisibleLight shadowLight, int shadowLightIndex, UniversalShadowData shadowData, Matrix4x4 lightProjectionMatrix, float shadowResolution)
        {
            return GetShadowBias(ref shadowLight, shadowLightIndex, shadowData.bias, shadowData.supportsSoftShadows, shadowData.depthBiasMode, shadowData.normalBiasMode, lightProjectionMatrix, shadowResolution, UniversalRenderPipeline.asset);
        }

        static float GetShadowTexelSizeInWorldSpace(ref VisibleLight shadowLight, Matrix4x4 lightProjectionMatrix, float shadowResolution, float receiverBiasTexels = 0.0f)
        {
            float frustumSize;
            if (shadowLight.lightType == LightType.Directional)
            {
                // Frustum size is guaranteed to be a cube as we wrap shadow frustum around a sphere
                frustumSize = 2.0f / lightProjectionMatrix.m00;
            }
            else if (shadowLight.lightType == LightType.Spot)
            {
                // For perspective projections, shadow texel size varies with depth
                // It will only work well if done in receiver side in the pixel shader. Currently UniversalRP
                // do bias on caster side in vertex shader. When we add shader quality tiers we can properly
                // handle this. For now, as a poor approximation we do a constant bias and compute the size of
                // the frustum as if it was orthogonal considering the size at mid point between near and far planes.
                // Depending on how big the light range is, it will be good enough with some tweaks in bias
                frustumSize = Mathf.Tan(shadowLight.spotAngle * 0.5f * Mathf.Deg2Rad) * shadowLight.range; // half-width (in world-space units) of shadow frustum's "far plane"
            }
            else if (shadowLight.lightType == LightType.Point)
            {
                // [Copied from above case:]
                // "For perspective projections, shadow texel size varies with depth
                //  It will only work well if done in receiver side in the pixel shader. Currently UniversalRP
                //  do bias on caster side in vertex shader. When we add shader quality tiers we can properly
                //  handle this. For now, as a poor approximation we do a constant bias and compute the size of
                //  the frustum as if it was orthogonal considering the size at mid point between near and far planes.
                //  Depending on how big the light range is, it will be good enough with some tweaks in bias"
                // Note: HDRP uses normalBias both in HDShadowUtils.CalcGuardAnglePerspective and HDShadowAlgorithms/EvalShadow_NormalBias (receiver bias)
                float fovBias = Internal.AdditionalLightsShadowCasterPass.GetPointLightShadowFrustumFovBiasInDegrees((int)shadowResolution, (shadowLight.light.shadows == LightShadows.Soft), receiverBiasTexels);
                // Note: the same fovBias was also used to compute ShadowUtils.ExtractPointLightMatrix
                float cubeFaceAngle = 90 + fovBias;
                frustumSize = Mathf.Tan(cubeFaceAngle * 0.5f * Mathf.Deg2Rad) * shadowLight.range; // half-width (in world-space units) of shadow frustum's "far plane"
            }
            else
            {
                Debug.LogWarning("Only point, spot and directional shadow casters are supported in universal pipeline");
                frustumSize = 0.0f;
            }

            return frustumSize / shadowResolution;
        }

        /// <summary>
        /// Returns the world-space distance by which shadow sampling positions are offset along the receiver's surface normal, respecting the normal bias mode configured in the URP graphics settings.
        /// In the shadow-receiver normal bias mode the value is scaled by the light's soft shadow PCF kernel radius, since wider kernels sample farther from the receiver.
        /// Pair this with GetShadowBias when writing a custom shadow pass: in the shadow-receiver normal bias mode the caster-side normal bias (the y component of GetShadowBias) is 0 and the offset returned here is applied at sampling time instead.
        /// </summary>
        /// <param name="shadowLight">The visible light casting the shadows.</param>
        /// <param name="visibleLightIndex">The index of the visible light.</param>
        /// <param name="shadowData">Data containing shadow settings.</param>
        /// <param name="lightProjectionMatrix">The projection matrix of the shadow slice.</param>
        /// <param name="shadowResolution">The resolution of the shadow slice, in texels.</param>
        /// <returns>The world-space receiver normal bias of the light's shadow slice. In the shadow-caster normal bias mode the value is 0.</returns>
        public static float GetReceiverNormalBias(ref VisibleLight shadowLight, int visibleLightIndex, UniversalShadowData shadowData, Matrix4x4 lightProjectionMatrix, float shadowResolution)
        {
            return GetReceiverNormalBias(ref shadowLight, shadowData.bias, shadowData.normalBiasMode, shadowData.supportsSoftShadows, visibleLightIndex, lightProjectionMatrix, shadowResolution, UniversalRenderPipeline.asset);
        }

        static float GetReceiverNormalBias(ref VisibleLight shadowLight, List<Vector4> bias, ShadowNormalBiasMode normalBiasMode, bool supportsSoftShadows, int visibleLightIndex, Matrix4x4 lightProjectionMatrix, float shadowResolution, UniversalRenderPipelineAsset asset)
        {
            float normalBias = 0.0f;
            float texelSize = 0.0f;
            float softShadowKernelRadius = 1.0f;
            if (normalBiasMode == ShadowNormalBiasMode.Receiver)
            {
                Debug.Assert(bias != null && visibleLightIndex >= 0 && visibleLightIndex < bias.Count, "GetReceiverNormalBias expects a visible light index with an entry in the shadow bias data.");
                normalBias = bias[visibleLightIndex].y;
                softShadowKernelRadius = GetSoftShadowKernelRadius(ref shadowLight, supportsSoftShadows, asset);
                texelSize = GetShadowTexelSizeInWorldSpace(ref shadowLight, lightProjectionMatrix, shadowResolution, normalBias * softShadowKernelRadius);
            }

            return GetReceiverNormalBias(normalBias, normalBiasMode, texelSize, softShadowKernelRadius);
        }

        internal static float GetReceiverNormalBias(float normalBias, ShadowNormalBiasMode normalBiasMode, float texelSize, float softShadowKernelRadius)
        {
            switch (normalBiasMode)
            {
                case ShadowNormalBiasMode.Caster:
                    return 0.0f;
                case ShadowNormalBiasMode.Receiver:
                    return normalBias * texelSize * softShadowKernelRadius;
                default:
                    Debug.Assert(false, "Unexpected normal bias mode.");
                    return 0.0f;
            }
        }

        static Vector4 GetShadowBias(ref VisibleLight shadowLight, int shadowLightIndex, List<Vector4> bias, bool supportsSoftShadows, ShadowDepthBiasMode depthBiasMode, ShadowNormalBiasMode normalBiasMode, Matrix4x4 lightProjectionMatrix, float shadowResolution, UniversalRenderPipelineAsset asset)
        {
            if (bias == null || shadowLightIndex < 0 || shadowLightIndex >= bias.Count)
            {
                Debug.Assert(false, "GetShadowBias expects a shadow light index with an entry in the shadow bias data.");
                return Vector4.zero;
            }

            // depth and normal bias scale is in shadowmap texel size in world space
            float softShadowKernelRadius = GetSoftShadowKernelRadius(ref shadowLight, supportsSoftShadows, asset);
            // The point light guard angle grows with the receiver normal bias, so the texel size must account for it too
            float receiverBiasTexels = normalBiasMode == ShadowNormalBiasMode.Receiver
                ? bias[shadowLightIndex].y * softShadowKernelRadius
                : 0.0f;
            float texelSize = GetShadowTexelSizeInWorldSpace(ref shadowLight, lightProjectionMatrix, shadowResolution, receiverBiasTexels);
            float depthBias = GetCasterDepthBias(bias[shadowLightIndex].x, depthBiasMode, normalBiasMode, texelSize);
            float normalBias = GetCasterNormalBias(bias[shadowLightIndex].y, normalBiasMode, texelSize, shadowLight.lightType);

            depthBias *= softShadowKernelRadius;
            normalBias *= softShadowKernelRadius;

            return new Vector4(depthBias, normalBias, (float)shadowLight.lightType, 0.0f);
        }

        internal static float GetCasterDepthBias(float depthBias, ShadowDepthBiasMode depthBiasMode, ShadowNormalBiasMode normalBiasMode, float texelSize)
        {
            switch (depthBiasMode)
            {
                case ShadowDepthBiasMode.SlopeScale:
                    return normalBiasMode == ShadowNormalBiasMode.Caster ? -k_SlopeScaleCasterDepthBias * texelSize : 0.0f;
                case ShadowDepthBiasMode.Legacy:
                    return -depthBias * texelSize;
                default:
                    Debug.Assert(false, "Unexpected depth bias mode.");
                    return -depthBias * texelSize;
            }
        }

        internal static float GetCasterNormalBias(float normalBias, ShadowNormalBiasMode normalBiasMode, float texelSize, LightType lightType)
        {
            // Point lights get no caster normal bias, matching the Built-In RP convention:
            if (lightType == LightType.Point)
                return 0.0f;

            switch (normalBiasMode)
            {
                case ShadowNormalBiasMode.Caster:
                    return -normalBias * texelSize;
                case ShadowNormalBiasMode.Receiver:
                    return 0.0f;
                default:
                    Debug.Assert(false, "Unexpected normal bias mode.");
                    return -normalBias * texelSize;
            }
        }

        internal static float GetSoftShadowKernelRadius(ref VisibleLight shadowLight, bool supportsSoftShadows, UniversalRenderPipelineAsset asset)
        {
            if (!supportsSoftShadows || shadowLight.light.shadows != LightShadows.Soft)
                return 1.0f;

            SoftShadowQuality softShadowQuality = GetPerLightSoftShadowQuality(shadowLight.light, asset);

            // PCSS sizes its own filter, and widening the kernel would collapse its penumbra
            if (IsPCSSEnabled(shadowLight.lightType, softShadowQuality, asset))
                return 1.0f;

            return GetSoftShadowKernelRadius(softShadowQuality);
        }

        internal static SoftShadowQuality ResolveSoftShadowQuality(SoftShadowQuality softShadowQuality, UniversalRenderPipelineAsset asset)
        {
            if (softShadowQuality == SoftShadowQuality.UsePipelineSettings)
                softShadowQuality = asset != null ? asset.softShadowQuality : SoftShadowQuality.Medium;

            // Packing a quality of 0 would read as soft shadows off in the shader
            return softShadowQuality < SoftShadowQuality.Low ? SoftShadowQuality.Low : softShadowQuality;
        }

        internal static float GetSoftShadowKernelRadius(SoftShadowQuality softShadowQuality)
        {
            // TODO: depth and normal bias assume sample is no more than 1 texel away from shadowmap
            // This is not true with PCF. Ideally we need to do either
            // cone base bias (based on distance to center sample)
            // or receiver place bias based on derivatives.
            // For now we scale it by the PCF kernel size of non-mobile platforms (5x5)
            float kernelRadius = 2.5f;

            switch (softShadowQuality)
            {
                case SoftShadowQuality.High: kernelRadius = 3.5f; break; // 7x7
                case SoftShadowQuality.Medium: kernelRadius = 2.5f; break; // 5x5
                case SoftShadowQuality.Low: kernelRadius = 1.5f; break; // 3x3
                default: break;
            }

            return kernelRadius;
        }

        static SoftShadowQuality GetPerLightSoftShadowQuality(Light light, UniversalRenderPipelineAsset asset)
        {
            SoftShadowQuality softShadowQuality = SoftShadowQuality.UsePipelineSettings;
            if (SupportsPerLightSoftShadowQuality() && light != null && light.TryGetComponent(out UniversalAdditionalLightData additionalLightData))
                softShadowQuality = additionalLightData.softShadowQuality;

            return ResolveSoftShadowQuality(softShadowQuality, asset);
        }

        static bool IsPCSSEnabled(LightType lightType, SoftShadowQuality resolvedSoftShadowQuality, UniversalRenderPipelineAsset asset)
        {
            if (asset == null || !SupportsPerLightSoftShadowQuality() || resolvedSoftShadowQuality < SoftShadowQuality.High)
                return false;

            switch (lightType)
            {
                case LightType.Directional:
                    return asset.mainLightShadowsPCSS;
                case LightType.Spot:
                case LightType.Point:
                    return asset.additionalLightShadowsPCSS;
                default:
                    return false;
            }
        }

        internal static bool IsPCSSEnabled(ref VisibleLight shadowLight, UniversalAdditionalLightData additionalLightData, bool supportsSoftShadows, UniversalRenderPipelineAsset asset)
        {
            Light light = shadowLight.light;
            if (light == null || !supportsSoftShadows || light.shadows != LightShadows.Soft)
                return false;

            SoftShadowQuality softShadowQuality = additionalLightData != null ? additionalLightData.softShadowQuality : SoftShadowQuality.UsePipelineSettings;
            return IsPCSSEnabled(shadowLight.lightType, ResolveSoftShadowQuality(softShadowQuality, asset), asset);
        }

        // Takes the resolved quality packed into the y channel of the shadow params. Spot and point lights share the toggle.
        internal static bool IsAdditionalLightPCSSEnabled(float packedSoftShadowQuality, UniversalRenderPipelineAsset asset)
        {
            return IsPCSSEnabled(LightType.Spot, (SoftShadowQuality)(int)packedSoftShadowQuality, asset);
        }

        // Ignores the asset toggle so the inspector can explain that it is off
        internal static bool IsPCSSAvailableForLight(Light light, UniversalRenderPipelineAsset asset)
        {
            if (asset == null || light == null || !asset.supportsSoftShadows || light.shadows != LightShadows.Soft)
                return false;

            if (!SupportsPerLightSoftShadowQuality())
                return false;

            return GetPerLightSoftShadowQuality(light, asset) >= SoftShadowQuality.High;
        }

        internal static int GetPCSSTemporalFrameIndex(UniversalCameraData cameraData)
        {
            // The rotation only resolves under a temporal filter, and the wrap keeps InterleavedGradientNoise precise
            return cameraData.IsTemporalAAEnabled() ? TemporalAA.CalculateTaaFrameIndex(ref cameraData.taaSettings) & 1023 : 0;
        }

        internal static void GetPCSSPresetSampleCounts(PCSSSampleQuality quality, out int blockerSampleCount, out int filterSampleCount)
        {
            switch (quality)
            {
                case PCSSSampleQuality.Low:
                    blockerSampleCount = UniversalAdditionalLightData.k_LowSoftShadowBlockerSampleCount;
                    filterSampleCount = UniversalAdditionalLightData.k_LowSoftShadowFilterSampleCount;
                    break;

                case PCSSSampleQuality.High:
                    blockerSampleCount = UniversalAdditionalLightData.k_HighSoftShadowBlockerSampleCount;
                    filterSampleCount = UniversalAdditionalLightData.k_HighSoftShadowFilterSampleCount;
                    break;

                case PCSSSampleQuality.Medium:
                default:
                    Debug.Assert(quality == PCSSSampleQuality.Medium, "Only the Low, Medium and High PCSS presets have sample counts.");
                    blockerSampleCount = UniversalAdditionalLightData.k_DefaultSoftShadowBlockerSampleCount;
                    filterSampleCount = UniversalAdditionalLightData.k_DefaultSoftShadowFilterSampleCount;
                    break;
            }
        }

        static bool UsesCustomPCSSSettings(UniversalAdditionalLightData additionalLightData)
        {
            return additionalLightData != null && additionalLightData.softShadowPCSSSampleQuality == PCSSSampleQuality.Custom;
        }

        internal static void GetPCSSSampleCounts(UniversalAdditionalLightData additionalLightData, out int blockerSampleCount, out int filterSampleCount)
        {
            if (UsesCustomPCSSSettings(additionalLightData))
            {
                blockerSampleCount = additionalLightData.softShadowBlockerSampleCount;
                filterSampleCount = additionalLightData.softShadowFilterSampleCount;
            }
            else
            {
                PCSSSampleQuality quality = additionalLightData != null ? additionalLightData.softShadowPCSSSampleQuality : PCSSSampleQuality.Medium;
                GetPCSSPresetSampleCounts(quality, out blockerSampleCount, out filterSampleCount);
            }
        }

        internal static void ComputeAdditionalLightPCSSParams(float shapeRadius, UniversalAdditionalLightData additionalLightData, int sliceResolution, Matrix4x4 sliceProjectionMatrix, out Vector4 pcssParams, out Vector4 zBufferParams)
        {
            float softnessScale = additionalLightData != null ? additionalLightData.softShadowScale : UniversalAdditionalLightData.k_DefaultSoftShadowScale;
            float minFilterSize = additionalLightData != null ? additionalLightData.softShadowMinFilterSize : UniversalAdditionalLightData.k_DefaultSoftShadowMinFilterSize;
            GetPCSSSampleCounts(additionalLightData, out int blockerSampleCount, out int filterSampleCount);

            // Same fit as HDGpuLightsBuilder.GetNonDirectionalSoftness in HDRP, keep the two in sync
            float x = shapeRadius * softnessScale;
            float x2 = x * x;
            float softness = 0.02403461f + 3.452916f * x - 1.362672f * x2 + 0.6700115f * x2 * x + 0.2159474f * x2 * x2;
            softness /= 100.0f;
            softness *= sliceResolution / 512.0f;  // Make it resolution independent whereas the baseline is 512

            // Divided by 1000 so the user range is [0, 1], as in HDRP
            float minFilterRadius = minFilterSize * 0.001f;

            pcssParams = new Vector4(softness, minFilterRadius, Mathf.Clamp(blockerSampleCount, 1, k_PCSSMaxSampleCount), Mathf.Clamp(filterSampleCount, 1, k_PCSSMaxSampleCount));

            // From the projection, so the point light near plane clamp and guard angle are included
            float m22 = sliceProjectionMatrix.m22;
            float m23 = sliceProjectionMatrix.m23;
            float near = m23 / (m22 - 1.0f);
            float far = m23 / (m22 + 1.0f);
            zBufferParams = SystemInfo.usesReversedZBuffer
                ? new Vector4((far - near) / near, 1.0f, (far - near) / (near * far), 1.0f / far)
                : new Vector4((near - far) / near, far / near, (near - far) / (near * far), 1.0f / near);
        }

        internal static void ComputeMainLightPCSSParams(UniversalAdditionalLightData additionalLightData, ShadowSliceData[] cascadeSlices, int cascadeCount, int atlasWidth, int atlasHeight, ref MainLightShadowCascadeVectors params0, ref MainLightShadowCascadeVectors params1, ref MainLightShadowCascadeVectors atlas)
        {
            float angularDiameter = UniversalAdditionalLightData.k_DefaultSoftShadowAngularDiameter;
            float softnessScale = UniversalAdditionalLightData.k_DefaultSoftShadowScale;
            float maxPenumbraSize = UniversalAdditionalLightData.k_DefaultSoftShadowMaxPenumbraSize;
            float maxSamplingDistance = UniversalAdditionalLightData.k_DefaultSoftShadowMaxSamplingDistance;
            float minFilterSizeTexels = UniversalAdditionalLightData.k_DefaultSoftShadowMinFilterSizeTexels;
            float minFilterMaxAngularDiameter = UniversalAdditionalLightData.k_DefaultSoftShadowMinFilterMaxAngularDiameter;
            float blockerSearchAngularDiameter = UniversalAdditionalLightData.k_DefaultSoftShadowBlockerSearchAngularDiameter;
            float blockerSamplingClumpExponent = UniversalAdditionalLightData.k_DefaultSoftShadowBlockerSamplingClumpExponent;
            if (additionalLightData != null)
            {
                angularDiameter = additionalLightData.softShadowAngularDiameter;
                softnessScale = additionalLightData.softShadowScale;
                minFilterSizeTexels = additionalLightData.softShadowMinFilterSizeTexels;
            }

            if (UsesCustomPCSSSettings(additionalLightData))
            {
                maxPenumbraSize = additionalLightData.softShadowMaxPenumbraSize;
                maxSamplingDistance = additionalLightData.softShadowMaxSamplingDistance;
                minFilterMaxAngularDiameter = additionalLightData.softShadowMinFilterMaxAngularDiameter;
                blockerSearchAngularDiameter = additionalLightData.softShadowBlockerSearchAngularDiameter;
                blockerSamplingClumpExponent = additionalLightData.softShadowBlockerSamplingClumpExponent;
            }

            float lightAngularDiameter = softnessScale * angularDiameter;
            float halfTan = HalfAngularDiameterTangent(lightAngularDiameter);

            // The blocker search cone can only be widened, never narrowed below the cone of the light itself
            float blockerSearchTan = HalfAngularDiameterTangent(Mathf.Max(blockerSearchAngularDiameter, lightAngularDiameter));
            float minFilterTan = HalfAngularDiameterTangent(Mathf.Max(Mathf.Max(minFilterMaxAngularDiameter, blockerSearchAngularDiameter), lightAngularDiameter));

            // The shader takes the square root of the normalized range, so the authored exponent is halved
            float clumpExponent = 0.5f * blockerSamplingClumpExponent;
            float maxBlockerDistance = maxPenumbraSize / (2.0f * halfTan);

            float oneOverAtlasWidth = 1.0f / atlasWidth;
            float oneOverAtlasHeight = 1.0f / atlasHeight;

            for (int i = 0; i < cascadeCount; ++i)
            {
                // The culling matrices are OpenGL style while the shader uses [0, 1] depth
                Matrix4x4 projectionMatrix = cascadeSlices[i].projectionMatrix;
                float projectionDepthToRadialScale = 2.0f * Mathf.Abs(projectionMatrix.m00 / projectionMatrix.m22);
                float worldToDepthScale = 0.5f * Mathf.Abs(projectionMatrix.m22);

                float depth2RadialScale = halfTan * projectionDepthToRadialScale;

                params0[i] = new Vector4(depth2RadialScale, 1.0f / depth2RadialScale, maxBlockerDistance * worldToDepthScale, maxSamplingDistance * worldToDepthScale);
                params1[i] = new Vector4(minFilterSizeTexels, 1.0f / (minFilterTan * projectionDepthToRadialScale), 1.0f / (blockerSearchTan * projectionDepthToRadialScale), clumpExponent);
                atlas[i] = new Vector4(cascadeSlices[i].offsetX * oneOverAtlasWidth, cascadeSlices[i].offsetY * oneOverAtlasHeight, cascadeSlices[i].resolution * oneOverAtlasWidth, cascadeSlices[i].resolution * oneOverAtlasHeight);
            }

            // See _MainLightPCSSAtlas in GlobalShaderVariables.hlsl
            for (int i = cascadeCount; i < MainLightShadowCascadeVectors.count; ++i)
            {
                params0[i] = Vector4.zero;
                params1[i] = Vector4.zero;
                atlas[i] = Vector4.zero;
            }
        }

        internal static Vector4 GetMainLightPCSSScalars(UniversalAdditionalLightData additionalLightData, int tileResolution)
        {
            GetPCSSSampleCounts(additionalLightData, out int blockerSampleCount, out int filterSampleCount);

            // Tile texel size, since the shader compares the minimum filter radius in tile space
            return new Vector4(1.0f / tileResolution, Mathf.Clamp(blockerSampleCount, 1, k_PCSSMaxSampleCount), Mathf.Clamp(filterSampleCount, 1, k_PCSSMaxSampleCount), 0.0f);
        }

        static float HalfAngularDiameterTangent(float angularDiameterInDegrees)
        {
            // The shader divides by the radial scales this feeds
            return Mathf.Max(Mathf.Tan(0.5f * Mathf.Deg2Rad * angularDiameterInDegrees), 1e-6f);
        }

        /// <summary>
        /// Extract scale and bias from a fade distance to achieve a linear fading of the fade distance.
        /// </summary>
        /// <param name="fadeDistance">Distance at which object should be totally fade</param>
        /// <param name="border">Normalized distance of fade</param>
        /// <param name="scale">[OUT] Slope of the fading on the fading part</param>
        /// <param name="bias">[OUT] Ordinate of the fading part at abscissa 0</param>
        internal static void GetScaleAndBiasForLinearDistanceFade(float fadeDistance, float border, out float scale, out float bias)
        {
            // To avoid division from zero
            // This values ensure that fade within cascade will be 0 and outside 1
            if (border < 0.0001f)
            {
                float multiplier = 1000f; // To avoid blending if difference is in fractions
                scale = multiplier;
                bias = -fadeDistance * multiplier;
                return;
            }

            border = 1 - border;
            border *= border;

            // Fade with distance calculation is just a linear fade from 90% of fade distance to fade distance. 90% arbitrarily chosen but should work well enough.
            float distanceFadeNear = border * fadeDistance;
            scale = 1.0f / (fadeDistance - distanceFadeNear);
            bias = -distanceFadeNear / (fadeDistance - distanceFadeNear);
        }

        /// <summary>
        /// Sets up the shadow bias, light direction and position for rendering.
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="shadowLight"></param>
        /// <param name="shadowBias"></param>
        public static void SetupShadowCasterConstantBuffer(CommandBuffer cmd, ref VisibleLight shadowLight, Vector4 shadowBias)
        {
            SetupShadowCasterConstantBuffer(CommandBufferHelpers.GetRasterCommandBuffer(cmd), ref shadowLight, shadowBias);
        }

        internal static void SetupShadowCasterConstantBuffer(RasterCommandBuffer cmd, ref VisibleLight shadowLight, Vector4 shadowBias)
        {
            SetShadowBias(cmd, shadowBias);

            // Light direction is currently used in shadow caster pass to apply shadow normal offset (normal bias).
            Vector3 lightDirection = -shadowLight.localToWorldMatrix.GetColumn(2);
            SetLightDirection(cmd, lightDirection);

            // For punctual lights, computing light direction at each vertex position provides more consistent results (shadow shape does not change when "rotating the point light" for example)
            Vector3 lightPosition = shadowLight.localToWorldMatrix.GetColumn(3);
            SetLightPosition(cmd, lightPosition);
        }

        internal static void SetShadowBias(RasterCommandBuffer cmd, Vector4 shadowBias)
        {
            cmd.SetGlobalVector(ShaderPropertyId.shadowBias, shadowBias);
        }

        internal static void SetLightDirection(RasterCommandBuffer cmd, Vector3 lightDirection)
        {
            cmd.SetGlobalVector(ShaderPropertyId.lightDirection, new Vector4(lightDirection.x, lightDirection.y, lightDirection.z, 0.0f));
        }

        internal static void SetLightPosition(RasterCommandBuffer cmd, Vector3 lightPosition)
        {
            cmd.SetGlobalVector(ShaderPropertyId.lightPosition, new Vector4(lightPosition.x, lightPosition.y, lightPosition.z, 1.0f));
        }

        internal static void SetCameraPosition(RasterCommandBuffer cmd, Vector3 worldSpaceCameraPos)
        {
            cmd.SetGlobalVector(ShaderPropertyId.worldSpaceCameraPos, worldSpaceCameraPos);
        }

        internal static bool ShouldUseShadowDepthClamp(bool isDirectional)
        {
            return isDirectional && SystemInfo.supportsDepthClamp;
        }

        private static RenderTextureDescriptor GetTemporaryShadowTextureDescriptor(int width, int height, int bits)
        {
            var format = GraphicsFormatUtility.GetDepthStencilFormat(bits, 0);
            RenderTextureDescriptor rtd = new RenderTextureDescriptor(width, height, GraphicsFormat.None, format);
            rtd.shadowSamplingMode = RenderingUtils.SupportsRenderTextureFormat(RenderTextureFormat.Shadowmap) ? ShadowSamplingMode.CompareDepths : ShadowSamplingMode.None;
            return rtd;
        }

        /// <summary>
        /// Gets a temporary render texture for shadows.
        /// This function has been deprecated. Use AllocShadowRT or ShadowRTReAllocateIfNeeded instead.
        /// </summary>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="bits">The number of depth bits.</param>
        /// <returns>A shadow render texture.</returns>
        [Obsolete("Use AllocShadowRT or ShadowRTReAllocateIfNeeded. #from(2022.1) #breakingFrom(2023.1)", true)]
        public static RenderTexture GetTemporaryShadowTexture(int width, int height, int bits)
        {
            var rtd = GetTemporaryShadowTextureDescriptor(width, height, bits);
            var shadowTexture = RenderTexture.GetTemporary(rtd);
            shadowTexture.filterMode = m_ForceShadowPointSampling ? FilterMode.Point : FilterMode.Bilinear;
            shadowTexture.wrapMode = TextureWrapMode.Clamp;
            return shadowTexture;
        }

        /// <summary>
        /// Return true if handle does not match the requirements
        /// </summary>
        /// <param name="handle">RTHandle to check (can be null).</param>
        /// <param name="width">Width of the RTHandle to match.</param>
        /// <param name="height">Height of the RTHandle to match.</param>
        /// <param name="bits">Depth bits of the RTHandle to match.</param>
        /// <param name="anisoLevel">Anisotropic filtering level of the RTHandle to match.</param>
        /// <param name="mipMapBias">Bias applied to mipmaps during filtering of the RTHandle to match.</param>
        /// <param name="name">Name of the RTHandle of the RTHandle to match.</param>
        /// <returns>If the RTHandle needs to be re-allocated</returns>
        public static bool ShadowRTNeedsReAlloc(RTHandle handle, int width, int height, int bits, int anisoLevel, float mipMapBias, string name)
        {
            if (handle == null || handle.rt == null)
                return true;
            var descriptor = GetTemporaryShadowTextureDescriptor(width, height, bits);
            if (m_ForceShadowPointSampling)
            {
                if (handle.rt.filterMode != FilterMode.Point)
                    return true;
            }
            else
            {
                if (handle.rt.filterMode != FilterMode.Bilinear)
                    return true;
            }

            TextureDesc shadowDesc = RTHandleResourcePool.CreateTextureDesc(descriptor, TextureSizeMode.Explicit, anisoLevel, mipMapBias, m_ForceShadowPointSampling ? FilterMode.Point : FilterMode.Bilinear, TextureWrapMode.Clamp, name);
            return RenderingUtils.RTHandleNeedsReAlloc(handle, shadowDesc, false);
        }

        /// <summary>
        /// Allocate a Shadow Map
        /// </summary>
        /// <param name="width">Width of the Shadow Map.</param>
        /// <param name="height">Height of the Shadow Map.</param>
        /// <param name="bits">Minimum depth bits of the Shadow Map.</param>
        /// <param name="anisoLevel">Anisotropic filtering level of the Shadow Map.</param>
        /// <param name="mipMapBias">Bias applied to mipmaps during filtering of the Shadow Map.</param>
        /// <param name="name">Name of the Shadow Map.</param>
        /// <returns>If an RTHandle for the Shadow Map</returns>
        public static RTHandle AllocShadowRT(int width, int height, int bits, int anisoLevel, float mipMapBias, string name)
        {
            var rtd = GetTemporaryShadowTextureDescriptor(width, height, bits);
            return RTHandles.Alloc(rtd, m_ForceShadowPointSampling ? FilterMode.Point : FilterMode.Bilinear, TextureWrapMode.Clamp, isShadowMap: true, name: name);
        }

        /// <summary>
        /// Allocate a Shadow Map or re-allocate if it doesn't match requirements.
        /// For use only if the map requirements changes at runtime.
        /// </summary>
        /// <param name="handle">RTHandle to check (can be null).</param>
        /// <param name="width">Width of the Shadow Map.</param>
        /// <param name="height">Height of the Shadow Map.</param>
        /// <param name="bits">Minimum depth bits of the Shadow Map.</param>
        /// <param name="anisoLevel">Anisotropic filtering level of the Shadow Map.</param>
        /// <param name="mipMapBias">Bias applied to mipmaps during filtering of the Shadow Map.</param>
        /// <param name="name">Name of the Shadow Map.</param>
        /// <returns>If the RTHandle was re-allocated</returns>
        public static bool ShadowRTReAllocateIfNeeded(ref RTHandle handle, int width, int height, int bits, int anisoLevel = 1, float mipMapBias = 0, string name = "")
        {
            if (ShadowRTNeedsReAlloc(handle, width, height, bits, anisoLevel, mipMapBias, name))
            {
                handle?.Release();
                handle = AllocShadowRT(width, height, bits, anisoLevel, mipMapBias, name);
                return true;
            }
            return false;
        }

        static Matrix4x4 GetShadowTransform(Matrix4x4 proj, Matrix4x4 view)
        {
            // Currently CullResults ComputeDirectionalShadowMatricesAndCullingPrimitives doesn't
            // apply z reversal to projection matrix. We need to do it manually here.
            if (SystemInfo.usesReversedZBuffer)
            {
                proj.m20 = -proj.m20;
                proj.m21 = -proj.m21;
                proj.m22 = -proj.m22;
                proj.m23 = -proj.m23;
            }

            Matrix4x4 worldToShadow = proj * view;

            var textureScaleAndBias = Matrix4x4.identity;
            textureScaleAndBias.m00 = 0.5f;
            textureScaleAndBias.m11 = 0.5f;
            textureScaleAndBias.m22 = 0.5f;
            textureScaleAndBias.m03 = 0.5f;
            textureScaleAndBias.m23 = 0.5f;
            textureScaleAndBias.m13 = 0.5f;
            // textureScaleAndBias maps texture space coordinates from [-1,1] to [0,1]

            // Apply texture scale and offset to save a MAD in shader.
            return textureScaleAndBias * worldToShadow;
        }

        internal static void GetShadowStrengthAndSoftShadows(Light light, UniversalAdditionalLightData additionalLightData, bool softShadowsEnabled, UniversalRenderPipelineAsset asset, out float shadowStrength, out float softShadows)
        {
            softShadows = softShadowsEnabled ? 1.0f : 0.0f;
            shadowStrength = light.shadowStrength;
            var softShadowQuality = additionalLightData != null ? additionalLightData.softShadowQuality : SoftShadowQuality.UsePipelineSettings;
            softShadows *= (int)ResolveSoftShadowQuality(softShadowQuality, asset);
        }

        internal static void GetMainLightShadowParams(Light light, bool softShadowsEnabled, out float softShadows, out float shadowStrength)
        {
            if (light == null)
            {
                softShadows = softShadowsEnabled ? 1.0f : 0.0f;
                shadowStrength = 1.0f;
                return;
            }

            light.TryGetComponent(out UniversalAdditionalLightData additionalLightData);
            GetShadowStrengthAndSoftShadows(light, additionalLightData, softShadowsEnabled, UniversalRenderPipeline.asset, out shadowStrength, out softShadows);
        }

        internal static Vector4 GetAdditionalLightShadowParams(Light light, bool softShadowsEnabled, float lightTypeIdentifier, int perLightFirstShadowSliceIndex)
        {
            Debug.Assert(light != null);
            light.TryGetComponent(out UniversalAdditionalLightData additionalLightData);
            return GetAdditionalLightShadowParams(light, additionalLightData, softShadowsEnabled, lightTypeIdentifier, perLightFirstShadowSliceIndex);
        }

        internal static Vector4 GetAdditionalLightShadowParams(Light light, UniversalAdditionalLightData additionalLightData, bool softShadowsEnabled, float lightTypeIdentifier, int perLightFirstShadowSliceIndex)
        {
            Debug.Assert(light != null);
            GetShadowStrengthAndSoftShadows(light, additionalLightData, softShadowsEnabled, UniversalRenderPipeline.asset, out float shadowStrength, out float softShadows);
            return new Vector4(shadowStrength, softShadows, lightTypeIdentifier, perLightFirstShadowSliceIndex);
        }

        internal static bool ShouldEnableKeywordForEmptyShadowmap(bool stripShadowsOffVariants, bool shadowsEnabled)
        {
            // Enable the realtime shadow keyword only when realtime shadows are enabled (so the shadows-on
            // variant is kept in the build) and the shadows-off variants are stripped (so it is the
            // only one available). The zeroed params below then make that variant resolve to no shadow.
            return stripShadowsOffVariants && shadowsEnabled;
        }

        // Decides whether the empty (keyword-only) shadow pass should write real shadow params instead of the
        // zeroed defaults. They are only needed when the shadows-off variants are stripped and either realtime
        // shadows are enabled or a light samples shadowmasks (for example so its shadow strength can be used).
        internal static bool ShouldComputeEmptyShadowmapParams(bool stripShadowsOffVariants, bool shadowsEnabled, bool lightCastsBakedShadows)
        {
            // Realtime shadows are off but the light still samples baked/mixed (shadowmask) shadows.
            bool usesBakedShadows = !shadowsEnabled && lightCastsBakedShadows;
            return stripShadowsOffVariants && (shadowsEnabled || usesBakedShadows);
        }

        internal static bool SupportsPerLightSoftShadowQuality()
        {
            bool supportsPerLightSoftShadowQuality = true;
            #if ENABLE_VR && ENABLE_XR_MODULE
            #if PLATFORM_WINRT || PLATFORM_ANDROID
                // We are using static branches on Quest2 + HL for performance reasons
                supportsPerLightSoftShadowQuality = !PlatformAutoDetect.isXRMobile;
            #endif
            #endif
            return supportsPerLightSoftShadowQuality;
        }

        internal static void SetPerLightSoftShadowKeyword(RasterCommandBuffer cmd, UniversalShadowData shadowData, bool hasSoftShadows)
        {
            if (SupportsPerLightSoftShadowQuality())
                SetPerLightQualitySoftShadowKeywords(cmd, shadowData, hasSoftShadows);
            else
                // keep static quality keywords in lockstep with the per-light shadow keywords (UUM-147509)
                SetSoftShadowQualityShaderKeywords(cmd, shadowData, hasSoftShadows);
        }

        // PCSS is layered on the per-light soft shadow keyword, so shaders without it keep the PCF filters
        static void SetPerLightQualitySoftShadowKeywords(RasterCommandBuffer cmd, UniversalShadowData shadowData, bool enabled)
        {
            cmd.SetKeyword(ShaderGlobalKeywords.SoftShadows, enabled);
            cmd.SetKeyword(ShaderGlobalKeywords.ShadowsPCSS, enabled && (shadowData.isMainLightPCSSEnabled || shadowData.isAdditionalLightsPCSSEnabled));
        }

        internal static void SetSoftShadowQualityShaderKeywords(RasterCommandBuffer cmd, UniversalShadowData shadowData)
        {
            SetSoftShadowQualityShaderKeywords(cmd, shadowData, shadowData.isKeywordSoftShadowsEnabled);
        }

        internal static void SetSoftShadowQualityShaderKeywords(RasterCommandBuffer cmd, UniversalShadowData shadowData, bool enabled)
        {
            if (SupportsPerLightSoftShadowQuality())
            {
                SetPerLightQualitySoftShadowKeywords(cmd, shadowData, enabled);
                cmd.SetKeyword(ShaderGlobalKeywords.SoftShadowsLow, false);
                cmd.SetKeyword(ShaderGlobalKeywords.SoftShadowsMedium, false);
                cmd.SetKeyword(ShaderGlobalKeywords.SoftShadowsHigh, false);
            }
            else
            {
                // XR mobile uses the static quality keywords, which never take the PCSS path
                cmd.SetKeyword(ShaderGlobalKeywords.SoftShadows, enabled);
                cmd.SetKeyword(ShaderGlobalKeywords.ShadowsPCSS, false);
                if (enabled && UniversalRenderPipeline.asset?.softShadowQuality == SoftShadowQuality.Low)
                {
                    cmd.SetKeyword(ShaderGlobalKeywords.SoftShadowsLow, true);
                    cmd.SetKeyword(ShaderGlobalKeywords.SoftShadowsMedium, false);
                    cmd.SetKeyword(ShaderGlobalKeywords.SoftShadowsHigh, false);
                    cmd.SetKeyword(ShaderGlobalKeywords.SoftShadows, false);
                }
                else if (enabled && UniversalRenderPipeline.asset?.softShadowQuality == SoftShadowQuality.Medium)
                {
                    cmd.SetKeyword(ShaderGlobalKeywords.SoftShadowsLow, false);
                    cmd.SetKeyword(ShaderGlobalKeywords.SoftShadowsMedium, true);
                    cmd.SetKeyword(ShaderGlobalKeywords.SoftShadowsHigh, false);
                    cmd.SetKeyword(ShaderGlobalKeywords.SoftShadows, false);
                }
                else if (enabled && UniversalRenderPipeline.asset?.softShadowQuality == SoftShadowQuality.High)
                {
                    cmd.SetKeyword(ShaderGlobalKeywords.SoftShadowsLow, false);
                    cmd.SetKeyword(ShaderGlobalKeywords.SoftShadowsMedium, false);
                    cmd.SetKeyword(ShaderGlobalKeywords.SoftShadowsHigh, true);
                    cmd.SetKeyword(ShaderGlobalKeywords.SoftShadows, false);
                }
                else
                {
                    cmd.SetKeyword(ShaderGlobalKeywords.SoftShadowsLow, false);
                    cmd.SetKeyword(ShaderGlobalKeywords.SoftShadowsMedium, false);
                    cmd.SetKeyword(ShaderGlobalKeywords.SoftShadowsHigh, false);
                }
            }
        }

        internal static bool IsValidShadowCastingLight(UniversalLightData lightData, int i)
        {
            ref VisibleLight shadowLight = ref lightData.visibleLights.UnsafeElementAt(i);
            Light light = shadowLight.light;

            if (light == null)
                return false;

            return IsValidShadowCastingLight(lightData, i, shadowLight.lightType, light.shadows, light.shadowStrength);
        }

        internal static bool IsValidShadowCastingLight(UniversalLightData lightData, int i, LightType lightType, LightShadows lightShadows, float shadowStrength)
        {
            if (i == lightData.mainLightIndex)
                return false;

            // Directional and light shadows are not supported in the shadow map atlas
            if (lightType == LightType.Directional)
                return false;

            return lightShadows != LightShadows.None && shadowStrength > 0.0f;
        }

        internal static int GetPunctualLightShadowSlicesCount(in LightType lightType)
        {
            switch (lightType)
            {
                case LightType.Spot:
                    return 1;
                case LightType.Point:
                    return 6;
                default:
                    return 0;
            }
        }

        internal static bool FastApproximately(float a, float b)
        {
            return Mathf.Abs(a - b) < 0.000001f;
        }

        internal static bool FastApproximately(Vector4 a, Vector4 b)
        {
            return FastApproximately(a.x, b.x)
                && FastApproximately(a.y, b.y)
                && FastApproximately(a.z, b.z)
                && FastApproximately(a.w, b.w);
        }

        internal const int kMinimumPunctualLightHardShadowResolution = 8;
        internal const int kMinimumPunctualLightSoftShadowResolution = 16;
        // Minimal shadow map resolution required to have meaningful shadows visible during lighting
        internal static int MinimalPunctualLightShadowResolution(bool softShadow)
        {
            return softShadow ? kMinimumPunctualLightSoftShadowResolution : kMinimumPunctualLightHardShadowResolution;
        }

        static bool s_WarnedNoShadowmapStencilFormat;

        internal static GraphicsFormat GetShadowmapDepthStencilFormat(bool stencilBuffer)
        {
            if (stencilBuffer)
            {
                // If we need stencil bits, pick whichever supported format is the smallest
                if (SystemInfo.IsFormatSupported(GraphicsFormat.D16_UNorm_S8_UInt, GraphicsFormatUsage.Render))
                    return GraphicsFormat.D16_UNorm_S8_UInt;
                if (SystemInfo.IsFormatSupported(GraphicsFormat.D24_UNorm_S8_UInt, GraphicsFormatUsage.Render))
                    return GraphicsFormat.D24_UNorm_S8_UInt;
                if (SystemInfo.IsFormatSupported(GraphicsFormat.D32_SFloat_S8_UInt, GraphicsFormatUsage.Render))
                    return GraphicsFormat.D32_SFloat_S8_UInt;

                // No depth-stencil format is supported here: the shadowmap falls back to depth-only and
                // stencil writes silently no-op. Warn once so an enabled Shadowmap Stencil toggle that
                // can't be honored on this device isn't invisible.
                if (!s_WarnedNoShadowmapStencilFormat)
                {
                    s_WarnedNoShadowmapStencilFormat = true;
                    Debug.LogWarning("Shadowmap Stencil is enabled but this device supports no depth-stencil shadowmap format; falling back to a depth-only shadowmap (stencil writes will have no effect).");
                }
            }

            return GraphicsFormatUtility.GetDepthStencilFormat(16);
        }
    }
}
