using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    class AttributeVariantFromCurve : IVariantProvider
    {
        public Dictionary<string, object[]> variants
        {
            get
            {
                return new Dictionary<string, object[]>
                {
                    { "attribute", FromCurveAttributeProvider.AllAttributeFromCurve }
                };
            }
        }
    }

    class FromCurveAttributeProvider : IStringProvider
    {
        public static string[] AllAttributeFromCurve = VFXAttribute.AllIncludingVariadicWritable.Except(new VFXAttribute[] { VFXAttribute.Age, VFXAttribute.Lifetime, VFXAttribute.Alive }.Select(e => e.name)).ToArray();

        public string[] GetAvailableString()
        {
            return AllAttributeFromCurve;
        }
    }
}

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Attribute/Curve", variantProvider = typeof(AttributeVariantFromCurve))]
    class AttributeFromCurve : VFXBlock
    {
        public enum CurveSampleMode
        {
            OverLife,
            BySpeed,
            Random,
            RandomUniformPerParticle
        }

        public enum ComputeMode
        {
            Uniform,
            PerComponent
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), StringProvider(typeof(FromCurveAttributeProvider))]
        public string attribute = VFXAttribute.AllIncludingVariadic.First();

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public AttributeCompositionMode Composition = AttributeCompositionMode.Overwrite;

        [VFXSetting, Tooltip("How to sample the curve")]
        public CurveSampleMode SampleMode = CurveSampleMode.OverLife;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public ComputeMode Mode = ComputeMode.PerComponent;

        [VFXSetting]
        public VariadicChannelOptions channels = VariadicChannelOptions.XYZ;
        private static readonly char[] channelNames = new char[] { 'x', 'y', 'z' };

        public override string libraryName
        {
            get
            {
                return VFXBlockUtility.GetNameString(Composition) + " " + ObjectNames.NicifyVariableName(attribute) + " from Curve";
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
                    default:
                        throw new NotImplementedException("Invalid CurveSampleMode");
                }
            }
        }

        public override VFXContextType compatibleContexts { get { return VFXContextType.kUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                var attrib = currentAttribute;
                VFXAttributeMode attributeMode = (Composition != AttributeCompositionMode.Overwrite) ? VFXAttributeMode.ReadWrite : VFXAttributeMode.Write;
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

                yield return new VFXAttributeInfo(VFXAttribute.Age, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Lifetime, VFXAttributeMode.Read);

                if (SampleMode == CurveSampleMode.BySpeed)
                    yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.Read);

                if (SampleMode == CurveSampleMode.Random) yield return new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWrite);
                if (SampleMode == CurveSampleMode.RandomUniformPerParticle) yield return new VFXAttributeInfo(VFXAttribute.ParticleId, VFXAttributeMode.Read);
            }
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

                int attributeSize = VFXExpression.TypeToSize(attrib.type);
                int loopCount = 1;
                if (attrib.variadic == VFXVariadic.True)
                {
                    attributeSize = channels.ToString().Length;
                    loopCount = attributeSize;
                }

                string channelSource = GetFetchValueString(GenerateLocalAttributeName(attrib.name), attributeSize, Mode, SampleMode);

                for (int i = 0; i < loopCount; i++)
                {
                    string paramPostfix = (attrib.variadic == VFXVariadic.True) ? "." + channelNames[i] : "";
                    string attributePostfix = (attrib.variadic == VFXVariadic.True) ? char.ToUpper(channels.ToString()[i]).ToString() : "";

                    if (Composition == AttributeCompositionMode.Blend)
                        channelSource += VFXBlockUtility.GetComposeString(Composition, attrib.name + attributePostfix, "value" + paramPostfix, "Blend");
                    else
                        channelSource += VFXBlockUtility.GetComposeString(Composition, attrib.name + attributePostfix, "value" + paramPostfix);

                    if (i < loopCount - 1)
                        channelSource += "\n";
                }

                source += channelSource;

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
                    output += string.Format("value = SampleGradient({0}, t).rgb;\n", localName);
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
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(Vector2), "SpeedRange"));

                string localName = GenerateLocalAttributeName(attrib.name);
                if (Mode == ComputeMode.Uniform || size == 1)
                {
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(AnimationCurve), localName));
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

                if (Composition == AttributeCompositionMode.Blend)
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
