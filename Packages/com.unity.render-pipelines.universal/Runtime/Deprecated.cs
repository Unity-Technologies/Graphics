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

        /// <summary>
        /// Class containing texture resources used in URP.
        /// </summary>
        [Serializable, ReloadGroup]
        [Obsolete("Moved to UniversalRenderPipelineRuntimeTextures on GraphicsSettings. #from(2023.3)", false)]
        public sealed class TextureResources
        {
            /// <summary>
            /// Pre-baked blue noise textures.
            /// </summary>
            [Reload("Textures/BlueNoise64/L/LDR_LLL1_0.png")]
            public Texture2D blueNoise64LTex;

            /// <summary>
            /// Bayer matrix texture.
            /// </summary>
            [Reload("Textures/BayerMatrix.png")]
            public Texture2D bayerMatrixTex;

            /// <summary>
            /// Check if the textures need reloading.
            /// </summary>
            /// <returns>True if any of the textures need reloading.</returns>
            public bool NeedsReload()
            {
                return blueNoise64LTex == null || bayerMatrixTex == null;
            }
        }

        [Obsolete("Moved to UniversalRenderPipelineRuntimeTextures on GraphicsSettings. #from(2023.3)", false)]
        [SerializeField]
        TextureResources m_Textures;

        /// <summary>
        /// Returns asset texture resources
        /// </summary>
        [Obsolete("Moved to UniversalRenderPipelineRuntimeTextures on GraphicsSettings. #from(2023.3)", false)]
        public TextureResources textures
        {
            get
            {
                if (m_Textures == null)
                    m_Textures = new TextureResources();

#if UNITY_EDITOR
                if (m_Textures.NeedsReload())
                    ResourceReloader.ReloadAllNullIn(this, packagePath);
#endif

                return m_Textures;
            }
        }
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

    public abstract partial class ScriptableRendererData
    {
        /// <summary>
        /// Class contains references to shader resources used by Rendering Debugger.
        /// </summary>
        [Obsolete("Moved to UniversalRenderPipelineDebugShaders on GraphicsSettings. #from(2023.3)", false)]
        [Serializable, ReloadGroup]
        public sealed class DebugShaderResources
        {
            /// <summary>
            /// Debug shader used to output interpolated vertex attributes.
            /// </summary>
            [Obsolete("Moved to UniversalRenderPipelineDebugShaders on GraphicsSettings. #from(2023.3)", false)]
            [Reload("Shaders/Debug/DebugReplacement.shader")]
            public Shader debugReplacementPS;

            /// <summary>
            /// Debug shader used to output HDR Chromacity mapping.
            /// </summary>
            [Obsolete("Moved to UniversalRenderPipelineDebugShaders on GraphicsSettings. #from(2023.3)", false)]
            [Reload("Shaders/Debug/HDRDebugView.shader")]
            public Shader hdrDebugViewPS;

#if UNITY_EDITOR
            /// <summary>
            /// Debug shader used to output world position and world normal for the pixel under the cursor.
            /// </summary>
            [Obsolete("Moved to UniversalRenderPipelineDebugShaders on GraphicsSettings. #from(2023.3)", false)]
            [Reload("Shaders/Debug/ProbeVolumeSamplingDebugPositionNormal.compute")]
            public ComputeShader probeVolumeSamplingDebugComputeShader;
#endif
        }

        /// <summary>
        /// Container for shader resources used by Rendering Debugger.
        /// </summary>
        [Obsolete("Moved to UniversalRenderPipelineDebugShaders on GraphicsSettings. #from(2023.3)", false)]
        public DebugShaderResources debugShaders;

        /// <summary>
        /// Class contains references to shader resources used by APV.
        /// </summary>
        [Serializable, ReloadGroup]
        [Obsolete("Probe volume debug resource are now in the ProbeVolumeDebugResources class.")]
        public sealed class ProbeVolumeResources
        {
            /// <summary>
            /// Debug shader used to render probes in the volume.
            /// </summary>
            [Obsolete("This shader is now in the ProbeVolumeDebugResources class.")]
            public Shader probeVolumeDebugShader;

            /// <summary>
            /// Debug shader used to display fragmentation of the GPU memory.
            /// </summary>
            [Obsolete("This shader is now in the ProbeVolumeDebugResources class.")]
            public Shader probeVolumeFragmentationDebugShader;

            /// <summary>
            /// Debug shader used to draw the offset direction used for a probe.
            /// </summary>
            [Obsolete("This shader is now in the ProbeVolumeDebugResources class.")]
            public Shader probeVolumeOffsetDebugShader;

            /// <summary>
            /// Debug shader used to draw the sampling weights of the probe volume.
            /// </summary>
            [Obsolete("This shader is now in the ProbeVolumeDebugResources class.")]
            public Shader probeVolumeSamplingDebugShader;

            /// <summary>
            /// Debug mesh used to draw the sampling weights of the probe volume.
            /// </summary>
            [Obsolete("This shader is now in the ProbeVolumeDebugResources class.")]
            public Mesh probeSamplingDebugMesh;

            /// <summary>
            /// Texture with the numbers dor sampling weights.
            /// </summary>
            [Obsolete("This shader is now in the ProbeVolumeDebugResources class.")]
            public Texture2D probeSamplingDebugTexture;

            /// <summary>
            /// Compute Shader used for Blending.
            /// </summary>
            [Obsolete("This shader is now in the ProbeVolumeRuntimeResources class.")]
            public ComputeShader probeVolumeBlendStatesCS;
        }

        /// <summary>
        /// Probe volume resources used by URP
        /// </summary>
        [Obsolete("Probe volume debug resource are now in the ProbeVolumeDebugResources class.")]
        public ProbeVolumeResources probeVolumeResources;
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
