using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    enum AttributeCompositionMode
    {
        Overwrite,
        Add,
        Scale,
        Blend
    }

    enum TextureDataEncoding
    {
        UnsignedNormalized,
        Signed
    }

    enum ColorApplicationMode
    {
        Color = 1 << 0,
        Alpha = 1 << 1,
        ColorAndAlpha = Color | Alpha,
    }

    class VFXBlockUtility
    {
        public static string GetComposeFormatString(AttributeCompositionMode mode)
        {
            switch (mode)
            {
                case AttributeCompositionMode.Overwrite: return "{0} = {1};";
                case AttributeCompositionMode.Add: return "{0} += {1};";
                case AttributeCompositionMode.Scale: return "{0} *= {1};";
                case AttributeCompositionMode.Blend: return "{0} = lerp({0},{1},{2});";
                default: throw new System.NotImplementedException("VFXBlockUtility.GetComposeFormatString() does not implement return string for : " + mode.ToString());
            }
        }
    }
}
