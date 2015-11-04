using UnityEditor.Graphs;

namespace UnityEditor.MaterialGraph
{
    [Title("Math/Lerp Node")]
    class LerpNode : Function3Input, IGeneratesFunction
    {
        public override void OnCreate()
        {
            name = "LerpNode";
            base.OnCreate();
        }

        protected override string GetFunctionName() {return "unity_lerp_" + precision; }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            foreach (var precision in m_PrecisionNames)
            {
                outputString.AddShaderChunk(GetFunctionPrototype("first", "second", "s"), false);
                outputString.AddShaderChunk("{", false);
                outputString.Indent();
                outputString.AddShaderChunk("return lerp(first, second, s);", false);
                outputString.Deindent();
                outputString.AddShaderChunk("}", false);
            }

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
