using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
	[Title ("Art/Conversion/HSVtoRGB")]
	public class HSVtoRGBNode : Function1Input, IGeneratesFunction
	{
		public HSVtoRGBNode ()
		{
			name = "HSVtoRGB";
		}

		protected override string GetFunctionName ()
		{
			return "unity_hsvtorgb_" + precision;
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
		//Reference code from:https://github.com/Unity-Technologies/PostProcessing/blob/master/PostProcessing/Resources/Shaders/ColorGrading.cginc#L175
		public void GenerateNodeFunction (ShaderGenerator visitor, GenerationMode generationMode)
		{
			var outputString = new ShaderGenerator ();
			outputString.AddShaderChunk (GetFunctionPrototype ("arg1"), false);
			outputString.AddShaderChunk ("{", false);
			outputString.Indent ();
			outputString.AddShaderChunk (precision + "4 K = " + precision + "4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);", false);
			outputString.AddShaderChunk (precision + "3 P = abs(frac(arg1.xxx + K.xyz) * 6.0 - K.www);", false);
			outputString.AddShaderChunk ("return arg1.z * lerp(K.xxx, saturate(P - K.xxx), arg1.y);", false);
			outputString.Deindent ();
			outputString.AddShaderChunk ("}", false);

			visitor.AddShaderChunk (outputString.GetShaderString (0), true);
		}
	}
}
