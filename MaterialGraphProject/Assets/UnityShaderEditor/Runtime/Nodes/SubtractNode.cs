using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Subtract Node")]
    class SubtractNode : Function2Input, IGeneratesFunction
    {
        public SubtractNode()
        {
            name = "SubtractNode";
        }

        protected override string GetFunctionName() {return "unity_subtract_" + precision; }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();

            outputString.AddShaderChunk(GetFunctionPrototype("arg1", "arg2"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();
            outputString.AddShaderChunk("return arg1 - arg2;", false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);


            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
