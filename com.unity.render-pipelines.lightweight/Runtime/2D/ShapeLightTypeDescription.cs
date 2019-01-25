using System;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    [Serializable]
    public struct _2DLightOperationDescription
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
        [SerializeField] internal Color globalColor;
        [SerializeField] internal TextureChannel maskTextureChannel;
        [SerializeField] internal BlendMode blendMode;
        [SerializeField] internal float renderTextureScale;
        [SerializeField] internal BlendFactors customBlendFactors;

        internal Vector2 blendFactors
        {
            get
            {
                var result = new Vector2();

                switch (blendMode)
                {
                    case BlendMode.Additive:
                        result.x = 0.0f;
                        result.y = 1.0f;
                        break;
                    case BlendMode.Modulate:
                        result.x = 1.0f;
                        result.y = 0.0f;
                        break;
                    case BlendMode.Modulate2X:
                        result.x = 2.0f;
                        result.y = 0.0f;
                        break;
                    case BlendMode.Subtractive:
                        result.x = 0.0f;
                        result.y = -1.0f;
                        break;
                    case BlendMode.Custom:
                        result.x = customBlendFactors.modulate;
                        result.y = customBlendFactors.additve;
                        break;
                    default:
                        result = Vector2.zero;
                        break;
                }

                return result;
            }
        }

        internal Vector4 maskTextureChannelFilter
        {
            get
            {
                switch(maskTextureChannel)
                {
                    case TextureChannel.R:
                        return new Vector4(1.0f, 0.0f, 0.0f, 0.0f);
                    case TextureChannel.G:
                        return new Vector4(0.0f, 1.0f, 0.0f, 0.0f);
                    case TextureChannel.B:
                        return new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
                    case TextureChannel.A:
                        return new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
                    case TextureChannel.None:
                    default:
                        return Vector4.zero;
                }
            }
        }
    }
}
