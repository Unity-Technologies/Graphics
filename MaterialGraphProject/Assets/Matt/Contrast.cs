using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
	[Title ("Math/Color/Contrast")]
	public class Contrast : Function3Input, IGeneratesFunction
	{
		public Contrast()
		{
			name = "Contrast";
		}

		protected override string GetFunctionName ()
		{
			return "unity_contrast_" + precision;
		}

		protected override MaterialSlot GetInputSlot1 ()
		{
			return new MaterialSlot (InputSlot1Id, GetInputSlot1Name (), kInputSlot1ShaderName, SlotType.Input, SlotValueType.Vector3, Vector4.zero);
		}

        protected override MaterialSlot GetInputSlot2()
        {
            return new MaterialSlot(InputSlot2Id, GetInputSlot2Name(), kInputSlot2ShaderName, SlotType.Input, SlotValueType.Vector1, Vector4.zero);
        }

        protected override MaterialSlot GetInputSlot3()
        {
            return new MaterialSlot(InputSlot3Id, GetInputSlot3Name(), kInputSlot3ShaderName, SlotType.Input, SlotValueType.Vector1, Vector4.zero);
        }

        protected override MaterialSlot GetOutputSlot ()
		{
			return new MaterialSlot (OutputSlotId, GetOutputSlotName (), kOutputSlotShaderName, SlotType.Output, SlotValueType.Vector3, Vector4.zero);
		}

        protected override string GetInputSlot2Name()
        {
            return "Contrast";
        }

        protected override string GetInputSlot3Name()
        {
            return "Midpoint";
        }

        // Contrast (reacts better when applied in log)
        // Optimal range: [0.0, 2.0]]
        // From PostProcessing
        public void GenerateNodeFunction (ShaderGenerator visitor, GenerationMode generationMode)
		{
			var outputString = new ShaderGenerator ();
			outputString.AddShaderChunk (GetFunctionPrototype ("arg1", "arg2", "arg3"), false);
			outputString.AddShaderChunk ("{", false);
			outputString.Indent ();
            outputString.AddShaderChunk ("return (arg1 - arg3) * arg2 + arg3;", false);
            outputString.Deindent ();
			outputString.AddShaderChunk ("}", false);

            visitor.AddShaderChunk (outputString.GetShaderString (0), true);
		}
	}
}
