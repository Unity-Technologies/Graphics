using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Texture", "Sample Texture 3D")]
    class SampleTexture3DNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        public const int OutputSlotId = 0;
        public const int TextureInputId = 1;
        public const int UVInput = 2;
        public const int SamplerInput = 3;

        const string kOutputSlotName = "Out";
        const string kTextureInputName = "Texture";
        const string kUVInputName = "UV";
        const string kSamplerInputName = "Sampler";

        public override bool hasPreview { get { return true; } }

        public SampleTexture3DNode()
        {
            name = "Sample Texture 3D";
            UpdateNodeAfterDeserialization();
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero, ShaderStageCapability.Fragment));
            AddSlot(new Texture3DInputMaterialSlot(TextureInputId, kTextureInputName, kTextureInputName));
            AddSlot(new Vector3MaterialSlot(UVInput, kUVInputName, kUVInputName, SlotType.Input, Vector3.zero));
            AddSlot(new SamplerStateMaterialSlot(SamplerInput, kSamplerInputName, kSamplerInputName, SlotType.Input));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId, TextureInputId, UVInput, SamplerInput });
        }

        // Node generations
        public virtual void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var uvName = GetSlotValue(UVInput, generationMode);

            //Sampler input slot
            var samplerSlot = FindInputSlot<MaterialSlot>(SamplerInput);
            var edgesSampler = owner.GetEdges(samplerSlot.slotReference);

            var id = GetSlotValue(TextureInputId, generationMode);
            var result = string.Format("$precision4 {0} = SAMPLE_TEXTURE3D({1}.tex, {2}.samplerstate, {3});"
                    , GetVariableNameForSlot(OutputSlotId)
                    , id
                    , edgesSampler.Any() ? GetSlotValue(SamplerInput, generationMode) : id
                    , uvName);

            sb.AppendLine(result);
        }
    }
}
