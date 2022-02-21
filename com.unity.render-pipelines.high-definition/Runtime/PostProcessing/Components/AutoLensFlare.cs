using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [VolumeComponentMenu("Post-processing/AutoLensFlare")]
    public sealed class AutoLensFlare : VolumeComponent, IPostProcessComponent
    {
        [Header("General")]
        /// <summary>
        /// Controls the global intensity of the Lens Flare effect.
        /// </summary>
        [Tooltip("Controls the global intensity of the Lens Flare effect.")]
        public ClampedFloatParameter intensityMultiplier = new ClampedFloatParameter(1f, 0f, 8f);
        /// <summary>
        /// Controls the intensity of the opposite Lens Flare. Those flares are sampled using scaled and inverted screen coordinates.
        /// </summary>
        [Tooltip("Controls the intensity of the opposite Lens Flare. Those flares are sampled using scaled and inverted screen coordinates.")]
        public ClampedFloatParameter firstFlareIntensity = new ClampedFloatParameter(0.75f, 0f, 1f);
        /// <summary>
        /// Controls the intensity of the opposite Lens Flare.
        /// </summary>
        [Tooltip("Controls the intensity of the opposite Lens Flare. Those flares are sampled using scaled screen coordinates.")]
        public ClampedFloatParameter secondaryFlareIntensity = new ClampedFloatParameter(0.5f, 0f, 1f);
        /// <summary>
        /// Controls the intensity of the opposite Lens Flare.
        /// </summary>
        [Tooltip("Controls the intensity of the warped Lens Flare. Those flares are sampled using polar UV coordinates.")]
        public ClampedFloatParameter warpedFlareIntensity = new ClampedFloatParameter(0.25f, 0f, 1f);
        /// <summary>
        /// Controls the intensity of the vignette effect to occlude the Lens Flare effect at the center of the screen
        /// </summary>
        [Tooltip("Controls the intensity of the vignette effect to occlude the Lens Flare effect at the center of the screen.")]
        public ClampedFloatParameter vignetteIntensity = new ClampedFloatParameter(1f, 0f, 1f);


        // public ClampedFloatParameter blurSize = new ClampedFloatParameter(4f, 0f, 16f);
        // public ClampedIntParameter blurSampleCount = new ClampedIntParameter(4, 2, 8);

        [Header("Chromatic Abberation")]
        /// <summary>
        /// Specifies a Texture which HDRP uses to shift the hue of chromatic aberrations. If null, HDRP creates a default texture.
        /// </summary>
        [Tooltip("Specifies a Texture which HDRP uses to shift the hue of chromatic aberrations. If null, HDRP creates a default texture.")]
        public Texture2DParameter spectralLut = new Texture2DParameter(null);
        /// <summary>
        /// Controls the strength of the Chromatic Aberration effect. The higher the value, the more light is dispersed on the sides of the screen
        /// </summary>
        [Tooltip("Controls the strength of the Chromatic Aberration effect. The higher the value, the more light is dispersed on the sides of the screen.")]
        public ClampedFloatParameter chromaticAbberationIntensity = new ClampedFloatParameter(0.5f, 0f, 1f);
        /// <summary>
        /// Controls the number of samples HDRP uses to render the effect. A lower sample number results in better performance.
        /// </summary>
        [Tooltip("Controls the number of samples HDRP uses to render the effect. A lower sample number results in better performance.")]
        public ClampedIntParameter chromaticAbberationSampleCount = new ClampedIntParameter(6, 2, 24);



        // public ClampedFloatParameter blurContribution = new ClampedFloatParameter(0.0f, 0f, 1f);
        // public ClampedFloatParameter chromaContribution = new ClampedFloatParameter(1f, 0f, 1f);

        /// <summary>
        /// Mandatory function, cannot have an Override without it
        /// </summary>
        /// <returns></returns>
        public bool IsActive()
        {
            return intensityMultiplier.value > 0f;
        }
    }

}
