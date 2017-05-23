namespace UnityEngine.MaterialGraph
{
    [Title("Vector/Projection Node")]
    public class VectorProjectionNode : Function2Input, IGeneratesFunction
    {
        public VectorProjectionNode()
        {
            name = "VectorProjection";
        }

        protected override string GetFunctionName()
        {
            return "unity_vector_projection_" + precision;
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype("arg1", "arg2"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();
            outputString.AddShaderChunk("return arg2 * dot(arg1, arg2) / dot(arg2, arg2);", false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
