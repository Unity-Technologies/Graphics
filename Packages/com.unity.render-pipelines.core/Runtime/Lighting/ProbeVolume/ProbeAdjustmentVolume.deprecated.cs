using System;

namespace UnityEngine.Rendering
{
    public partial class ProbeAdjustmentVolume
    {
        /// <summary>Whether to invalidate all probes falling within this volume.</summary>
        [Obsolete("This field is only kept for migration purpose. Use mode instead. #from(2023.1)")] public bool invalidateProbes = false;

        /// <summary>Whether to use a custom threshold for dilation for probes falling withing this volume.</summary>
        [Obsolete("This field is only kept for migration purpose. Use mode instead. #from(2023.1)")] public bool overrideDilationThreshold = false;
    }
}