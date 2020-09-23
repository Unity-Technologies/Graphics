// This file should be used as a container for things on its
// way to being deprecated and removed in future releases

using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering.Universal
{
    public abstract partial class ScriptableRenderPass
    {
        // This callback method will be removed. Please use OnCameraCleanup() instead.
        public virtual void FrameCleanup(CommandBuffer cmd) => OnCameraCleanup(cmd);
    }

    /// <summary>
    /// Default renderer for Universal RP.
    /// This renderer is supported on all Universal RP supported platforms.
    /// It uses a classic forward rendering strategy with per-object light culling.
    /// </summary>
    [Obsolete("ForwardRenderer has been deprecated. Use StandardRenderer instead (UnityUpgradable) -> StandardRenderer", true)]
    public sealed class ForwardRenderer: ScriptableRenderer
    {
        public ForwardRenderer(ForwardRendererData data) : base(data)
        {
            throw new NotSupportedException("ForwardRenderer has been deprecated. Use StandardRenderer instead");
        }

        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            throw new NotSupportedException("ForwardRenderer has been deprecated. Use StandardRenderer instead");
        }

        public override void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            throw new NotSupportedException("ForwardRenderer has been deprecated. Use StandardRenderer instead");
        }

        public override void SetupCullingParameters(ref ScriptableCullingParameters cullingParameters, ref CameraData cameraData)
        {
            throw new NotSupportedException("ForwardRenderer has been deprecated. Use StandardRenderer instead");
        }

        public override void FinishRendering(CommandBuffer cmd)
        {
            throw new NotSupportedException("ForwardRenderer has been deprecated. Use StandardRenderer instead");
        }
    }
}
