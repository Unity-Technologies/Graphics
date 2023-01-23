// This file should be used as a container for things on its
// way to being deprecated and removed in future releases
using System;
using System.ComponentModel;

namespace UnityEngine.Rendering.Universal
{
    public abstract partial class ScriptableRenderPass
    {
        // This callback method will be removed. Please use OnCameraCleanup() instead.
        /// <summary>
        /// Cleanup any allocated resources that were created during the execution of this render pass.
        /// </summary>
        /// <param name="cmd">Use this CommandBuffer to cleanup any generated data. </param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual void FrameCleanup(CommandBuffer cmd) => OnCameraCleanup(cmd);
    }

    namespace Internal
    {
        public partial class AdditionalLightsShadowCasterPass
        {
            /// <summary>
            /// The ID for the additional shadows buffer ID.
            /// This has been deprecated. Shadow slice matrix is now passed to the GPU using an entry in buffer m_AdditionalLightsWorldToShadow_SSBO.
            /// </summary>
            [Obsolete("AdditionalLightsShadowCasterPass.m_AdditionalShadowsBufferId was deprecated. Shadow slice matrix is now passed to the GPU using an entry in buffer m_AdditionalLightsWorldToShadow_SSBO", true)]
            public static int m_AdditionalShadowsBufferId;

            /// <summary>
            /// The ID for the additional shadows buffer ID.
            /// This has been deprecated. hadow slice index is now passed to the GPU using last member of an entry in buffer m_AdditionalShadowParams_SSBO.
            /// </summary>
            [Obsolete("AdditionalLightsShadowCasterPass.m_AdditionalShadowsIndicesId was deprecated. Shadow slice index is now passed to the GPU using last member of an entry in buffer m_AdditionalShadowParams_SSBO", true)]
            public static int m_AdditionalShadowsIndicesId;
        }
    }

    /// <summary>
    /// Previously contained the settings to control how many cascades to use. It is now deprecated.
    /// </summary>
    [Obsolete("This is obsolete, please use shadowCascadeCount instead.", true)]
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

    /// <summary>
    /// Specifies the logging level for shader variants.
    /// This is obsolete, UnityEngine.Rendering.ShaderVariantLogLevel instead.
    /// </summary>
    [Obsolete("This is obsolete, UnityEngine.Rendering.ShaderVariantLogLevel instead.", true)]
    public enum ShaderVariantLogLevel
    {
        /// <summary>Disable all log for shader variants.</summary>
        Disabled,

        /// <summary>Only logs SRP Shaders when logging shader variants.</summary>
        [InspectorName("Only URP Shaders")]
        OnlyUniversalRPShaders,

        /// <summary>Logs all shader variants.</summary>
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
        [Obsolete("Use UniversalRenderPipelineGlobalSettings.instance.shaderVariantLogLevel", true)]
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
        [Obsolete("This is obsolete, please use shadowCascadeCount instead.", true)]
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
        /// <summary>
        /// The render target identifier for camera depth.
        /// This is obsolete, cameraDepth has been renamed to cameraDepthTarget.
        /// </summary>
        [Obsolete("cameraDepth has been renamed to cameraDepthTarget. (UnityUpgradable) -> cameraDepthTarget", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public RenderTargetIdentifier cameraDepth
        {
            get => m_CameraDepthTarget.nameID;
        }
    }

    public sealed partial class Bloom : VolumeComponent, IPostProcessComponent
    {
        // Deprecated in 13.x.x
        /// <summary>
        /// The number of final iterations to skip in the effect processing sequence.
        /// This is obsolete, please use maxIterations instead.
        /// </summary>
        [Obsolete("This is obsolete, please use maxIterations instead.", true)]
        [Tooltip("The number of final iterations to skip in the effect processing sequence.")]
        public ClampedIntParameter skipIterations = new ClampedIntParameter(1, 0, 16);
    }
}
