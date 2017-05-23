namespace UnityEngine.MaterialGraph
{
    [Title("Math/Basic/Divide")]
    public class DivNode : Function2Input, IGeneratesFunction
    {
        public DivNode()
        {
            name = "Divide";
        }

        protected override string GetFunctionName() {return "unity_div_" + precision; }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype("arg1", "arg2"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();
            outputString.AddShaderChunk("return arg1 / arg2;", false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
