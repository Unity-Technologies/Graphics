using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX.Block
{
    class SetAttributeVariantReadWritable : VariantProvider
    {
        protected override sealed Dictionary<string, object[]> variants
        {
            get
            {
                return new Dictionary<string, object[]>
                {
                    { "attribute", VFXAttribute.AllIncludingVariadicReadWritable.Cast<object>().ToArray() },
                    { "Source", new object[] { SetAttribute.ValueSource.Slot, SetAttribute.ValueSource.Source } },
                    { "Composition", new object[] { AttributeCompositionMode.Overwrite, AttributeCompositionMode.Add, AttributeCompositionMode.Multiply, AttributeCompositionMode.Blend } }
                };
            }
        }
    }

    [VFXInfo(category = "Attribute/Set", variantProvider = typeof(SetAttributeVariantReadWritable))]
    class SetAttribute : VFXBlock
    {
        public enum ValueSource
        {
            Slot,
            Source
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), StringProvider(typeof(ReadWritableAttributeProvider))]
        public string attribute;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public AttributeCompositionMode Composition = AttributeCompositionMode.Overwrite;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public ValueSource Source = ValueSource.Slot;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public RandomMode Random = RandomMode.Off;

        [VFXSetting]
        public VariadicChannelOptions channels = VariadicChannelOptions.XYZ;
        private static readonly char[] channelNames = new char[] { 'x', 'y', 'z' };

        public override string libraryName
        {
            get
            {
                if (!attributeIsValid)
                    return string.Empty;

                return ComposeName("");
            }
        }

        private bool attributeIsValid
        {
            get
            {
                return !string.IsNullOrEmpty(attribute);
            }
        }


        public override string name
        {
            get
            {
                if (!attributeIsValid)
                    return string.Empty;

                string variadicName = (currentAttribute.variadic == VFXVariadic.True) ? "." + channels.ToString() : "";
                return ComposeName(variadicName);
            }
        }

        private string ComposeName(string variadicName)
        {
            string attributeName = ObjectNames.NicifyVariableName(attribute) + variadicName;
            switch (Source)
            {
                case ValueSource.Slot: return VFXBlockUtility.GetNameString(Composition) + " " + attributeName + " " + VFXBlockUtility.GetNameString(Random);
                case ValueSource.Source: return "Inherit Source " + attributeName + " (" + VFXBlockUtility.GetNameString(Composition) + ")";
                default: return "NOT IMPLEMENTED : " + Source;
            }
        }

        public override VFXContextType compatibleContexts { get { return VFXContextType.InitAndUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.Particle; } }

        public override void Sanitize(int version)
        {
            if (VFXBlockUtility.SanitizeAttribute(ref attribute, ref channels, version))
                Invalidate(InvalidationCause.kSettingChanged);

            base.Sanitize(version);
            if (version <= 1 && inputSlots.Any(o => o.spaceable))
            {
                //Space has been added with on a few specific attributes, automatically copying space from context
                var contextSpace = GetParent().space;
                foreach (var slot in inputSlots.Where(o => o.spaceable))
                {
                    slot.space = contextSpace;
                }
                Debug.Log(string.Format("Sanitizing attribute {0} : settings space to {1} (retrieved from context)", attribute, contextSpace));
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                if (Source != ValueSource.Slot)
                    yield return "Random";

                if (!attributeIsValid || currentAttribute.variadic == VFXVariadic.False)
                    yield return "channels";

                foreach (var setting in base.filteredOutSettings)
                    yield return setting;
            }
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                if (attributeIsValid)
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
        }

        static private string GenerateLocalAttributeName(string name)
        {
            return name[0].ToString().ToUpper() + name.Substring(1);
        }

        public override string source
        {
            get
            {
                if (!attributeIsValid)
                    return string.Empty;

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
                    if (Source == ValueSource.Slot)
                    {
                        if (Random == RandomMode.Off)
                            channelSource = VFXBlockUtility.GetRandomMacroString(Random, attributeSize, paramPostfix, GenerateLocalAttributeName(attrib.name));
                        else
                            channelSource = VFXBlockUtility.GetRandomMacroString(Random, attributeSize, paramPostfix, "Min", "Max");
                    }
                    else
                    {
                        channelSource = VFXBlockUtility.GetRandomMacroString(RandomMode.Off, attributeSize, paramPostfix, "Value");
                    }

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

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                if (attributeIsValid)
                {
                    foreach (var param in base.parameters)
                    {
                        if ((param.name == "Value" || param.name == "Min" || param.name == "Max") && Source == ValueSource.Source)
                            continue;

                        yield return param;
                    }

                    if (Source == ValueSource.Source)
                    {
                        VFXExpression sourceExpression = null;
                        var attrib = currentAttribute;
                        if (attrib.variadic == VFXVariadic.True)
                        {
                            var currentChannels = channels.ToString().Select(c => char.ToUpper(c));
                            var currentChannelsExpression = currentChannels.Select(o =>
                            {
                                var subAttrib = VFXAttribute.Find(attribute + o);
                                return new VFXAttributeExpression(subAttrib, VFXAttributeLocation.Source);
                            }).ToArray();

                            if (currentChannelsExpression.Length == 1)
                                sourceExpression = currentChannelsExpression[0];
                            else
                                sourceExpression = new VFXExpressionCombine(currentChannelsExpression);
                        }
                        else
                        {
                            sourceExpression = new VFXAttributeExpression(attrib, VFXAttributeLocation.Source);
                        }
                        yield return new VFXNamedExpression(sourceExpression, "Value");
                    }
                }
            }
        }

        private int ChannelToIndex(char channel)
        {
            switch (channel)
            {
                default:
                case 'X': return 0;
                case 'Y': return 1;
                case 'Z': return 2;
                case 'W': return 3;
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                if (attributeIsValid)
                {
                    if (Source != ValueSource.Source)
                    {
                        var attrib = currentAttribute;

                        VFXPropertyAttribute[] attr = null;
                        if (attrib.Equals(VFXAttribute.Color))
                            attr = VFXPropertyAttribute.Create(new ShowAsColorAttribute());

                        Type slotType = VFXExpression.TypeToType(attrib.type);
                        object content = attrib.value.GetContent();

                        if (attrib.space != SpaceableType.None)
                        {
                            var contentAsVector3 = (Vector3)content;
                            switch (attrib.space)
                            {
                                case SpaceableType.Position: content = (Position)contentAsVector3; break;
                                case SpaceableType.Direction: content = (DirectionType)contentAsVector3; break;
                                case SpaceableType.Vector: content = (Vector)contentAsVector3; break;
                                default: throw new InvalidOperationException("Space is not handled for attribute : " + attrib.name + " space : " + attrib.space);
                            }
                            slotType = content.GetType();
                        }

                        if (attrib.variadic == VFXVariadic.True)
                        {
                            string channelsString = channels.ToString();

                            int length = channelsString.Length;
                            switch (length)
                            {
                                case 1:
                                    slotType = typeof(float);
                                    content = ((Vector3)content)[ChannelToIndex(channelsString[0])];
                                    break;
                                case 2:
                                    slotType = typeof(Vector2);
                                    Vector2 v = (Vector2)(Vector3)content;
                                    for (int i = 0; i < 2; i++)
                                        v[i] = ((Vector3)content)[ChannelToIndex(channelsString[i])];
                                    content = v;
                                    break;
                                case 3:
                                    slotType = typeof(Vector3);
                                    break;
                                default:
                                    break;
                            }
                        }

                        if (Random == RandomMode.Off)
                        {
                            yield return new VFXPropertyWithValue(new VFXProperty(slotType, GenerateLocalAttributeName(attrib.name)) { attributes = attr }, content);
                        }
                        else
                        {
                            yield return new VFXPropertyWithValue(new VFXProperty(slotType, "Min") { attributes = attr }, content);
                            yield return new VFXPropertyWithValue(new VFXProperty(slotType, "Max") { attributes = attr }, content);
                        }
                    }

                    if (Composition == AttributeCompositionMode.Blend)
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "Blend", VFXPropertyAttribute.Create(new RangeAttribute(0.0f, 1.0f))));
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
