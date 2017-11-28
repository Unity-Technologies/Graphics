using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX
{
    class AttributeVariantWritable : IVariantProvider
    {
        public Dictionary<string, object[]> variants
        {
            get
            {
                return new Dictionary<string, object[]>
                {
                    { "attribute", VFXAttribute.All.Where(o => !VFXAttribute.AllReadOnly.Contains(o)).Cast<object>().ToArray() }
                };
            }
        }
    }
}

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Attribute", variantProvider = typeof(AttributeVariantWritable))]
    class SetAttribute : VFXBlock
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        [StringProvider(typeof(AttributeProvider))]
        public string attribute = VFXAttribute.All.First();

        [VFXSetting]
        public AttributeCompositionMode Composition = AttributeCompositionMode.Overwrite;

        [VFXSetting]
        public RandomMode Random = RandomMode.Off;

        public override string name { get { return "Set Attribute " + attribute; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kInitAndUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                var attributes = new List<VFXAttributeInfo>();
                VFXAttributeMode attributeMode = (Composition != AttributeCompositionMode.Overwrite) ? VFXAttributeMode.ReadWrite : VFXAttributeMode.Write;
                attributes.Add(new VFXAttributeInfo(currentAttribute, attributeMode));

                if (Random != RandomMode.Off)
                    attributes.Add(new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWrite));

                return attributes;
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
                string source = VFXBlockUtility.GetRandomMacroString(Random, attribute);

                if (Random == RandomMode.Off)
                    source = string.Format(source, GenerateLocalAttributeName(attribute.name));
                else
                    source = string.Format(source, "Min", "Max");

                if (Composition == AttributeCompositionMode.Blend)
                    source = string.Format(VFXBlockUtility.GetComposeFormatString(Composition), attribute.name, source, "Blend");
                else
                    source = string.Format(VFXBlockUtility.GetComposeFormatString(Composition), attribute.name, source);

                return source;
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = new List<VFXPropertyWithValue>();

                if (Random == RandomMode.Off)
                {
                    properties.Add(new VFXPropertyWithValue(new VFXProperty(VFXExpression.TypeToType(currentAttribute.type), GenerateLocalAttributeName(currentAttribute.name)), currentAttribute.value.GetContent()));
                }
                else
                {
                    properties.Add(new VFXPropertyWithValue(new VFXProperty(VFXExpression.TypeToType(currentAttribute.type), "Min")));
                    properties.Add(new VFXPropertyWithValue(new VFXProperty(VFXExpression.TypeToType(currentAttribute.type), "Max"), currentAttribute.value.GetContent()));
                }

                if (Composition == AttributeCompositionMode.Blend)
                {
                    properties.Add(new VFXPropertyWithValue(new VFXProperty(typeof(float), "Blend")));
                }

                return properties;
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
