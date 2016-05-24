using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    public abstract class Function2Input : AbstractMaterialNode, IGeneratesBodyCode
    {
        public override bool hasPreview
        {
            get { return true; }
        }

        protected Function2Input(AbstractMaterialGraph owner)
            : base(owner)
        {
            AddSlot(GetInputSlot1());
            AddSlot(GetInputSlot2());
            AddSlot(GetOutputSlot());
            RemoveSlotsNameNotMatching(validSlots);
        }

        protected string[] validSlots
        {
            get { return new[] {GetInputSlot1Name(), GetInputSlot2Name(), GetOutputSlotName()}; }
        }

        protected MaterialSlot GetInputSlot1()
        {
            return new MaterialSlot(this, GetInputSlot1Name(), GetInputSlot1Name(), SlotType.Input, SlotValueType.Dynamic, Vector4.zero);
        }

        protected MaterialSlot GetInputSlot2()
        {
            return new MaterialSlot(this, GetInputSlot2Name(), GetInputSlot2Name(), SlotType.Input, SlotValueType.Dynamic, Vector4.zero);
        }

        protected MaterialSlot GetOutputSlot()
        {
            return new MaterialSlot(this, GetOutputSlotName(), GetOutputSlotName(), SlotType.Output, SlotValueType.Dynamic, Vector4.zero);
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
            var outputSlot = FindMaterialOutputSlot(GetOutputSlotName());
            var inputSlot1 = FindMaterialInputSlot(GetInputSlot1Name());
            var inputSlot2 = FindMaterialInputSlot(GetInputSlot2Name());

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
            get { return ConvertConcreteSlotValueTypeToString(FindMaterialOutputSlot(GetOutputSlotName()).concreteValueType); }
        }

        public string input1Dimension
        {
            get { return ConvertConcreteSlotValueTypeToString(FindMaterialInputSlot(GetInputSlot1Name()).concreteValueType); }
        }

        public string input2Dimension
        {
            get { return ConvertConcreteSlotValueTypeToString(FindMaterialInputSlot(GetInputSlot2Name()).concreteValueType); }
        }
    }
}
