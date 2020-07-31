using System;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Settings to control how image comparison is performed by <c>ImageAssert.</c>
    /// </summary>
    [Serializable]
    public class ImageComparisonSettings
    {
        /// <summary>
        /// The width to use for the rendered image. If a reference image already exists for this
        /// test and has a different size the test will fail.
        /// </summary>
        [Tooltip("The width to use for the rendered image.")]
        public int TargetWidth = 512;

        /// <summary>
        /// The height to use for the rendered image. If a reference image already exists for this
        /// test and has a different size the test will fail.
        /// </summary>
        [Tooltip("The height to use for the rendered image.")]
        public int TargetHeight = 512;

        /// <summary>
        /// The permitted perceptual difference between individual pixels of the images.
        /// 
        /// The deltaE for each pixel of the image is compared and any differences below this
        /// threshold are ignored.
        /// </summary>
        [Tooltip("The permitted perceptual difference between individual pixels of the images.")]
        public float PerPixelCorrectnessThreshold;

        /// <summary>
        /// The maximum permitted average error value across the entire image. If the average
        /// per-pixel difference across the image is above this value, the images are considered
        /// not to be equal.
        /// </summary>
        [Tooltip("The maximum permitted average error value across the entire image.")]
        public float AverageCorrectnessThreshold;

        /// <summary>
        /// Use HDR rendering
        /// </summary>
        [Tooltip("If enabled, render textures will be created with DefaultHDR format.")]
        public bool UseHDR = false;

        /// <summary>
        /// Use back buffer capture
        /// </summary>
        [Tooltip("If enabled, tests will use the back buffer, as opposed to a render texture.")]
        public bool UseBackBuffer = false;

        /// <summary>
        /// If using Back Buffer, reference images need to be 16:9 aspect ratio
        /// </summary>
        [Tooltip("If using Back Buffer, reference images need to be in one of these resolutions.")]
        public Resolution ImageResolution;
        public enum Resolution
        {
            w1920h1080,
            w1600h900,
            w1280h720,
            w960h540,
            w640h360
        }
    }
}
