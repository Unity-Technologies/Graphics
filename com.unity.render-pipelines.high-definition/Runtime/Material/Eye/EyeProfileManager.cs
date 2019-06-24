using System;
using UnityEngine.Rendering;


namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable, VolumeComponentMenu("EyeProfileManager")]
    public sealed class EyeProfileManager : VolumeComponent
    {
        public TextureParameter albedoTexture = new TextureParameter(null);
        public TextureParameter maskTexture = new TextureParameter(null);

        public BoolParameter useCustomNormalMap = new BoolParameter(false);
        public BoolParameter useCustomRoughness = new BoolParameter(false);

        public ClampedFloatParameter bumpiness = new ClampedFloatParameter(1.0f, 0.0f, 5.0f);

        // optional
        public TextureParameter normalTexture = new TextureParameter(null);
        public TextureParameter roguhnessTexture = new TextureParameter(null);


        public bool IsActive()
        {
            return true;
        }
    }
}
