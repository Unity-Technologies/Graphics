using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using System.Globalization;

namespace UnityEditor.VFX.Block
{
    class CustomAttributeUtility
    {
        public enum Signature
        {
            Float,
            Vector2,
            Vector3,
            Vector4,
            Bool,
            Uint,
            Int
        }

        internal static VFXValueType GetValueType(Signature signature)
        {
            switch (signature)
            {
                default:
                case Signature.Float: return VFXValueType.Float;
                case Signature.Vector2: return VFXValueType.Float2;
                case Signature.Vector3: return VFXValueType.Float3;
                case Signature.Vector4: return VFXValueType.Float4;
                case Signature.Int: return VFXValueType.Int32;
                case Signature.Uint: return VFXValueType.Uint32;
                case Signature.Bool: return VFXValueType.Boolean;
            }
        }
    }

    class AttributeCustomProvider : VariantProvider
    {
        public override IEnumerable<Variant> ComputeVariants()
        {
            var compositions = new[] { AttributeCompositionMode.Add, AttributeCompositionMode.Overwrite, AttributeCompositionMode.Multiply, AttributeCompositionMode.Blend };
            foreach (var composition in compositions)
            {
                yield return new Variant(
                    new[]
                    {
                        new KeyValuePair<string, object>("attribute", "CustomAttribute"),
                        new KeyValuePair<string, object>("Composition", composition),
                    },
                    new[] { "custom" });
            }
        }
    }

    [VFXInfo(category = "Attribute/{0}", variantProvider = typeof(AttributeCustomProvider), experimental = true)]
    class SetCustomAttribute : VFXBlock
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Delayed]
        public string attribute = "CustomAttribute";

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public AttributeCompositionMode Composition = AttributeCompositionMode.Overwrite;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public RandomMode Random = RandomMode.Off;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public CustomAttributeUtility.Signature AttributeType = CustomAttributeUtility.Signature.Float;

        public override string libraryName => $"{VFXBlockUtility.GetNameString(Composition)} {ObjectNames.NicifyVariableName(attribute)}";

        public override string name
        {
            get
            {
                string attributeName = attribute;
                return VFXBlockUtility.GetNameString(Composition) + " " + attributeName + " " + VFXBlockUtility.GetNameString(Random) + " (" + AttributeType.ToString() + ")";
            }
        }
        public override VFXContextType compatibleContexts { get { return VFXContextType.InitAndUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.Particle; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                var attrib = currentAttribute;
                VFXAttributeMode attributeMode = (Composition == AttributeCompositionMode.Overwrite) ? VFXAttributeMode.Write : VFXAttributeMode.ReadWrite;

                yield return new VFXAttributeInfo(attrib, attributeMode);

                if (Random != RandomMode.Off)
                    yield return new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWrite);
            }
        }

        static private string GenerateLocalAttributeName(string name)
        {
            return "_" + name[0].ToString().ToUpper(CultureInfo.InvariantCulture) + name.Substring(1);
        }

        public override string source
        {
            get
            {
                var attrib = currentAttribute;
                string source = "";

                int attributeSize = VFXExpression.TypeToSize(attrib.type);
                string channelSource = "";

                if (Random == RandomMode.Off)
                    channelSource = VFXBlockUtility.GetRandomMacroString(Random, attributeSize, "", GenerateLocalAttributeName(attrib.name));
                else
                    channelSource = VFXBlockUtility.GetRandomMacroString(Random, attributeSize, "", "Min", "Max");

                if (Composition == AttributeCompositionMode.Blend)
                    source = VFXBlockUtility.GetComposeString(Composition, attrib.name, channelSource, "Blend");
                else
                    source = VFXBlockUtility.GetComposeString(Composition, attrib.name, channelSource);

                return source;
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var attrib = currentAttribute;

                Type slotType = VFXExpression.TypeToType(attrib.type);
                object content = attrib.value.GetContent();

                if (Random == RandomMode.Off)
                    yield return new VFXPropertyWithValue(new VFXProperty(slotType, GenerateLocalAttributeName(attrib.name)), content);
                else
                {
                    yield return new VFXPropertyWithValue(new VFXProperty(slotType, "Min"));
                    yield return new VFXPropertyWithValue(new VFXProperty(slotType, "Max"), content);
                }

                if (Composition == AttributeCompositionMode.Blend)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "Blend", new RangeAttribute(0.0f, 1.0f)));
            }
        }

        private VFXAttribute currentAttribute
        {
            get
            {
                return new VFXAttribute(attribute, CustomAttributeUtility.GetValueType(AttributeType));
            }
        }
    }
}
