using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// A volume component that holds settings for the color lookup effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Post-processing/Color Lookup")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [URPHelpURL("integration-with-post-processing")]
    public sealed class ColorLookup : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// A 2D Lookup Texture (LUT) to use for color grading.
        /// </summary>
        [Tooltip("A 2D Lookup Texture (LUT) to use for color grading.")]
        public TextureParameter texture = new TextureParameter(null);

        /// <summary>
        /// Controls how much of the lookup texture will contribute to the color grading effect.
        /// </summary>
        [Tooltip("How much of the lookup texture will contribute to the color grading effect.")]
        public ClampedFloatParameter contribution = new ClampedFloatParameter(0f, 0f, 1f);

        /// <inheritdoc/>
        public bool IsActive() => contribution.value > 0f && ValidateLUT();

        /// <inheritdoc/>
        [Obsolete("Unused #from(2023.1)", false)]
        public bool IsTileCompatible() => true;

        /// <summary>
        /// Validates the lookup texture assigned to the volume component.
        /// </summary>
        /// <returns>True if the texture is valid, false otherwise.</returns>
        public bool ValidateLUT()
        {
            var asset = UniversalRenderPipeline.asset;
            if (asset == null || texture.value == null)
                return false;

            int lutSize = asset.colorGradingLutSize;
            if (texture.value.height != lutSize)
                return false;

            bool valid = false;

            switch (texture.value)
            {
                case Texture2D t:
                    valid |= t.width == lutSize * lutSize
                        && !GraphicsFormatUtility.IsSRGBFormat(t.graphicsFormat);
                    break;
                case RenderTexture rt:
                    valid |= rt.dimension == TextureDimension.Tex2D
                        && rt.width == lutSize * lutSize
                        && !rt.sRGB;
                    break;
            }

            return valid;
        }
    }
}
