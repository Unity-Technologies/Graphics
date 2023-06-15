using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using Object = System.Object;

namespace UnityEditor.VFX.Block
{
    class AttributeFromMapProvider : VariantProvider
    {
        public override IEnumerable<Variant> ComputeVariants()
        {
            var compositions = new[] { AttributeCompositionMode.Add, AttributeCompositionMode.Overwrite, AttributeCompositionMode.Multiply, AttributeCompositionMode.Blend };
            var attributes = VFXAttribute.AllIncludingVariadicReadWritable.Except(new[] { VFXAttribute.Alive.name }).ToArray();
            var sampleModes = Enum.GetValues(typeof(AttributeFromMap.AttributeMapSampleMode)).OfType<AttributeFromMap.AttributeMapSampleMode>().ToArray();

            foreach (var attribute in attributes)
            {
                foreach (var composition in compositions)
                {
                    foreach (var sampleMode in sampleModes)
                    {
                        yield return new Variant(
                            new[]
                            {
                                new KeyValuePair<string, object>("attribute", attribute),
                                new KeyValuePair<string, object>("Composition", composition),
                                new KeyValuePair<string, object>("SampleMode", sampleMode)
                            },
                            new[] { attribute, VFXBlockUtility.GetNameString(composition) });
                    }
                }
            }
        }
    }

    [VFXHelpURL("Block-SetAttributeFromMap")]
    [VFXInfo(category = "Attribute/{0}/Map/{1}", variantProvider = typeof(AttributeFromMapProvider))]
    class AttributeFromMap : VFXBlock
    {
        // TODO: Let's factorize this this into a utility class
        public enum AttributeMapSampleMode
        {
            IndexRelative,
            Index,
            Sequential,
            Sample2DLOD,
            Sample3DLOD,
            Random,
            RandomConstantPerParticle,
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), StringProvider(typeof(ReadWritableAttributeProvider)), Tooltip("Target Attribute")]
        public string attribute = VFXAttribute.AllIncludingVariadicWritable.First();

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("Specifies what operation to perform on the chosen attribute. The value derived from this block can overwrite, add to, multiply with, or blend with the existing attribute value.")]
        public AttributeCompositionMode Composition = AttributeCompositionMode.Overwrite;

        [VFXSetting, Tooltip("Specifies the mode by which to sample the attribute map. This can be done via an index, sequentially, by sampling a 2D/3D texture, or randomly.")]
        public AttributeMapSampleMode SampleMode = AttributeMapSampleMode.RandomConstantPerParticle;

        [VFXSetting, SerializeField, Tooltip("Specifies how Unity handles the sample when the index is out of the point cache bounds.")]
        private VFXOperatorUtility.SequentialAddressingMode mode = VFXOperatorUtility.SequentialAddressingMode.Wrap;

        [VFXSetting, Tooltip("Specifies which channels to use in this block. This is useful for only affecting the relevant data if not all channels are used.")]
        public VariadicChannelOptions channels = VariadicChannelOptions.XYZ;
        private static readonly char[] channelNames = new char[] { 'x', 'y', 'z' };

        [VFXSetting, SerializeField, Tooltip("When enabled, you can specify the number of points contained in the attribute map. This is useful when the number of points is smaller than the texture size.")]
        private bool usePointCount = false;

        public override string libraryName => $"{VFXBlockUtility.GetNameString(Composition)} {ObjectNames.NicifyVariableName(attribute)} from Map ({ObjectNames.NicifyVariableName(SampleMode.ToString())})";

        public override string name
        {
            get
            {
                string variadicName = (currentAttribute.variadic == VFXVariadic.True) ? "." + channels : "";
                return $"{VFXBlockUtility.GetNameString(Composition)} {ObjectNames.NicifyVariableName(attribute) + variadicName} from Map";
            }
        }

        public override VFXContextType compatibleContexts { get; } = VFXContextType.InitAndUpdateAndOutput;
        public override VFXDataType compatibleData { get; } = VFXDataType.Particle;

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

                if (SampleMode == AttributeMapSampleMode.Sequential)
                    yield return new VFXAttributeInfo(VFXAttribute.ParticleId, VFXAttributeMode.Read);
                if (SampleMode == AttributeMapSampleMode.Random)
                    yield return new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWrite);
                if (SampleMode == AttributeMapSampleMode.RandomConstantPerParticle)
                    yield return new VFXAttributeInfo(VFXAttribute.ParticleId, VFXAttributeMode.Read);
            }
        }

        public override void Sanitize(int version)
        {
            if (VFXBlockUtility.SanitizeAttribute(ref attribute, ref channels, version))
                Invalidate(InvalidationCause.kSettingChanged);

            base.Sanitize(version);
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (string setting in base.filteredOutSettings)
                    yield return setting;
                var attrib = VFXAttribute.Find(attribute);
                if (attrib.variadic == VFXVariadic.False)
                    yield return "channels";
                if (SampleMode == AttributeMapSampleMode.Sample2DLOD ||
                    SampleMode == AttributeMapSampleMode.Sample3DLOD)
                    yield return "usePointCount";
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                // Texture Property (2D/3D)
                string textureInputPropertiesType = "InputProperties2DTexture";

                if (SampleMode == AttributeMapSampleMode.Sample3DLOD)
                    textureInputPropertiesType = "InputProperties3DTexture";

                var properties = PropertiesFromType(textureInputPropertiesType);
                if(usePointCount && SampleMode != AttributeMapSampleMode.Sample2DLOD && SampleMode != AttributeMapSampleMode.Sample3DLOD)
                    properties = properties.Concat(PropertiesFromType("InputPropertiesPointCount"));

                // Sample Mode
                switch (SampleMode)
                {
                    case AttributeMapSampleMode.IndexRelative:
                        properties = properties.Concat(PropertiesFromType("InputPropertiesRelative"));
                        break;
                    case AttributeMapSampleMode.Index:
                        properties = properties.Concat(PropertiesFromType("InputPropertiesIndex"));
                        break;
                    case AttributeMapSampleMode.Sample2DLOD:
                        properties = properties.Concat(PropertiesFromType("InputPropertiesSample2DLOD"));
                        break;
                    case AttributeMapSampleMode.Sample3DLOD:
                        properties = properties.Concat(PropertiesFromType("InputPropertiesSample3DLOD"));
                        break;
                    case AttributeMapSampleMode.RandomConstantPerParticle:
                        properties = properties.Concat(PropertiesFromType("InputPropertiesRandomConstant"));
                        break;
                    default:
                        break;
                }

                // Need Composition Input Properties?
                if (Composition == AttributeCompositionMode.Blend)
                {
                    properties = properties.Concat(PropertiesFromType("InputPropertiesBlend"));
                }

                // Scale and Bias for the values, depending on the property type
                var attrib = currentAttribute;
                if (VFXExpression.IsUniform(attrib.type))
                {
                    int count = VFXExpression.TypeToSize(attrib.type);
                    if (attrib.variadic == VFXVariadic.True)
                        count = channels.ToString().Length;

                    string scaleInputPropertiesType;
                    switch (count)
                    {
                        default:
                        case 1:
                            scaleInputPropertiesType = "InputPropertiesScaleFloat";
                            break;
                        case 2:
                            scaleInputPropertiesType = "InputPropertiesScaleFloat2";
                            break;
                        case 3:
                            scaleInputPropertiesType = "InputPropertiesScaleFloat3";
                            break;
                        case 4:
                            scaleInputPropertiesType = "InputPropertiesScaleFloat4";
                            break;
                    }

                    properties = properties.Concat(PropertiesFromType(scaleInputPropertiesType));
                }

                return properties;
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var param in base.parameters)
                    yield return param;
                if (SampleMode == AttributeMapSampleMode.Sample2DLOD || SampleMode == AttributeMapSampleMode.Sample3DLOD)
                {
                    //Do nothing
                }
                else
                {
                    var particleIdExpr = new VFXAttributeExpression(VFXAttribute.ParticleId);
                    var attribMapExpr = GetExpressionsFromSlots(this).First(o => o.name == "attributeMap").exp;
                    var height = new VFXExpressionTextureHeight(attribMapExpr);
                    var width = new VFXExpressionTextureWidth(attribMapExpr);
                    var texSizeExpr = height * width;
                    var countExpr = texSizeExpr;
                    if (usePointCount)
                    {
                        var pointCountExpr = GetExpressionsFromSlots(this).First(o => o.name == "pointCount").exp;
                        countExpr = new VFXExpressionMin(pointCountExpr, texSizeExpr);
                    }


                    VFXExpression samplePos = VFXValue.Constant(0);

                    switch (SampleMode)
                    {
                        case AttributeMapSampleMode.IndexRelative:
                            var relativePosExpr = GetExpressionsFromSlots(this).First(o => o.name == "relativePos").exp;
                            samplePos = VFXOperatorUtility.ApplyAddressingMode(
                                new VFXExpressionCastFloatToUint(relativePosExpr * new VFXExpressionCastUintToFloat(countExpr)),
                                countExpr,
                                mode);
                            break;
                        case AttributeMapSampleMode.Index:
                            var indexExpr = GetExpressionsFromSlots(this).First(o => o.name == "index").exp;
                            samplePos = VFXOperatorUtility.ApplyAddressingMode(indexExpr, countExpr, mode);
                            break;
                        case AttributeMapSampleMode.Sequential:
                            samplePos = VFXOperatorUtility.ApplyAddressingMode(particleIdExpr, countExpr, mode);
                            break;
                        case AttributeMapSampleMode.Random:
                            var randExpr = VFXOperatorUtility.BuildRandom(VFXSeedMode.PerParticle, false, new RandId(this));
                            samplePos = new VFXExpressionCastFloatToUint(randExpr * new VFXExpressionCastUintToFloat(countExpr));
                            break;
                        case AttributeMapSampleMode.RandomConstantPerParticle:
                            var seedExpr = GetExpressionsFromSlots(this).First(o => o.name == "Seed").exp;
                            var randFixedExpr = VFXOperatorUtility.BuildRandom(VFXSeedMode.PerParticle, true, new RandId(this), seedExpr);
                            samplePos = new VFXExpressionCastFloatToUint(randFixedExpr * new VFXExpressionCastUintToFloat(countExpr));
                            break;
                    }
                    var y = samplePos / width;
                    var x = samplePos - (y * width);
                    var outputType = VFXExpression.TypeToType(currentAttribute.type);
                    var type = typeof(VFXExpressionSampleAttributeMap<>).MakeGenericType(outputType);
                    var outputExpr = Activator.CreateInstance(type, new object[] { attribMapExpr, x, y });

                    yield return new VFXNamedExpression((VFXExpression)outputExpr, "value");
                }
            }
        }

        public override string source
        {
            get
            {
                string biasScale = "value = (value  + valueBias) * valueScale;";
                string output = "";
                var attrib = currentAttribute;
                string attributeName = attrib.name;
                int loopCount = 1;

                VFXValueType valueType = attrib.type;
                if (attrib.variadic == VFXVariadic.True)
                {
                    loopCount = channels.ToString().Length;
                    switch (loopCount)
                    {
                        case 1:
                            valueType = VFXValueType.Float;
                            break;
                        case 2:
                            valueType = VFXValueType.Float2;
                            break;
                        case 3:
                            valueType = VFXValueType.Float3;
                            break;
                        default:
                            break;
                    }
                }

                if (SampleMode == AttributeMapSampleMode.Sample2DLOD || SampleMode == AttributeMapSampleMode.Sample3DLOD)
                {
                    output += string.Format(@"
{0} value = ({0})attributeMap.t.SampleLevel(attributeMap.s, SamplePosition, LOD);
{1}
", GetCompatTypeString(valueType), biasScale);
                }
                else // All other SampleModes
                {
                    output += biasScale;
                }

                for (int i = 0; i < loopCount; i++)
                {
                    string paramPostfix = (attrib.variadic == VFXVariadic.True) ? "." + channelNames[i] : "";
                    string attributePostfix = (attrib.variadic == VFXVariadic.True) ? char.ToUpper(channels.ToString()[i]).ToString() : "";

                    if (Composition != AttributeCompositionMode.Blend)
                        output += VFXBlockUtility.GetComposeString(Composition, attributeName + attributePostfix, "value" + paramPostfix);
                    else
                        output += VFXBlockUtility.GetComposeString(Composition, attributeName + attributePostfix, "value" + paramPostfix, "blend");

                    if (i < loopCount - 1)
                        output += "\n";
                }
                return output;
            }
        }

        public class InputProperties2DTexture
        {
            [Tooltip("AttributeMap texture to read attributes from")]
            public Texture2D attributeMap = VFXResources.defaultResources.noiseTexture;
        }
        public class InputProperties3DTexture
        {
            [Tooltip("3D AttributeMap texture to read attributes from")]
            public Texture3D attributeMap = VFXResources.defaultResources.vectorField;
        }

        public class InputPropertiesRelative
        {
            [Tooltip("Position in range [0..1] to sample")]
            public float relativePos = 0.0f;
        }

        public class InputPropertiesIndex
        {
            [Tooltip("Absolute index to sample")]
            public uint index = 0;
        }

        public class InputPropertiesSample2DLOD
        {
            [Tooltip("Absolute index to sample")]
            public Vector2 SamplePosition = Vector2.zero;
            public float LOD = 0.0f;
        }
        public class InputPropertiesSample3DLOD
        {
            [Tooltip("Absolute index to sample")]
            public Vector3 SamplePosition = Vector2.zero;
            public float LOD = 0.0f;
        }
        public class InputPropertiesRandomConstant
        {
            [Tooltip("Seed to compute the constant random")]
            public uint Seed = 0;
        }

        public class InputPropertiesBlend
        {
            [Tooltip("Blend fraction with previous value")]
            public float blend = 0.5f;
        }

        public class InputPropertiesScaleFloat
        {
            [Tooltip("Bias Applied to the read float value")]
            public float valueBias = 0.0f;
            [Tooltip("Scale Applied to the read float value")]
            public float valueScale = 1.0f;
        }
        public class InputPropertiesScaleFloat2
        {
            [Tooltip("Bias Applied to the read Vector2 value")]
            public Vector2 valueBias = new Vector2(0.0f, 0.0f);
            [Tooltip("Scale Applied to the read Vector2 value")]
            public Vector2 valueScale = new Vector2(1.0f, 1.0f);
        }
        public class InputPropertiesScaleFloat3
        {
            [Tooltip("Bias Applied to the read Vector3 value")]
            public Vector3 valueBias = new Vector3(0.0f, 0.0f, 0.0f);
            [Tooltip("Scale Applied to the read Vector3 value")]
            public Vector3 valueScale = new Vector3(1.0f, 1.0f, 1.0f);
        }

        public class InputPropertiesScaleFloat4
        {
            [Tooltip("Bias Applied to the read Vector4 value")]
            public Vector4 valueBias = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
            [Tooltip("Scale Applied to the read Vector4 value")]
            public Vector4 valueScale = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        }

        public class InputPropertiesPointCount
        {
            [Tooltip("Sets the number of elements in the attribute map.")]
            public uint pointCount = 0u;
        }

        private VFXAttribute currentAttribute { get { return VFXAttribute.Find(attribute); } }

        private static string GetCompatTypeString(VFXValueType valueType)
        {
            if (!VFXExpression.IsUniform(valueType))
                throw new InvalidOperationException("Trying to fetch an attribute of type: " + valueType);

            return VFXExpression.TypeToCode(valueType);
        }
    }
}
