using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Artistic", "Filter", "Fade Transition")]
    class FadeTransitionNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction
    {
        public const int OutputSlotFadeId = 0;
        public const int NoiseValue = 1;
        public const int FadeValue = 2;
        public const int FadeContrast = 3;

        const string kOutputSlotFadeName = "Fade";
        const string kNoiseValueInputName = "NoiseValue";
        const string kFadeValueInputName = "FadeValue";
        const string kFadeContrastInputName = "FadeContrast";

        public override bool hasPreview { get { return true; } }

        public FadeTransitionNode()
        {
            name = "Fade Transition";
            synonyms = new string[] { "fade" };
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(OutputSlotFadeId, kOutputSlotFadeName, kOutputSlotFadeName, SlotType.Output, 0.0f, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(NoiseValue, kNoiseValueInputName, kNoiseValueInputName, SlotType.Input, 0.5f, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(FadeValue, kFadeValueInputName, kFadeValueInputName, SlotType.Input, 0.5f, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(FadeContrast, kFadeContrastInputName, kFadeContrastInputName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));

            RemoveSlotsNameNotMatching(new[] { OutputSlotFadeId, NoiseValue, FadeValue, FadeContrast });
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.ProvideFunction($"Unity_FadeTransitionNode_ApplyFade_$precision", s =>
            {
                s.AppendLine($"$precision Unity_FadeTransitionNode_ApplyFade_$precision($precision noise, $precision fadeValue, $precision fadeContrast)");
                using (s.BlockScope())
                {
                    s.AppendLine($"$precision ret = saturate(fadeValue*(fadeContrast+1)+(noise-1)*fadeContrast);");
                    s.AppendLine("return ret;");
                }
            });
        }

        // Node generations
        public virtual void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var noiseValueName = GetSlotValue(NoiseValue, generationMode);

            var fadeValueName = GetSlotValue(FadeValue, generationMode);
            var fadeContrastName = GetSlotValue(FadeContrast, generationMode);

            var result = string.Format("$precision {0} = Unity_FadeTransitionNode_ApplyFade_$precision({1}, {2}, {3});"
                , GetVariableNameForSlot(OutputSlotFadeId)
                , noiseValueName
                , fadeValueName
                , fadeContrastName
                );

            sb.AppendLine(result);
        }

    }
}
