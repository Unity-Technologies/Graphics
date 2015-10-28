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
                outputString.AddShaderChunk("inline " + precision + "4 unity_lerp_" + precision + " (" + precision + "4 first, " + precision + "4 second, " + precision + "4 s)", false);
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
