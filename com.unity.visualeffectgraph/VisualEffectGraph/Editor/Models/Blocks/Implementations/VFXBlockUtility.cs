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
        public static string GetComposeString(AttributeCompositionMode mode, params string[] parameters)
        {
            switch (mode)
            {
                case AttributeCompositionMode.Overwrite: return string.Format("{0} = {1};", parameters);
                case AttributeCompositionMode.Add: return string.Format("{0} += {1};", parameters);
                case AttributeCompositionMode.Scale: return string.Format("{0} *= {1};", parameters);
                case AttributeCompositionMode.Blend: return string.Format("{0} = lerp({0},{1},{2});", parameters);
                default: throw new System.NotImplementedException("VFXBlockUtility.GetComposeFormatString() does not implement return string for : " + mode.ToString());
            }
        }

        public static string GetRandomMacroString(RandomMode mode , VFXAttribute attribute, params string[] parameters)
        {
            switch (mode)
            {
                case RandomMode.Off:
                    return parameters[0];
                case RandomMode.Uniform:
                    return string.Format("lerp({0},{1},RAND)", parameters);
                case RandomMode.PerComponent:
                    string rand = GetRandStringFromSize(VFXExpression.TypeToSize(attribute.type));
                    return string.Format("lerp({0},{1}," + rand + ")", parameters);
                default: throw new System.NotImplementedException("VFXBlockUtility.GetRandomMacroString() does not implement return string for RandomMode : " + mode.ToString());
            }
        }

        public static string GetRandStringFromSize(int size)
        {
            if (size < 0 || size > 4)
                throw new ArgumentOutOfRangeException("Size can be only of 1, 2, 3 or 4");

            return "RAND" + ((size != 1) ? size.ToString() : "");
        }

        public static IEnumerable<VFXAttributeInfo> GetReadableSizeAttributes(VFXData data, int nbComponents = 3)
        {
            if (nbComponents < 1 || nbComponents > 3)
                throw new ArgumentException("NbComponents must be between 1 and 3");

            if (nbComponents >= 1)
                yield return new VFXAttributeInfo(VFXAttribute.SizeX, VFXAttributeMode.Read);
            if (nbComponents >= 2 && data.IsCurrentAttributeUsed(VFXAttribute.SizeY))
                yield return new VFXAttributeInfo(VFXAttribute.SizeY, VFXAttributeMode.Read);
            if (nbComponents >= 3 && data.IsCurrentAttributeUsed(VFXAttribute.SizeZ))
                yield return new VFXAttributeInfo(VFXAttribute.SizeY, VFXAttributeMode.Read);
        }

        public static string GetSizeVector(VFXData data, int nbComponents = 3)
        {
            if (nbComponents < 1 || nbComponents > 3)
                throw new ArgumentException("NbComponents must be between 1 and 3");

            string sizeX = nbComponents >= 1 && data.IsCurrentAttributeUsed(VFXAttribute.SizeX) ? "sizeX" : VFXAttribute.kDefaultSize.ToString();
            string sizeY = nbComponents >= 2 && data.IsCurrentAttributeUsed(VFXAttribute.SizeY) ? "sizeY" : "sizeX";
            string sizeZ = nbComponents >= 3 && data.IsCurrentAttributeUsed(VFXAttribute.SizeZ) ? "sizeZ" : string.Format("min({0},{1})", sizeX, sizeY);
            return string.Format("float3({0},{1},{2})", sizeX, sizeY, sizeZ);
        }
    }
}
