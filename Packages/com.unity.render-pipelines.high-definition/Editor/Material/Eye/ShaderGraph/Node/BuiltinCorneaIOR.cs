using System.Reflection;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.ShaderGraph
{
    [SRPFilter(typeof(HDRenderPipeline))]
    [Title("Utility", "High Definition Render Pipeline", "Eye", "Builtin Cornea IOR (Preview)")]
    class BuiltinCorneaIOR : AbstractMaterialNode, IGeneratesBodyCode
    {
        public BuiltinCorneaIOR()
        {
            name = "Builtin Cornea IOR (Preview)";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("BuiltinCorneaIOR");

        const int kBuiltinCorneaIORSlotId = 0;
        const string kBuiltinCorneaIORSlotName = "BuiltinCorneaIOR";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Output
            AddSlot(new Vector1MaterialSlot(kBuiltinCorneaIORSlotId, kBuiltinCorneaIORSlotName, kBuiltinCorneaIORSlotName, SlotType.Output, 0));

            RemoveSlotsNameNotMatching(new[]
            {
                // Output
                kBuiltinCorneaIORSlotId,
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.ForReals)
            {
                sb.AppendLine("$precision {0} = BUILTIN_CORNEA_IOR;",
                    GetVariableNameForSlot(kBuiltinCorneaIORSlotId)
                );
            }
            else
            {
                sb.AppendLine("$precision {0} = 0.0;",
                    GetVariableNameForSlot(kBuiltinCorneaIORSlotId)
                );
            }
        }
    }
}
