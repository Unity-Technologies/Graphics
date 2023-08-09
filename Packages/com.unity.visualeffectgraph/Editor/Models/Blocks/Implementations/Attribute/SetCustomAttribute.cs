using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Block
{
    static class CustomAttributeUtility
    {
        private static readonly Regex s_NameValidationRegex = new Regex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

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

        internal static bool IsShaderCompilableName(string name)
        {
            return s_NameValidationRegex.IsMatch(name);
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

        public override string name => VFXBlockUtility.GetNameString(Composition) + " '" + attribute + "' " + VFXBlockUtility.GetNameString(Random) + " (" + AttributeType + ")";
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

        private static string GenerateLocalAttributeName(string name)
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

        internal sealed override void GenerateErrors(VFXInvalidateErrorReporter manager)
        {
            base.GenerateErrors(manager);

            var attributeName = currentAttribute.name;
            if (!CustomAttributeUtility.IsShaderCompilableName(attributeName))
            {
                manager.RegisterError("InvalidCustomAttributeName", VFXErrorType.Error, $"Custom attribute name '{attributeName}' is not valid.\n -The name must not contain spaces or any special character\n -The name must not start with a digit character");
            }
        }

        private VFXAttribute currentAttribute => new VFXAttribute(attribute, CustomAttributeUtility.GetValueType(AttributeType));
    }
}
