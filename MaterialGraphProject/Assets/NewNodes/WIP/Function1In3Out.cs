using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    //Custom version of FunctionXInput
    public abstract class Function1In3Out : AbstractMaterialNode, IGeneratesBodyCode
    {
        protected const string kInputSlotShaderName = "Input";
        protected const string kOutputSlot0ShaderName = "Output0";
        protected const string kOutputSlot1ShaderName = "Output1";
        protected const string kOutputSlot2ShaderName = "Output2";
        protected const string kOutputSlot3ShaderName = "Output3";

        public const int InputSlotId = 0;
        public const int OutputSlot0Id = 1;
        public const int OutputSlot1Id = 2;
        public const int OutputSlot2Id = 3;
        public const int OutputSlot3Id = 4;

        public override bool hasPreview
        {
            get { return true; }
        }

        protected Function1In3Out()
        {
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(GetInputSlot());
            AddSlot(GetOutputSlot0());
            AddSlot(GetOutputSlot1());
            AddSlot(GetOutputSlot2());
            AddSlot(GetOutputSlot3());
            RemoveSlotsNameNotMatching(validSlots);
        }

        protected int[] validSlots
        {
            get { return new[] { InputSlotId, OutputSlot0Id, OutputSlot1Id, OutputSlot2Id, OutputSlot3Id }; }
            //get { return new[] { InputSlotId, OutputSlot1Id, OutputSlot2Id, OutputSlot3Id }; }
        }

        protected virtual MaterialSlot GetInputSlot()
        {
            return new MaterialSlot(InputSlotId, GetInputSlotName(), kInputSlotShaderName, SlotType.Input, SlotValueType.Dynamic, Vector4.zero);
        }

        protected virtual MaterialSlot GetOutputSlot0()
        {
            return new MaterialSlot(OutputSlot0Id, GetOutputSlot0Name(), kOutputSlot0ShaderName, SlotType.Output, SlotValueType.Dynamic, Vector4.zero);
        }

        protected virtual MaterialSlot GetOutputSlot1()
        {
            return new MaterialSlot(OutputSlot1Id, GetOutputSlot1Name(), kOutputSlot1ShaderName, SlotType.Output, SlotValueType.Dynamic, Vector4.zero);
        }

        protected virtual MaterialSlot GetOutputSlot2()
        {
            return new MaterialSlot(OutputSlot2Id, GetOutputSlot2Name(), kOutputSlot2ShaderName, SlotType.Output, SlotValueType.Dynamic, Vector4.zero);
        }

        protected virtual MaterialSlot GetOutputSlot3()
        {
            return new MaterialSlot(OutputSlot3Id, GetOutputSlot3Name(), kOutputSlot3ShaderName, SlotType.Output, SlotValueType.Dynamic, Vector4.zero);
        }

        protected virtual string GetInputSlotName()
        {
            return "Input";
        }

        protected virtual string GetOutputSlot0Name()
        {
            return "Output0";
        }

        protected virtual string GetOutputSlot1Name()
        {
            return "Output1";
        }

        protected virtual string GetOutputSlot2Name()
        {
            return "Output2";
        }

        protected virtual string GetOutputSlot3Name()
        {
            return "Output3";
        }

        protected abstract string GetFunctionName();

        //Need more args?
        protected virtual string GetFunctionPrototype(string arg1Name)
        {
            return "inline " + precision + output1Dimension + " " + GetFunctionName() + " ("
                   + precision + inputDimension + " " + arg1Name + ") ";
                   //+ "out " + precision + output2Dimension + " " + arg2Name + ", "
                  // + "out " + precision + output3Dimension + " " + arg3Name + ")";
        }

        public virtual void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            NodeUtils.SlotConfigurationExceptionIfBadConfiguration(this, new[] { InputSlotId }, new[] { OutputSlot0Id, OutputSlot1Id, OutputSlot2Id, OutputSlot3Id });
            //NodeUtils.SlotConfigurationExceptionIfBadConfiguration(this, new[] { InputSlotId }, new[] { OutputSlot1Id, OutputSlot2Id, OutputSlot3Id });
            string inputValue = GetSlotValue(InputSlotId, generationMode);
            visitor.AddShaderChunk(precision + outputDimension + " " + GetVariableNameForSlot(OutputSlot0Id) + " = " + GetFunctionCallBody(inputValue) + ";", true);
            //visitor.AddShaderChunk(precision + output1Dimension + " " + GetVariableNameForSlot(OutputSlot1Id) + " = " + GetFunctionCallBody(inputValue) + ";", true);
        }

        protected virtual string GetFunctionCallBody(string inputValue)
        {
            return GetFunctionName() + " (" + inputValue + ")";
        }

        public string outputDimension
        {
            get { return ConvertConcreteSlotValueTypeToString(FindOutputSlot<MaterialSlot>(OutputSlot0Id).concreteValueType); }
        }

        public string output1Dimension
        {
            get { return ConvertConcreteSlotValueTypeToString(FindOutputSlot<MaterialSlot>(OutputSlot1Id).concreteValueType); }
        }

        public string output2Dimension
        {
            get { return ConvertConcreteSlotValueTypeToString(FindOutputSlot<MaterialSlot>(OutputSlot2Id).concreteValueType); }
        }

        public string output3Dimension
        {
            get { return ConvertConcreteSlotValueTypeToString(FindOutputSlot<MaterialSlot>(OutputSlot3Id).concreteValueType); }
        }

        private string inputDimension
        {
            get { return ConvertConcreteSlotValueTypeToString(FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType); }
        }
    }
}
