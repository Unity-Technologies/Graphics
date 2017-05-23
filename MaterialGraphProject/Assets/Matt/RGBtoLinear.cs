using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
	[Title ("Math/Color/RGBtoLinear")]
	public class RGBtoLinearNode : Function1Input, IGeneratesFunction
	{
		public RGBtoLinearNode()
		{
			name = "RGBtoLinear";
		}

		protected override string GetFunctionName ()
		{
			return "unity_rgbtolinear_" + precision;
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
            outputString.AddShaderChunk (precision + "3 linearRGBLo = arg1 / 12.92;", false);
            outputString.AddShaderChunk (precision + "3 linearRGBHi = pow(max(abs((arg1 + 0.055) / 1.055), 1.192092896e-07), "+precision+"3(2.4, 2.4, 2.4));", false);
            outputString.AddShaderChunk ("return " + precision + "3(arg1 <= 0.04045) ? linearRGBLo : linearRGBHi;", false);
			outputString.Deindent ();
			outputString.AddShaderChunk ("}", false);

            visitor.AddShaderChunk (outputString.GetShaderString (0), true);
		}
	}
}
