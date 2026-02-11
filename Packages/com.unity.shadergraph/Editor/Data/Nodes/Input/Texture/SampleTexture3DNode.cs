using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Texture", "Sample Texture 3D")]
    class SampleTexture3DNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        public const int OutputSlotRGBAId = 0;
        public const int OutputSlotRId = 5;
        public const int OutputSlotGId = 6;
        public const int OutputSlotBId = 7;
        public const int OutputSlotAId = 8;
        public const int TextureInputId = 1;
        public const int UVInput = 2;
        public const int SamplerInput = 3;
        public const int LodInput = 4;

        const string kTextureInputName = "Texture";
        const string kUVInputName = "UV";
        const string kSamplerInputName = "Sampler";

        RGBANodeOutput m_RGBAPins = RGBANodeOutput.NewDefault();
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
            var capabilities = ShaderStageCapability.Fragment;
            if (m_MipSamplingMode == Texture3DMipSamplingMode.LOD)
                capabilities |= ShaderStageCapability.Vertex;

            m_RGBAPins.SetCapabilities(capabilities);

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
            m_RGBAPins.CreateNodes(this, ShaderStageCapability.None, OutputSlotRGBAId, OutputSlotRId, OutputSlotGId, OutputSlotBId, OutputSlotAId);
            AddSlot(new Texture3DInputMaterialSlot(TextureInputId, kTextureInputName, kTextureInputName));
            AddSlot(new Vector3MaterialSlot(UVInput, kUVInputName, kUVInputName, SlotType.Input, Vector3.zero));
            AddSlot(new SamplerStateMaterialSlot(SamplerInput, kSamplerInputName, kSamplerInputName, SlotType.Input));
            UpdateMipSamplingModeInputs();
            RemoveSlotsNameNotMatching(new[] { OutputSlotRGBAId, OutputSlotRId, OutputSlotGId, OutputSlotBId, OutputSlotAId, TextureInputId, UVInput, SamplerInput, LodInput });
        }

        // Node generations
        public virtual void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var uvName = GetSlotValue(UVInput, generationMode);
            string outputVectorVariableName = GetVariableNameForSlot(OutputSlotRGBAId);

            //Sampler input slot
            var samplerSlot = FindInputSlot<MaterialSlot>(SamplerInput);
            var edgesSampler = owner.GetEdges(samplerSlot.slotReference);

            // Sample
            var id = GetSlotValue(TextureInputId, generationMode);
            var result = string.Format("$precision4 {0} = {1}({2}.tex, {3}.samplerstate, {4} {5});"
                , outputVectorVariableName
                , MipSamplingModesUtils.Get3DTextureSamplingMacro(m_MipSamplingMode, usePlatformMacros: false)
                , id
                , edgesSampler.Any() ? GetSlotValue(SamplerInput, generationMode) : id
                , uvName
                , MipSamplingModesUtils.GetSamplerMipArgs(this, m_MipSamplingMode, m_Mip3DSamplingInputs, generationMode));

            sb.AppendLine(result);

            // Decode HDR
            SampleTexture2DNode.AppendHDRDecodeOperation(sb, outputVectorVariableName, id);

            // Extract components
            sb.AppendLine(string.Format("$precision {0} = {1}.r;", GetVariableNameForSlot(OutputSlotRId), outputVectorVariableName));
            sb.AppendLine(string.Format("$precision {0} = {1}.g;", GetVariableNameForSlot(OutputSlotGId), outputVectorVariableName));
            sb.AppendLine(string.Format("$precision {0} = {1}.b;", GetVariableNameForSlot(OutputSlotBId), outputVectorVariableName));
            sb.AppendLine(string.Format("$precision {0} = {1}.a;", GetVariableNameForSlot(OutputSlotAId), outputVectorVariableName));
        }
    }
}
