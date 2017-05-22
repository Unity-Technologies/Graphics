using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
	[Title ("Math/Color/RGBtoHCV")]
	public class RGBtoHCVNode : Function1Input, IGeneratesFunction
	{
		public RGBtoHCVNode ()
		{
			name = "RGBtoHCV";
		}

		protected override string GetFunctionName ()
		{
			return "unity_rgbtohcv_" + precision;
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
			outputString.AddShaderChunk (precision + "4 P = (arg1.g < arg1.b)?" + precision + "4(arg1.bg, -1.0, 2.0/3.0):" + precision + "4(arg1.gb, 0.0, -1.0/3.0);", false);
			outputString.AddShaderChunk (precision + "4 Q = (arg1.r < P.x)?" + precision + "4(P.xyw, arg1.r):" + precision + "4(arg1.r, P.yzx);", false);
			outputString.AddShaderChunk (precision + " C = Q.x - min(Q.w, Q.y);", false);
			outputString.AddShaderChunk (precision + " H = abs((Q.w - Q.y)/(6 * C + 1e-10)+Q.z);", false);
			outputString.AddShaderChunk ("return " + precision + "3(H,C,Q.x);", false);
			outputString.Deindent ();
			outputString.AddShaderChunk ("}", false);

			visitor.AddShaderChunk (outputString.GetShaderString (0), true);
		}
	}
}
