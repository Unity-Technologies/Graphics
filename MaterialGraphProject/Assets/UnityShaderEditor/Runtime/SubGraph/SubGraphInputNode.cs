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
            AddSlot(new MaterialSlot(-nextSlotId, "Input " + nextSlotId, "Input" + nextSlotId, SlotType.Output, SlotValueType.Vector4, Vector4.zero, true));
			return -nextSlotId;
        }

        public override void RemoveSlot()
        {
            var lastSlotId = GetOutputSlots<ISlot>().Count();
            if (lastSlotId == 0)
                return;

            RemoveSlot(-lastSlotId);
        }


		public override void UpdateNodeAfterDeserialization()
		{
			base.UpdateNodeAfterDeserialization();
			foreach (var slot in GetOutputSlots<MaterialSlot>())
			{
				slot.showValue = true; 
			}
		}

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.ForReals)
                return;

            foreach (var slot in GetOutputSlots<MaterialSlot>())
            {
                var outDimension = ConvertConcreteSlotValueTypeToString(slot.concreteValueType);
                visitor.AddShaderChunk("float" + outDimension + " " + GetVariableNameForSlot(slot.id) + ";", true);
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
						m_Name = GetVariableNameForSlot (s.id),
						m_PropType = ConvertConcreteSlotValueTypeToPropertyType (s.concreteValueType),
						m_Vector4 = s.currentValue,
						m_Float = s.currentValue.x,
						m_Color = s.currentValue
					}
				);
			}
        }
    }
}
