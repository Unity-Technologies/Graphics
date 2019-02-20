using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable, VolumeComponentMenu("Post-processing/Color Lookup")]
    public sealed class ColorLookup : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("A custom 3D texture lookup table to apply.")]
        public TextureParameter texture = new TextureParameter(null);

        [Tooltip("How much of the lookup texture will contribute to the color grading effect.")]
        public ClampedFloatParameter contribution = new ClampedFloatParameter(1f, 0f, 1f);

        public bool IsActive()
        {
            return texture.value != null
                && contribution.value > 0f;
        }

        public bool ValidateTexture()
        {
            var hdAsset = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            if (hdAsset == null || texture.value == null)
                return false;

            if (texture.value.width != hdAsset.currentPlatformRenderPipelineSettings.postProcessSettings.lutSize)
                return false;

            bool valid = false;

            switch (texture.value)
            {
                case Texture3D t:
                    valid |= t.width == t.height
                          && t.height == t.depth;
                    break;
                case RenderTexture rt:
                    valid |= rt.dimension == TextureDimension.Tex3D
                          && rt.width == rt.height
                          && rt.height == rt.volumeDepth;
                    break;
            }

            return valid;
        }
    }
}
