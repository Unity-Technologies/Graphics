using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Matrix/MultiplyMatrixByVector")]
    public class MatrixMultiplyByVectorNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction
    {
        protected const string kInputSlot1ShaderName = "Input1";
        protected const string kInputSlot2ShaderName = "Input2";
        protected const string kOutputSlotShaderName = "Output";

        public const int InputSlot1Id = 0;
        public const int InputSlot2Id = 1;
        public const int OutputSlotId = 2;

        public override bool hasPreview
        {
            get { return false; }
        }

        public MatrixMultiplyByVectorNode()
        {
            name = "MultiplyMatrixByVector";
            UpdateNodeAfterDeserialization();
        }

        protected string GetFunctionName()
        {
            return "unity_matrix_multiplybyvector_" + precision;
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
            get { return new[] { InputSlot1Id, InputSlot2Id, OutputSlotId }; }
        }

        protected MaterialSlot GetInputSlot1()
        {
            return new MaterialSlot(InputSlot1Id, GetInputSlot1Name(), kInputSlot1ShaderName, SlotType.Input, SlotValueType.Matrix4, Vector4.zero);
        }

        protected MaterialSlot GetInputSlot2()
        {
            return new MaterialSlot(InputSlot2Id, GetInputSlot2Name(), kInputSlot2ShaderName, SlotType.Input, SlotValueType.Vector4, Vector4.zero);
        }

        protected MaterialSlot GetOutputSlot()
        {
            return new MaterialSlot(OutputSlotId, GetOutputSlotName(), kOutputSlotShaderName, SlotType.Output, SlotValueType.Vector4, Vector4.zero);
        }

        protected virtual string GetInputSlot1Name()
        {
            return "Input1";
        }

        protected virtual string GetInputSlot2Name()
        {
            return "Input2";
        }

        protected string GetOutputSlotName()
        {
            return "Output";
        }

        protected string GetFunctionPrototype(string arg1Name, string arg2Name)
        {
            return "inline " + precision + outputDimension + " " + GetFunctionName() + " ("
                + precision + input1Dimension + " " + arg1Name + ", "
                + precision + input2Dimension + " " + arg2Name + ")";
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            NodeUtils.SlotConfigurationExceptionIfBadConfiguration(this, new[] { InputSlot1Id, InputSlot2Id }, new[] { OutputSlotId });
            string input1Value = GetSlotValue(InputSlot1Id, generationMode);
            string input2Value = GetSlotValue(InputSlot2Id, generationMode);
            visitor.AddShaderChunk(precision + outputDimension + " " + GetVariableNameForSlot(OutputSlotId) + " = " + GetFunctionCallBody(input1Value, input2Value) + ";", true);
        }

        protected string GetFunctionCallBody(string input1Value, string input2Value)
        {
            return GetFunctionName() + " (" + input1Value + ", " + input2Value + ")";
        }

        public string outputDimension
        {
            get { return ConvertConcreteSlotValueTypeToString(FindInputSlot<MaterialSlot>(InputSlot2Id).concreteValueType); }
        }

        private string input1Dimension
        {
            get { return ConvertConcreteSlotValueTypeToString(FindInputSlot<MaterialSlot>(InputSlot1Id).concreteValueType); }
        }

        private string input2Dimension
        {
            get { return ConvertConcreteSlotValueTypeToString(FindInputSlot<MaterialSlot>(InputSlot2Id).concreteValueType); }
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype("arg1", "arg2"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();
            outputString.AddShaderChunk("return mul(arg1, arg2);", false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
