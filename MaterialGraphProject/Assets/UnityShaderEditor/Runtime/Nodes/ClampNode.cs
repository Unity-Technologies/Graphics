using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Clamp Node")]
    public class ClampNode : Function3Input, IGeneratesFunction
    {
        public ClampNode()
        {
            name = "ClampNode";
        }

        protected override string GetFunctionName() {return "unity_clamp_" + precision; }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk("inline " + precision + "4 unity_clamp_" + precision + " (" + precision + "4 arg1, " + precision + "4 minval, " + precision + "4 maxval)", false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();
            outputString.AddShaderChunk("return clamp(arg1, minval, maxval);", false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);
            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }

    }
}
