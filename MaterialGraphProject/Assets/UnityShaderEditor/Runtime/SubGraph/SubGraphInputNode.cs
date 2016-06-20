using System.Collections.Generic;

namespace UnityEngine.MaterialGraph
{
    public class SubGraphInputNode : AbstractSubGraphIONode, IGenerateProperties
    {
        public SubGraphInputNode()
        {
            name = "SubGraphInputs";
        }
/*
        public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            base.GeneratePropertyBlock(visitor, generationMode);

            if (!generationMode.IsPreview())
                return;

            foreach (var slot in outputSlots)
            {
                if (slot.edges.Count == 0)
                    continue;

                var defaultValue = GetSlotDefaultValue(slot.name);
                if (defaultValue != null)
                    defaultValue.GeneratePropertyBlock(visitor, generationMode);
            }
        }

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode, ConcreteSlotValueType slotValueType)
        {
            base.GeneratePropertyUsages(visitor, generationMode, slotValueType);

            if (!generationMode.IsPreview())
                return;

            foreach (var slot in outputSlots)
            {
                if (slot.edges.Count == 0)
                    continue;

                var defaultValue = GetSlotDefaultValue(slot.name);
                if (defaultValue != null)
                    defaultValue.GeneratePropertyUsages(visitor, generationMode, slotValueType);
            }
        }

        protected override void CollectPreviewMaterialProperties (List<PreviewProperty> properties)
        {
            base.CollectPreviewMaterialProperties(properties);
            foreach (var slot in outputSlots)
            {
                if (slot.edges.Count == 0)
                    continue;

                var defaultOutput = GetSlotDefaultValue(slot.name);
                if (defaultOutput == null)
                    continue;

                var pp = new PreviewProperty
                {
                    m_Name = defaultOutput.inputName,
                    m_PropType = PropertyType.Vector4,
                    m_Vector4 = defaultOutput.defaultValue
                };
                properties.Add (pp);
            }
        }*/
    }
}
