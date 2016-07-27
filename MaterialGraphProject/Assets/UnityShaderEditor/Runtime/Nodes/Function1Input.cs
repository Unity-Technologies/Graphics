using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    public abstract class Function1Input : AbstractMaterialNode, IGeneratesBodyCode
    {
        protected const string kInputSlotShaderName = "Input";
        protected const string kOutputSlotShaderName = "Output";

        public const int InputSlotId = 0;
        public const int OutputSlotId = 1;

        public override bool hasPreview
        {
            get { return true; }
        }

        protected Function1Input()
        {
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(GetInputSlot());
            AddSlot(GetOutputSlot());
            RemoveSlotsNameNotMatching(validSlots);
        }
   
        protected int[] validSlots
        {
            get { return new[] { InputSlotId, OutputSlotId }; }
        }

        protected virtual MaterialSlot GetInputSlot()
        {
            return new MaterialSlot(InputSlotId, GetInputSlotName(), kInputSlotShaderName, SlotType.Input, SlotValueType.Dynamic, Vector4.zero);
        }

        protected virtual MaterialSlot GetOutputSlot()
        {
            return new MaterialSlot(OutputSlotId, GetOutputSlotName(), kOutputSlotShaderName, SlotType.Output, SlotValueType.Dynamic, Vector4.zero);
        }

        protected virtual string GetInputSlotName() {return "Input"; }
        protected virtual string GetOutputSlotName() {return "Output"; }

        protected abstract string GetFunctionName();
        
        protected virtual string GetFunctionPrototype(string argName)
        {
            return "inline " + precision + outputDimension + " " + GetFunctionName() + " ("
                + precision + inputDimension + " " + argName + ")";
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputSlot = FindOutputSlot<MaterialSlot>(OutputSlotId);
            var inputSlot = FindInputSlot<MaterialSlot>(InputSlotId);

            if (inputSlot == null || outputSlot == null)
            {
                Debug.LogError("Invalid slot configuration on node: " + name);
                return;
            }

            var inputValue = GetSlotValue(inputSlot, generationMode);
            visitor.AddShaderChunk(precision + outputDimension + " " + GetVariableNameForSlot(outputSlot) + " = " + GetFunctionCallBody(inputValue) + ";", true);
        }

        protected virtual string GetFunctionCallBody(string inputValue)
        {
            return GetFunctionName() + " (" + inputValue + ")";
        }

        public string outputDimension
        {
            get { return ConvertConcreteSlotValueTypeToString(FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType); }
        }
        public string inputDimension
        {
            get { return ConvertConcreteSlotValueTypeToString(FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType); }
        }
    }
}
