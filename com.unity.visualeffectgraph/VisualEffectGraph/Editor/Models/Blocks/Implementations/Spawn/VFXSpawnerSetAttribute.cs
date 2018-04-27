using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Spawn", variantProvider = typeof(AttributeVariantReadWritable))]
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
                switch (randomMode)
                {
                    case RandomMode.Off:
                        yield return new VFXPropertyWithValue(new VFXProperty(VFXExpression.TypeToType(currentAttribute.type), currentAttribute.name), currentAttribute.value.GetContent());
                        break;
                    case RandomMode.Uniform:
                    case RandomMode.PerComponent:
                        yield return new VFXPropertyWithValue(new VFXProperty(VFXExpression.TypeToType(currentAttribute.type), "Min"), currentAttribute.value.GetContent());
                        yield return new VFXPropertyWithValue(new VFXProperty(VFXExpression.TypeToType(currentAttribute.type), "Max"), currentAttribute.value.GetContent());
                        break;
                }
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {

                int size = VFXExpression.TypeToSize(currentAttribute.type);

                if(randomMode == RandomMode.Off)
                {
                    return base.parameters;
                }

                VFXExpression random;

                if (size == 1)
                {
                    random = new VFXExpressionRandom();
                }
                else
                {
                    switch (randomMode)
                    {
                        default:
                        case RandomMode.PerComponent:
                            random = new VFXExpressionCombine(Enumerable.Repeat(0, size).Select(_ => new VFXExpressionRandom()).ToArray());
                            break;
                        case RandomMode.Uniform:
                            random = new VFXExpressionCombine(Enumerable.Repeat(new VFXExpressionRandom(), size).ToArray());
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
