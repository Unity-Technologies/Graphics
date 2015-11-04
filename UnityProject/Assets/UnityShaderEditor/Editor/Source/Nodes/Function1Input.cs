using UnityEditor.Graphs;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    abstract class Function1Input : BaseMaterialNode, IGeneratesBodyCode
    {
        public override bool hasPreview
        {
            get { return true; }
        }
        
        public override void OnEnable()
        {
            base.OnEnable();
            AddSlot(GetInputSlot());
            AddSlot(GetOutputSlot());
            RemoveSlotsNameNotMatching(validSlots);
        }

        protected string[] validSlots
        {
            get { return new[] {GetInputSlotName(), GetOutputSlotName()}; }
        }

        protected virtual MaterialGraphSlot GetInputSlot()
        {
            var slot = new Slot(SlotType.InputSlot, GetInputSlotName());
            return new MaterialGraphSlot(slot, SlotValueType.Dynamic);
        }

        protected virtual MaterialGraphSlot GetOutputSlot()
        {
            var slot = new Slot(SlotType.OutputSlot, GetOutputSlotName());
            return new MaterialGraphSlot(slot, SlotValueType.Dynamic);
        }

        protected virtual string GetInputSlotName() {return "Input"; }
        protected virtual string GetOutputSlotName() {return "Output"; }

        protected abstract string GetFunctionName();

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputSlot = FindOutputSlot(GetOutputSlotName());
            var inputSlot = FindInputSlot(GetInputSlotName());

            if (inputSlot == null || outputSlot == null)
            {
                Debug.LogError("Invalid slot configuration on node: " + name);
                return;
            }

            bool inputConnected = inputSlot.edges.Count > 0;

            string inputValue;
            if (inputConnected)
            {
                var inputDateProvider = inputSlot.edges[0].fromSlot.node as BaseMaterialNode;
                inputValue = inputDateProvider.GetOutputVariableNameForSlot(inputSlot.edges[0].fromSlot, generationMode);
            }
            else
            {
                var defaultValue = GetSlotDefaultValue(inputSlot.name);
                inputValue = defaultValue.GetDefaultValue(generationMode, concreteInputSlotValueTypes[inputSlot.name]);
            }

            visitor.AddShaderChunk(precision + outputDimension + " " + GetOutputVariableNameForSlot(outputSlot, generationMode) + " = " + GetFunctionCallBody(inputValue) + ";", true);
        }

        protected virtual string GetFunctionCallBody(string inputValue)
        {
            return GetFunctionName() + " (" + inputValue + ")";
        }

        public string outputDimension
        {
            get { return ConvertConcreteSlotValueTypeToString(concreteOutputSlotValueTypes[GetOutputSlotName()]); }
        }

        public string input1Dimension
        {
            get { return ConvertConcreteSlotValueTypeToString(concreteInputSlotValueTypes[GetInputSlotName()]); }
        }
    }
}
