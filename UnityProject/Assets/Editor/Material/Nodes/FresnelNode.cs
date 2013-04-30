namespace UnityEditor.Graphs.Material
{
	[Title("Math/Fresnel Node")]
	class FresnelNode : Function2Input, IGeneratesFunction
	{
		public override void Init()
		{
			name = "FresnelNode";
			base.Init();
		}

		protected override string GetFunctionName() { return "unity_fresnel_" + precision; }

		public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
		{
			var outputString = new ShaderGenerator();
			
			foreach (var precision in m_PrecisionNames)
			{
				outputString.AddShaderChunk("inline " + precision + "4 unity_fresnel_" + precision + " (" + precision + "4 arg1, " + precision + "4 arg2)", false);
				outputString.AddShaderChunk("{", false);
				outputString.Indent();
				outputString.AddShaderChunk("return (1.0 - dot (normalize (arg1.xyz), normalize (arg2.xyz))).xxxx;", false);
				outputString.Deindent();
				outputString.AddShaderChunk("}", false);
			}

			visitor.AddShaderChunk(outputString.GetShaderString(0), true);
		}
	}
}
