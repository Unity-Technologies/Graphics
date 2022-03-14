// This file should be used as a container for things on its
// way to being deprecated and removed in future releases
using System;
using System.ComponentModel;

namespace UnityEngine.Rendering.Universal
{
    public abstract partial class ScriptableRenderPass
    {
        // This callback method will be removed. Please use OnCameraCleanup() instead.
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual void FrameCleanup(CommandBuffer cmd) => OnCameraCleanup(cmd);
    }

    /// <summary>
    /// Default renderer for Universal RP.
    /// This renderer is supported on all Universal RP supported platforms.
    /// It uses a classic forward rendering strategy with per-object light culling.
    /// </summary>
    [Obsolete("ForwardRenderer has been deprecated (UnityUpgradable) -> UniversalRenderer", true)]
    public sealed class ForwardRenderer : ScriptableRenderer
    {
        private static readonly string k_ErrorMessage = "ForwardRenderer has been deprecated. Use UniversalRenderer instead";

        public ForwardRenderer(ForwardRendererData data) : base(data)
        {
            throw new NotSupportedException(k_ErrorMessage);
        }

        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            throw new NotSupportedException(k_ErrorMessage);
        }

        public override void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            throw new NotSupportedException(k_ErrorMessage);
        }

        public override void SetupCullingParameters(ref ScriptableCullingParameters cullingParameters, ref CameraData cameraData)
        {
            throw new NotSupportedException(k_ErrorMessage);
        }

        public override void FinishRendering(CommandBuffer cmd)
        {
            throw new NotSupportedException(k_ErrorMessage);
        }

        internal override void SwapColorBuffer(CommandBuffer cmd)
        {
            throw new NotSupportedException(k_ErrorMessage);
        }

        internal override RTHandle GetCameraColorFrontBuffer(CommandBuffer cmd)
        {
            throw new NotImplementedException();
        }
    }

    namespace Internal
    {
        public partial class AdditionalLightsShadowCasterPass
        {
            [Obsolete("AdditionalLightsShadowCasterPass.m_AdditionalShadowsBufferId was deprecated. Shadow slice matrix is now passed to the GPU using an entry in buffer m_AdditionalLightsWorldToShadow_SSBO", false)]
            public static int m_AdditionalShadowsBufferId;
            [Obsolete("AdditionalLightsShadowCasterPass.m_AdditionalShadowsIndicesId was deprecated. Shadow slice index is now passed to the GPU using last member of an entry in buffer m_AdditionalShadowParams_SSBO", false)]
            public static int m_AdditionalShadowsIndicesId;
        }
    }

    /// <summary>
    /// Previously contained the settings to control how many cascades to use. It is now deprecated.
    /// </summary>
    [Obsolete("This is obsolete, please use shadowCascadeCount instead.", false)]
    public enum ShadowCascadesOption
    {
        /// <summary>
        /// No cascades used for the shadows
        /// </summary>
        NoCascades,
        /// <summary>
        /// Two cascades used for the shadows
        /// </summary>
        TwoCascades,
        /// <summary>
        /// Four cascades used for the shadows
        /// </summary>
        FourCascades,
    }

    [Obsolete("This is obsolete, UnityEngine.Rendering.ShaderVariantLogLevel instead.", false)]
    public enum ShaderVariantLogLevel
    {
        Disabled,
        [InspectorName("Only URP Shaders")]
        OnlyUniversalRPShaders,
        [InspectorName("All Shaders")]
        AllShaders
    }

    public partial class UniversalRenderPipelineAsset
    {
        [SerializeField] int m_ShaderVariantLogLevel;

#pragma warning disable 618 // Obsolete warning
        /// <summary>
        /// Previously returned the shader variant log level for this Render Pipeline Asset but is now deprecated.
        /// </summary>
        [Obsolete("Use UniversalRenderPipelineGlobalSettings.instance.shaderVariantLogLevel", false)]
        public ShaderVariantLogLevel shaderVariantLogLevel
        {
            get { return (ShaderVariantLogLevel)UniversalRenderPipelineGlobalSettings.instance.shaderVariantLogLevel; }
            set { UniversalRenderPipelineGlobalSettings.instance.shaderVariantLogLevel = (Rendering.ShaderVariantLogLevel)value; }
        }
#pragma warning restore 618 // Obsolete warning

#pragma warning disable 618 // Obsolete warning
        [Obsolete("This is obsolete, please use shadowCascadeCount instead.", false)]
        [SerializeField] ShadowCascadesOption m_ShadowCascades = ShadowCascadesOption.NoCascades;

        /// <summary>
        /// Previously used insted of shadowCascadeCount. Please use that instead.
        /// </summary>
        [Obsolete("This is obsolete, please use shadowCascadeCount instead.", false)]
        public ShadowCascadesOption shadowCascadeOption
        {
            get
            {
                switch (shadowCascadeCount)
                {
                    case 1: return ShadowCascadesOption.NoCascades;
                    case 2: return ShadowCascadesOption.TwoCascades;
                    case 4: return ShadowCascadesOption.FourCascades;
                    default: throw new InvalidOperationException("Cascade count is not compatible with obsolete API, please use shadowCascadeCount instead.");
                }
                ;
            }
            set
            {
                switch (value)
                {
                    case ShadowCascadesOption.NoCascades:
                        shadowCascadeCount = 1;
                        break;
                    case ShadowCascadesOption.TwoCascades:
                        shadowCascadeCount = 2;
                        break;
                    case ShadowCascadesOption.FourCascades:
                        shadowCascadeCount = 4;
                        break;
                    default:
                        throw new InvalidOperationException("Cascade count is not compatible with obsolete API, please use shadowCascadeCount instead.");
                }
            }
        }
#pragma warning restore 618 // Obsolete warning
    }

    public abstract partial class ScriptableRenderer
    {
        // Deprecated in 10.x
        [Obsolete("cameraDepth has been renamed to cameraDepthTarget. (UnityUpgradable) -> cameraDepthTarget")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public RenderTargetIdentifier cameraDepth
        {
            get => m_CameraDepthTarget.nameID;
        }
    }

    public sealed partial class Bloom : VolumeComponent, IPostProcessComponent
    {
        // Deprecated in 13.x.x
        [Obsolete("This is obsolete, please use maxIterations instead.", false)]
        [Tooltip("The number of final iterations to skip in the effect processing sequence.")]
        public ClampedIntParameter skipIterations = new ClampedIntParameter(1, 0, 16);
    }
}
