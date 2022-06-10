using System.Reflection;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.ShaderGraph
{
    [SRPFilter(typeof(HDRenderPipeline))]
    [Title("Utility", "High Definition Render Pipeline", "Eye", "Builtin Iris Planet Offset (Preview)")]
    class BuiltinIrisPlaneOffset : AbstractMaterialNode, IGeneratesBodyCode
    {
        public BuiltinIrisPlaneOffset()
        {
            name = "Builtin Iris Plane Offset (Preview)";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("BuiltinIrisPlaneOffset");

        const int kBuiltinIrisPlaneOffsetSlotId = 0;
        const string kBuiltinIrisPlaneOffsetSlotName = "BuiltinIrisPlaneOffset";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Output
            AddSlot(new Vector1MaterialSlot(kBuiltinIrisPlaneOffsetSlotId, kBuiltinIrisPlaneOffsetSlotName, kBuiltinIrisPlaneOffsetSlotName, SlotType.Output, 0));

            RemoveSlotsNameNotMatching(new[]
            {
                // Output
                kBuiltinIrisPlaneOffsetSlotId,
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.ForReals)
            {
                sb.AppendLine("$precision {0} = BUILTIN_IRIS_PLANE_OFFSET;",
                    GetVariableNameForSlot(kBuiltinIrisPlaneOffsetSlotId)
                );
            }
            else
            {
                sb.AppendLine("$precision {0} = 0.0;",
                    GetVariableNameForSlot(kBuiltinIrisPlaneOffsetSlotId)
                );
            }
        }
    }
}
