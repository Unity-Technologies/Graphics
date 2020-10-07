using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Attribute", variantProvider = typeof(AttributeVariantReadWritableNoVariadic))]
    class VFXSpawnerSetAttribute : VFXAbstractSpawner
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), StringProvider(typeof(AttributeProvider))]
        public string attribute = VFXAttribute.AllReadWritable.First();

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public RandomMode randomMode = RandomMode.Off;

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

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
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
                return new[] { new VFXNamedExpression(VFXOperatorUtility.Lerp(min.exp, max.exp, random), currentAttribute.name)};
            }
        }

        public override string name { get { return string.Format("Set SpawnEvent {0} {1}", ObjectNames.NicifyVariableName(attribute), VFXBlockUtility.GetNameString(randomMode)); } }
        public override VFXTaskType spawnerType { get { return VFXTaskType.SetAttributeSpawner; } }
    }
}
