using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine.VFX;

namespace UnityEditor.VFX.Block
{
    class AttributeProviderSpawner : VariantProvider, IStringProvider
    {
        public static readonly string[] kSupportedAttributesFromSpawnContext;

        static AttributeProviderSpawner()
        {
            kSupportedAttributesFromSpawnContext = VFXAttributesManager
                    .GetBuiltInNamesOrCombination(true, false, false, false)
                    .Concat(new[] { VFXAttribute.SpawnCount.name, VFXAttribute.SpawnTime.name })
                    .ToArray();
        }

        public override IEnumerable<Variant> GetVariants()
        {
            // Todo: should I add a sub-provider for random variants?
            foreach (var attribute in kSupportedAttributesFromSpawnContext)
            {
                yield return new Variant(
                    $"Set SpawnEvent {ObjectNames.NicifyVariableName(attribute)}",
                    "Attribute",
                    typeof(VFXSpawnerSetAttribute),
                    new[] {new KeyValuePair<string, object>("attribute", attribute)});
            }
        }

        public string[] GetAvailableString() => kSupportedAttributesFromSpawnContext;
    }

    [VFXHelpURL("Block-SetSpawnEvent")]
    [VFXInfo(category = "Attribute", variantProvider = typeof(AttributeProviderSpawner))]
    class VFXSpawnerSetAttribute : VFXAbstractSpawner
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), StringProvider(typeof(AttributeProviderSpawner))]
        public string attribute;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public RandomMode randomMode = RandomMode.Off;

        private bool attributeIsValid => !string.IsNullOrEmpty(attribute);

        public override IEnumerable<VFXAttribute> usedAttributes
        {
            get { yield return currentAttribute; }
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
        public override VFXTaskType spawnerType => VFXTaskType.SetAttributeSpawner;
    }
}
