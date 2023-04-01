using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Block
{
    class AttributeProviderSpawner : VariantProvider, IStringProvider
    {
        private static readonly string[] kReadOnlyExceptFromSpawnContext = new[] { VFXAttribute.SpawnCount.name, VFXAttribute.SpawnTime.name };

        protected sealed override Dictionary<string, object[]> variants { get; } = new Dictionary<string, object[]>
        {
            {
                "attribute",
                VFXAttribute.AllReadWritable.Concat(kReadOnlyExceptFromSpawnContext).Cast<object>().ToArray()
            }
        };

        public string[] GetAvailableString()
        {
            var validAttributes = VFXAttribute.AllIncludingVariadicExceptWriteOnly;
            validAttributes = validAttributes.Concat(kReadOnlyExceptFromSpawnContext).ToArray();
            return validAttributes;
        }
    }

    [VFXHelpURL("Block-SetSpawnEvent")]
    [VFXInfo(category = "Attribute", variantProvider = typeof(AttributeProviderSpawner))]
    class VFXSpawnerSetAttribute : VFXAbstractSpawner
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), StringProvider(typeof(AttributeProviderSpawner))]
        public string attribute;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public RandomMode randomMode = RandomMode.Off;

        private bool attributeIsValid
        {
            get
            {
                return !string.IsNullOrEmpty(attribute);
            }
        }

        private VFXAttribute currentAttribute
        {
            get
            {
                return VFXAttribute.Find(attribute);
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                if (attributeIsValid)
                {
                    var attrib = currentAttribute;

                    VFXPropertyAttributes attr = new VFXPropertyAttributes();
                    if (attrib.Equals(VFXAttribute.Color))
                        attr = new VFXPropertyAttributes(new ShowAsColorAttribute());

                    Type slotType = VFXExpression.TypeToType(attrib.type);

                    if (randomMode == RandomMode.Off)
                        yield return new VFXPropertyWithValue(new VFXProperty(slotType, currentAttribute.name, attr), currentAttribute.value.GetContent());
                    else
                    {
                        yield return new VFXPropertyWithValue(new VFXProperty(slotType, "Min", attr), currentAttribute.value.GetContent());
                        yield return new VFXPropertyWithValue(new VFXProperty(slotType, "Max", attr), currentAttribute.value.GetContent());
                    }
                }
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                if (!attributeIsValid)
                    return Enumerable.Empty<VFXNamedExpression>();

                int size = VFXExpression.TypeToSize(currentAttribute.type);

                if (randomMode == RandomMode.Off)
                {
                    return base.parameters;
                }

                VFXExpression random;

                if (size == 1)
                {
                    random = new VFXExpressionRandom(false, new RandId(this, 0));
                }
                else
                {
                    switch (randomMode)
                    {
                        default:
                        case RandomMode.PerComponent:
                            random = new VFXExpressionCombine(Enumerable.Range(0, size).Select(i => new VFXExpressionRandom(false, new RandId(this, i))).ToArray());
                            break;
                        case RandomMode.Uniform:
                            random = new VFXExpressionCombine(Enumerable.Repeat(new VFXExpressionRandom(false, new RandId(this, 0)), size).ToArray());
                            break;
                    }
                }

                var min = base.parameters.First(o => o.name == "Min");
                var max = base.parameters.First(o => o.name == "Max");
                return new[] { new VFXNamedExpression(VFXOperatorUtility.Lerp(min.exp, max.exp, random), currentAttribute.name) };
            }
        }

        public override string name
        {
            get
            {
                if (!attributeIsValid)
                    return string.Empty;
                return $"Set SpawnEvent {ObjectNames.NicifyVariableName(attribute)} {VFXBlockUtility.GetNameString(randomMode)}";
            }
        }
        public override VFXTaskType spawnerType { get { return VFXTaskType.SetAttributeSpawner; } }
    }
}
