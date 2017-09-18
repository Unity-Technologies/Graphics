using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    public class SubGraphInputNode : AbstractSubGraphIONode
    {
        public SubGraphInputNode()
        {
            name = "SubGraphInputs";
        }

        public override int AddSlot()
        {
            var nextSlotId = GetOutputSlots<ISlot>().Count() + 1;
            AddSlot(new MaterialSlot(-nextSlotId, "Input " + nextSlotId, "Input" + nextSlotId, SlotType.Output, SlotValueType.Vector4, Vector4.zero));
            return -nextSlotId;
        }

        public override void RemoveSlot()
        {
            var lastSlotId = GetOutputSlots<ISlot>().Count();
            if (lastSlotId == 0)
                return;

            RemoveSlot(-lastSlotId);
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            foreach (var slot in GetOutputSlots<MaterialSlot>())
            {
                IShaderProperty property;
                switch (slot.concreteValueType)
                {
                    case ConcreteSlotValueType.Vector4:
                        property = new Vector4ShaderProperty();
                        break;
                    case ConcreteSlotValueType.Vector3:
                        property = new Vector3ShaderProperty();
                        break;
                    case ConcreteSlotValueType.Vector2:
                        property = new Vector2ShaderProperty();
                        break;
                    case ConcreteSlotValueType.Vector1:
                        property = new FloatShaderProperty();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                property.generatePropertyBlock = false;
                properties.AddShaderProperty(property);
            }
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            base.CollectPreviewMaterialProperties(properties);
            foreach (var s in GetOutputSlots<MaterialSlot>())
            {
                properties.Add
                (
                    new PreviewProperty
                    {
                        m_Name = GetVariableNameForSlot(s.id),
                        m_PropType = ConvertConcreteSlotValueTypeToPropertyType(s.concreteValueType),
                        m_Vector4 = s.currentValue,
                        m_Float = s.currentValue.x,
                        m_Color = s.currentValue
                    }
                );
            }
        }
    }
}
