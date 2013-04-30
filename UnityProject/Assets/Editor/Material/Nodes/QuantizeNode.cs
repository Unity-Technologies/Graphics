namespace UnityEditor.Graphs.Material
{
	[Title("Math/Quantize Node")]
	class QuantizeNode : Function2Input, IGeneratesFunction
	{
		public override void Init()
		{
			name = "QuantizeNode";
			base.Init();
		}

		protected override string GetFunctionName () {return "unity_quantize_"+precision;}

		public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
		{
			var outputString = new ShaderGenerator ();
			foreach (var precision in m_PrecisionNames)
			{
				outputString.AddShaderChunk("inline " + precision + "4 unity_quantize_" + precision + " (" + precision + "4 input, " + precision + "4 stepsize)", false);
				outputString.AddShaderChunk("{", false);
				outputString.Indent();
				outputString.AddShaderChunk("return floor(input / stepsize) * stepsize;", false);
				outputString.Deindent();
				outputString.AddShaderChunk("}", false);
			}

			visitor.AddShaderChunk (outputString.GetShaderString (0), true);
		}
	}
}
