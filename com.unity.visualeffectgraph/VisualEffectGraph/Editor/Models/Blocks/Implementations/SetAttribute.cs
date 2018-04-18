using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Attribute/Set", variantProvider = typeof(AttributeVariantReadWritable))]
    class SetAttribute : VFXBlock
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), StringProvider(typeof(ReadWritableAttributeProvider))]
        public string attribute = VFXAttribute.AllIncludingVariadic.First();

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public AttributeCompositionMode Composition = AttributeCompositionMode.Overwrite;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public RandomMode Random = RandomMode.Off;

        [VFXSetting]
        public VariadicChannelOptions channels = VariadicChannelOptions.XYZ;
        private static readonly char[] channelNames = new char[] { 'x', 'y', 'z' };

        public override string name { get { return VFXBlockUtility.GetNameString(Composition) + " " + attribute + " " + VFXBlockUtility.GetNameString(Random); } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kInitAndUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                var attrib = currentAttribute;
                VFXAttributeMode attributeMode = (Composition == AttributeCompositionMode.Overwrite) ? VFXAttributeMode.Write : VFXAttributeMode.ReadWrite;
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
                var attrib = currentAttribute;
                string source = "";

                int attributeSize = VFXExpression.TypeToSize(attrib.type);
                int loopCount = 1;
                if (attrib.variadic == VFXVariadic.True)
                {
                    attributeSize = 1;
                    loopCount = channels.ToString().Length;
                }

                for (int i = 0; i < loopCount; i++)
                {
                    string paramPostfix = (attrib.variadic == VFXVariadic.True) ? "." + channelNames[i] : "";
                    string attributePostfix = (attrib.variadic == VFXVariadic.True) ? char.ToUpper(channels.ToString()[i]).ToString() : "";

                    string channelSource = "";
                    if (Random == RandomMode.Off)
                        channelSource = VFXBlockUtility.GetRandomMacroString(Random, attributeSize, paramPostfix, GenerateLocalAttributeName(attrib.name));
                    else
                        channelSource = VFXBlockUtility.GetRandomMacroString(Random, attributeSize, paramPostfix, "Min", "Max");

                    if (Composition == AttributeCompositionMode.Blend)
                        channelSource = VFXBlockUtility.GetComposeString(Composition, attrib.name + attributePostfix, channelSource, "Blend");
                    else
                        channelSource = VFXBlockUtility.GetComposeString(Composition, attrib.name + attributePostfix, channelSource);

                    if (i < loopCount - 1)
                        channelSource += "\n";
                    source += channelSource;
                }

                return source;
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (string setting in base.filteredOutSettings) yield return setting;
                var attrib = VFXAttribute.Find(attribute);
                if (attrib.variadic == VFXVariadic.False) yield return "channels";
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var attrib = currentAttribute;

                VFXPropertyAttribute[] attr = null;
                if (attrib.Equals(VFXAttribute.Color))
                    attr = VFXPropertyAttribute.Create(new ShowAsColorAttribute());

                Type slotType = VFXExpression.TypeToType(attrib.type);
                object content = attrib.value.GetContent();
                if (attrib.variadic == VFXVariadic.True)
                {
                    switch (channels.ToString().Length)
                    {
                        case 1:
                            slotType = typeof(float);
                            content = ((Vector3)content).x;
                            break;
                        case 2:
                            slotType = typeof(Vector2);
                            content = (Vector2)(Vector3)content;
                            break;
                        case 3:
                            slotType = typeof(Vector3);
                            break;
                        default:
                            break;
                    }
                }

                if (Random == RandomMode.Off)
                    yield return new VFXPropertyWithValue(new VFXProperty(slotType, GenerateLocalAttributeName(attrib.name)) { attributes = attr }, content);
                else
                {
                    yield return new VFXPropertyWithValue(new VFXProperty(slotType, "Min") { attributes = attr });
                    yield return new VFXPropertyWithValue(new VFXProperty(slotType, "Max") { attributes = attr }, content);
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
