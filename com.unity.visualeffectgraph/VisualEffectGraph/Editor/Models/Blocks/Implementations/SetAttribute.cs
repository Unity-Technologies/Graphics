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
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), StringProvider(typeof(WritableAttributeProvider))]
        public string attribute = VFXAttribute.All.First();

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public AttributeCompositionMode Composition = AttributeCompositionMode.Overwrite;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public RandomMode Random = RandomMode.Off;

        public override string name { get { return VFXBlockUtility.GetNameString(Composition) + " " + attribute + " " + VFXBlockUtility.GetNameString(Random); } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kInitAndUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                VFXAttributeMode attributeMode = (Composition != AttributeCompositionMode.Overwrite) ? VFXAttributeMode.ReadWrite : VFXAttributeMode.Write;
                yield return new VFXAttributeInfo(currentAttribute, attributeMode);

                if (Random != RandomMode.Off)
                    yield return new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWrite);
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
                string source = "";

                if (Random == RandomMode.Off)
                    source = VFXBlockUtility.GetRandomMacroString(Random, attribute, GenerateLocalAttributeName(attribute.name));
                else
                    source = VFXBlockUtility.GetRandomMacroString(Random, attribute, "Min", "Max");

                if (Composition == AttributeCompositionMode.Blend)
                    source = VFXBlockUtility.GetComposeString(Composition, attribute.name, source, "Blend");
                else
                    source = VFXBlockUtility.GetComposeString(Composition, attribute.name, source);

                return source;
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                VFXPropertyAttribute[] attr = null;
                if (currentAttribute.Equals(VFXAttribute.Color))
                    attr = VFXPropertyAttribute.Create(new ShowAsColorAttribute());

                if (Random == RandomMode.Off)
                    yield return new VFXPropertyWithValue(new VFXProperty(VFXExpression.TypeToType(currentAttribute.type), GenerateLocalAttributeName(currentAttribute.name)) { attributes = attr }, currentAttribute.value.GetContent());
                else
                {
                    yield return new VFXPropertyWithValue(new VFXProperty(VFXExpression.TypeToType(currentAttribute.type), "Min") { attributes = attr });
                    yield return new VFXPropertyWithValue(new VFXProperty(VFXExpression.TypeToType(currentAttribute.type), "Max") { attributes = attr }, currentAttribute.value.GetContent());
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
            if (attribute == "size")   attribute = "sizeX";
            else if (attribute == "angle")  attribute = "angleZ";

            base.Sanitize();
        }
    }
}
