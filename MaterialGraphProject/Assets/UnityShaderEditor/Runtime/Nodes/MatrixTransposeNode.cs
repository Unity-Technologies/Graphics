namespace UnityEngine.MaterialGraph
{
    [Title("Matrix/Transpose Node")]
    public class MatrixTransposeNode : Function1Input, IGeneratesFunction
    {
        public MatrixTransposeNode()
        {
            name = "MatrixTranspose";
        }

        protected override string GetFunctionName()
        {
            return "unity_matrix_transpose_" + precision;
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
    }
}
