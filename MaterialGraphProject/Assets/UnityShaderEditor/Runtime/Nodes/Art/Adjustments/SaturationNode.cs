using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
	[Title ("Art/Adjustments/Saturation")]
	public class SaturationNode : Function2Input, IGeneratesFunction
	{
		public SaturationNode()
		{
			name = "Saturation";
		}

		protected override string GetFunctionName ()
		{
			return "unity_saturation_" + precision;
		}

		protected override MaterialSlot GetInputSlot1 ()
		{
			return new MaterialSlot (InputSlot1Id, GetInputSlot1Name (), kInputSlot1ShaderName, SlotType.Input, SlotValueType.Vector3, Vector4.zero);
		}

        protected override MaterialSlot GetInputSlot2()
        {
            return new MaterialSlot(InputSlot2Id, GetInputSlot2Name(), kInputSlot2ShaderName, SlotType.Input, SlotValueType.Vector1, Vector4.zero);
        }

        protected override MaterialSlot GetOutputSlot ()
		{
			return new MaterialSlot (OutputSlotId, GetOutputSlotName (), kOutputSlotShaderName, SlotType.Output, SlotValueType.Vector3, Vector4.zero);
		}

        protected override string GetInputSlot2Name()
        {
            return "Saturation";
        }

        // RGB Saturation (closer to a vibrance effect than actual saturation)
        // Recommended workspace: ACEScg (linear)
        // Optimal range: [0.0, 2.0]
        // From PostProcessing
        public void GenerateNodeFunction (ShaderGenerator visitor, GenerationMode generationMode)
		{
			var outputString = new ShaderGenerator ();
			outputString.AddShaderChunk (GetFunctionPrototype ("arg1", "arg2"), false);
			outputString.AddShaderChunk ("{", false);
			outputString.Indent ();
            outputString.AddShaderChunk(precision+" luma = dot(arg1, " + precision + outputDimension + "(0.2126729, 0.7151522, 0.0721750));", false);
            outputString.AddShaderChunk ("return luma.xxx + arg2.xxx * (arg1 - luma.xxx);", false);
            outputString.Deindent ();
			outputString.AddShaderChunk ("}", false);

            visitor.AddShaderChunk (outputString.GetShaderString (0), true);
		}
	}
}
