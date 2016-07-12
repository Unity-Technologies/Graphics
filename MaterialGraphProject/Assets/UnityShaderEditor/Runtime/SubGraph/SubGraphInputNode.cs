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
        public override void AddSlot()
        {
            var index = GetOutputSlots<ISlot>().Count();
            AddSlot(new MaterialSlot("Input" + index, "Input" + index, SlotType.Output, index, SlotValueType.Vector4, Vector4.zero));
        }

        public override void RemoveSlot()
        {
            var index = GetOutputSlots<ISlot>().Count();
            if (index == 0)
                return;

            RemoveSlot("Input" + (index - 1));
        }
        
        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode, ConcreteSlotValueType valueType)
        {
            foreach (var slot in GetOutputSlots<MaterialSlot>())
            {
                var outDimension = ConvertConcreteSlotValueTypeToString(slot.concreteValueType);
                visitor.AddShaderChunk("float" + outDimension + " " + GetOutputVariableNameForSlot(slot) + ";", true);
            }
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            base.CollectPreviewMaterialProperties(properties);
            foreach (var slot in GetOutputSlots<MaterialSlot>())
            {
                properties.Add(
                     new PreviewProperty
                     {
                         m_Name = GetOutputVariableNameForSlot(slot),
                         m_PropType = PropertyType.Vector4,
                         m_Vector4 = slot.defaultValue
                     }
                );
            }
        }
    }
}
