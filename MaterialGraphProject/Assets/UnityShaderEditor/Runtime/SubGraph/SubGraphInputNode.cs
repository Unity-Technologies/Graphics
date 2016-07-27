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
        
        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            foreach (var slot in GetOutputSlots<MaterialSlot>())
            {
                var outDimension = ConvertConcreteSlotValueTypeToString(slot.concreteValueType);
                visitor.AddShaderChunk("float" + outDimension + " " + GetVariableNameForSlot(slot.id) + ";", true);
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
                         m_Name = GetVariableNameForSlot(slot.id),
                         m_PropType = PropertyType.Vector4,
                         m_Vector4 = slot.defaultValue
                     }
                );
            }
        }
    }
}
