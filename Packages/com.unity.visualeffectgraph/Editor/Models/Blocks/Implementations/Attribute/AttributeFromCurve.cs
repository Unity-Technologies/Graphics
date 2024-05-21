using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using System.Globalization;

namespace UnityEditor.VFX.Block
{
    class AttributeFromCurveVariantProvider : VariantProvider
    {
        private readonly string m_Attribute;

        public AttributeFromCurveVariantProvider(string attribute)
        {
            m_Attribute = attribute;
        }

        public override IEnumerable<Variant> GetVariants()
        {
            var compositions = new[] { AttributeCompositionMode.Add, AttributeCompositionMode.Overwrite, AttributeCompositionMode.Multiply, AttributeCompositionMode.Blend };
            var sampleModes = new[] { AttributeFromCurve.CurveSampleMode.OverLife, AttributeFromCurve.CurveSampleMode.BySpeed, AttributeFromCurve.CurveSampleMode.Random }.ToArray();

            foreach (var composition in compositions)
            {
                foreach (var sampleMode in sampleModes)
                {
                    if (m_Attribute == VFXAttribute.Age.name &&
                        (sampleMode == AttributeFromCurve.CurveSampleMode.OverLife || (composition == AttributeCompositionMode.Overwrite && sampleMode == AttributeFromCurve.CurveSampleMode.BySpeed)))
                    {
                        continue;
                    }

                    if (m_Attribute == VFXAttribute.Velocity.name && sampleMode == AttributeFromCurve.CurveSampleMode.BySpeed)
                    {
                        continue;
                    }

                    // This is the main variant settings
                    if (composition == AttributeCompositionMode.Overwrite && sampleMode == AttributeFromCurve.CurveSampleMode.OverLife)
                    {
                        continue;
                    }

                    var compositionSynonym = VFXBlockUtility.GetCompositionSynonym(composition);
                    var compositionString = VFXBlockUtility.GetNameString(composition);
                    yield return new Variant(
                        compositionString.Label().AppendLiteral(m_Attribute).AppendLabel(VFXBlockUtility.GetNameString(sampleMode)),
                        VFXLibraryStringHelper.Separator(compositionString, 0),
                        typeof(AttributeFromCurve),
                        new[]
                        {
                            new KeyValuePair<string, object>("attribute", m_Attribute),
                            new KeyValuePair<string, object>("Composition", composition),
                            new KeyValuePair<string, object>("SampleMode", sampleMode)
                        },
                        null,
                        m_Attribute != VFXAttribute.Color.name ? compositionSynonym : compositionSynonym.Concat(new []{ "Gradient" }).ToArray());
                }
            }
        }
    }

    class AttributeFromCurveProvider : VariantProvider
    {
        public override IEnumerable<Variant> GetVariants()
        {
            var setSynonyms = VFXBlockUtility.GetCompositionSynonym(AttributeCompositionMode.Overwrite);
            var groups = VFXAttributesManager
                .GetBuiltInAttributesOrCombination(true, false, false, false)
                .Except(new[] { VFXAttribute.Alive })
                .GroupBy(x => x.category);

            foreach (var group in groups)
            {
                foreach (var attribute in group)
                {
                    var sampleMode = attribute.name != VFXAttribute.Age.name ? AttributeFromCurve.CurveSampleMode.OverLife : AttributeFromCurve.CurveSampleMode.BySpeed;
                    yield return new Variant(
                        "Set".Label(false).AppendLiteral(attribute.name).AppendLabel(VFXBlockUtility.GetNameString(sampleMode)),
                        $"Attribute from Curve/{attribute.category}",
                        typeof(AttributeFromCurve),
                        new[]
                        {
                            new KeyValuePair<string, object>("attribute", attribute.name),
                            new KeyValuePair<string, object>("Composition", AttributeCompositionMode.Overwrite),
                            new KeyValuePair<string, object>("SampleMode", sampleMode)
                        },
                        () => new AttributeFromCurveVariantProvider(attribute.name),
                        attribute.name != VFXAttribute.Color.name ? setSynonyms : setSynonyms.Concat(new []{ "Gradient" }).ToArray());
                }
            }
        }
    }

    [VFXHelpURL("Block-SetAttributeFromCurve")]
    [VFXInfo(variantProvider = typeof(AttributeFromCurveProvider))]
    class AttributeFromCurve : VFXBlock
    {
        public enum CurveSampleMode
        {
            OverLife,
            BySpeed,
            Random,
            RandomConstantPerParticle,
            Custom
        }

        public enum ComputeMode
        {
            Uniform,
            PerComponent
        }

        public enum ColorApplicationMode
        {
            Color = 1 << 0,
            Alpha = 1 << 1,
            ColorAndAlpha = Color | Alpha,
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), StringProvider(typeof(ReadWritableAttributeProvider))]
        public string attribute = VFXAttributesManager.GetBuiltInNamesOrCombination(true, false, false, false).First();

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("Specifies what operation to perform on the chosen attribute. The value derived from this block can overwrite, add to, multiply with, or blend with the existing attribute value.")]
        public AttributeCompositionMode Composition = AttributeCompositionMode.Overwrite;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("Specifies what operation to perform on the alpha value. The value derived from this block can overwrite, add to, multiply with, or blend with the existing alpha value.")]
        public AttributeCompositionMode AlphaComposition = AttributeCompositionMode.Overwrite;

        [VFXSetting, Tooltip("Specifies the method by which to sample the curve. This can be over the particle's lifetime, its speed, randomly, or through a user-specified value.")]
        public CurveSampleMode SampleMode = CurveSampleMode.OverLife;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("Specifies whether the block operates per component or uniformly across all components of the chosen attribute.")]
        public ComputeMode Mode = ComputeMode.PerComponent;

        [VFXSetting, Tooltip("Specifies whether the color is applied to RGB, alpha, or both.")]
        public ColorApplicationMode ColorMode = ColorApplicationMode.ColorAndAlpha;

        [VFXSetting, Tooltip("Specifies which channels to use in this block. This is useful for only affecting the relevant data if not all channels are used.")]
        public VariadicChannelOptions channels = VariadicChannelOptions.XYZ;
        private static readonly char[] channelNames = new char[] { 'x', 'y', 'z' };

        private string GenerateName()
        {
            var variadicName = currentAttribute.variadic == VFXVariadic.True ? "." + channels : string.Empty;
            return VFXBlockUtility.GetNameString(Composition).Label(false).AppendLiteral(attribute) + variadicName.AppendLabel(SampleMode.ToString());
        }

        public override string name => GenerateName();

        public override VFXContextType compatibleContexts => VFXContextType.InitAndUpdateAndOutput;
        public override VFXDataType compatibleData => VFXDataType.Particle;

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                VFXAttributeMode attributeMode = (Composition != AttributeCompositionMode.Overwrite) ? VFXAttributeMode.ReadWrite : VFXAttributeMode.Write;
                VFXAttributeMode alphaAttributeMode = (AlphaComposition != AttributeCompositionMode.Overwrite) ? VFXAttributeMode.ReadWrite : VFXAttributeMode.Write;

                var attrib = currentAttribute;
                if (attrib.Equals(VFXAttribute.Color))
                {
                    if ((ColorMode & ColorApplicationMode.Color) != 0)
                        yield return new VFXAttributeInfo(VFXAttribute.Color, attributeMode);
                    if ((ColorMode & ColorApplicationMode.Alpha) != 0)
                        yield return new VFXAttributeInfo(VFXAttribute.Alpha, alphaAttributeMode);
                }
                else
                {
                    if (attrib.variadic == VFXVariadic.True)
                    {
                        string channelsString = channels.ToString();
                        for (int i = 0; i < channelsString.Length; i++)
                            yield return new VFXAttributeInfo(VFXAttributesManager.FindBuiltInOnly(attrib.name + channelsString[i]), attributeMode);
                    }
                    else
                    {
                        yield return new VFXAttributeInfo(attrib, attributeMode);
                    }
                }

                switch (SampleMode)
                {
                    case CurveSampleMode.OverLife:
                        yield return new VFXAttributeInfo(VFXAttribute.Age, VFXAttributeMode.Read);
                        yield return new VFXAttributeInfo(VFXAttribute.Lifetime, VFXAttributeMode.Read);
                        break;

                    case CurveSampleMode.BySpeed:
                        yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.Read);
                        break;

                    case CurveSampleMode.Random:
                        yield return new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWrite);
                        break;

                    case CurveSampleMode.RandomConstantPerParticle:
                        yield return new VFXAttributeInfo(VFXAttribute.ParticleId, VFXAttributeMode.Read);
                        break;

                    default:
                        break;
                }
            }
        }

        public override void Sanitize(int version)
        {
            if (GetGraph() is {} graph)
            {
                if (VFXBlockUtility.SanitizeAttribute(graph, ref attribute, ref channels, version))
                {
                    Invalidate(InvalidationCause.kSettingChanged);
                }
            }
            else
            {
                Debug.LogError($"Trying to find attribute '{attribute}' when graph is not available");
            }

            base.Sanitize(version);
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                var attrib = currentAttribute;

                if (attrib.variadic == VFXVariadic.False)
                    yield return "channels";
                if (VFXExpression.TypeToSize(attrib.type) == 1)
                    yield return "Mode";

                if (!currentAttribute.Equals(VFXAttribute.Color))
                {
                    yield return "ColorMode";
                    yield return "AlphaComposition";
                }

                foreach (var setting in base.filteredOutSettings)
                    yield return setting;
            }
        }

        static private string GenerateLocalAttributeName(string name)
        {
            return name[0].ToString().ToUpper(CultureInfo.InvariantCulture) + name.Substring(1);
        }

        public override string source
        {
            get
            {
                string source = "";

                var attrib = currentAttribute;

                bool isColor = currentAttribute.Equals(VFXAttribute.Color);
                int attributeCount = isColor ? 2 : 1;

                int attributeSize = isColor ? 4 : VFXExpression.TypeToSize(attrib.type);
                int loopCount = 1;
                if (attrib.variadic == VFXVariadic.True)
                {
                    attributeSize = channels.ToString().Length;
                    loopCount = attributeSize;
                }

                source += GetFetchValueString(GenerateLocalAttributeName(attrib.name), attributeSize, Mode, SampleMode);

                int attributeAddedCount = 0;
                for (int attribIndex = 0; attribIndex < attributeCount; attribIndex++)
                {
                    string attribName = attrib.name;
                    if (isColor)
                    {
                        if (((int)ColorMode & (1 << attribIndex)) == 0)
                            continue;
                        if (attribIndex == 1)
                            attribName = VFXAttribute.Alpha.name;
                    }

                    string channelSource = "";
                    if (attributeAddedCount > 0)
                        channelSource += "\n";

                    for (int i = 0; i < loopCount; i++)
                    {
                        AttributeCompositionMode compositionMode = Composition;
                        string paramPostfix = (attrib.variadic == VFXVariadic.True) ? "." + channelNames[i] : "";

                        if (isColor)
                        {
                            if (attribIndex == 0)
                            {
                                paramPostfix = ".rgb";
                            }
                            else
                            {
                                paramPostfix = ".a";
                                compositionMode = AlphaComposition;
                            }
                        }

                        string attributePostfix = (attrib.variadic == VFXVariadic.True) ? char.ToUpper(channels.ToString()[i]).ToString() : "";

                        if (compositionMode == AttributeCompositionMode.Blend)
                            channelSource += VFXBlockUtility.GetComposeString(compositionMode, attribName + attributePostfix, "value" + paramPostfix, "Blend");
                        else
                            channelSource += VFXBlockUtility.GetComposeString(compositionMode, attribName + attributePostfix, "value" + paramPostfix);

                        if (i < loopCount - 1)
                            channelSource += "\n";
                    }

                    source += channelSource;
                    attributeAddedCount++;
                }

                return source;
            }
        }

        private string GetFetchValueString(string localName, int size, ComputeMode computeMode, CurveSampleMode sampleMode)
        {
            string output;
            switch (SampleMode)
            {
                case CurveSampleMode.OverLife: output = "float t = age / lifetime;\n"; break;
                case CurveSampleMode.BySpeed: output = "float t = saturate((length(velocity) - SpeedRange.x) * SpeedRange.y);\n"; break;
                case CurveSampleMode.Random: output = "float t = RAND;\n"; break;
                case CurveSampleMode.RandomConstantPerParticle: output = "float t = FIXED_RAND(Seed);\n"; break;
                case CurveSampleMode.Custom: output = "float t = SampleTime;\n"; break;
                default:
                    throw new NotImplementedException("Invalid CurveSampleMode");
            }

            output += $"float{(size == 1 ? "" : size.ToString())} value = 0.0f;\n";

            if (computeMode == ComputeMode.Uniform || size == 1)
            {
                output += $"value = SampleCurve({localName}, t);\n";
            }
            else
            {
                if (currentAttribute.Equals(VFXAttribute.Color))
                {
                    output += $"value = SampleGradient({localName}, t);\n";
                }
                else
                {
                    if (size > 0) output += $"value[0] = SampleCurve({localName + "_x"}, t);\n";
                    if (size > 1) output += $"value[1] = SampleCurve({localName + "_y"}, t);\n";
                    if (size > 2) output += $"value[2] = SampleCurve({localName + "_z"}, t);\n";
                    if (size > 3) output += $"value[3] = SampleCurve({localName + "_w"}, t);\n";
                }
            }

            return output;
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var attrib = currentAttribute;

                int size = VFXExpression.TypeToSize(attrib.type);
                if (attrib.variadic == VFXVariadic.True)
                    size = channels.ToString().Length;

                string localName = GenerateLocalAttributeName(attrib.name);
                if (Mode == ComputeMode.Uniform || size == 1)
                {
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(AnimationCurve), localName), VFXResources.defaultResources.animationCurve);
                }
                else
                {
                    if (attrib.Equals(VFXAttribute.Color))
                    {
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(Gradient), localName), VFXResources.defaultResources.gradient);
                    }
                    else
                    {
                        if (size > 0) yield return new VFXPropertyWithValue(new VFXProperty(typeof(AnimationCurve), localName + "_x"), VFXResources.defaultResources.animationCurve);
                        if (size > 1) yield return new VFXPropertyWithValue(new VFXProperty(typeof(AnimationCurve), localName + "_y"), VFXResources.defaultResources.animationCurve);
                        if (size > 2) yield return new VFXPropertyWithValue(new VFXProperty(typeof(AnimationCurve), localName + "_z"), VFXResources.defaultResources.animationCurve);
                        if (size > 3) yield return new VFXPropertyWithValue(new VFXProperty(typeof(AnimationCurve), localName + "_w"), VFXResources.defaultResources.animationCurve);
                    }
                }

                if (SampleMode == CurveSampleMode.BySpeed)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(Vector2), "SpeedRange", new MinAttribute(0.0f)), new Vector2(0.0f, 1.0f));
                else if (SampleMode == CurveSampleMode.Custom)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "SampleTime"));
                else if (SampleMode == CurveSampleMode.RandomConstantPerParticle)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(uint), "Seed"));

                if (Composition == AttributeCompositionMode.Blend || (attrib.Equals(VFXAttribute.Color) && AlphaComposition == AttributeCompositionMode.Blend))
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "Blend"));
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                VFXExpression speedRange = null;
                foreach (var p in GetExpressionsFromSlots(this))
                {
                    if (p.name == "SpeedRange")
                        speedRange = p.exp;
                    else
                        yield return p;
                }

                if (SampleMode == CurveSampleMode.BySpeed)
                {
                    var speedRangeComponents = VFXOperatorUtility.ExtractComponents(speedRange).ToArray();
                    // speedRange.y = 1 / (speedRange.y - speedRange.x)
                    var speedRangeDelta = speedRangeComponents[1] - speedRangeComponents[0];
                    speedRangeComponents[1] = VFXOperatorUtility.OneExpression[VFXValueType.Float] / speedRangeDelta;
                    yield return new VFXNamedExpression(new VFXExpressionCombine(speedRangeComponents), "SpeedRange");
                }

                if (SampleMode == CurveSampleMode.RandomConstantPerParticle)
                {
                    yield return new VFXNamedExpression(VFXBuiltInExpression.SystemSeed, "systemSeed");
                }

            }
        }

        protected override void OnAdded()
        {
            base.OnAdded();
            // When using custom attribute we need to access to the graph to find the custom attribute
            // and the graph is only available after the node being added to it.
            if (GetGraph() is {} graph && graph.attributesManager.IsCustom(attribute))
            {
                Invalidate(InvalidationCause.kSettingChanged);
            }
        }

        public VFXAttribute currentAttribute
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
                return new VFXAttribute(attribute, VFXValueType.Float, null);
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
    }
}
