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
        public static string[] AllAttributeOverLife = VFXAttribute.AllWritable.Except(new VFXAttribute[] { VFXAttribute.Age, VFXAttribute.Lifetime, VFXAttribute.Alive }.Select(e => e.name)).ToArray();

        public string[] GetAvailableString()
        {
            return AllAttributeOverLife;
        }
    }
}

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Attribute", variantProvider = typeof(AttributeVariantOverLife))]
    class AttributeOverLife : VFXBlock
    {
        public enum ComputeMode
        {
            Uniform,
            PerComponent
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), StringProvider(typeof(OverLifeAttributeProvider))]
        public string attribute = VFXAttribute.All.First();

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public AttributeCompositionMode Composition = AttributeCompositionMode.Overwrite;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public ComputeMode Mode = ComputeMode.PerComponent;

        public override string name { get { return VFXBlockUtility.GetNameString(Composition) + " " + ObjectNames.NicifyVariableName(attribute) + " over Life"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                VFXAttributeMode attributeMode = (Composition != AttributeCompositionMode.Overwrite) ? VFXAttributeMode.ReadWrite : VFXAttributeMode.Write;
                yield return new VFXAttributeInfo(currentAttribute, attributeMode);
                yield return new VFXAttributeInfo(VFXAttribute.Age, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Lifetime, VFXAttributeMode.Read);
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
                var attribute = currentAttribute;

                string source;

                source = GetFetchValueString(attribute, Mode);

                if (Composition == AttributeCompositionMode.Blend)
                    source += VFXBlockUtility.GetComposeString(Composition, attribute.name, "value" , "Blend");
                else
                    source += VFXBlockUtility.GetComposeString(Composition, attribute.name, "value");

                return source;
            }
        }

        public string GetFetchValueString(VFXAttribute attribute, ComputeMode mode)
        {
            string localName = GenerateLocalAttributeName(attribute.name);
            int size = VFXExpression.TypeToSize(currentAttribute.type);

            if (mode == ComputeMode.Uniform)
            {
                return string.Format("float value = SampleCurve({0}, age / lifetime);\n", localName);
            }
            else
            {
                if (currentAttribute.Equals(VFXAttribute.Color))
                    return string.Format("float3 value = SampleGradient({0}, age / lifetime).rgb;\n", localName);
                else
                {
                    string typeString = "float" + (size == 1 ? "" : size.ToString());
                    string output = string.Format("float t = age / lifetime;\n{0} value = 0.0;\n", typeString);
                    if (size > 0) output += string.Format("value{1} = SampleCurve({0}, t);\n", localName + (size == 1 ? "" : "_x"), (size == 1 ? "" : "[0]"));
                    if (size > 1) output += string.Format("value[1] = SampleCurve({0}, t);\n", localName + "_y");
                    if (size > 2) output += string.Format("value[2] = SampleCurve({0}, t);\n", localName + "_z");
                    if (size > 3) output += string.Format("value[3] = SampleCurve({0}, t);\n", localName + "_w");
                    return output;
                }
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                string localName = GenerateLocalAttributeName(currentAttribute.name);
                if (Mode == ComputeMode.Uniform)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(AnimationCurve), localName));
                else
                {
                    if (currentAttribute.Equals(VFXAttribute.Color))
                    {
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(Gradient), localName), VFXResources.defaultResources.Gradient);
                    }
                    else
                    {
                        int size = VFXExpression.TypeToSize(currentAttribute.type);
                        if (size > 0) yield return new VFXPropertyWithValue(new VFXProperty(typeof(AnimationCurve), localName + (size == 1 ? "" : "_x")), VFXResources.defaultResources.AnimationCurve);
                        if (size > 1) yield return new VFXPropertyWithValue(new VFXProperty(typeof(AnimationCurve), localName + "_y"), VFXResources.defaultResources.AnimationCurve);
                        if (size > 2) yield return new VFXPropertyWithValue(new VFXProperty(typeof(AnimationCurve), localName + "_z"), VFXResources.defaultResources.AnimationCurve);
                        if (size > 3) yield return new VFXPropertyWithValue(new VFXProperty(typeof(AnimationCurve), localName + "_w"), VFXResources.defaultResources.AnimationCurve);
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
    }
}
