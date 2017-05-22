namespace UnityEngine.MaterialGraph
{
	[Title ("Math/Range/OneMinus")]
	public class OneMinusNode : Function1Input, IGeneratesFunction
	{
		public OneMinusNode ()
		{
			name = "OneMinus";
		}

		protected override string GetFunctionName ()
		{
			return "unity_oneminus_" + precision;
		}

		public void GenerateNodeFunction (ShaderGenerator visitor, GenerationMode generationMode)
		{
			var outputString = new ShaderGenerator ();
			outputString.AddShaderChunk (GetFunctionPrototype ("arg1"), false);
			outputString.AddShaderChunk ("{", false);
			outputString.Indent ();
			outputString.AddShaderChunk ("return arg1 * -1 + 1;", false);
			outputString.Deindent ();
			outputString.AddShaderChunk ("}", false);

			visitor.AddShaderChunk (outputString.GetShaderString (0), true);
		}
	}
}
