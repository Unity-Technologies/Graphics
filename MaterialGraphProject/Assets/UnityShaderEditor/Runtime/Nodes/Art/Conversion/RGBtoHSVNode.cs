using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
	[Title ("Art/Conversion/RGBtoHSV")]
	public class RGBtoHSVNode : Function1Input, IGeneratesFunction
	{
		public RGBtoHSVNode ()
		{
			name = "RGBtoHSV";
		}

		protected override string GetFunctionName ()
		{
			return "unity_rgbtohsv_" + precision;
		}

		protected override MaterialSlot GetInputSlot ()
		{
			return new MaterialSlot (InputSlotId, GetInputSlotName (), kInputSlotShaderName, SlotType.Input, SlotValueType.Vector3, Vector4.zero);
		}

		protected override MaterialSlot GetOutputSlot ()
		{
			return new MaterialSlot (OutputSlotId, GetOutputSlotName (), kOutputSlotShaderName, SlotType.Output, SlotValueType.Vector3, Vector4.zero);
		}

		//TODO:Externalize
		//Reference code from:http://www.chilliant.com/rgb2hsv.html
		public void GenerateNodeFunction (ShaderGenerator visitor, GenerationMode generationMode)
		{
			var outputString = new ShaderGenerator ();
			outputString.AddShaderChunk (GetFunctionPrototype ("arg1"), false);
			outputString.AddShaderChunk ("{", false);
			outputString.Indent ();
			outputString.AddShaderChunk (precision + "4 K = " + precision + "4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);", false);
			outputString.AddShaderChunk (precision + "4 P = lerp(" + precision + "4(arg1.bg, K.wz), " + precision + "4(arg1.gb, K.xy), step(arg1.b, arg1.g));", false);
			outputString.AddShaderChunk (precision + "4 Q = lerp(" + precision + "4(P.xyw, arg1.r), " + precision + "4(arg1.r, P.yzx), step(P.x, arg1.r));", false);
			outputString.AddShaderChunk (precision + " D = Q.x - min(Q.w, Q.y);", false);
			outputString.AddShaderChunk (precision + " E = 1e-10;", false);
			outputString.AddShaderChunk ("return " + precision + "3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), Q.x);", false);
			outputString.Deindent ();
			outputString.AddShaderChunk ("}", false);

			visitor.AddShaderChunk (outputString.GetShaderString (0), true);
		}
	}
}
