using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Texture", "Texture Size")]
    class Texture2DPropertiesNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        public const int OutputSlotWId = 0;
        public const int OutputSlotHId = 2;
        public const int OutputSlotTWId = 3;
        public const int OutputSlotTHId = 4;
        public const int TextureInputId = 1;
        const string kOutputSlotWName = "Width";
        const string kOutputSlotHName = "Height";
        const string kOutputSlotTWName = "Texel Width";
        const string kOutputSlotTHName = "Texel Height";
        const string kTextureInputName = "Texture";

        public override bool hasPreview { get { return false; } }

        public Texture2DPropertiesNode()
        {
            name = "Texture Size";
            synonyms = new string[] { "texel size" };
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(OutputSlotWId, kOutputSlotWName, kOutputSlotWName, SlotType.Output, 0, ShaderStageCapability.All));
            AddSlot(new Vector1MaterialSlot(OutputSlotHId, kOutputSlotHName, kOutputSlotHName, SlotType.Output, 0, ShaderStageCapability.All));
            AddSlot(new Vector1MaterialSlot(OutputSlotTWId, kOutputSlotTWName, kOutputSlotTWName, SlotType.Output, 0, ShaderStageCapability.All));
            AddSlot(new Vector1MaterialSlot(OutputSlotTHId, kOutputSlotTHName, kOutputSlotTHName, SlotType.Output, 0, ShaderStageCapability.All));
            AddSlot(new Texture2DInputMaterialSlot(TextureInputId, kTextureInputName, kTextureInputName));
            RemoveSlotsNameNotMatching(new[] { OutputSlotWId, OutputSlotHId, OutputSlotTWId, OutputSlotTHId, TextureInputId });
        }

        // Node generations
        public virtual void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            sb.AppendLine(string.Format("$precision {0} = {1}.texelSize.z;", GetVariableNameForSlot(OutputSlotWId), GetSlotValue(TextureInputId, generationMode)));
            sb.AppendLine(string.Format("$precision {0} = {1}.texelSize.w;", GetVariableNameForSlot(OutputSlotHId), GetSlotValue(TextureInputId, generationMode)));
            sb.AppendLine(string.Format("$precision {0} = {1}.texelSize.x;", GetVariableNameForSlot(OutputSlotTWId), GetSlotValue(TextureInputId, generationMode)));
            sb.AppendLine(string.Format("$precision {0} = {1}.texelSize.y;", GetVariableNameForSlot(OutputSlotTHId), GetSlotValue(TextureInputId, generationMode)));
        }
    }
}
