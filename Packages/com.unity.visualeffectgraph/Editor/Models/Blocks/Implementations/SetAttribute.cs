using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Block
{
    class AttributeVariantProvider : VariantProvider
    {
        private readonly string m_Attribute;

        public AttributeVariantProvider(string attribute)
        {
            m_Attribute = attribute;
        }

        public override IEnumerable<Variant> GetVariants()
        {
            var randoms = new[] { RandomMode.Off, RandomMode.Uniform, RandomMode.PerComponent };
            var sources = new[] { SetAttribute.ValueSource.Slot, SetAttribute.ValueSource.Source };
            var compositions = new[] { AttributeCompositionMode.Overwrite, AttributeCompositionMode.Add, AttributeCompositionMode.Multiply, AttributeCompositionMode.Blend };

            var attributeRefSize = VFXExpressionHelper.GetSizeOfType(VFXAttributesManager.FindBuiltInOnly(m_Attribute).type);;
            foreach (var random in randoms)
            {
                foreach (var source in sources)
                {
                    foreach (var composition in compositions)
                    {
                        if (random != RandomMode.Off && source == SetAttribute.ValueSource.Source)
                            continue;

                        if (composition != AttributeCompositionMode.Overwrite && source == SetAttribute.ValueSource.Source)
                            continue;

                        if (composition != AttributeCompositionMode.Overwrite && m_Attribute == VFXAttribute.Alive.name)
                            continue;

                        if (random == RandomMode.PerComponent && attributeRefSize == 1)
                            continue;

                        // This is the main variant settings
                        if (composition == AttributeCompositionMode.Overwrite && source == SetAttribute.ValueSource.Slot && random == RandomMode.Off)
                            continue;

                        string name;
                        var compositionString = $"{VFXBlockUtility.GetNameString(composition)}";
                        var synonyms = VFXBlockUtility.GetCompositionSynonym(composition);
                        if (source != SetAttribute.ValueSource.Source)
                        {
                            name = compositionString.Label().AppendLiteral(m_Attribute);
                            if (random != RandomMode.Off)
                                name = name.AppendLabel($"Random {random}");
                        }
                        else
                        {
                            name = compositionString.Label().AppendLiteral(m_Attribute).AppendLabel("From Source");
                            synonyms = synonyms.Concat(new [] { "Inherit" }).ToArray();
                        }

                        yield return new Variant(
                            name,
                            m_Attribute != VFXAttribute.Alive.name ? VFXLibraryStringHelper.Separator(compositionString, 1) : null,
                            typeof(SetAttribute),
                            new[]
                            {
                                new KeyValuePair<string, object>("attribute", m_Attribute),
                                new KeyValuePair<string, object>("Random", random),
                                new KeyValuePair<string, object>("Source", source),
                                new KeyValuePair<string, object>("Composition", composition)
                            },
                            null,
                            synonyms);
                    }
                }
            }
        }
    }

    class SetAttributeVariantReadWritable : VariantProvider
    {
        public override IEnumerable<Variant> GetVariants()
        {
            var groups = VFXAttributesManager
                .GetBuiltInAttributesOrCombination(true, false, false, true)
                .Except(new []{ VFXAttribute.EventCount })
                .GroupBy(x => x.category);

            var setSynonyms = VFXBlockUtility.GetCompositionSynonym(AttributeCompositionMode.Overwrite);
            foreach (var group in groups)
            {
                foreach (var attribute in group)
                {
                    yield return new Variant(
                        "Set".Label(false).AppendLiteral(attribute.name),
                        $"Attribute/{attribute.category}", // If the category starts with # it's interpreted as a separator. Then the # character is followed by a number for sorting purpose
                        typeof(SetAttribute),
                        new[]
                        {
                            new KeyValuePair<string, object>("attribute", attribute.name),
                            new KeyValuePair<string, object>("Random", RandomMode.Off),
                            new KeyValuePair<string, object>("Source", SetAttribute.ValueSource.Slot),
                            new KeyValuePair<string, object>("Composition", AttributeCompositionMode.Overwrite)
                        },
                        () => new AttributeVariantProvider(attribute.name),
                        setSynonyms);
                }
            }
        }
    }

    [VFXHelpURL("Block-SetAttribute")]
    [VFXInfo(variantProvider = typeof(SetAttributeVariantReadWritable))]
    class SetAttribute : VFXBlock
    {
        public enum ValueSource
        {
            Slot,
            Source
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), StringProvider(typeof(ReadWritableAttributeProvider))]
        public string attribute;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("Specifies what operation to perform on the chosen attribute. The input value can overwrite, add to, multiply with, or blend with the existing attribute value.")]
        public AttributeCompositionMode Composition = AttributeCompositionMode.Overwrite;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("Specifies the source of the attribute data. 'Slot' enables a user input to modify the value, while a 'Source' attribute derives its value from a Spawn event attribute or inherits it from a parent system via a GPU event.")]
        public ValueSource Source = ValueSource.Slot;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("Specifies whether random values can be derived from this block. Random values can be turned off, derived per component, or be uniform.")]
        public RandomMode Random = RandomMode.Off;

        [VFXSetting, Tooltip("Specifies which channels to use in this block. This is useful for only storing relevant data if not all channels are used.")]
        public VariadicChannelOptions channels = VariadicChannelOptions.XYZ;
        private static readonly char[] channelNames = new char[] { 'x', 'y', 'z' };

        public override string name => ComputeName();

        private bool TryGetAttribute(out VFXAttribute vfxAttribute)
        {
            vfxAttribute = currentAttribute;
            return !string.IsNullOrEmpty(vfxAttribute.name);
        }

        private string ComputeName()
        {
            if (!TryGetAttribute(out var vfxAttribute))
            {
                vfxAttribute = new VFXAttribute { name = attribute };
            }

            if (Source != ValueSource.Slot && Source != ValueSource.Source)
                throw new NotImplementedException(Source.ToString());

            var builder = new StringBuilder(24);
            if (Source == ValueSource.Slot)
                builder.Append(VFXBlockUtility.GetNameString(Composition).Label(false));
            else
            {
                builder.Append(VFXBlockUtility.GetNameString(Composition).Label(false).AppendLiteral(attribute));
                if (vfxAttribute.variadic == VFXVariadic.True)
                    builder.AppendFormat(".{0}", channels.ToString());
                builder.Append("From Source".Label());
                return builder.ToString();
            }

            builder.Append(attribute.Literal());
            if (vfxAttribute.variadic == VFXVariadic.True)
                builder.AppendFormat(".{0}", channels.ToString());

            if (Source == ValueSource.Slot)
            {
                if (Random != RandomMode.Off)
                    builder.Append($"Random {Random}".Label());
            }
            else
                builder.AppendFormat(" ({0})", VFXBlockUtility.GetNameString(Composition));

            return builder.ToString();
        }

        public override VFXContextType compatibleContexts => VFXContextType.InitAndUpdateAndOutput;
        public override VFXDataType compatibleData => VFXDataType.Particle;

        public override void Sanitize(int version)
        {
            if (VFXBlockUtility.SanitizeAttribute(GetGraph(), ref attribute, ref channels, version))
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
                Debug.Log($"Sanitizing attribute {attribute} : settings space to {contextSpace} (retrieved from context)");
            }
        }

        public override void CheckGraphBeforeImport()
        {
            base.CheckGraphBeforeImport();
            SyncCustomAttributeIfNeeded();
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                if (Source != ValueSource.Slot)
                    yield return "Random";

                if (!TryGetAttribute(out var vfxAttribute) || vfxAttribute.variadic == VFXVariadic.False)
                    yield return "channels";

                foreach (var setting in base.filteredOutSettings)
                    yield return setting;
            }
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                if (TryGetAttribute(out var attrib))
                {
                    var attributeMode = Composition == AttributeCompositionMode.Overwrite ? VFXAttributeMode.Write : VFXAttributeMode.ReadWrite;
                    if (attrib.variadic == VFXVariadic.True)
                    {
                        var channelsString = channels.ToString();
                        foreach (var channel in channelsString)
                            yield return new VFXAttributeInfo(VFXAttributesManager.FindBuiltInOnly(attrib.name + channel), attributeMode);
                    }
                    else
                    {
                        yield return new VFXAttributeInfo(attrib, attributeMode);
                    }
                    if (Random != RandomMode.Off && Source == ValueSource.Slot)
                        yield return new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWrite);
                }
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
                if (!TryGetAttribute(out var attrib))
                    return string.Empty;

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
                            channelSource = VFXBlockUtility.GetRandomMacroString(Random, attributeSize, paramPostfix, "A", "B");
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
                if (TryGetAttribute(out var attrib))
                {
                    foreach (var param in base.parameters)
                    {
                        if (param.name is "Value" or "A" or "B" && Source == ValueSource.Source)
                            continue;
                        yield return param;
                    }

                    if (Source == ValueSource.Source)
                    {
                        VFXExpression sourceExpression = null;
                        if (attrib.variadic == VFXVariadic.True)
                        {
                            var currentChannels = channels.ToString().Select(c => char.ToUpper(c));
                            var currentChannelsExpression = currentChannels.Select(o =>
                            {
                                var subAttrib = VFXAttributesManager.FindBuiltInOnly(attribute + o);
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
                if (TryGetAttribute(out var attrib))
                {
                    if (Source != ValueSource.Source)
                    {
                        TooltipAttribute tooltip = new TooltipAttribute(attrib.description);
                        var attr = attrib.Equals(VFXAttribute.Color)
                            ? new VFXPropertyAttributes(new ShowAsColorAttribute(), tooltip)
                            : new VFXPropertyAttributes(tooltip);


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
                            yield return new VFXPropertyWithValue(new VFXProperty(slotType, GenerateLocalAttributeName(attrib.name), attr), content);
                        }
                        else
                        {
                            yield return new VFXPropertyWithValue(new VFXProperty(slotType, "A", attr), content);
                            yield return new VFXPropertyWithValue(new VFXProperty(slotType, "B", attr), content);
                        }
                    }

                    if (Composition == AttributeCompositionMode.Blend)
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "Blend", new RangeAttribute(0.0f, 1.0f)));
                }
            }
        }

        private VFXAttribute currentAttribute
        {
            get
            {
                if (GetGraph() is { } graph)
                {
                    if (graph.attributesManager.TryFind(attribute, out var vfxAttribute))
                    {
                        return vfxAttribute;
                    }
                }
                else // Happens when the node is not yet added to the graph, but should be ok as soon as it's added (see OnAdded)
                {
                    var attr = VFXAttributesManager.FindBuiltInOnly(attribute);
                    if (string.Compare(attribute, attr.name, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        return attr;
                    }
                }

                // Temporary attribute
                return default;
            }
        }

        public override void Rename(string oldName, string newName)
        {
            if (GetGraph() is {} graph && graph.attributesManager.IsCustom(newName))
            {
                attribute = newName;
                SyncSlots(VFXSlot.Direction.kInput, true);
            }
        }

        internal sealed override void GenerateErrors(VFXErrorReporter report)
        {
            base.GenerateErrors(report);

            if (!CustomAttributeUtility.IsShaderCompilableName(attribute))
            {
                report.RegisterError("InvalidCustomAttributeName", VFXErrorType.Error, $"Custom attribute name '{attribute}' is not valid.\n -The name must not contain spaces or any special character\n -The name must not start with a digit character", this);
            }

            if (Source == ValueSource.Source && GetParent() is { } parentContext && (parentContext.contextType & VFXContextType.UpdateAndOutput) != 0)
            {
                report.RegisterError("SourceNotAllowed", VFXErrorType.Warning, "Inherit attribute is not supported in Update and Output contexts", this);
            }

            if (GetNbInputSlots() == 0)
            {
                return;
            }

            if (attribute == VFXAttribute.Lifetime.name)
            {
                var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation | VFXExpressionContextOption.ConstantFolding);
                var expression = GetInputSlot(0).GetExpression();
                context.RegisterExpression(expression);
                context.Compile();

                if (context.GetReduced(expression) is var lifeTimeExpression &&
                    lifeTimeExpression.Is(VFXExpression.Flags.Constant) &&
                    lifeTimeExpression.Get<float>() is var lifeTime &&
                    lifeTime > 1e4)
                {
                    report.RegisterError("TooLongLifeTime", VFXErrorType.Warning, $"The lifetime is pretty high: {TimeSpan.FromSeconds(lifeTime):hh\\hmm\\m}.\nYou might prefer to make immortal particles by removing the Set Lifetime block.", this);
                }
            }

            if (attribute == VFXAttribute.scale.name)
            {
                var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation | VFXExpressionContextOption.ConstantFolding);
                var expression = GetInputSlot(0).GetExpression();
                context.RegisterExpression(expression);
                context.Compile();

                if (context.GetReduced(expression) is var scaleExpression &&
                    scaleExpression.Is(VFXExpression.Flags.Constant))
                {
                    if (scaleExpression.valueType == VFXValueType.Float && Mathf.Approximately(scaleExpression.Get<float>(), 0f) ||
                        scaleExpression.valueType == VFXValueType.Float2 && scaleExpression.Get<Vector2>() is var vec2 && Mathf.Approximately(vec2.x * vec2.y, 0f) ||
                        scaleExpression.valueType == VFXValueType.Float3 && scaleExpression.Get<Vector3>() is var vec3 && Mathf.Approximately(vec3.x * vec3.y * vec3.z, 0f) )
                    {
                        report.RegisterError("ZeroScaleValue", VFXErrorType.Warning, "Scale is set to zero", this);
                    }
                }
            }
        }

        protected override void OnAdded()
        {
            base.OnAdded();
            SyncCustomAttributeIfNeeded();
        }

        private void SyncCustomAttributeIfNeeded()
        {
            var graph = GetGraph();
            if (graph != null)
            {
                if (graph.attributesManager.IsCustom(attribute))
                {
                    Invalidate(InvalidationCause.kUIChangedTransient);
                    SyncSlots(VFXSlot.Direction.kInput, true);
                }
                else if (!string.IsNullOrEmpty(attribute) && !graph.attributesManager.TryFind(attribute, out _))
                {
                    graph.TryAddCustomAttribute(attribute, VFXValueType.Float, string.Empty, false, out _);
                    graph.SetCustomAttributeDirty();
                    Invalidate(InvalidationCause.kUIChangedTransient);
                }
            }
        }
    }
}
