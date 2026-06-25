using UnityEditor.Graphing;
using UnityEditor.Rendering.UITK.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "UI", "Element Opacity")]
    [SubTargetFilter(typeof(IUISubTarget))]
    class ElementOpacityNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireUITK
    {
        public const int OpacitySlotId = 0;

        private const string kOpacitySlotName = "Opacity";

        public override bool hasPreview { get { return false; } }

        public ElementOpacityNode()
        {
            name = "Element Opacity";
            synonyms = new string[] { "alpha", "fade" };
            UpdateNodeAfterDeserialization();
        }

        public override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(OpacitySlotId, kOpacitySlotName, kOpacitySlotName, SlotType.Output, 1.0f));
            RemoveSlotsNameNotMatching(new[] { OpacitySlotId });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            string opacityVar = GetVariableNameForSlot(OpacitySlotId);

            if (generationMode == GenerationMode.Preview)
            {
                // No element data is bound in preview; show the identity opacity.
                sb.AppendLine("$precision {0} = 1.0;", opacityVar);
                return;
            }

            // The per-element opacity the vertex reads. Applied automatically at the end of the fragment
            // unless Automatic Opacity is disabled in the Graph Settings, in which case apply it yourself.
            sb.AppendLine("$precision {0} = IN.opacity;", opacityVar);
        }

        public bool RequiresUITK(ShaderStageCapability stageCapability)
        {
            return true;
        }
    }
}
