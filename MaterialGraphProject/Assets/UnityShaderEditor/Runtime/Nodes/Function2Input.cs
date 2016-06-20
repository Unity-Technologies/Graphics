using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    public abstract class Function2Input : AbstractMaterialNode, IGeneratesBodyCode
    {
        public override bool hasPreview
        {
            get { return true; }
        }

        protected Function2Input()
        {
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
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

        protected virtual MaterialSlot GetInputSlot1()
        {
            return new MaterialSlot(GetInputSlot1Name(), GetInputSlot1Name(), SlotType.Input, 0, SlotValueType.Dynamic, Vector4.zero);
        }

        protected virtual MaterialSlot GetInputSlot2()
        {
            return new MaterialSlot(GetInputSlot2Name(), GetInputSlot2Name(), SlotType.Input, 1, SlotValueType.Dynamic, Vector4.zero);
        }

        protected virtual MaterialSlot GetOutputSlot()
        {
            return new MaterialSlot(GetOutputSlotName(), GetOutputSlotName(), SlotType.Output, 0, SlotValueType.Dynamic, Vector4.zero);
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
            var outputSlot = FindOutputSlot<MaterialSlot>(GetOutputSlotName());
            var inputSlot1 = FindInputSlot<MaterialSlot>(GetInputSlot1Name());
            var inputSlot2 = FindInputSlot<MaterialSlot>(GetInputSlot2Name());

            if (inputSlot1 == null || inputSlot2 == null || outputSlot == null)
            {
                Debug.LogError("Invalid slot configuration on node: " + name);
                return;
            }

            string input1Value = GetSlotValue(inputSlot1, generationMode);
            string input2Value = GetSlotValue(inputSlot2, generationMode);
            visitor.AddShaderChunk(precision + outputDimension + " " + GetOutputVariableNameForSlot(outputSlot) + " = " + GetFunctionCallBody(input1Value, input2Value) + ";", true);
        }

        protected virtual string GetFunctionCallBody(string input1Value, string input2Value)
        {
            return GetFunctionName() + " (" + input1Value + ", " + input2Value + ")";
        }

        public string outputDimension
        {
            get { return ConvertConcreteSlotValueTypeToString(FindOutputSlot<MaterialSlot>(GetOutputSlotName()).concreteValueType); }
        }

        public string input1Dimension
        {
            get { return ConvertConcreteSlotValueTypeToString(FindInputSlot<MaterialSlot>(GetInputSlot1Name()).concreteValueType); }
        }

        public string input2Dimension
        {
            get { return ConvertConcreteSlotValueTypeToString(FindInputSlot<MaterialSlot>(GetInputSlot2Name()).concreteValueType); }
        }
    }
}
