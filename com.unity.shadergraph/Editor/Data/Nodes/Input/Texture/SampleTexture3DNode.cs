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
        public const int LodInput = 4;

        const string kOutputSlotName = "Out";
        const string kTextureInputName = "Texture";
        const string kUVInputName = "UV";
        const string kSamplerInputName = "Sampler";

        Mip3DSamplingInputs m_Mip3DSamplingInputs = Mip3DSamplingInputs.NewDefault();

        public override bool hasPreview { get { return true; } }

        [SerializeField]
        private Texture3DMipSamplingMode m_MipSamplingMode = Texture3DMipSamplingMode.Standard;
        internal Texture3DMipSamplingMode mipSamplingMode
        {
            set { m_MipSamplingMode = value; UpdateMipSamplingModeInputs(); }
            get { return m_MipSamplingMode; }
        }

        private void UpdateMipSamplingModeInputs()
        {
            m_Mip3DSamplingInputs = MipSamplingModesUtils.CreateMip3DSamplingInputs(
                this, m_MipSamplingMode, m_Mip3DSamplingInputs, LodInput);
        }

        public SampleTexture3DNode()
        {
            name = "Sample Texture 3D";
            synonyms = new string[] { "volume", "tex3d" };
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero, ShaderStageCapability.Fragment));
            AddSlot(new Texture3DInputMaterialSlot(TextureInputId, kTextureInputName, kTextureInputName));
            AddSlot(new Vector3MaterialSlot(UVInput, kUVInputName, kUVInputName, SlotType.Input, Vector3.zero));
            AddSlot(new SamplerStateMaterialSlot(SamplerInput, kSamplerInputName, kSamplerInputName, SlotType.Input));
            UpdateMipSamplingModeInputs();
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
            var result = string.Format("$precision4 {0} = {1}({2}.tex, {3}.samplerstate, {4} {5});"
                , GetVariableNameForSlot(OutputSlotId)
                , MipSamplingModesUtils.Get3DTextureSamplingMacro(m_MipSamplingMode, usePlatformMacros: false)
                , id
                , edgesSampler.Any() ? GetSlotValue(SamplerInput, generationMode) : id
                , uvName
                , MipSamplingModesUtils.GetSamplerMipArgs(this, m_MipSamplingMode, m_Mip3DSamplingInputs, generationMode));

            sb.AppendLine(result);
        }
    }
}
