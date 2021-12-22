using System;
using System.Diagnostics;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable, VolumeComponentMenuForRenderPipeline("Shadowing/Capsule Shadows", typeof(HDRenderPipeline))]
    public class CapsuleShadowsVolumeComponent : VolumeComponent
    {
        /// <summary>
        /// When enabled, HDRP processes Capsule Shadows for this Volume.
        /// </summary>
        public BoolParameter enable = new BoolParameter(true);

        CapsuleShadowsVolumeComponent()
        {
            displayName = "Capsule Shadows";
        }
    }
}
