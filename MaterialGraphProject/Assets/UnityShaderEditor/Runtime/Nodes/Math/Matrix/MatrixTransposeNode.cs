using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Matrix/TransposeMatrix")]
    public class MatrixTransposeNode : Function1Input, IGeneratesFunction
    {
        public MatrixTransposeNode()
        {
            name = "TransposeMatrix";
        }

        protected override string GetFunctionName()
        {
            return "unity_matrix_transpose_" + precision;
        }

        public override bool hasPreview
        {
            get { return false; }
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype("arg1"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();
            outputString.AddShaderChunk("return transpose(arg1);", false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }

        protected override MaterialSlot GetInputSlot()
        {
            return new MaterialSlot(InputSlotId, GetInputSlotName(), kInputSlotShaderName, SlotType.Input, SlotValueType.Matrix4, Vector4.zero);
        }

        protected override MaterialSlot GetOutputSlot()
        {
            return new MaterialSlot(OutputSlotId, GetOutputSlotName(), kOutputSlotShaderName, SlotType.Output, SlotValueType.Matrix4, Vector4.zero);
        }
    }
}
