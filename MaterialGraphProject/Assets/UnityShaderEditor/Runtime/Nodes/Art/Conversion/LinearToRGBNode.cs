using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
	[Title ("Art/Conversion/LineartoRGB")]
	public class LineartoRGBNode : Function1Input, IGeneratesFunction
	{
		public LineartoRGBNode()
		{
			name = "LineartoRGB";
		}

		protected override string GetFunctionName ()
		{
			return "unity_lineartorgb_" + precision;
		}

		protected override MaterialSlot GetInputSlot ()
		{
			return new MaterialSlot (InputSlotId, GetInputSlotName (), kInputSlotShaderName, SlotType.Input, SlotValueType.Vector3, Vector4.zero);
		}

		protected override MaterialSlot GetOutputSlot ()
		{
			return new MaterialSlot (OutputSlotId, GetOutputSlotName (), kOutputSlotShaderName, SlotType.Output, SlotValueType.Vector3, Vector4.zero);
		}

		public void GenerateNodeFunction (ShaderGenerator visitor, GenerationMode generationMode)
		{
			var outputString = new ShaderGenerator ();
			outputString.AddShaderChunk (GetFunctionPrototype ("arg1"), false);
			outputString.AddShaderChunk ("{", false);
			outputString.Indent ();
            outputString.AddShaderChunk (precision + "3 sRGBLo = arg1 * 12.92;", false);
            outputString.AddShaderChunk (precision + "3 sRGBHi = (pow(max(abs(arg1), 1.192092896e-07), "+precision+ "3(1.0 / 2.4, 1.0 / 2.4, 1.0 / 2.4)) * 1.055) - 0.055;", false);
            outputString.AddShaderChunk ("return " + precision + "3(arg1 <= 0.0031308) ? sRGBLo : sRGBHi;", false);
			outputString.Deindent ();
			outputString.AddShaderChunk ("}", false);

            visitor.AddShaderChunk (outputString.GetShaderString (0), true);
		}
	}
}
