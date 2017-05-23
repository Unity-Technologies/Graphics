namespace UnityEngine.MaterialGraph
{
    [Title("Matrix/Multiply Node")]
    public class MatrixMultiplyNode : Function2Input, IGeneratesFunction
    {
        public MatrixMultiplyNode()
        {
            name = "MatrixMultiplyNode";
        }

        protected override string GetFunctionName()
        {
            return "unity_matrix_multiply_" + precision;
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
