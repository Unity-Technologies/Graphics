using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Texture", "Calculate Level Of Detail Texture 2D")]
    class CalculateLevelOfDetailTexture2DNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireMeshUV
    {
        [SerializeField]
        bool m_Clamp = true;
        [ToggleControl]
        public ToggleData clamp
        {
            get => new ToggleData(m_Clamp);
            set
            {
                m_Clamp = value.isOn;
                Dirty(ModificationScope.Node);
            }
        }

        public const int OutputSlotLODId = 0;
        public const int TextureInputId = 1;
        public const int UVInput = 2;
        public const int SamplerInput = 3;

        const string kOutputSlotLODName = "LOD";
        const string kTextureInputName = "Texture";
        const string kUVInputName = "UV";
        const string kSamplerInputName = "Sampler";

        public override bool hasPreview { get { return true; } }

        public CalculateLevelOfDetailTexture2DNode()
        {
            name = "Calculate Level Of Detail Texture 2D";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(OutputSlotLODId, kOutputSlotLODName, kOutputSlotLODName, SlotType.Output, 0, ShaderStageCapability.Fragment));
            AddSlot(new Texture2DInputMaterialSlot(TextureInputId, kTextureInputName, kTextureInputName));
            AddSlot(new UVMaterialSlot(UVInput, kUVInputName, kUVInputName, UVChannel.UV0));
            AddSlot(new SamplerStateMaterialSlot(SamplerInput, kSamplerInputName, kSamplerInputName, SlotType.Input));

            RemoveSlotsNameNotMatching(new[] { OutputSlotLODId, TextureInputId, UVInput, SamplerInput });
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

            sb.AppendLine("#if (SHADER_TARGET >= 41)");
            {
                var func = m_Clamp ? "CalculateLevelOfDetail" : "CalculateLevelOfDetailUnclamped";
                sb.AppendLine(string.Format("$precision {0} = {1}.tex.{2}({3}.samplerstate, {4});"
                    , GetVariableNameForSlot(OutputSlotLODId)
                    , id
                    , func
                    , edgesSampler.Any() ? GetSlotValue(SamplerInput, generationMode) : id
                    , uvName));
            }
            sb.AppendLine("#else");
            {
                var dUVdx = string.Format("ddx({0})", uvName);
                var dUVdy = string.Format("ddy({0})", uvName);
                var delta_max_sqr = string.Format("max(dot({0}, {0}), dot({1}, {1}))", dUVdx, dUVdy);
                sb.AppendLine(string.Format("$precision {0};", GetVariableNameForSlot(OutputSlotLODId)));
                sb.AppendLine("{");
                sb.AppendLine(string.Format("uint2 {0}_dimension; uint {0}_levels; {0}.tex.GetDimensions(0, {0}_dimension.x, {0}_dimension.y, {0}_levels);", id));
                sb.AppendLine(string.Format("{0} = {1}_levels - 1 + 0.5f*log2(max(1e-6, {2}));", GetVariableNameForSlot(OutputSlotLODId), id, delta_max_sqr));
                if (m_Clamp)
                {
                    sb.AppendLine(string.Format("{0} = clamp({0}, 0, {1}_levels-1);", GetVariableNameForSlot(OutputSlotLODId), id));
                }
                sb.AppendLine("}");
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
