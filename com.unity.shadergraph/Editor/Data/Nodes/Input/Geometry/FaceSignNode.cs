using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
	[Title("Input", "Geometry", "Face Sign")]
	public class FaceSignNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireFaceSign
	{
		public FaceSignNode()
		{
			name = "Face Sign";
			UpdateNodeAfterDeserialization();
		}

		public override string documentationURL
		{
			get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Face-Sign-Node"; }
		}

		public override bool hasPreview { get { return false; } }

		public const int OutputSlotId = 0;
        private const string kOutputSlotName = "Is Front";

		public override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new BooleanMaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, true, ShaderStageCapability.Fragment));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            visitor.AddShaderChunk(string.Format("{0} {1} = IN.{2};", precision, GetVariableNameForSlot(OutputSlotId), ShaderGeneratorNames.FaceSign), true);
        }

		public bool RequiresFaceSign(ShaderStageCapability stageCapability = ShaderStageCapability.Fragment)
		{
			return true;
		}
	}
}
