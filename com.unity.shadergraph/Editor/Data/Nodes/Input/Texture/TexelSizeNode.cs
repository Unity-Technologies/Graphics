using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{

    [Title("Input", "Texture", "Texel Size")]
    class Texture2DPropertiesNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireMeshUV
    {
        public const int OutputSlotWId = 0;
        public const int OutputSlotHId = 2;
        public const int TextureInputId = 1;
        const string kOutputSlotWName = "Width";
        const string kOutputSlotHName = "Height";
        const string kTextureInputName = "Texture";

        public override bool hasPreview { get { return false; } }

        public Texture2DPropertiesNode()
        {
            name = "Texel Size";
            UpdateNodeAfterDeserialization();
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(OutputSlotWId, kOutputSlotWName, kOutputSlotWName, SlotType.Output, 0, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(OutputSlotHId, kOutputSlotHName, kOutputSlotHName, SlotType.Output, 0, ShaderStageCapability.Fragment));
            AddSlot(new Texture2DInputMaterialSlot(TextureInputId, kTextureInputName, kTextureInputName));
            RemoveSlotsNameNotMatching(new[] { OutputSlotWId, OutputSlotHId, TextureInputId });
        }

        // Node generations
        public virtual void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
			sb.AppendLine(string.Format("$precision {0} = {1}_TexelSize.z;", GetVariableNameForSlot(OutputSlotWId), GetSlotValue(TextureInputId, generationMode)));
			sb.AppendLine(string.Format("$precision {0} = {1}_TexelSize.w;", GetVariableNameForSlot(OutputSlotHId), GetSlotValue(TextureInputId, generationMode)));
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            return true;
        }
    }
}
