using System;
using System.Collections.Generic;
using System.Globalization;

using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [Obsolete]
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

        public override string name => VFXBlockUtility.GetNameString(Composition) + " '" + attribute + "' " + VFXBlockUtility.GetNameString(Random) + " (" + AttributeType + ")";
        public override VFXContextType compatibleContexts => VFXContextType.InitAndUpdateAndOutput;
        public override VFXDataType compatibleData => VFXDataType.Particle;

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

        protected override void OnAdded()
        {
            Sanitize(0);
        }

        public override void Sanitize(int version)
        {
            GetGraph().TryAddCustomAttribute(attribute, CustomAttributeUtility.GetValueType(AttributeType), string.Empty, false, out var vfxAttribute);
            var setAttribute = ScriptableObject.CreateInstance<SetAttribute>();
            if (attribute != vfxAttribute.name)
            {
                Debug.Log($"[Sanitize] Set Custom Attribute: {attribute} has been renamed into {vfxAttribute.name}");
            }
            setAttribute.attribute = vfxAttribute.name;
            setAttribute.Composition = Composition;
            setAttribute.Random = Random;
            setAttribute.ResyncSlots(true);
            ReplaceModel(setAttribute, this, true, false);
            VFXBlock.CopyInputLinks(setAttribute, this);
        }

        internal sealed override void GenerateErrors(VFXErrorReporter report)
        {
            base.GenerateErrors(report);

            var attributeName = currentAttribute.name;
            if (!CustomAttributeUtility.IsShaderCompilableName(attributeName))
            {
                report.RegisterError("InvalidCustomAttributeName", VFXErrorType.Error, $"Custom attribute name '{attributeName}' is not valid.\n -The name must not contain spaces or any special character\n -The name must not start with a digit character", this);
            }
        }

        private VFXAttribute currentAttribute => new (attribute, CustomAttributeUtility.GetValueType(AttributeType), string.Empty);
    }
}
