using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable, VolumeComponentMenu("EyeDataInfo")]
    public sealed class EyeDataInfo : VolumeComponent
    {
        public TextureParameter albedoTexture = new TextureParameter(null);
        public TextureParameter maskTexture = new TextureParameter(null);
        public BoolParameter useCustomNormalMap = new BoolParameter(false);
        public BoolParameter useCustomRoughness = new BoolParameter(false);
        public ClampedFloatParameter bumpiness = new ClampedFloatParameter(1.0f, 0.0f, 5.0f);

        // optional
        public TextureParameter normalTexture = new TextureParameter(null);
        public TextureParameter roguhnessTexture = new TextureParameter(null);

        private BoolParameter needsUpdating = new BoolParameter(true);

        public bool NeedsUpdating()
        {
            return needsUpdating.value;
        }

        public void ToggleUpdateDone()
        {
            needsUpdating.value = false;
        }

    }
}
