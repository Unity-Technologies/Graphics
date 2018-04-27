using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    class SetAttributeVariantReadWritable : IVariantProvider
    {
        public Dictionary<string, object[]> variants
        {
            get
            {
                return new Dictionary<string, object[]>
                {
                    { "attribute", VFXAttribute.AllReadWritable.Cast<object>().ToArray() },
                    { "Source", new object[] { SetAttribute.ValueSource.Slot, SetAttribute.ValueSource.Source } },
                };
            }
        }
    }


    [VFXInfo(category = "Attribute", variantProvider = typeof(SetAttributeVariantReadWritable))]
    class SetAttribute : VFXBlock
    {
        public enum ValueSource
        {
            Slot,
            Source
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), StringProvider(typeof(ReadWritableAttributeProvider))]
        public string attribute = VFXAttribute.All.First();

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public AttributeCompositionMode Composition = AttributeCompositionMode.Overwrite;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public ValueSource Source = ValueSource.Slot;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public RandomMode Random = RandomMode.Off;

        public override string name
        {
            get
            {
                string attributeName = ObjectNames.NicifyVariableName(attribute);
                switch (Source)
                {
                    case ValueSource.Slot: return VFXBlockUtility.GetNameString(Composition) + " " + attributeName + " " + VFXBlockUtility.GetNameString(Random);
                    case ValueSource.Source:
                        return "Inherit Source " + attributeName + " (" + VFXBlockUtility.GetNameString(Composition) + ")";
                    default: return "NOT IMPLEMENTED : " + Source;
                }
            }
        }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kInitAndUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                if (Source != ValueSource.Slot)
                    yield return "Random";

                foreach (var setting in base.filteredOutSettings)
                    yield return setting;
            }
        }

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

                if (Source == ValueSource.Slot)
                {
                    if (Random == RandomMode.Off)
                        source = VFXBlockUtility.GetRandomMacroString(Random, attribute, GenerateLocalAttributeName(attribute.name));
                    else
                        source = VFXBlockUtility.GetRandomMacroString(Random, attribute, "Min", "Max");
                }
                else
                {
                    source = VFXBlockUtility.GetRandomMacroString(RandomMode.Off, attribute, "Value");
                }


                if (Composition == AttributeCompositionMode.Blend)
                    source = VFXBlockUtility.GetComposeString(Composition, attribute.name, source, "Blend");
                else
                    source = VFXBlockUtility.GetComposeString(Composition, attribute.name, source);

                return source;
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var param in base.parameters)
                {
                    if ((param.name == "Value" || param.name == "Min" || param.name == "Max") && Source == ValueSource.Source)
                        continue;

                    yield return param;
                }

                if (Source == ValueSource.Source)
                {
                    var attribute = VFXAttribute.Find(this.attribute);
                    yield return new VFXNamedExpression(new VFXAttributeExpression(attribute, VFXAttributeLocation.Source), "Value");
                }
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                VFXPropertyAttribute[] attr = null;
                if (currentAttribute.Equals(VFXAttribute.Color))
                    attr = VFXPropertyAttribute.Create(new ShowAsColorAttribute());

                if (Source == ValueSource.Slot)
                {
                    if (Random == RandomMode.Off)
                        yield return new VFXPropertyWithValue(new VFXProperty(VFXExpression.TypeToType(currentAttribute.type), GenerateLocalAttributeName(currentAttribute.name)) { attributes = attr }, currentAttribute.value.GetContent());
                    else
                    {
                        yield return new VFXPropertyWithValue(new VFXProperty(VFXExpression.TypeToType(currentAttribute.type), "Min") { attributes = attr });
                        yield return new VFXPropertyWithValue(new VFXProperty(VFXExpression.TypeToType(currentAttribute.type), "Max") { attributes = attr }, currentAttribute.value.GetContent());
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
            if (attribute == "size")   attribute = "sizeX";
            else if (attribute == "angle")  attribute = "angleZ";

            base.Sanitize();
        }
    }
}
