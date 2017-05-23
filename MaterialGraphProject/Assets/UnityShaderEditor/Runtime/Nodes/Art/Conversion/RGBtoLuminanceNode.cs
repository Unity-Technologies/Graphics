using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
	[Title ("Art/Conversion/RGBtoLuminance")]
	public class RGBtoLuminanceNode : Function1Input, IGeneratesFunction
	{
		public RGBtoLuminanceNode()
		{
			name = "RGBtoLuminance";
		}

		protected override string GetFunctionName ()
		{
			return "unity_rgbtoluminance_" + precision;
		}

		protected override MaterialSlot GetInputSlot ()
		{
			return new MaterialSlot (InputSlotId, GetInputSlotName (), kInputSlotShaderName, SlotType.Input, SlotValueType.Vector3, Vector4.zero);
		}

		protected override MaterialSlot GetOutputSlot ()
		{
			return new MaterialSlot (OutputSlotId, GetOutputSlotName (), kOutputSlotShaderName, SlotType.Output, SlotValueType.Vector1, Vector4.zero);
		}

        // Convert rgb to luminance with rgb in linear space with sRGB primaries and D65 white point (from PostProcessing)
        public void GenerateNodeFunction (ShaderGenerator visitor, GenerationMode generationMode)
		{
			var outputString = new ShaderGenerator ();
			outputString.AddShaderChunk (GetFunctionPrototype ("arg1"), false);
			outputString.AddShaderChunk ("{", false);
			outputString.Indent ();
			outputString.AddShaderChunk ("return dot(arg1, "+precision+outputDimension+"(0.2126729, 0.7151522, 0.0721750));", false);
            outputString.Deindent ();
			outputString.AddShaderChunk ("}", false);

			visitor.AddShaderChunk (outputString.GetShaderString (0), true);
		}
	}
}
