using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX
{
    class AttributeVariantOverLife : IVariantProvider
    {
        public Dictionary<string, object[]> variants
        {
            get
            {
                return new Dictionary<string, object[]>
                {
                    { "attribute", OverLifeAttributeProvider.AllAttributeOverLife }
                };
            }
        }
    }

    class OverLifeAttributeProvider : IStringProvider
    {
        public static string[] AllAttributeOverLife = VFXAttribute.AllIncludingVariadicWritable.Except(new VFXAttribute[] { VFXAttribute.Age, VFXAttribute.Lifetime, VFXAttribute.Alive }.Select(e => e.name)).ToArray();

        public string[] GetAvailableString()
        {
            return AllAttributeOverLife;
        }
    }
}

namespace UnityEditor.VFX.Block
{
    //[VFXInfo(category = "Attribute/Lifetime")] DEPRECATED
    class AttributeOverLife : VFXBlock
    {
        public enum ComputeMode
        {
            Uniform,
            PerComponent
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), StringProvider(typeof(OverLifeAttributeProvider))]
        public string attribute = VFXAttribute.AllIncludingVariadic.First();

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public AttributeCompositionMode Composition = AttributeCompositionMode.Overwrite;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public ComputeMode Mode = ComputeMode.PerComponent;

        [VFXSetting]
        public VariadicChannelOptions channels = VariadicChannelOptions.XYZ;
        private static readonly char[] channelNames = new char[] { 'x', 'y', 'z' };

        public override string name { get { return VFXBlockUtility.GetNameString(Composition) + " " + ObjectNames.NicifyVariableName(attribute) + " over Life"; } }
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
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                if (currentAttribute.variadic == VFXVariadic.False)
                    yield return "channels";

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

                string channelSource = GetFetchValueString(GenerateLocalAttributeName(attrib.name), attributeSize, Mode);

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

        public string GetFetchValueString(string localName, int size, ComputeMode mode)
        {
            string output = "float t = age / lifetime;\n";
            output += string.Format("float{0} value = 0.0f;\n", (size == 1) ? "" : size.ToString());

            if (mode == ComputeMode.Uniform)
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
                    if (size > 0) output += string.Format("value{1} = SampleCurve({0}, t);\n", localName + (size == 1 ? "" : "_x"), (size == 1 ? "" : "[0]"));
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

                string localName = GenerateLocalAttributeName(attrib.name);
                if (Mode == ComputeMode.Uniform)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(AnimationCurve), localName), VFXResources.defaultResources.animationCurve);
                else
                {
                    if (attrib.Equals(VFXAttribute.Color))
                    {
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(Gradient), localName), VFXResources.defaultResources.gradient);
                    }
                    else
                    {
                        int size = VFXExpression.TypeToSize(attrib.type);
                        if (attrib.variadic == VFXVariadic.True)
                            size = channels.ToString().Length;

                        if (size > 0) yield return new VFXPropertyWithValue(new VFXProperty(typeof(AnimationCurve), localName + (size == 1 ? "" : "_x")), VFXResources.defaultResources.animationCurve);
                        if (size > 1) yield return new VFXPropertyWithValue(new VFXProperty(typeof(AnimationCurve), localName + "_y"), VFXResources.defaultResources.animationCurve);
                        if (size > 2) yield return new VFXPropertyWithValue(new VFXProperty(typeof(AnimationCurve), localName + "_z"), VFXResources.defaultResources.animationCurve);
                        if (size > 3) yield return new VFXPropertyWithValue(new VFXProperty(typeof(AnimationCurve), localName + "_w"), VFXResources.defaultResources.animationCurve);
                    }
                }

                if (Composition == AttributeCompositionMode.Blend)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "Blend"));
            }
        }

        private VFXAttribute currentAttribute
        {
            get
            {
                return VFXAttribute.Find(attribute);
            }
        }

        public override void Sanitize()
        {
            Debug.Log("Sanitizing Graph: Automatically replace AttributeOverLife with AttributeFromCurve");

            var attributeFromCurve = CreateInstance<AttributeFromCurve>();

            attributeFromCurve.SetSettingValue("attribute", attribute);
            attributeFromCurve.SetSettingValue("Composition", Composition);
            attributeFromCurve.SetSettingValue("Mode", (AttributeFromCurve.ComputeMode)Mode);
            attributeFromCurve.SetSettingValue("SampleMode", AttributeFromCurve.CurveSampleMode.OverLife);
            attributeFromCurve.SetSettingValue("channels", channels);

            // Transfer links
            VFXSlot.CopyLinksAndValue(attributeFromCurve.GetInputSlot(0), GetInputSlot(0), true);

            ReplaceModel(attributeFromCurve, this);
        }
    }
}
