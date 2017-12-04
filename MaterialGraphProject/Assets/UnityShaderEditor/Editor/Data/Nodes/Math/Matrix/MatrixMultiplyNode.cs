using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Matrix", "MultiplyMatrix")]
    public class MatrixMultiplyNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction
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

        public MatrixMultiplyNode()
        {
            name = "MultiplyMatrix";
            UpdateNodeAfterDeserialization();
        }

        protected string GetFunctionName()
        {
            return "unity_matrix_multiply_" + precision;
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
            return new Matrix4MaterialSlot(InputSlot1Id, GetInputSlot1Name(), kInputSlot1ShaderName, SlotType.Input);
        }

        protected MaterialSlot GetInputSlot2()
        {
            return new Matrix4MaterialSlot(InputSlot2Id, GetInputSlot2Name(), kInputSlot2ShaderName, SlotType.Input);
        }

        protected MaterialSlot GetOutputSlot()
        {
            return new Matrix4MaterialSlot(OutputSlotId, GetOutputSlotName(), kOutputSlotShaderName, SlotType.Output);
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
            return string.Format("inline {0} {1} ({2} {3}, {4} {5})", ConvertConcreteSlotValueTypeToString(precision, FindInputSlot<MaterialSlot>(InputSlot2Id).concreteValueType), GetFunctionName(), ConvertConcreteSlotValueTypeToString(precision, FindInputSlot<MaterialSlot>(InputSlot1Id).concreteValueType), arg1Name, ConvertConcreteSlotValueTypeToString(precision, FindInputSlot<MaterialSlot>(InputSlot2Id).concreteValueType), arg2Name);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            NodeUtils.SlotConfigurationExceptionIfBadConfiguration(this, new[] { InputSlot1Id, InputSlot2Id }, new[] { OutputSlotId });
            string input1Value = GetSlotValue(InputSlot1Id, generationMode);
            string input2Value = GetSlotValue(InputSlot2Id, generationMode);
            visitor.AddShaderChunk(string.Format("{0} {1} = {2};", ConvertConcreteSlotValueTypeToString(precision, FindInputSlot<MaterialSlot>(InputSlot2Id).concreteValueType), GetVariableNameForSlot(OutputSlotId), GetFunctionCallBody(input1Value, input2Value)), true);
        }

        protected string GetFunctionCallBody(string input1Value, string input2Value)
        {
            return GetFunctionName() + " (" + input1Value + ", " + input2Value + ")";
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
