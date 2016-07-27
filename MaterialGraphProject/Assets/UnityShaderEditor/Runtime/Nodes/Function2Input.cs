using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    public abstract class Function2Input : AbstractMaterialNode, IGeneratesBodyCode
    {
        protected const string kInputSlot1ShaderName = "Input1";
        protected const string kInputSlot2ShaderName = "Input2";
        protected const string kOutputSlotShaderName = "Output";

        public const int InputSlot1Id = 0;
        public const int InputSlot2Id = 1;
        public const int OutputSlotId = 2;

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

        protected int[] validSlots
        {
            get { return new[] {InputSlot1Id, InputSlot2Id, OutputSlotId}; }
        }

        protected virtual MaterialSlot GetInputSlot1()
        {
            return new MaterialSlot(InputSlot1Id, GetInputSlot1Name(), kInputSlot1ShaderName, SlotType.Input, SlotValueType.Dynamic, Vector4.zero);
        }

        protected virtual MaterialSlot GetInputSlot2()
        {
            return new MaterialSlot(InputSlot2Id, GetInputSlot2Name(), kInputSlot2ShaderName, SlotType.Input, SlotValueType.Dynamic, Vector4.zero);
        }

        protected virtual MaterialSlot GetOutputSlot()
        {
            return new MaterialSlot(OutputSlotId, GetOutputSlotName(), kOutputSlotShaderName, SlotType.Output, SlotValueType.Dynamic, Vector4.zero);
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
            var outputSlot = FindOutputSlot<MaterialSlot>(OutputSlotId);
            var inputSlot1 = FindInputSlot<MaterialSlot>(InputSlot1Id);
            var inputSlot2 = FindInputSlot<MaterialSlot>(InputSlot2Id);

            if (inputSlot1 == null || inputSlot2 == null || outputSlot == null)
            {
                Debug.LogError("Invalid slot configuration on node: " + name);
                return;
            }

            string input1Value = GetSlotValue(inputSlot1, generationMode);
            string input2Value = GetSlotValue(inputSlot2, generationMode);
            visitor.AddShaderChunk(precision + outputDimension + " " + GetVariableNameForSlot(outputSlot) + " = " + GetFunctionCallBody(input1Value, input2Value) + ";", true);
        }

        protected virtual string GetFunctionCallBody(string input1Value, string input2Value)
        {
            return GetFunctionName() + " (" + input1Value + ", " + input2Value + ")";
        }

        public string outputDimension
        {
            get { return ConvertConcreteSlotValueTypeToString(FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType); }
        }

        private string input1Dimension
        {
            get { return ConvertConcreteSlotValueTypeToString(FindInputSlot<MaterialSlot>(InputSlot1Id).concreteValueType); }
        }

        private string input2Dimension
        {
            get { return ConvertConcreteSlotValueTypeToString(FindInputSlot<MaterialSlot>(InputSlot2Id).concreteValueType); }
        }
    }
}
