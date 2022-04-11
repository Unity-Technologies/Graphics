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

        internal override RenderTargetIdentifier GetCameraColorFrontBuffer(CommandBuffer cmd)
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

    [Obsolete("This is obsolete, please use shadowCascadeCount instead.", false)]
    public enum ShadowCascadesOption
    {
        NoCascades,
        TwoCascades,
        FourCascades,
    }
    public partial class UniversalRenderPipelineAsset
    {
#pragma warning disable 618 // Obsolete warning
        [Obsolete("This is obsolete, please use shadowCascadeCount instead.", false)]
        [SerializeField] ShadowCascadesOption m_ShadowCascades = ShadowCascadesOption.NoCascades;

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
            get => m_CameraDepthTarget;
        }
    }
}
