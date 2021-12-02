using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Geometry", "LOD Fade")]
    class LODFadeNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        public const int OutputSlotFadeId = 0;
        public const int OutputSlotQuantizedId = 1;
        const string kOutputSlotFadeName = "Fade";
        const string kOutputSlotQuantizedName = "Quantized Fade";


        public override bool hasPreview { get { return false; } }

        public LODFadeNode()
        {
            name = "LOD Fade";
            synonyms = new string[] { "fade", "disolve", "cross fade", "blend", "level of detail" };
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(OutputSlotFadeId, kOutputSlotFadeName, kOutputSlotFadeName, SlotType.Output, 0, ShaderStageCapability.All));
            AddSlot(new Vector1MaterialSlot(OutputSlotQuantizedId, kOutputSlotQuantizedName, kOutputSlotQuantizedName, SlotType.Output, 0, ShaderStageCapability.All));
            RemoveSlotsNameNotMatching(new[] { OutputSlotFadeId, OutputSlotQuantizedId });
        }

        // Node generations
        public virtual void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            sb.AppendLine(string.Format("$precision {0} = unity_LODFade.x;", GetVariableNameForSlot(OutputSlotFadeId)));
            sb.AppendLine(string.Format("$precision {0} = unity_LODFade.y;", GetVariableNameForSlot(OutputSlotQuantizedId)));
        }
    }
}
