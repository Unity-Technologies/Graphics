namespace UnityEditor.MaterialGraph
{
    [Title("Math/SmoothStep Node")]
    class SmoothStepNode : Function3Input, IGeneratesFunction
    {
        public override void Init()
        {
            base.Init();
            name = "SmoothStepNode";
        }

        protected override string GetFunctionName() {return "unity_smoothstep_" + precision; }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            foreach (var precision in m_PrecisionNames)
            {
                outputString.AddShaderChunk("inline " + precision + "4 unity_smoothstep_" + precision + " (" + precision + "4 input, " + precision + "4 edge1, " + precision + "4 edge2)", false);
                outputString.AddShaderChunk("{", false);
                outputString.Indent();
                outputString.AddShaderChunk("return smoothstep(edge1, edge2, input);", false);
                outputString.Deindent();
                outputString.AddShaderChunk("}", false);
            }

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
