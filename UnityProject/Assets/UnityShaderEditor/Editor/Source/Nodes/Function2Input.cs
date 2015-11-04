using System.Linq;
using UnityEditor.Graphs;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    public abstract class Function2Input : BaseMaterialNode, IGeneratesBodyCode
    {
        public override bool hasPreview
        {
            get { return true; }
        }

        public override void OnEnable()
        {
            base.OnEnable();
            AddSlot(GetInputSlot1());
            AddSlot(GetInputSlot2());
            AddSlot(GetOutputSlot());
            RemoveSlotsNameNotMatching(validSlots);
        }

        protected string[] validSlots
        {
            get { return new[] {GetInputSlot1Name(), GetInputSlot2Name(), GetOutputSlotName()}; }
        }

        protected virtual MaterialGraphSlot GetInputSlot1()
        {
            var slot = new Slot(SlotType.InputSlot, GetInputSlot1Name());
            return new MaterialGraphSlot(slot, SlotValueType.Dynamic);
        }

        protected virtual MaterialGraphSlot GetInputSlot2()
        {
            var slot = new Slot(SlotType.InputSlot, GetInputSlot2Name());
            return new MaterialGraphSlot(slot, SlotValueType.Dynamic);
        }

        protected virtual MaterialGraphSlot GetOutputSlot()
        {
            var slot = new Slot(SlotType.OutputSlot, GetOutputSlotName());
            return new MaterialGraphSlot(slot, SlotValueType.Dynamic);
        }
        
        protected virtual string GetInputSlot1Name()
        {
            return "Input1";
        }

        protected virtual string GetInputSlot2Name()
        {
            return "Input2";
        }

        protected virtual string GetOutputSlotName()
        {
            return "Output";
        }

        protected abstract string GetFunctionName();

        protected virtual string GetFunctionPrototype(string arg1Name, string arg2Name)
        {
            return "inline " + precision + outputDimension + " " + GetFunctionName() + " (" 
                + precision + input1Dimension + " " + arg1Name + ", " 
                + precision + input2Dimension + " " + arg2Name + ")";
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputSlot = FindOutputSlot(GetOutputSlotName());
            var inputSlot1 = FindInputSlot(GetInputSlot1Name());
            var inputSlot2 = FindInputSlot(GetInputSlot2Name());

            if (inputSlot1 == null || inputSlot2 == null || outputSlot == null)
            {
                Debug.LogError("Invalid slot configuration on node: " + name);
                return;
            }

            string input1Value = GetSlotValue(inputSlot1, generationMode);
            string input2Value = GetSlotValue(inputSlot2, generationMode);
            visitor.AddShaderChunk(precision + outputDimension + " " + GetOutputVariableNameForSlot(outputSlot, generationMode) + " = " + GetFunctionCallBody(input1Value, input2Value) + ";", true);
        }

        protected virtual string GetFunctionCallBody(string input1Value, string input2Value)
        {
            return GetFunctionName() + " (" + input1Value + ", " + input2Value + ")";
        }

        public string outputDimension
        {
            get { return ConvertConcreteSlotValueTypeToString(concreteOutputSlotValueTypes[GetOutputSlotName()]); }
        }

        public string input1Dimension
        {
            get { return ConvertConcreteSlotValueTypeToString(concreteInputSlotValueTypes[GetInputSlot1Name()]); }
        }

        public string input2Dimension
        {
            get { return ConvertConcreteSlotValueTypeToString(concreteInputSlotValueTypes[GetInputSlot2Name()]); }
        }
    }
}
