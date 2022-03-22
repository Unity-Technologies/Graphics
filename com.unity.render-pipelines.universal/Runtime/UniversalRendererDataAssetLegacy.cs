using System;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Scripting.APIUpdating;


namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Class containing resources needed for the <c>UniversalRenderer</c>.
    /// </summary>
    [Serializable, ReloadGroup, ExcludeFromPreset]
    [URPHelpURL("urp-universal-renderer")]
    [Obsolete("UniversalRenderer is no longer an asset.")]
    //[MovedFrom(true, sourceClassName: "UniversalRendererData")]
    public class UniversalRendererDataAssetLegacy : ScriptableRendererDataAssetLegacy, ISerializationCallbackReceiver
    {
        protected override ScriptableRenderer Create()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Class containing shader resources used in URP.
        /// </summary>
        [Serializable, ReloadGroup]
        public sealed class ShaderResources
        {
            /// <summary>
            /// Blit shader.
            /// </summary>
            [Reload("Shaders/Utils/Blit.shader")]
            public Shader blitPS;

            /// <summary>
            /// Copy Depth shader.
            /// </summary>
            [Reload("Shaders/Utils/CopyDepth.shader")]
            public Shader copyDepthPS;

            /// <summary>
            /// Screen Space Shadows shader.
            /// </summary>
            [Obsolete("Obsolete, this feature will be supported by new 'ScreenSpaceShadows' renderer feature")]
            public Shader screenSpaceShadowPS;

            /// <summary>
            /// Sampling shader.
            /// </summary>
            [Reload("Shaders/Utils/Sampling.shader")]
            public Shader samplingPS;

            /// <summary>
            /// Stencil Deferred shader.
            /// </summary>
            [Reload("Shaders/Utils/StencilDeferred.shader")]
            public Shader stencilDeferredPS;

            /// <summary>
            /// Fallback error shader.
            /// </summary>
            [Reload("Shaders/Utils/FallbackError.shader")]
            public Shader fallbackErrorPS;

            /// <summary>
            /// Fallback loading shader.
            /// </summary>
            [Reload("Shaders/Utils/FallbackLoading.shader")]
            public Shader fallbackLoadingPS;

            // Core blitter shaders, adapted from HDRP
            // TODO: move to core and share with HDRP
            [Reload("Shaders/Utils/CoreBlit.shader"), SerializeField]
            internal Shader coreBlitPS;
            [Reload("Shaders/Utils/CoreBlitColorAndDepth.shader"), SerializeField]
            internal Shader coreBlitColorAndDepthPS;

            /// <summary>
            /// Camera Motion Vectors shader.
            /// </summary>
            [Reload("Shaders/CameraMotionVectors.shader")]
            public Shader cameraMotionVector;

            /// <summary>
            /// Object Motion Vectors shader.
            /// </summary>
            [Reload("Shaders/ObjectMotionVectors.shader")]
            public Shader objectMotionVector;
        }

        /// <summary>
        /// Resources needed for Post Processing.
        /// </summary>
        public PostProcessData postProcessData = null;

#if ENABLE_VR && ENABLE_XR_MODULE
        /// <summary>
        /// Shader resources needed in URP for XR.
        /// </summary>
        [Reload("Runtime/Data/XRSystemData.asset")]
        public XRSystemData xrSystemData = null;
#endif

        /// <summary>
        /// Shader resources used in URP.
        /// </summary>
        public ShaderResources shaders = null;

        const int k_LatestAssetVersion = 2;
        [SerializeField] int m_AssetVersion = 0;
        [SerializeField] LayerMask m_OpaqueLayerMask = -1;
        [SerializeField] LayerMask m_TransparentLayerMask = -1;
        [SerializeField] StencilStateData m_DefaultStencilState = new StencilStateData() { passOperation = StencilOp.Replace }; // This default state is compatible with deferred renderer.
        [SerializeField] bool m_ShadowTransparentReceive = true;
        [SerializeField] RenderingMode m_RenderingMode = RenderingMode.Forward;
        [SerializeField] DepthPrimingMode m_DepthPrimingMode = DepthPrimingMode.Disabled; // Default disabled because there are some outstanding issues with Text Mesh rendering.
        [SerializeField] CopyDepthMode m_CopyDepthMode = CopyDepthMode.AfterTransparents;
        [SerializeField] bool m_AccurateGbufferNormals = false;
        [SerializeField] bool m_ClusteredRendering = false;
        const TileSize k_DefaultTileSize = TileSize._32;
        [SerializeField] TileSize m_TileSize = k_DefaultTileSize;
        [SerializeField] IntermediateTextureMode m_IntermediateTextureMode = IntermediateTextureMode.Always;


        /// <inheritdoc/>
        protected override void OnEnable()
        {
            base.OnEnable();

            // Upon asset creation, OnEnable is called and `shaders` reference is not yet initialized
            // We need to call the OnEnable for data migration when updating from old versions of UniversalRP that
            // serialized resources in a different format. Early returning here when OnEnable is called
            // upon asset creation is fine because we guarantee new assets get created with all resources initialized.
            if (shaders == null)
                return;

            ReloadAllNullProperties();
        }

        private void ReloadAllNullProperties()
        {
#if UNITY_EDITOR
            ResourceReloader.TryReloadAllNullIn(this, UniversalRenderPipelineAsset.packagePath);

            if (postProcessData != null)
                ResourceReloader.TryReloadAllNullIn(postProcessData, UniversalRenderPipelineAsset.packagePath);

#if ENABLE_VR && ENABLE_XR_MODULE
            ResourceReloader.TryReloadAllNullIn(xrSystemData, UniversalRenderPipelineAsset.packagePath);
#endif
#endif
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            m_AssetVersion = k_LatestAssetVersion;
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (m_AssetVersion <= 1)
            {
                // To avoid breaking existing projects, keep the old AfterOpaques behaviour. The new AfterTransparents default will only apply to new projects.
                m_CopyDepthMode = CopyDepthMode.AfterOpaques;
            }


            m_AssetVersion = k_LatestAssetVersion;
        }

        //TODO: Upgrading up sub assets to the URPAsset should be run before upgrading.
        protected override ScriptableRendererData UpgradeRendererWithoutAsset(UniversalRenderPipelineAsset URPAsset)
        {
            UniversalRendererData rendererData = new UniversalRendererData();
            //UniversalRenderPipelineAsset settings:
            rendererData.supportsCameraDepthTexture = URPAsset.m_RequireDepthTexture;
            rendererData.supportsCameraOpaqueTexture = URPAsset.m_RequireOpaqueTexture;

            rendererData.mainLightRenderingMode = URPAsset.m_MainLightRenderingMode;
            rendererData.supportsMainLightShadows = URPAsset.m_MainLightShadowsSupported;
            rendererData.mainLightShadowmapResolution = (int)URPAsset.m_MainLightShadowmapResolution;

            rendererData.additionalLightsRenderingMode = URPAsset.m_MainLightRenderingMode;
            rendererData.SetAdditionalLightsPerObjectLimit(URPAsset.m_AdditionalLightsPerObjectLimit);
            rendererData.supportsAdditionalLightShadows = URPAsset.m_AdditionalLightShadowsSupported;

            rendererData.additionalLightsShadowmapResolution = (int)URPAsset.m_AdditionalLightsShadowmapResolution;
            rendererData.additionalLightsShadowResolutionTierLow = URPAsset.m_AdditionalLightsShadowResolutionTierLow;
            rendererData.additionalLightsShadowResolutionTierMedium = URPAsset.m_AdditionalLightsShadowResolutionTierMedium;
            rendererData.additionalLightsShadowResolutionTierHigh = URPAsset.m_AdditionalLightsShadowResolutionTierHigh;
            rendererData.shadowDistance = URPAsset.m_ShadowDistance;
            rendererData.shadowCascadeCount = URPAsset.m_ShadowCascadeCount;
            rendererData.cascade2Split = URPAsset.m_Cascade2Split;
            rendererData.cascade3Split = URPAsset.m_Cascade3Split;
            rendererData.cascade4Split = URPAsset.m_Cascade4Split;
            rendererData.cascadeBorder = URPAsset.m_CascadeBorder;
            rendererData.shadowDepthBias = URPAsset.m_ShadowDepthBias;
            rendererData.shadowNormalBias = URPAsset.m_ShadowNormalBias;
            rendererData.supportsSoftShadows = URPAsset.m_SoftShadowsSupported;
            rendererData.conservativeEnclosingSphere = URPAsset.m_ConservativeEnclosingSphere;
            rendererData.numIterationsEnclosingSphere = URPAsset.m_NumIterationsEnclosingSphere;
            rendererData.supportsMixedLighting = URPAsset.m_MixedLightingSupported;
            rendererData.supportsLightLayers = URPAsset.m_SupportsLightLayers;
            rendererData.reflectionProbeBlending = URPAsset.m_ReflectionProbeBlending;
            rendererData.reflectionProbeBoxProjection = URPAsset.m_ReflectionProbeBoxProjection;
            rendererData.SetAdditionalLightsCookieResolution(URPAsset.m_AdditionalLightsCookieResolution);
            rendererData.SetAdditionalLightsCookieFormat(URPAsset.m_AdditionalLightsCookieFormat);

            //UniversalRendererData settings:
            if (shaders != null)
            {
                rendererData.shaders = new UniversalRendererData.ShaderResources();
                rendererData.shaders.blitPS = shaders.blitPS;
                rendererData.shaders.copyDepthPS = shaders.copyDepthPS;
                rendererData.shaders.screenSpaceShadowPS = shaders.screenSpaceShadowPS;
                rendererData.shaders.samplingPS = shaders.samplingPS;
                rendererData.shaders.stencilDeferredPS = shaders.stencilDeferredPS;
                rendererData.shaders.fallbackErrorPS = shaders.fallbackErrorPS;
                rendererData.shaders.fallbackLoadingPS = shaders.fallbackLoadingPS;
                rendererData.shaders.coreBlitPS = shaders.coreBlitPS;
                rendererData.shaders.coreBlitColorAndDepthPS = shaders.coreBlitColorAndDepthPS;
                rendererData.shaders.cameraMotionVector = shaders.cameraMotionVector;
                rendererData.shaders.objectMotionVector = shaders.objectMotionVector;
            }

            rendererData.xrSystemData = xrSystemData;

            rendererData.opaqueLayerMask = m_OpaqueLayerMask;
            rendererData.transparentLayerMask = m_TransparentLayerMask;
            rendererData.defaultStencilState = m_DefaultStencilState;
            rendererData.shadowTransparentReceive = m_ShadowTransparentReceive;
            rendererData.renderingMode = m_RenderingMode;
            rendererData.depthPrimingMode = m_DepthPrimingMode;
            rendererData.copyDepthMode = m_CopyDepthMode;
            rendererData.accurateGbufferNormals = m_AccurateGbufferNormals;
            rendererData.clusteredRendering = m_ClusteredRendering;
            rendererData.tileSize = m_TileSize;
            rendererData.intermediateTextureMode = m_IntermediateTextureMode;

            return rendererData;
        }
    }
}
