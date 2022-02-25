#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif
using System;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Assertions;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Defines if Unity will copy the depth that can be bound in shaders as _CameraDepthTexture after the opaques pass or after the transparents pass.
    /// </summary>
    public enum CopyDepthMode
    {
        /// <summary>Depth will be copied after the opaques pass</summary>
        AfterOpaques,
        /// <summary>Depth will be copied after the transparents pass</summary>
        AfterTransparents
    }

    /// <summary>
    /// Class containing resources needed for the <c>UniversalRenderer</c>.
    /// </summary>
    [Serializable, ReloadGroup, ExcludeFromPreset]
    [URPHelpURL("urp-universal-renderer")]
    public class UniversalRendererData : ScriptableRendererData, ISerializationCallbackReceiver
    {
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
        [SerializeField] bool m_RequireDepthTexture = false;
        [SerializeField] bool m_RequireOpaqueTexture = false;
        [SerializeField] Downsampling m_OpaqueDownsampling = Downsampling._2xBilinear;
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


        public static readonly int AdditionalLightsDefaultShadowResolutionTierLow = 256;
        public static readonly int AdditionalLightsDefaultShadowResolutionTierMedium = 512;
        public static readonly int AdditionalLightsDefaultShadowResolutionTierHigh = 1024;

        internal const int k_ShadowCascadeMinCount = 1;
        internal const int k_ShadowCascadeMaxCount = 4;

        // Main directional light Settings
        [SerializeField] LightRenderingMode m_MainLightRenderingMode = LightRenderingMode.PerPixel;
        [SerializeField] bool m_MainLightShadowsSupported = true;
        [SerializeField] ShadowResolution m_MainLightShadowmapResolution = ShadowResolution._2048;

        // Additional lights settings
        [SerializeField] LightRenderingMode m_AdditionalLightsRenderingMode = LightRenderingMode.PerPixel;
        [SerializeField] int m_AdditionalLightsPerObjectLimit = 4;
        [SerializeField] bool m_AdditionalLightShadowsSupported = false;
        [SerializeField] ShadowResolution m_AdditionalLightsShadowmapResolution = ShadowResolution._2048;

        [SerializeField] int m_AdditionalLightsShadowResolutionTierLow = AdditionalLightsDefaultShadowResolutionTierLow;
        [SerializeField] int m_AdditionalLightsShadowResolutionTierMedium = AdditionalLightsDefaultShadowResolutionTierMedium;
        [SerializeField] int m_AdditionalLightsShadowResolutionTierHigh = AdditionalLightsDefaultShadowResolutionTierHigh;
        // Shadows Settings
        [SerializeField] float m_ShadowDistance = 50.0f;
        [SerializeField] int m_ShadowCascadeCount = 1;
        [SerializeField] float m_Cascade2Split = 0.25f;
        [SerializeField] Vector2 m_Cascade3Split = new Vector2(0.1f, 0.3f);
        [SerializeField] Vector3 m_Cascade4Split = new Vector3(0.067f, 0.2f, 0.467f);
        [SerializeField] float m_CascadeBorder = 0.2f;
        [SerializeField] float m_ShadowDepthBias = 1.0f;
        [SerializeField] float m_ShadowNormalBias = 1.0f;
        [SerializeField] bool m_SoftShadowsSupported = false;
        [SerializeField] bool m_ConservativeEnclosingSphere = true;
        [SerializeField] int m_NumIterationsEnclosingSphere = 64;

        [SerializeField] bool m_MixedLightingSupported = true;
        [SerializeField] bool m_SupportsLightLayers = false;

        // Reflection Probes
        [SerializeField] bool m_ReflectionProbeBlending = false;
        [SerializeField] bool m_ReflectionProbeBoxProjection = false;

        // Light Cookie Settings
        [SerializeField] LightCookieResolution m_AdditionalLightsCookieResolution = LightCookieResolution._2048;
        [SerializeField] LightCookieFormat m_AdditionalLightsCookieFormat = LightCookieFormat.ColorHigh;


        public UniversalRendererData() : base() { }

        protected override ScriptableRenderer Create()
        {
            if (!Application.isPlaying)
            {
                ReloadAllNullProperties();
            }
            return new UniversalRenderer(this);
        }

        /// <summary>
        /// When true, the pipeline creates a depth texture that can be read in shaders. The depth texture can be accessed as _CameraDepthTexture. This setting can be overridden per camera.
        /// </summary>
        public bool supportsCameraDepthTexture { get { return m_RequireOpaqueTexture; } set { m_RequireOpaqueTexture = value; } }
        /// <summary>
        /// When true, the pipeline creates a texture that contains a copy of the color buffer after rendering opaque objects. This texture can be accessed in shaders as _CameraOpaqueTexture. This setting can be overridden per camera.
        /// </summary>
        public bool supportsCameraOpaqueTexture { get { return m_RequireDepthTexture; } set { m_RequireDepthTexture = value; } }

        /// <summary>
        /// Returns the downsampling method used when copying the camera color texture after rendering opaques.
        /// </summary>
        public Downsampling opaqueDownsampling
        {
            get { return m_OpaqueDownsampling; }
        }

        /// <summary>
        /// Specifies the <c>LightRenderingMode</c> for the main light used by this <c>UniversalRenderPipelineAsset</c>.
        /// </summary>
        /// <see cref="LightRenderingMode"/>
        /// </summary>
        public LightRenderingMode mainLightRenderingMode
        {
            get { return m_MainLightRenderingMode; }
            internal set { m_MainLightRenderingMode = value; }
        }

        public bool supportsMainLightShadows
        {
            get { return m_MainLightShadowsSupported; }
            internal set { m_MainLightShadowsSupported = value; }
        }

        public int mainLightShadowmapResolution
        {
            get { return (int)m_MainLightShadowmapResolution; }
            internal set { m_MainLightShadowmapResolution = (ShadowResolution)value; }
        }

        public LightRenderingMode additionalLightsRenderingMode
        {
            get { return m_AdditionalLightsRenderingMode; }
            internal set { m_AdditionalLightsRenderingMode = value; }
        }

        public int maxAdditionalLightsCount
        {
            get { return m_AdditionalLightsPerObjectLimit; }
            set { m_AdditionalLightsPerObjectLimit = ValidatePerObjectLights(value); }
        }

        public bool supportsAdditionalLightShadows
        {
            get { return m_AdditionalLightShadowsSupported; }
            internal set { m_AdditionalLightShadowsSupported = value; }
        }

        public int additionalLightsShadowmapResolution
        {
            get { return (int)m_AdditionalLightsShadowmapResolution; }
            internal set { m_AdditionalLightsShadowmapResolution = (ShadowResolution)value; }
        }

        /// <summary>
        /// Returns the additional light shadow resolution defined for tier "Low" in the UniversalRenderPipeline asset.
        /// </summary>
        public int additionalLightsShadowResolutionTierLow
        {
            get { return (int)m_AdditionalLightsShadowResolutionTierLow; }
            internal set { additionalLightsShadowResolutionTierLow = value; }
        }

        /// <summary>
        /// Returns the additional light shadow resolution defined for tier "Medium" in the UniversalRenderPipeline asset.
        /// </summary>
        public int additionalLightsShadowResolutionTierMedium
        {
            get { return (int)m_AdditionalLightsShadowResolutionTierMedium; }
            internal set { m_AdditionalLightsShadowResolutionTierMedium = value; }
        }

        /// <summary>
        /// Returns the additional light shadow resolution defined for tier "High" in the UniversalRenderPipeline asset.
        /// </summary>
        public int additionalLightsShadowResolutionTierHigh
        {
            get { return (int)m_AdditionalLightsShadowResolutionTierHigh; }
            internal set { additionalLightsShadowResolutionTierHigh = value; }
        }

        /// <summary>
        /// Controls the maximum distance at which shadows are visible.
        /// </summary>
        public float shadowDistance
        {
            get { return m_ShadowDistance; }
            set { m_ShadowDistance = Mathf.Max(0.0f, value); }
        }

        /// <summary>
        /// Returns the number of shadow cascades.
        /// </summary>
        public int shadowCascadeCount
        {
            get { return m_ShadowCascadeCount; }
            set
            {
                if (value < k_ShadowCascadeMinCount || value > k_ShadowCascadeMaxCount)
                {
                    throw new ArgumentException($"Value ({value}) needs to be between {k_ShadowCascadeMinCount} and {k_ShadowCascadeMaxCount}.");
                }
                m_ShadowCascadeCount = value;
            }
        }

        /// <summary>
        /// Returns the split value.
        /// </summary>
        /// <returns>Returns a Float with the split value.</returns>
        public float cascade2Split
        {
            get { return m_Cascade2Split; }
            internal set { m_Cascade2Split = value; }
        }

        /// <summary>
        /// Returns the split values.
        /// </summary>
        /// <returns>Returns a Vector2 with the split values.</returns>
        public Vector2 cascade3Split
        {
            get { return m_Cascade3Split; }
            internal set { m_Cascade3Split = value; }
        }

        /// <summary>
        /// Returns the split values.
        /// </summary>
        /// <returns>Returns a Vector3 with the split values.</returns>
        public Vector3 cascade4Split
        {
            get { return m_Cascade4Split; }
            internal set { m_Cascade4Split = value; }
        }

        /// <summary>
        /// Last cascade fade distance in percentage.
        /// </summary>
        public float cascadeBorder
        {
            get { return m_CascadeBorder; }
            set { cascadeBorder = value; }
        }

        /// <summary>
        /// The Shadow Depth Bias, controls the offset of the lit pixels.
        /// </summary>
        public float shadowDepthBias
        {
            get { return m_ShadowDepthBias; }
            set { m_ShadowDepthBias = ValidateShadowBias(value); }
        }

        /// <summary>
        /// Controls the distance at which the shadow casting surfaces are shrunk along the surface normal.
        /// </summary>
        public float shadowNormalBias
        {
            get { return m_ShadowNormalBias; }
            set { m_ShadowNormalBias = ValidateShadowBias(value); }
        }

        /// <summary>
        /// Supports Soft Shadows controls the Soft Shadows.
        /// </summary>
        public bool supportsSoftShadows
        {
            get { return m_SoftShadowsSupported; }
            internal set { m_SoftShadowsSupported = value; }
        }

        /// <summary>
        /// Returns true if the Render Pipeline Asset supports mixed lighting, false otherwise.
        /// </summary>
        /// <see href="https://docs.unity3d.com/Manual/LightMode-Mixed.html"/>
        public bool supportsMixedLighting
        {
            get { return m_MixedLightingSupported; }
        }

        /// <summary>
        /// Returns true if the Render Pipeline Asset supports light layers, false otherwise.
        /// </summary>
        public bool supportsLightLayers
        {
            get { return m_SupportsLightLayers; }
        }

        /// <summary>
        /// Set to true to enable a conservative method for calculating the size and position of the minimal enclosing sphere around the frustum cascade corner points for shadow culling.
        /// </summary>
        public bool conservativeEnclosingSphere
        {
            get { return m_ConservativeEnclosingSphere; }
            set { m_ConservativeEnclosingSphere = value; }
        }

        /// <summary>
        /// Set the number of iterations to reduce the cascade culling enlcosing sphere to be closer to the absolute minimun enclosing sphere, but will also require more CPU computation for increasing values.
        /// This parameter is used only when conservativeEnclosingSphere is set to true. Default value is 64.
        /// </summary>
        public int numIterationsEnclosingSphere
        {
            get { return m_NumIterationsEnclosingSphere; }
            set { m_NumIterationsEnclosingSphere = value; }
        }

        public bool reflectionProbeBlending
        {
            get { return m_ReflectionProbeBlending; }
            internal set { m_ReflectionProbeBlending = value; }
        }

        public bool reflectionProbeBoxProjection
        {
            get { return m_ReflectionProbeBoxProjection; }
            internal set { m_ReflectionProbeBoxProjection = value; }
        }

        private static GraphicsFormat[][] s_LightCookieFormatList = new GraphicsFormat[][]
        {
            /* Grayscale Low */ new GraphicsFormat[] {GraphicsFormat.R8_UNorm},
            /* Grayscale High*/ new GraphicsFormat[] {GraphicsFormat.R16_UNorm},
            /* Color Low     */ new GraphicsFormat[] {GraphicsFormat.R5G6B5_UNormPack16, GraphicsFormat.B5G6R5_UNormPack16, GraphicsFormat.R5G5B5A1_UNormPack16, GraphicsFormat.B5G5R5A1_UNormPack16},
            /* Color High    */ new GraphicsFormat[] {GraphicsFormat.A2B10G10R10_UNormPack32, GraphicsFormat.R8G8B8A8_SRGB, GraphicsFormat.B8G8R8A8_SRGB},
            /* Color HDR     */ new GraphicsFormat[] {GraphicsFormat.B10G11R11_UFloatPack32},
        };

        internal int GetAdditionalLightsShadowResolution(int additionalLightsShadowResolutionTier)
        {
            if (additionalLightsShadowResolutionTier <= UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierLow /* 0 */)
                return additionalLightsShadowResolutionTierLow;

            if (additionalLightsShadowResolutionTier == UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierMedium /* 1 */)
                return additionalLightsShadowResolutionTierMedium;

            if (additionalLightsShadowResolutionTier >= UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierHigh /* 2 */)
                return additionalLightsShadowResolutionTierHigh;

            return additionalLightsShadowResolutionTierMedium;
        }

        internal Vector2Int additionalLightsCookieResolution => new Vector2Int((int)m_AdditionalLightsCookieResolution, (int)m_AdditionalLightsCookieResolution);


        float ValidateShadowBias(float value)
        {
            return Mathf.Max(0.0f, Mathf.Min(value, UniversalRenderPipeline.maxShadowBias));
        }

        int ValidatePerObjectLights(int value)
        {
            return System.Math.Max(0, System.Math.Min(value, UniversalRenderPipeline.maxPerObjectLights));
        }
        internal GraphicsFormat additionalLightsCookieFormat
        {
            get
            {
                GraphicsFormat result = GraphicsFormat.None;
                foreach (var format in s_LightCookieFormatList[(int)m_AdditionalLightsCookieFormat])
                {
                    if (SystemInfo.IsFormatSupported(format, FormatUsage.Render))
                    {
                        result = format;
                        break;
                    }
                }

                if (QualitySettings.activeColorSpace == ColorSpace.Gamma)
                    result = GraphicsFormatUtility.GetLinearFormat(result);

                // Fallback
                if (result == GraphicsFormat.None)
                {
                    result = GraphicsFormat.R8G8B8A8_UNorm;
                    Debug.LogWarning($"Additional Lights Cookie Format ({ m_AdditionalLightsCookieFormat.ToString() }) is not supported by the platform. Falling back to {GraphicsFormatUtility.GetBlockSize(result) * 8}-bit format ({GraphicsFormatUtility.GetFormatString(result)})");
                }

                return result;
            }
        }

        /// <summary>
        /// Use this to configure how to filter opaque objects.
        /// </summary>
        public LayerMask opaqueLayerMask
        {
            get => m_OpaqueLayerMask;
            set
            {
                SetDirty();
                m_OpaqueLayerMask = value;
            }
        }

        /// <summary>
        /// Use this to configure how to filter transparent objects.
        /// </summary>
        public LayerMask transparentLayerMask
        {
            get => m_TransparentLayerMask;
            set
            {
                SetDirty();
                m_TransparentLayerMask = value;
            }
        }

        public StencilStateData defaultStencilState
        {
            get => m_DefaultStencilState;
            set
            {
                SetDirty();
                m_DefaultStencilState = value;
            }
        }

        /// <summary>
        /// True if transparent objects receive shadows.
        /// </summary>
        public bool shadowTransparentReceive
        {
            get => m_ShadowTransparentReceive;
            set
            {
                SetDirty();
                m_ShadowTransparentReceive = value;
            }
        }

        /// <summary>
        /// Rendering mode.
        /// </summary>
        public RenderingMode renderingMode
        {
            get => m_RenderingMode;
            set
            {
                SetDirty();
                m_RenderingMode = value;
            }
        }

        /// <summary>
        /// Depth priming mode.
        /// </summary>
        public DepthPrimingMode depthPrimingMode
        {
            get => m_DepthPrimingMode;
            set
            {
                SetDirty();
                m_DepthPrimingMode = value;
            }
        }

        /// <summary>
        /// Copy depth mode.
        /// </summary>
        public CopyDepthMode copyDepthMode
        {
            get => m_CopyDepthMode;
            set
            {
                SetDirty();
                m_CopyDepthMode = value;
            }
        }

        /// <summary>
        /// Use Octaedron Octahedron normal vector encoding for gbuffer normals.
        /// The overhead is negligible from desktop GPUs, while it should be avoided for mobile GPUs.
        /// </summary>
        public bool accurateGbufferNormals
        {
            get => m_AccurateGbufferNormals;
            set
            {
                SetDirty();
                m_AccurateGbufferNormals = value;
            }
        }

        internal bool clusteredRendering
        {
            get => m_ClusteredRendering;
            set
            {
                SetDirty();
                m_ClusteredRendering = value;
            }
        }

        internal TileSize tileSize
        {
            get => m_TileSize;
            set
            {
                Assert.IsTrue(value.IsValid());
                SetDirty();
                m_TileSize = value;
            }
        }

        /// <summary>
        /// Controls when URP renders via an intermediate texture.
        /// </summary>
        public IntermediateTextureMode intermediateTextureMode
        {
            get => m_IntermediateTextureMode;
            set
            {
                SetDirty();
                m_IntermediateTextureMode = value;
            }
        }

        /// <inheritdoc/>
        public override void OnValidate()
        {
            base.OnValidate();
            if (!m_TileSize.IsValid())
            {
                m_TileSize = k_DefaultTileSize;
            }
        }

        /// <inheritdoc/>
        public override void OnEnable()
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

            if (UniversalRenderPipelineGlobalSettings.instance.postProcessData != null)
                ResourceReloader.TryReloadAllNullIn(UniversalRenderPipelineGlobalSettings.instance.postProcessData, UniversalRenderPipelineAsset.packagePath);

#if ENABLE_VR && ENABLE_XR_MODULE
            ResourceReloader.TryReloadAllNullIn(xrSystemData, UniversalRenderPipelineAsset.packagePath);
#endif
#endif
        }

        public override void OnBeforeSerialize()
        {
            m_AssetVersion = k_LatestAssetVersion;
        }

        public override void OnAfterDeserialize()
        {
            //TODO: fix the upgrade.
            if (m_AssetVersion <= 0)
            {
                var anyNonUrpRendererFeatures = false;

                foreach (var feature in m_RendererFeatures)
                {
                    try
                    {
                        if (feature.GetType().Assembly == typeof(UniversalRendererData).Assembly)
                        {
                            continue;
                        }
                    }
                    catch
                    {
                        // If we hit any exceptions while poking around assemblies,
                        // conservatively assume there was a non URP renderer feature.
                    }

                    anyNonUrpRendererFeatures = true;
                }

                // Replicate old intermediate texture behaviour in case of any non-URP renderer features,
                // where we cannot know if they properly declare needed inputs.
                m_IntermediateTextureMode = anyNonUrpRendererFeatures ? IntermediateTextureMode.Always : IntermediateTextureMode.Auto;
            }

            if (m_AssetVersion <= 1)
            {
                // To avoid breaking existing projects, keep the old AfterOpaques behaviour. The new AfterTransparents default will only apply to new projects.
                m_CopyDepthMode = CopyDepthMode.AfterOpaques;
            }


            m_AssetVersion = k_LatestAssetVersion;
        }
    }

    /// <summary>
    /// Class containing resources needed for the <c>UniversalRenderer</c>.
    /// </summary>
    [Serializable, ReloadGroup, ExcludeFromPreset]
    [URPHelpURL("urp-universal-renderer")]
    [Obsolete("UniversalRenderer is no longer an asset.")]
    [MovedFrom(false, sourceClassName: "UniversalRendererData")]
    public class UniversalRendererDataAssetLegacy : ScriptableRendererDataAssetLegacy, ISerializationCallbackReceiver
    {
        protected override ScriptableRenderer Create()
        {
            throw new NotImplementedException();
        }

#if UNITY_EDITOR
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812")]
        internal class CreateUniversalRendererAsset : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var instance = UniversalRenderPipelineAsset.CreateRendererAsset(pathName, RendererType.UniversalRenderer, false) as UniversalRendererDataAssetLegacy;
                Selection.activeObject = instance;
            }
        }

        [MenuItem("Assets/Create/Rendering/URP Universal Renderer", priority = CoreUtils.Sections.section3 + CoreUtils.Priorities.assetsCreateRenderingMenuPriority + 2)]
        static void CreateUniversalRendererData()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateUniversalRendererAsset>(), "New Custom Universal Renderer Data.asset", null, null);
        }

#endif

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
        protected override ScriptableRendererData UpgradeRendererWithoutAsset()
        {
            UniversalRendererData rendererData = new UniversalRendererData();
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
