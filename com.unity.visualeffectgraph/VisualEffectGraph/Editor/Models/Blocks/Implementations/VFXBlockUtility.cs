using System;
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

    enum RandomMode
    {
        Off,
        PerComponent,
        Uniform,
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

        public static string GetRandomMacroString(RandomMode mode , VFXAttribute attribute)
        {
            switch(mode)
            {
                case RandomMode.Off:
                    return "{0}";
                case RandomMode.Uniform:
                    return "lerp({0},{1},RAND)";
                case RandomMode.PerComponent:
                    string rand = GetRandStringFromSize(VFXExpression.TypeToSize(attribute.type));
                    return "lerp({0},{1}," + rand + ")";
                default: throw new System.NotImplementedException("VFXBlockUtility.GetRandomMacroString() does not implement return string for RandomMode : " + mode.ToString());
            }
        }

        public static string GetRandStringFromSize(int size)
        {
            switch(size)
            {
                case 1: return "RAND";
                case 2: return "RAND2";
                case 3: return "RAND3";
                case 4: return "RAND4";
                default: throw new System.NotImplementedException("VFXBlockUtility.GetRandStringFromSize() does not implement return string for component count : " + size.ToString());
            }
        }

    }
}
