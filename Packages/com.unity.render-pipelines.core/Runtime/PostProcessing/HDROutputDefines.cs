using UnityEngine.Serialization;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// The available options for range reduction/tonemapping when outputting to an HDR device.
    /// </summary>
    [GenerateHLSL]
    public enum HDRRangeReduction
    {
        /// <summary>
        /// No range reduction.
        /// </summary>
        None,
        /// <summary>
        /// Reinhard tonemapping.
        /// </summary>
        Reinhard,
        /// <summary>
        /// BT2390 Hermite spline EETF range reduction.
        /// </summary>
        BT2390,
        /// <summary>
        /// ACES tonemapping preset for 1000 nits displays.
        /// </summary>
        ACES1000Nits,
        /// <summary>
        /// ACES tonemapping preset for 2000 nits displays.
        /// </summary>
        ACES2000Nits,
        /// <summary>
        /// ACES tonemapping preset for 4000 nits displays.
        /// </summary>
        ACES4000Nits
    }
}
