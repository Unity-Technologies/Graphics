using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor.VFX.Operator;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.VFX.Block
{
    enum AttributeCompositionMode
    {
        Overwrite,
        Add,
        Multiply,
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
        private static readonly Dictionary<AttributeCompositionMode, string[]> s_SynonymMap = new()
        {
            { AttributeCompositionMode.Add, new [] {"+"} },
            { AttributeCompositionMode.Overwrite, new [] {"="} },
            { AttributeCompositionMode.Multiply, new [] {"*"} },
            { AttributeCompositionMode.Blend, new [] {"%"} },
        };

        public static string GetNameString(AttributeCompositionMode mode)
        {
            switch (mode)
            {
                case AttributeCompositionMode.Overwrite: return "Set";
                case AttributeCompositionMode.Add: return "Add";
                case AttributeCompositionMode.Multiply: return "Multiply";
                case AttributeCompositionMode.Blend: return "Blend";
                default: throw new ArgumentException();
            }
        }

        public static string GetNameString(RandomMode mode)
        {
            switch (mode)
            {
                case RandomMode.Off: return "";
                case RandomMode.PerComponent: return "Random (Per-component)";
                case RandomMode.Uniform: return "Random (Uniform)";
                default: throw new ArgumentException();
            }
        }

        public static string GetNameString(AttributeFromCurve.CurveSampleMode mode)
        {
            switch (mode)
            {
                case AttributeFromCurve.CurveSampleMode.OverLife: return "Over Life";
                case AttributeFromCurve.CurveSampleMode.BySpeed: return "By Speed";
                case AttributeFromCurve.CurveSampleMode.Random: return "Random from Curve";
                case AttributeFromCurve.CurveSampleMode.RandomConstantPerParticle: return "Random Constant/Particle";
                case AttributeFromCurve.CurveSampleMode.Custom: return "Custom";
                default: throw new ArgumentException();
            }
        }

        public static string GetNameString(AttributeFromMap.AttributeMapSampleMode mode)
        {
            switch (mode)
            {
                case AttributeFromMap.AttributeMapSampleMode.IndexRelative: return "Index Relative";
                case AttributeFromMap.AttributeMapSampleMode.Index: return "Index";
                case AttributeFromMap.AttributeMapSampleMode.Sequential: return "Sequential";
                case AttributeFromMap.AttributeMapSampleMode.Sample2DLOD: return "2D";
                case AttributeFromMap.AttributeMapSampleMode.Sample3DLOD: return "3D";
                case AttributeFromMap.AttributeMapSampleMode.Random: return "Random";
                case AttributeFromMap.AttributeMapSampleMode.RandomConstantPerParticle: return "Random Constant/Particle";
                default: throw new ArgumentException();
            }
        }

        public static string GetComposeString(AttributeCompositionMode mode, params string[] parameters)
        {
            switch (mode)
            {
                case AttributeCompositionMode.Overwrite: return string.Format("{0} = {1};", parameters);
                case AttributeCompositionMode.Add: return string.Format("{0} += {1};", parameters);
                case AttributeCompositionMode.Multiply: return string.Format("{0} *= {1};", parameters);
                case AttributeCompositionMode.Blend: return string.Format("{0} = lerp({0},{1},{2});", parameters);
                default: throw new System.NotImplementedException("VFXBlockUtility.GetComposeFormatString() does not implement return string for : " + mode.ToString());
            }
        }

        public static string GetNameString(Noise.DimensionCount mode)
        {
            switch (mode)
            {
                case Noise.DimensionCount.One: return "1D";
                case Noise.DimensionCount.Two: return "2D";
                case Noise.DimensionCount.Three: return "3D";
                default: throw new NotImplementedException("VFXBlockUtility.GetNameString() does not implement return string for : " + mode);
            }
        }

        public static string GetNameString(CurlNoise.DimensionCount mode)
        {
            switch (mode)
            {
                case CurlNoise.DimensionCount.Two: return "2D";
                case CurlNoise.DimensionCount.Three: return "3D";
                default: throw new NotImplementedException("VFXBlockUtility.GetNameString() does not implement return string for : " + mode);
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

        private static bool ConvertToVariadicAttributeIfNeeded(VFXGraph vfxGraph, ref string attribName, out VariadicChannelOptions outChannel)
        {
            try
            {
                if (!vfxGraph.attributesManager.TryFind(attribName, out var attrib))
                {
                    throw new InvalidOperationException($"Could not find attribute {attribName}");
                }

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
                            throw new InvalidOperationException(string.Format("Cannot convert {0} to variadic version",
                                attrib.name));
                    }

                    // Just to ensure the attribute can be found
                    Assert.IsTrue(vfxGraph.attributesManager.Exist(attrib.name.Substring(0, attrib.name.Length - 1)));
                    outChannel = channel;

                    return true;
                }
            }
            catch (ArgumentException)
            {
            }

            outChannel = VariadicChannelOptions.X;
            return false;
        }

        static VariadicChannelOptions ChannelFromMask(string mask)
        {
            mask = mask.ToLower();
            if (mask == "x")
                return VariadicChannelOptions.X;
            else if (mask == "y")
                return VariadicChannelOptions.Y;
            else if (mask == "z")
                return VariadicChannelOptions.Z;
            else if (mask == "xy")
                return VariadicChannelOptions.XY;
            else if (mask == "xz")
                return VariadicChannelOptions.XZ;
            else if (mask == "yz")
                return VariadicChannelOptions.YZ;
            else if (mask == "xyz")
                return VariadicChannelOptions.XYZ;
            return VariadicChannelOptions.X;
        }

        static string MaskFromChannel(VariadicChannelOptions channel)
        {
            switch (channel)
            {
                case VariadicChannelOptions.X: return "x";
                case VariadicChannelOptions.Y: return "y";
                case VariadicChannelOptions.Z: return "z";
                case VariadicChannelOptions.XY: return "xy";
                case VariadicChannelOptions.XZ: return "xz";
                case VariadicChannelOptions.YZ: return "yz";
                case VariadicChannelOptions.XYZ: return "xyz";
            }
            throw new InvalidOperationException("MaskFromChannel missing for " + channel);
        }

        public static bool ConvertSizeAttributeIfNeeded(ref string attribName, ref VariadicChannelOptions channels)
        {
            if (attribName == "size")
            {
                if (channels == VariadicChannelOptions.X) // Consider sizeX as uniform
                {
                    return true;
                }
                else
                {
                    attribName = "scale";
                    return true;
                }
            }

            if (attribName == "sizeX")
            {
                attribName = "size";
                channels = VariadicChannelOptions.X;
                return true;
            }

            if (attribName == "sizeY")
            {
                attribName = "scale";
                channels = VariadicChannelOptions.Y;
                return true;
            }

            if (attribName == "sizeZ")
            {
                attribName = "scale";
                channels = VariadicChannelOptions.Z;
                return true;
            }

            return false;
        }

        public static bool SanitizeAttribute(VFXGraph graph, ref string attribName, ref VariadicChannelOptions channels, int version)
        {
            bool settingsChanged = false;
            string oldName = attribName;
            VariadicChannelOptions oldChannels = channels;

            if (version < 1 && channels == VariadicChannelOptions.XZ) // Enumerators have changed
            {
                channels = VariadicChannelOptions.XYZ;
                settingsChanged = true;
            }

            if (version < 1 && VFXBlockUtility.ConvertSizeAttributeIfNeeded(ref attribName, ref channels))
            {
                Debug.Log(string.Format("Sanitizing attribute: Convert {0} with channel {2} to {1}", oldName, attribName, oldChannels));
                settingsChanged = true;
            }

            // Changes attribute to variadic version
            VariadicChannelOptions newChannels;
            if (VFXBlockUtility.ConvertToVariadicAttributeIfNeeded(graph, ref attribName, out newChannels))
            {
                Debug.Log(string.Format("Sanitizing attribute: Convert {0} to variadic attribute {1} with channel {2}", oldName, attribName, newChannels));
                channels = newChannels;
                settingsChanged = true;
            }

            return settingsChanged;
        }

        public static bool SanitizeAttribute(VFXGraph graph, ref string attribName, ref string channelsMask, int version)
        {
            var channels = ChannelFromMask(channelsMask);
            var settingsChanged = SanitizeAttribute(graph, ref attribName, ref channels, version);
            channelsMask = MaskFromChannel(channels);
            return settingsChanged;
        }

        internal static string[] GetCompositionSynonym(AttributeCompositionMode mode) => s_SynonymMap[mode];
    }
}
