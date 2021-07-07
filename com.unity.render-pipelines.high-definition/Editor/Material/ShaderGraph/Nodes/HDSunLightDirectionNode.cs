using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [SRPFilter(typeof(HDRenderPipeline))]
    [Title("Input", "High Definition Render Pipeline", "HD Sun Light Direction")]
    class HDSunLightDirection : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction
    {
        public HDSunLightDirection()
        {
            name = "HD Sun Light Direction";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("SGNode-HD-Sun-Light-Direction");

        public const int OutputSlotId = 0;
        const string kOutputSlotName = "Output";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName , SlotType.Output, Vector3.zero));

            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        string GetFunctionName() => $"Unity_HDRP_GetSunLightDirection_$precision";

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.ProvideFunction(GetFunctionName(), s =>
            {
                s.AppendLine("$precision3 {0}()", GetFunctionName());
                using (s.BlockScope())
                {
                    s.AppendLine("#if defined(SHADERPASS) && (SHADERPASS != SHADERPASS_LIGHT_TRANSPORT)");
                    s.AppendLine("if (_DirectionalLightCount > 0)");
                    s.AppendLine("  return _DirectionalLightDatas[0].forward;");
                    s.AppendLine("#endif");
                    s.AppendLine("return $precision3(0.0f, 0.0f, 0.0f);");
                }
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            sb.AppendLine("$precision3 {0} = {1}();",
                GetVariableNameForSlot(OutputSlotId),
                GetFunctionName()
            );
        }
    }
}
