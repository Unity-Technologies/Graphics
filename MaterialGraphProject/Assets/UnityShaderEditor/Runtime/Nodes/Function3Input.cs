using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    public abstract class Function3Input : AbstractMaterialNode, IGeneratesBodyCode
    {
        private const string kInputSlot1ShaderName = "Input1";
        private const string kInputSlot2ShaderName = "Input2";
        private const string kInputSlot3ShaderName = "Input3";
        private const string kOutputSlotShaderName = "Output";

        public const int InputSlot1Id = 0;
        public const int InputSlot2Id = 1;
        public const int InputSlot3Id = 2;
        public const int OutputSlotId = 3;

        public override bool hasPreview
        {
            get { return true; }
        }

        protected Function3Input()
        {
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(GetInputSlot1());
            AddSlot(GetInputSlot2());
            AddSlot(GetInputSlot3());
            AddSlot(GetOutputSlot());
            RemoveSlotsNameNotMatching(validSlots);
        }

        protected int[] validSlots
        {
            get { return new[] {InputSlot1Id, InputSlot2Id, InputSlot3Id, OutputSlotId}; }
        }

        protected virtual MaterialSlot GetInputSlot1()
        {
            return new MaterialSlot(InputSlot1Id, GetInputSlot1Name(), kInputSlot1ShaderName, SlotType.Input, SlotValueType.Dynamic, Vector4.zero);
        }

        protected virtual MaterialSlot GetInputSlot2()
        {
            return new MaterialSlot(InputSlot2Id, GetInputSlot2Name(), kInputSlot2ShaderName, SlotType.Input, SlotValueType.Dynamic, Vector4.zero);
        }

        protected virtual MaterialSlot GetInputSlot3()
        {
            return new MaterialSlot(InputSlot3Id, GetInputSlot3Name(), kInputSlot3ShaderName, SlotType.Input, SlotValueType.Dynamic, Vector4.zero);
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

        protected virtual string GetInputSlot3Name()
        {
            return "Input3";
        }

        protected virtual string GetOutputSlotName()
        {
            return "Output";
        }

        protected abstract string GetFunctionName();

        protected virtual string GetFunctionPrototype(string arg1Name, string arg2Name, string arg3Name)
        {
            return "inline " + precision + outputDimension + " " + GetFunctionName() + " (" 
                + precision + input1Dimension + " " + arg1Name + ", " 
                + precision + input2Dimension + " " + arg2Name + ", "
                + precision + input3Dimension + " " + arg3Name + ")";
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputSlot = FindOutputSlot<MaterialSlot>(OutputSlotId);
            var inputSlot1 = FindInputSlot<MaterialSlot>(InputSlot1Id);
            var inputSlot2 = FindInputSlot<MaterialSlot>(InputSlot2Id);
            var inputSlot3 = FindInputSlot<MaterialSlot>(InputSlot3Id);

            if (inputSlot1 == null || inputSlot2 == null || inputSlot3 == null || outputSlot == null)
            {
                Debug.LogError("Invalid slot configuration on node: " + name);
                return;
            }

            string input1Value = GetSlotValue(inputSlot1, generationMode);
            string input2Value = GetSlotValue(inputSlot2, generationMode);
            string input3Value = GetSlotValue(inputSlot3, generationMode);

            visitor.AddShaderChunk(precision + outputDimension + " " + GetVariableNameForSlot(outputSlot) + " = " + GetFunctionCallBody(input1Value, input2Value, input3Value) + ";", true);
        }

        protected virtual string GetFunctionCallBody(string inputValue1, string inputValue2, string inputValue3)
        {
            return GetFunctionName() + " (" + inputValue1 + ", " + inputValue2 + ", " + inputValue3 + ")";
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

        public string input3Dimension
        {
            get { return ConvertConcreteSlotValueTypeToString(FindInputSlot<MaterialSlot>(InputSlot3Id).concreteValueType); }
        }
    }
}
