using UnityEditor.Graphing;
using UnityEditor.Rendering.UITK.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "UI", "Element Tint")]
    [SubTargetFilter(typeof(IUISubTarget))]
    class ElementTintNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireUITK
    {
        public const int TintSlotId = 0;

        private const string kTintSlotName = "Tint";

        public override bool hasPreview { get { return false; } }

        public ElementTintNode()
        {
            name = "Element Tint";
            synonyms = new string[] { "color", "vertex color", "dynamic color" };
            UpdateNodeAfterDeserialization();
        }

        public override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new ColorRGBAMaterialSlot(TintSlotId, kTintSlotName, kTintSlotName, SlotType.Output, Vector4.one));
            RemoveSlotsNameNotMatching(new[] { TintSlotId });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            string tintVar = GetVariableNameForSlot(TintSlotId);

            if (generationMode == GenerationMode.Preview)
            {
                // No element / dynamic-color data is bound in preview; show the identity tint.
                sb.AppendLine("$precision4 {0} = $precision4(1.0, 1.0, 1.0, 1.0);", tintVar);
                return;
            }

            // The straight tint (vertexColor * dynamicColor), interpolated in the fragment. It does not
            // include the per-element opacity (see the Element Opacity node).
            sb.AppendLine("$precision4 {0} = IN.color;", tintVar);
        }

        public bool RequiresUITK(ShaderStageCapability stageCapability)
        {
            return true;
        }
    }
}
