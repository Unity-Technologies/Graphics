namespace UnityEngine.MaterialGraph
{
    [Title("Math/Vector/DDXY")]
    public class DDXYNode : Function1Input, IGeneratesFunction
    {
        public DDXYNode()
        {
            name = "DDXY";
        }

        protected override string GetFunctionName()
        {
            return "unity_ddxy_" + precision;
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype("arg1"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();
            outputString.AddShaderChunk("return abs(ddx(arg1) + ddy(arg1));", false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}


