using System.Reflection;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.ShaderGraph
{
    [SRPFilter(typeof(HDRenderPipeline))]
    [Title("Utility", "High Definition Render Pipeline", "Eye", "Builtin Iris Radius")]
    class BuiltinIrisRadius : AbstractMaterialNode, IGeneratesBodyCode
    {
        public BuiltinIrisRadius()
        {
            name = "Builtin Iris Radius";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("BuiltinIrisRadius");

        const int kBuiltinIrisRadiusId = 0;
        const string kBuiltinIrisRadiusName = "BuiltinIrisRadius";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Output
            AddSlot(new Vector1MaterialSlot(kBuiltinIrisRadiusId, kBuiltinIrisRadiusName, kBuiltinIrisRadiusName, SlotType.Output, 0));

            RemoveSlotsNameNotMatching(new[]
            {
                // Output
                kBuiltinIrisRadiusId,
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.ForReals)
            {
                sb.AppendLine("$precision {0} = BUILTIN_IRIS_RADIUS;",
                    GetVariableNameForSlot(kBuiltinIrisRadiusId)
                );
            }
            else
            {
                sb.AppendLine("$precision {0} = 0.0;",
                    GetVariableNameForSlot(kBuiltinIrisRadiusId)
                );
            }
        }
    }
}
