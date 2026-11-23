using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Container class for various data used for shadows in URP.
    /// </summary>
    public class UniversalShadowData : ContextItem
    {
        /// <summary>
        /// True if main light shadows are enabled.
        /// </summary>
        public bool supportsMainLightShadows;

        /// <summary>
        /// True if main lights shadows are enabled in the URP Asset
        /// </summary>
        internal bool mainLightShadowsEnabled;

        /// <summary>
        /// The width of the main light shadow map.
        /// </summary>
        public int mainLightShadowmapWidth;

        /// <summary>
        /// The height of the main light shadow map.
        /// </summary>
        public int mainLightShadowmapHeight;

        /// <summary>
        /// The number of shadow cascades.
        /// </summary>
        public int mainLightShadowCascadesCount;

        /// <summary>
        /// The split between cascades.
        /// </summary>
        public Vector3 mainLightShadowCascadesSplit;

        /// <summary>
        /// Main light last cascade shadow fade border.
        /// Value represents the width of shadow fade that ranges from 0 to 1.
        /// Where value 0 is used for no shadow fade.
        /// </summary>
        public float mainLightShadowCascadeBorder;

        /// <summary>
        /// True if additional lights shadows are enabled.
        /// </summary>
        public bool supportsAdditionalLightShadows;

        /// <summary>
        /// True if additional lights shadows are enabled in the URP Asset
        /// </summary>
        internal bool additionalLightShadowsEnabled;

        /// <summary>
        /// The width of the additional light shadow map.
        /// </summary>
        public int additionalLightsShadowmapWidth;

        /// <summary>
        /// The height of the additional light shadow map.
        /// </summary>
        public int additionalLightsShadowmapHeight;

        /// <summary>
        /// True if soft shadows are enabled.
        /// </summary>
        public bool supportsSoftShadows;

        /// <summary>
        /// The number of bits used.
        /// </summary>
        [Obsolete("Use mainLightShadowmapDepthBits or additionalLightsShadowmapDepthBits. #from(7000.0)", false)]
        public int shadowmapDepthBufferBits;

        /// <summary>
        /// The depth buffer precision of the main light shadow map.
        /// </summary>
        public DepthBits mainLightShadowmapDepthBits;

        /// <summary>
        /// The depth buffer precision of the additional lights shadow map atlas.
        /// </summary>
        public DepthBits additionalLightsShadowmapDepthBits;

        /// <summary>
        /// A list of shadow bias.
        /// </summary>
        public List<Vector4> bias;

        /// <summary>
        /// A list of resolution for the shadow maps.
        /// </summary>
        public List<int> resolution;

        internal ShadowDepthBiasMode depthBiasMode;
        internal ShadowNormalBiasMode normalBiasMode;
        internal GraphicsFormat mainLightShadowmapFormat;
        internal GraphicsFormat additionalLightsShadowmapFormat;
        internal float softShadowKernelRadiusPercent;
        internal bool mainLightReceiverNormalBiasSkipped;

        internal bool isKeywordAdditionalLightShadowsEnabled;
        internal bool isKeywordSoftShadowsEnabled;
        internal bool isMainLightPCSSEnabled;
        internal bool isAdditionalLightsPCSSEnabled;
        internal int mainLightShadowResolution;
        internal int mainLightRenderTargetWidth;
        internal int mainLightRenderTargetHeight;

        internal NativeArray<URPLightShadowCullingInfos> visibleLightsShadowCullingInfos;
        internal AdditionalLightsShadowAtlasLayout shadowAtlasLayout;

        // Owned by AdditionalLightsShadowCasterPass
        internal GraphicsBuffer emptyAdditionalLightShadowsBuffer;
        internal GraphicsBuffer extraForwardAdditionalLightShadowsBuffer;
        internal GraphicsBuffer extraForwardAdditionalLightPCSSBuffer;
        internal Vector4[] extraForwardAdditionalLightShadowParams;
        internal Vector4[] extraForwardAdditionalLightReceiverNormalBiases;
        internal short[] visibleLightIndexToAdditionalLightIndex;
        internal bool[] visibleLightIndexToIsCastingShadows;

        internal ShadowPassGroup shadowPassGroup;

        // True when this camera pass should reuse a previously rendered shadow map. Currently only used with XR QuadViews.
        internal bool useCachedShadowMap;
        // True when shadow map caching is enabled for the camera. Currently only used with XR QuadViews.
        internal bool shadowMapCachingEnabled;

        /// <inheritdoc/>
        public override void Reset()
        {
            supportsMainLightShadows = false;
            mainLightShadowmapWidth = 0;
            mainLightShadowmapHeight = 0;
            mainLightShadowCascadesCount = 0;
            mainLightShadowCascadesSplit = Vector3.zero;
            mainLightShadowCascadeBorder = 0.0f;
            supportsAdditionalLightShadows = false;
            additionalLightsShadowmapWidth = 0;
            additionalLightsShadowmapHeight = 0;
            supportsSoftShadows = false;
#pragma warning disable 618 // Obsolete warning
            shadowmapDepthBufferBits = 0;
#pragma warning restore 618 // Obsolete warning
            mainLightShadowmapDepthBits = DepthBits.None;
            additionalLightsShadowmapDepthBits = DepthBits.None;
            mainLightShadowmapFormat = GraphicsFormat.None;
            additionalLightsShadowmapFormat = GraphicsFormat.None;
            bias?.Clear();
            resolution?.Clear();
            depthBiasMode = ShadowDepthBiasMode.Legacy;
            normalBiasMode = ShadowNormalBiasMode.Caster;
            softShadowKernelRadiusPercent = 0.0f;
            mainLightReceiverNormalBiasSkipped = false;

            isKeywordAdditionalLightShadowsEnabled = false;
            isKeywordSoftShadowsEnabled = false;
            isMainLightPCSSEnabled = false;
            isAdditionalLightsPCSSEnabled = false;
            mainLightShadowResolution = 0;
            mainLightRenderTargetWidth = 0;
            mainLightRenderTargetHeight = 0;

            visibleLightsShadowCullingInfos = default;
            shadowAtlasLayout = default;
            shadowPassGroup = null;
            visibleLightIndexToAdditionalLightIndex = null;
            visibleLightIndexToIsCastingShadows = null;

            emptyAdditionalLightShadowsBuffer = null;
            extraForwardAdditionalLightShadowsBuffer = null;
            extraForwardAdditionalLightPCSSBuffer = null;
            extraForwardAdditionalLightShadowParams = null;
            extraForwardAdditionalLightReceiverNormalBiases = null;

            useCachedShadowMap = false;
            shadowMapCachingEnabled = false;
        }
    }
}
