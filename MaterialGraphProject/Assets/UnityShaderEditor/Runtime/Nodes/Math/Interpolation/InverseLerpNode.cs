namespace UnityEngine.MaterialGraph
{
	[Title ("Math/Interpolation/InverseLerp")]
	public class InverseLerpNode : Function3Input, IGeneratesFunction
	{
		public InverseLerpNode ()
		{
			name = "InverseLerp";
		}

		protected override string GetFunctionName ()
		{
			return "unity_inverselerp_" + precision;
		}

		protected override string GetInputSlot1Name()
		{
			return "InputA";
		}

		protected override string GetInputSlot2Name()
		{
			return "InputB";
		}

		protected override string GetInputSlot3Name()
		{
			return "T";
		}

		public void GenerateNodeFunction (ShaderGenerator visitor, GenerationMode generationMode)
		{
			var outputString = new ShaderGenerator ();
			outputString.AddShaderChunk (GetFunctionPrototype ("arg1", "arg2", "arg3"), false);
			outputString.AddShaderChunk ("{", false);
			outputString.Indent ();
			outputString.AddShaderChunk ("return (arg3 - arg1)/(arg2 - arg1);", false);
			outputString.Deindent ();
			outputString.AddShaderChunk ("}", false);

			visitor.AddShaderChunk (outputString.GetShaderString (0), true);
		}
	}
}
