using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    enum RandomMode
    {
        Off,
        PerComponent,
        Uniform,
    }

    class VFXBlockUtility
    {
        public static string GetNameString(AttributeCompositionMode mode)
        {
            switch (mode)
            {
                case AttributeCompositionMode.Overwrite: return "Set";
                case AttributeCompositionMode.Add: return "Add";
                case AttributeCompositionMode.Scale: return "Scale";
                case AttributeCompositionMode.Blend: return "Blend";
                default: throw new ArgumentException();
            }
        }

        public static string GetNameString(RandomMode mode)
        {
            switch (mode)
            {
                case RandomMode.Off: return "";
                case RandomMode.PerComponent: return "Random";
                case RandomMode.Uniform: return "Random";
                default: throw new ArgumentException();
            }
        }

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

        public static string GetRandomMacroString(RandomMode mode, int attributeSize, string postfix, params string[] parameters)
        {
            switch (mode)
            {
                case RandomMode.Off:
                    return parameters[0] + postfix;
                case RandomMode.Uniform:
                    return string.Format("lerp({0},{1},RAND)", parameters.Select(s => s + postfix).ToArray());
                case RandomMode.PerComponent:
                    string rand = GetRandStringFromSize(attributeSize);
                    return string.Format("lerp({0},{1}," + rand + ")", parameters.Select(s => s + postfix).ToArray());
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
            if (nbComponents >= 2 && data.IsCurrentAttributeWritten(VFXAttribute.SizeY))
                yield return new VFXAttributeInfo(VFXAttribute.SizeY, VFXAttributeMode.Read);
            if (nbComponents >= 3 && data.IsCurrentAttributeWritten(VFXAttribute.SizeZ))
                yield return new VFXAttributeInfo(VFXAttribute.SizeZ, VFXAttributeMode.Read);
        }

        public static string GetSizeVector(VFXContext context, int nbComponents = 3)
        {
            var data = context.GetData();

            string sizeX = data.IsCurrentAttributeRead(VFXAttribute.SizeX, context) ? "sizeX" : VFXAttribute.kDefaultSize.ToString();
            string sizeY = nbComponents >= 2 && data.IsCurrentAttributeRead(VFXAttribute.SizeY, context) ? "sizeY" : "sizeX";
            string sizeZ = nbComponents >= 3 && data.IsCurrentAttributeRead(VFXAttribute.SizeZ, context) ? "sizeZ" : string.Format("min({0},{1})", sizeX, sizeY);

            switch (nbComponents)
            {
                case 1: return sizeX;
                case 2: return string.Format("float2({0},{1})", sizeX, sizeY);
                case 3: return string.Format("float3({0},{1},{2})", sizeX, sizeY, sizeZ);
                default:
                    throw new ArgumentException("NbComponents must be between 1 and 3");
            }
        }

        public static string SetSizesFromVector(VFXContext context, string vector, int nbComponents = 3)
        {
            if (nbComponents < 1 || nbComponents > 3)
                throw new ArgumentException("NbComponents must be between 1 and 3");

            var data = context.GetData();

            string res = string.Empty;

            if (nbComponents >= 1 && data.IsCurrentAttributeWritten(VFXAttribute.SizeX, context))
                res += "sizeX = size.x;\n";
            if (nbComponents >= 2 && data.IsCurrentAttributeWritten(VFXAttribute.SizeY, context))
                res += "sizeY = size.y;\n";
            if (nbComponents >= 3 && data.IsCurrentAttributeWritten(VFXAttribute.SizeZ, context))
                res += "sizeZ = size.z;";

            return res;
        }

        public static bool ConvertToVariadicAttributeIfNeeded(string attribName, out string outAttribName, out VariadicChannelOptions outChannel)
        {
            var attrib = VFXAttribute.Find(attribName);

            if (attrib.variadic == VFXVariadic.BelongsToVariadic)
            {
                char component = attrib.name.ToLower().Last();
                VariadicChannelOptions channel;
                switch (component)
                {
                    case 'x':
                        channel = VariadicChannelOptions.X;
                        break;
                    case 'y':
                        channel = VariadicChannelOptions.Y;
                        break;
                    case 'z':
                        channel = VariadicChannelOptions.Z;
                        break;
                    default:
                        throw new InvalidOperationException(string.Format("Cannot convert {0} to variadic version", attrib.name));
                }

                outAttribName = VFXAttribute.Find(attrib.name.Substring(0, attrib.name.Length - 1)).name; // Just to ensure the attribute can be found
                outChannel = channel;

                return true;
            }

            outAttribName = string.Empty;
            outChannel = VariadicChannelOptions.X;
            return false;
        }
    }
}
