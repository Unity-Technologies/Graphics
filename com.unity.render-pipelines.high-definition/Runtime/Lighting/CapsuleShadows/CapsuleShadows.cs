using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable, VolumeComponentMenuForRenderPipeline("Shadowing/Capsule Shadows", typeof(HDRenderPipeline))]
    public class CapsuleShadows : VolumeComponent
    {
        /// <summary>
        /// When enabled, HDRP processes Capsule Shadows for this Volume.
        /// </summary>
        public BoolParameter enable = new BoolParameter(false);

        CapsuleShadows()
        {
            displayName = "Capsule Shadows";
        }
    }
}
