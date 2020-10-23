using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the Screen Space Refraction effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Lighting/Screen Space Refraction")]
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "Override-Screen-Space-Refraction" + Documentation.endURL)]
    public class ScreenSpaceRefraction : VolumeComponent
    {
        internal enum RefractionModel
        {
            None = 0,
            Box = 1,
            Sphere = 2,
            Thin = 3
        };

        /// <summary>
        /// Controls the distance at which HDRP fades out Screen Space Refraction near the edge of the screen. A value near 0 indicates a small fade distance at the edges,
        /// while increasing the value towards one will start the fade closer to the center of the screen.
        /// </summary>
        public ClampedFloatParameter screenFadeDistance = new ClampedFloatParameter(0.1f, 0.001f, 1.0f);
    }
}
