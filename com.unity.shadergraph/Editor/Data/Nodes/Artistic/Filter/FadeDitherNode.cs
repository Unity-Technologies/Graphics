using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Artistic", "Filter", "Fade Dither")]
    class FadeDitherNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireMeshUV, IGeneratesFunction
    {
        public const int OutputSlotFadeId = 0;
        public const int TextureInputId = 1;
        public const int UVInput = 2;
        public const int FadeValue = 3;
        public const int FadeSpeed = 4;
        public const int PixelRatio = 5;

        const string kOutputSlotFadeName = "Fade";
        const string kTextureInputName = "Texture";
        const string kUVInputName = "UV";
        const string kFadeValueInputName = "FadeValue";
        const string kFadeSpeedInputName = "FadeSpeed";
        const string kPixelRatioInputName = "PixelRatio";

        public override bool hasPreview { get { return true; } }

        public FadeDitherNode()
        {
            name = "Fade Dither";
            synonyms = new string[] { "fade" };
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(OutputSlotFadeId, kOutputSlotFadeName, kOutputSlotFadeName, SlotType.Output, 0.0f, ShaderStageCapability.Fragment));
            AddSlot(new Texture2DInputMaterialSlot(TextureInputId, kTextureInputName, kTextureInputName));
            AddSlot(new UVMaterialSlot(UVInput, kUVInputName, kUVInputName, UVChannel.UV0));
            AddSlot(new Vector1MaterialSlot(FadeValue, kFadeValueInputName, kFadeValueInputName, SlotType.Input, 0.5f, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(FadeSpeed, kFadeSpeedInputName, kFadeSpeedInputName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(PixelRatio, kPixelRatioInputName, kPixelRatioInputName, SlotType.Input, 0.5f, ShaderStageCapability.Fragment));

            RemoveSlotsNameNotMatching(new[] { OutputSlotFadeId, TextureInputId, UVInput, FadeValue, FadeSpeed, PixelRatio });
        }

        public override void Setup()
        {
            base.Setup();
            var textureSlot = FindInputSlot<Texture2DInputMaterialSlot>(TextureInputId);
            textureSlot.defaultType = Texture2DShaderProperty.DefaultType.White;
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.ProvideFunction($"Unity_FadeDitherNode_ApplyFade_$precision", s =>
            {
                s.AppendLine($"$precision Unity_FadeDitherNode_ApplyFade_$precision($precision noise, $precision fadeValue, $precision fadeSpeed)");
                using (s.BlockScope())
                {
                    s.AppendLine($"$precision ret = saturate(fadeValue*(fadeSpeed+1)+(noise-1)*fadeSpeed);");
                    //s.AppendLine($"$precision ret = noise;");
                    s.AppendLine("return ret;");
                }
            });
        }

        // Node generations
        public virtual void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var uvName = GetSlotValue(UVInput, generationMode);

            var fadeValueName = GetSlotValue(FadeValue, generationMode);
            var fadeSpeedName = GetSlotValue(FadeSpeed, generationMode);
            var pixelRatioName = GetSlotValue(PixelRatio, generationMode);

            var id = GetSlotValue(TextureInputId, generationMode);
            var result = string.Format("$precision {0} = Unity_FadeDitherNode_ApplyFade_$precision({1}({2}.tex, {3}.samplerstate, {2}.GetTransformedUV({4})*{7}).x, {5}, {6});"
                , GetVariableNameForSlot(OutputSlotFadeId)
                , "SAMPLE_TEXTURE2D"
                , id
                , id
                , uvName
                , fadeValueName
                , fadeSpeedName
                , pixelRatioName
                );

            sb.AppendLine(result);
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            using (var tempSlots = PooledList<MaterialSlot>.Get())
            {
                GetInputSlots(tempSlots);
                var result = false;
                foreach (var slot in tempSlots)
                {
                    if (slot.RequiresMeshUV(channel))
                    {
                        result = true;
                        break;
                    }
                }

                tempSlots.Clear();
                return result;
            }
        }
    }
}
