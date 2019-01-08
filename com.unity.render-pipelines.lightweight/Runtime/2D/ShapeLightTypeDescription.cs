using System;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    [Serializable]
    public struct _2DShapeLightTypeDescription
    {
        internal enum TextureChannel
        {
            None, R, G, B, A
        }

        internal enum BlendMode
        {
            Additive = 0,
            Modulate = 1,
            Modulate2X = 2,
            Subtractive = 3,
            Custom = 99
        }

        [Serializable]
        internal struct BlendFactors
        {
            [SerializeField] internal float modulate;
            [SerializeField] internal float additve;
        }

        public bool enabled;
        public string name;
        [SerializeField] internal TextureChannel maskTextureChannel;
        [SerializeField] internal BlendMode blendMode;
        [SerializeField] internal BlendFactors customBlendFactors;
        [SerializeField] internal float renderTextureScale;
    }
}
