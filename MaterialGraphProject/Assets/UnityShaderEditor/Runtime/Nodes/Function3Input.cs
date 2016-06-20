using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    public abstract class Function3Input : AbstractMaterialNode, IGeneratesBodyCode
    {
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
            AddSlot(GetOutputSlot());
            RemoveSlotsNameNotMatching(validSlots);
        }

        protected string[] validSlots
        {
            get { return new[] {GetInputSlot1Name(), GetInputSlot2Name(), GetInputSlot3Name(), GetOutputSlotName()}; }
        }

        protected MaterialSlot GetInputSlot1()
        {
            return new MaterialSlot(GetInputSlot1Name(), GetInputSlot1Name(), SlotType.Input, 0, SlotValueType.Dynamic, Vector4.zero);
        }

        protected MaterialSlot GetInputSlot2()
        {
            return new MaterialSlot(GetInputSlot2Name(), GetInputSlot2Name(), SlotType.Input, 1, SlotValueType.Dynamic, Vector4.zero);
        }

        protected MaterialSlot GetInputSlot3()
        {
            return new MaterialSlot(GetInputSlot3Name(), GetInputSlot3Name(), SlotType.Input, 2, SlotValueType.Dynamic, Vector4.zero);
        }

        protected MaterialSlot GetOutputSlot()
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
            var outputSlot = FindMaterialOutputSlot(GetOutputSlotName());
            var inputSlot1 = FindMaterialInputSlot(GetInputSlot1Name());
            var inputSlot2 = FindMaterialInputSlot(GetInputSlot2Name());
            var inputSlot3 = FindMaterialInputSlot(GetInputSlot3Name());

            if (inputSlot1 == null || inputSlot2 == null || inputSlot3 == null || outputSlot == null)
            {
                Debug.LogError("Invalid slot configuration on node: " + name);
                return;
            }

            string input1Value = GetSlotValue(inputSlot1, generationMode);
            string input2Value = GetSlotValue(inputSlot2, generationMode);
            string input3Value = GetSlotValue(inputSlot3, generationMode);

            visitor.AddShaderChunk(precision + outputDimension + " " + GetOutputVariableNameForSlot(outputSlot) + " = " + GetFunctionCallBody(input1Value, input2Value, input3Value) + ";", true);
        }

        protected virtual string GetFunctionCallBody(string inputValue1, string inputValue2, string inputValue3)
        {
            return GetFunctionName() + " (" + inputValue1 + ", " + inputValue2 + ", " + inputValue3 + ")";
        }

                protected virtual string GetFunctionCallBody(string inputValue)
        {
            return GetFunctionName() + " (" + inputValue + ")";
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

        public string input3Dimension
        {
            get { return ConvertConcreteSlotValueTypeToString(FindMaterialInputSlot(GetInputSlot3Name()).concreteValueType); }
        }
    }
}
