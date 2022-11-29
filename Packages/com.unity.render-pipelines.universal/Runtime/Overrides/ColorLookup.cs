using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    [Serializable, VolumeComponentMenuForRenderPipeline("Post-processing/Color Lookup", typeof(UniversalRenderPipeline))]
    public sealed class ColorLookup : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("A 2D Lookup Texture (LUT) to use for color grading.")]
        public TextureParameter texture = new TextureParameter(null);

        [Tooltip("How much of the lookup texture will contribute to the color grading effect.")]
        public ClampedFloatParameter contribution = new ClampedFloatParameter(0f, 0f, 1f);

        public bool IsActive() => contribution.value > 0f && ValidateLUT();

        public bool IsTileCompatible() => true;

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
