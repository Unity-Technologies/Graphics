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
    }
}
