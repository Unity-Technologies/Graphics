using System;
using System.Diagnostics;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Capsule Shadow Method parameter.
    /// </summary>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class CapsuleShadowMethodParameter : VolumeParameter<CapsuleShadowMethod>
    {
        /// <summary>
        /// Capsule Shadow Method parameter constructor.
        /// </summary>
        /// <param name="value">Capsule Shadow Method parameter.</param>
        /// <param name="overrideState">Initial override state.</param>
        public CapsuleShadowMethodParameter(CapsuleShadowMethod value, bool overrideState = false)
            : base(value, overrideState) { }
    }

    [Serializable, VolumeComponentMenuForRenderPipeline("Shadowing/Capsule Shadows", typeof(HDRenderPipeline))]
    public class CapsuleShadowsVolumeComponent : VolumeComponent
    {
        /// <summary>
        /// When enabled, HDRP processes Capsule Shadows for this Volume.
        /// </summary>
        public BoolParameter enable = new BoolParameter(true);

        // TODO: move to settings/debug?
        public CapsuleShadowMethodParameter shadowMethodDebug = new CapsuleShadowMethodParameter(CapsuleShadowMethod.FlattenThenClosestSphere);

        // TODO: move to settings/debug?
        public BoolParameter fadeSelfShadow = new BoolParameter(true);

        CapsuleShadowsVolumeComponent()
        {
            displayName = "Capsule Shadows";
        }
    }
}
