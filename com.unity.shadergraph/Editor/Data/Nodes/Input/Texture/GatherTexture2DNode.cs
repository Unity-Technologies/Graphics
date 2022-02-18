using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Texture", "Gather Texture 2D")]
    class GatherTexture2DNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireMeshUV
    {
        public const int OutputSlotRGBAId = 0;
        public const int OutputSlotRId = 5;
        public const int OutputSlotGId = 6;
        public const int OutputSlotBId = 7;
        public const int OutputSlotAId = 8;
        public const int TextureInputId = 1;
        public const int UVInput = 2;
        public const int SamplerInput = 3;
        public const int OffsetInput = 4;

        const string kOutputSlotRGBAName = "RGBA";
        const string kOutputSlotRName = "R";
        const string kOutputSlotGName = "G";
        const string kOutputSlotBName = "B";
        const string kOutputSlotAName = "A";
        const string kTextureInputName = "Texture";
        const string kUVInputName = "UV";
        const string kSamplerInputName = "Sampler";
        const string kOffsetInputName = "Offset";

        public override bool hasPreview { get { return true; } }

        public GatherTexture2DNode()
        {
            name = "Gather Texture 2D";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(OutputSlotRGBAId, kOutputSlotRGBAName, kOutputSlotRGBAName, SlotType.Output, Vector4.zero, ShaderStageCapability.All));
            AddSlot(new Vector1MaterialSlot(OutputSlotRId, kOutputSlotRName, kOutputSlotRName, SlotType.Output, 0, ShaderStageCapability.All));
            AddSlot(new Vector1MaterialSlot(OutputSlotGId, kOutputSlotGName, kOutputSlotGName, SlotType.Output, 0, ShaderStageCapability.All));
            AddSlot(new Vector1MaterialSlot(OutputSlotBId, kOutputSlotBName, kOutputSlotBName, SlotType.Output, 0, ShaderStageCapability.All));
            AddSlot(new Vector1MaterialSlot(OutputSlotAId, kOutputSlotAName, kOutputSlotAName, SlotType.Output, 0, ShaderStageCapability.All));
            AddSlot(new Texture2DInputMaterialSlot(TextureInputId, kTextureInputName, kTextureInputName));
            AddSlot(new UVMaterialSlot(UVInput, kUVInputName, kUVInputName, UVChannel.UV0));
            AddSlot(new SamplerStateMaterialSlot(SamplerInput, kSamplerInputName, kSamplerInputName, SlotType.Input));
            AddSlot(new Vector2MaterialSlot(OffsetInput, kOffsetInputName, kOffsetInputName, SlotType.Input, Vector2.zero, ShaderStageCapability.All, null, null, false, true));

            RemoveSlotsNameNotMatching(new[] { OutputSlotRGBAId, OutputSlotRId, OutputSlotGId, OutputSlotBId, OutputSlotAId, TextureInputId, UVInput, SamplerInput, OffsetInput });
        }

        public override void Setup()
        {
            base.Setup();
            var textureSlot = FindInputSlot<Texture2DInputMaterialSlot>(TextureInputId);
            textureSlot.defaultType = Texture2DShaderProperty.DefaultType.White;
        }

        // Node generations
        public virtual void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var uvName = GetSlotValue(UVInput, generationMode);

            //Sampler input slot
            var samplerSlot = FindInputSlot<MaterialSlot>(SamplerInput);
            var edgesSampler = owner.GetEdges(samplerSlot.slotReference);

            var id = GetSlotValue(TextureInputId, generationMode);
            var offset = GetSlotValue(OffsetInput, generationMode);

            sb.AppendLine("#if (SHADER_TARGET >= 41)");
            {
                sb.AppendLine(string.Format("$precision4 {0} = {1}.tex.Gather({2}.samplerstate, {3}, {4});"
                    , GetVariableNameForSlot(OutputSlotRGBAId)
                    , id
                    , edgesSampler.Any() ? GetSlotValue(SamplerInput, generationMode) : id
                    , uvName
                    , offset));
                sb.AppendLine(string.Format("$precision {0} = {1}.r;", GetVariableNameForSlot(OutputSlotRId), GetVariableNameForSlot(OutputSlotRGBAId)));
                sb.AppendLine(string.Format("$precision {0} = {1}.g;", GetVariableNameForSlot(OutputSlotGId), GetVariableNameForSlot(OutputSlotRGBAId)));
                sb.AppendLine(string.Format("$precision {0} = {1}.b;", GetVariableNameForSlot(OutputSlotBId), GetVariableNameForSlot(OutputSlotRGBAId)));
                sb.AppendLine(string.Format("$precision {0} = {1}.a;", GetVariableNameForSlot(OutputSlotAId), GetVariableNameForSlot(OutputSlotRGBAId)));
            }
            sb.AppendLine("#else");
            {
                // Gather offsets defined in this order:
                // (-,+),(+,+),(+,-),(-,-)

                var uvR = string.Format("(floor({0} * {1}.texelSize.zw + $precision2(-0.5, 0.5)) + trunc({2}) + $precision2(0.5, 0.5)) * {1}.texelSize.xy", uvName, id, offset);
                var uvG = string.Format("(floor({0} * {1}.texelSize.zw + $precision2(0.5, 0.5)) + trunc({2}) + $precision2(0.5, 0.5)) * {1}.texelSize.xy", uvName, id, offset);
                var uvB = string.Format("(floor({0} * {1}.texelSize.zw + $precision2(0.5, -0.5)) + trunc({2}) + $precision2(0.5, 0.5)) * {1}.texelSize.xy", uvName, id, offset);
                var uvA = string.Format("(floor({0} * {1}.texelSize.zw + $precision2(-0.5, -0.5)) + trunc({2}) + $precision2(0.5, 0.5)) * {1}.texelSize.xy", uvName, id, offset);

                sb.AppendLine(string.Format("$precision {0} = SAMPLE_TEXTURE2D_LOD({1}.tex, {2}.samplerstate, {3}, 0).r;", GetVariableNameForSlot(OutputSlotRId), id, edgesSampler.Any() ? GetSlotValue(SamplerInput, generationMode) : id, uvR));
                sb.AppendLine(string.Format("$precision {0} = SAMPLE_TEXTURE2D_LOD({1}.tex, {2}.samplerstate, {3}, 0).r;", GetVariableNameForSlot(OutputSlotGId), id, edgesSampler.Any() ? GetSlotValue(SamplerInput, generationMode) : id, uvG));
                sb.AppendLine(string.Format("$precision {0} = SAMPLE_TEXTURE2D_LOD({1}.tex, {2}.samplerstate, {3}, 0).r;", GetVariableNameForSlot(OutputSlotBId), id, edgesSampler.Any() ? GetSlotValue(SamplerInput, generationMode) : id, uvB));
                sb.AppendLine(string.Format("$precision {0} = SAMPLE_TEXTURE2D_LOD({1}.tex, {2}.samplerstate, {3}, 0).r;", GetVariableNameForSlot(OutputSlotAId), id, edgesSampler.Any() ? GetSlotValue(SamplerInput, generationMode) : id, uvA));

                sb.AppendLine(string.Format("$precision4 {0} = $precision4({1},{2},{3},{4});"
                    , GetVariableNameForSlot(OutputSlotRGBAId)
                    , GetVariableNameForSlot(OutputSlotRId)
                    , GetVariableNameForSlot(OutputSlotGId)
                    , GetVariableNameForSlot(OutputSlotBId)
                    , GetVariableNameForSlot(OutputSlotAId)));
            }
            sb.AppendLine("#endif");
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
