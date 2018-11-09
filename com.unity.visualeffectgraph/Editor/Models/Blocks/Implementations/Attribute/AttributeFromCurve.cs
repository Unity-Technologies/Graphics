using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Attribute/Curve", variantProvider = typeof(AttributeVariantReadWritable))]
    class AttributeFromCurve : VFXBlock
    {
        public enum CurveSampleMode
        {
            OverLife,
            BySpeed,
            Random,
            RandomUniformPerParticle,
            Custom
        }

        public enum ComputeMode
        {
            Uniform,
            PerComponent
        }

        public enum ColorApplicationMode
        {
            Color = 1 << 0,
            Alpha = 1 << 1,
            ColorAndAlpha = Color | Alpha,
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), StringProvider(typeof(ReadWritableAttributeProvider)), Tooltip("Target Attribute")]
        public string attribute = VFXAttribute.AllIncludingVariadicWritable.First();

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public AttributeCompositionMode Composition = AttributeCompositionMode.Overwrite;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public AttributeCompositionMode AlphaComposition = AttributeCompositionMode.Overwrite;

        [VFXSetting, Tooltip("How to sample the curve")]
        public CurveSampleMode SampleMode = CurveSampleMode.OverLife;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public ComputeMode Mode = ComputeMode.PerComponent;

        [VFXSetting, Tooltip("Select whether the color is applied to RGB, alpha, or both")]
        public ColorApplicationMode ColorMode = ColorApplicationMode.ColorAndAlpha;

        [VFXSetting]
        public VariadicChannelOptions channels = VariadicChannelOptions.XYZ;
        private static readonly char[] channelNames = new char[] { 'x', 'y', 'z' };

        public override string libraryName
        {
            get
            {
                string attributeSource = currentAttribute.Equals(VFXAttribute.Color) ? "from Gradient" : "from Curve";
                return string.Format("{0} {1} {2}", VFXBlockUtility.GetNameString(Composition), ObjectNames.NicifyVariableName(attribute), attributeSource);
            }
        }

        public override string name
        {
            get
            {
                string n = VFXBlockUtility.GetNameString(Composition) + " " + ObjectNames.NicifyVariableName(attribute);
                switch (SampleMode)
                {
                    case CurveSampleMode.OverLife: return n + " over Life";
                    case CurveSampleMode.BySpeed: return n + " by Speed";
                    case CurveSampleMode.Random: return n + " randomized";
                    case CurveSampleMode.RandomUniformPerParticle: return n + " randomized";
                    case CurveSampleMode.Custom: return n + " custom";
                    default:
                        throw new NotImplementedException("Invalid CurveSampleMode");
                }
            }
        }

        public override VFXContextType compatibleContexts { get { return VFXContextType.kInitAndUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                VFXAttributeMode attributeMode = (Composition != AttributeCompositionMode.Overwrite) ? VFXAttributeMode.ReadWrite : VFXAttributeMode.Write;
                VFXAttributeMode alphaAttributeMode = (AlphaComposition != AttributeCompositionMode.Overwrite) ? VFXAttributeMode.ReadWrite : VFXAttributeMode.Write;

                var attrib = currentAttribute;
                if (attrib.Equals(VFXAttribute.Color))
                {
                    if ((ColorMode & ColorApplicationMode.Color) != 0)
                        yield return new VFXAttributeInfo(VFXAttribute.Color, attributeMode);
                    if ((ColorMode & ColorApplicationMode.Alpha) != 0)
                        yield return new VFXAttributeInfo(VFXAttribute.Alpha, alphaAttributeMode);
                }
                else
                {
                    if (attrib.variadic == VFXVariadic.True)
                    {
                        string channelsString = channels.ToString();
                        for (int i = 0; i < channelsString.Length; i++)
                            yield return new VFXAttributeInfo(VFXAttribute.Find(attrib.name + channelsString[i]), attributeMode);
                    }
                    else
                    {
                        yield return new VFXAttributeInfo(attrib, attributeMode);
                    }
                }

                yield return new VFXAttributeInfo(VFXAttribute.Age, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Lifetime, VFXAttributeMode.Read);

                if (SampleMode == CurveSampleMode.BySpeed)
                    yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.Read);

                if (SampleMode == CurveSampleMode.Random) yield return new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWrite);
                if (SampleMode == CurveSampleMode.RandomUniformPerParticle) yield return new VFXAttributeInfo(VFXAttribute.ParticleId, VFXAttributeMode.Read);
            }
        }

        public override void Sanitize()
        {
            string newAttrib;
            VariadicChannelOptions channel;

            // Changes attribute to variadic version
            if (VFXBlockUtility.ConvertToVariadicAttributeIfNeeded(attribute, out newAttrib, out channel))
            {
                Debug.Log(string.Format("Sanitizing AttributeFromCurve: Convert {0} to variadic attribute {1} with channel {2}", attribute, newAttrib, channel));
                attribute = newAttrib;
                channels = channel;
                Invalidate(InvalidationCause.kSettingChanged);
            }

            base.Sanitize();
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                var attrib = currentAttribute;

                if (attrib.variadic == VFXVariadic.False)
                    yield return "channels";
                if (VFXExpression.TypeToSize(attrib.type) == 1)
                    yield return "Mode";

                if (!currentAttribute.Equals(VFXAttribute.Color))
                {
                    yield return "ColorMode";
                    yield return "AlphaComposition";
                }

                foreach (var setting in base.filteredOutSettings)
                    yield return setting;
            }
        }

        static private string GenerateLocalAttributeName(string name)
        {
            return name[0].ToString().ToUpper() + name.Substring(1);
        }

        public override string source
        {
            get
            {
                string source = "";

                var attrib = currentAttribute;

                bool isColor = currentAttribute.Equals(VFXAttribute.Color);
                int attributeCount = isColor ? 2 : 1;

                int attributeSize = isColor ? 4 : VFXExpression.TypeToSize(attrib.type);
                int loopCount = 1;
                if (attrib.variadic == VFXVariadic.True)
                {
                    attributeSize = channels.ToString().Length;
                    loopCount = attributeSize;
                }

                source += GetFetchValueString(GenerateLocalAttributeName(attrib.name), attributeSize, Mode, SampleMode);

                int attributeAddedCount = 0;
                for (int attribIndex = 0; attribIndex < attributeCount; attribIndex++)
                {
                    string attribName = attrib.name;
                    if (isColor)
                    {
                        if (((int)ColorMode & (1 << attribIndex)) == 0)
                            continue;
                        if (attribIndex == 1)
                            attribName = VFXAttribute.Alpha.name;
                    }

                    string channelSource = "";
                    if (attributeAddedCount > 0)
                        channelSource += "\n";

                    for (int i = 0; i < loopCount; i++)
                    {
                        AttributeCompositionMode compositionMode = Composition;
                        string paramPostfix = (attrib.variadic == VFXVariadic.True) ? "." + channelNames[i] : "";

                        if (isColor)
                        {
                            if (attribIndex == 0)
                            {
                                paramPostfix = ".rgb";
                            }
                            else
                            {
                                paramPostfix = ".a";
                                compositionMode = AlphaComposition;
                            }
                        }

                        string attributePostfix = (attrib.variadic == VFXVariadic.True) ? char.ToUpper(channels.ToString()[i]).ToString() : "";

                        if (compositionMode == AttributeCompositionMode.Blend)
                            channelSource += VFXBlockUtility.GetComposeString(compositionMode, attribName + attributePostfix, "value" + paramPostfix, "Blend");
                        else
                            channelSource += VFXBlockUtility.GetComposeString(compositionMode, attribName + attributePostfix, "value" + paramPostfix);

                        if (i < loopCount - 1)
                            channelSource += "\n";
                    }

                    source += channelSource;
                    attributeAddedCount++;
                }

                return source;
            }
        }

        public string GetFetchValueString(string localName, int size, ComputeMode computeMode, CurveSampleMode sampleMode)
        {
            string output;
            switch (SampleMode)
            {
                case CurveSampleMode.OverLife: output = "float t = age / lifetime;\n"; break;
                case CurveSampleMode.BySpeed: output = "float t = saturate((length(velocity) - SpeedRange.x) * SpeedRange.y);\n"; break;
                case CurveSampleMode.Random: output = "float t = RAND;\n"; break;
                case CurveSampleMode.RandomUniformPerParticle: output = "float t = FIXED_RAND(0x34634bc2);\n"; break;
                case CurveSampleMode.Custom: output = "float t = SampleTime;\n"; break;
                default:
                    throw new NotImplementedException("Invalid CurveSampleMode");
            }

            output += string.Format("float{0} value = 0.0f;\n", (size == 1) ? "" : size.ToString());

            if (computeMode == ComputeMode.Uniform || size == 1)
            {
                output += string.Format("value = SampleCurve({0}, t);\n", localName);
            }
            else
            {
                if (currentAttribute.Equals(VFXAttribute.Color))
                {
                    output += string.Format("value = SampleGradient({0}, t);\n", localName);
                }
                else
                {
                    if (size > 0) output += string.Format("value[0] = SampleCurve({0}, t);\n", localName + "_x");
                    if (size > 1) output += string.Format("value[1] = SampleCurve({0}, t);\n", localName + "_y");
                    if (size > 2) output += string.Format("value[2] = SampleCurve({0}, t);\n", localName + "_z");
                    if (size > 3) output += string.Format("value[3] = SampleCurve({0}, t);\n", localName + "_w");
                }
            }

            return output;
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var attrib = currentAttribute;

                int size = VFXExpression.TypeToSize(attrib.type);
                if (attrib.variadic == VFXVariadic.True)
                    size = channels.ToString().Length;

                if (SampleMode == CurveSampleMode.BySpeed)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(Vector2), "SpeedRange", new VFXPropertyAttribute[] { new VFXPropertyAttribute(VFXPropertyAttribute.Type.kMin, 0.0f) }));
                else if (SampleMode == CurveSampleMode.Custom)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "SampleTime"));

                string localName = GenerateLocalAttributeName(attrib.name);
                if (Mode == ComputeMode.Uniform || size == 1)
                {
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(AnimationCurve), localName), VFXResources.defaultResources.animationCurve);
                }
                else
                {
                    if (attrib.Equals(VFXAttribute.Color))
                    {
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(Gradient), localName), VFXResources.defaultResources.gradient);
                    }
                    else
                    {
                        if (size > 0) yield return new VFXPropertyWithValue(new VFXProperty(typeof(AnimationCurve), localName + "_x"), VFXResources.defaultResources.animationCurve);
                        if (size > 1) yield return new VFXPropertyWithValue(new VFXProperty(typeof(AnimationCurve), localName + "_y"), VFXResources.defaultResources.animationCurve);
                        if (size > 2) yield return new VFXPropertyWithValue(new VFXProperty(typeof(AnimationCurve), localName + "_z"), VFXResources.defaultResources.animationCurve);
                        if (size > 3) yield return new VFXPropertyWithValue(new VFXProperty(typeof(AnimationCurve), localName + "_w"), VFXResources.defaultResources.animationCurve);
                    }
                }

                if (Composition == AttributeCompositionMode.Blend || (attrib.Equals(VFXAttribute.Color) && AlphaComposition == AttributeCompositionMode.Blend))
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "Blend"));
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var p in GetExpressionsFromSlots(this).Where(e => e.name != "SpeedRange"))
                    yield return p;

                if (SampleMode == CurveSampleMode.BySpeed)
                {
                    var speedRange = inputSlots[0].GetExpression();
                    var speedRangeComponents = VFXOperatorUtility.ExtractComponents(speedRange).ToArray();
                    speedRangeComponents[1] = VFXOperatorUtility.OneExpression[VFXValueType.Float] / (speedRangeComponents[1] - speedRangeComponents[0]);
                    yield return new VFXNamedExpression(new VFXExpressionCombine(speedRangeComponents), "SpeedRange");
                }
            }
        }

        private VFXAttribute currentAttribute
        {
            get
            {
                return VFXAttribute.Find(attribute);
            }
        }
    }
}
