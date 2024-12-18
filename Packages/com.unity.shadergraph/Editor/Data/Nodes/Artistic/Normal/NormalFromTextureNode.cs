using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [FormerName("UnityEditor.ShaderGraph.NormalCreateNode")]
    [Title("Artistic", "Normal", "Normal From Texture")]
    class NormalFromTextureNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequireMeshUV
    {
        public const int TextureInputId = 0;
        public const int UVInputId = 1;
        public const int SamplerInputId = 2;
        public const int OffsetInputId = 3;
        public const int StrengthInputId = 4;
        public const int OutputSlotId = 5;

        const string k_TextureInputName = "Texture";
        const string k_UVInputName = "UV";
        const string k_SamplerInputName = "Sampler";
        const string k_OffsetInputName = "Offset";
        const string k_StrengthInputName = "Strength";
        const string k_OutputSlotName = "Out";

        public NormalFromTextureNode()
        {
            name = "Normal From Texture";
            synonyms = new string[] { "convert to normal", "bump map" };
            UpdateNodeAfterDeserialization();
        }

        string GetFunctionName()
        {
            return "Unity_NormalFromTexture_$precision";
        }

        public override bool hasPreview { get { return true; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Texture2DInputMaterialSlot(TextureInputId, k_TextureInputName, k_TextureInputName));
            AddSlot(new UVMaterialSlot(UVInputId, k_UVInputName, k_UVInputName, UVChannel.UV0));
            AddSlot(new SamplerStateMaterialSlot(SamplerInputId, k_SamplerInputName, k_SamplerInputName, SlotType.Input));
            AddSlot(new Vector1MaterialSlot(OffsetInputId, k_OffsetInputName, k_OffsetInputName, SlotType.Input, 0.5f));
            AddSlot(new Vector1MaterialSlot(StrengthInputId, k_StrengthInputName, k_StrengthInputName, SlotType.Input, 8f));
            AddSlot(new Vector3MaterialSlot(OutputSlotId, k_OutputSlotName, k_OutputSlotName, SlotType.Output, Vector3.zero, ShaderStageCapability.Fragment));
            RemoveSlotsNameNotMatching(new[] { TextureInputId, UVInputId, SamplerInputId, OffsetInputId, StrengthInputId, OutputSlotId });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var textureValue = GetSlotValue(TextureInputId, generationMode);
            var uvValue = GetSlotValue(UVInputId, generationMode);
            var offsetValue = GetSlotValue(OffsetInputId, generationMode);
            var strengthValue = GetSlotValue(StrengthInputId, generationMode);
            var outputValue = GetSlotValue(OutputSlotId, generationMode);

            var samplerSlot = FindInputSlot<MaterialSlot>(SamplerInputId);
            var edgesSampler = owner.GetEdges(samplerSlot.slotReference);
            string samplerValue;
            if (edgesSampler.Any())
                samplerValue = GetSlotValue(SamplerInputId, generationMode);
            else
                samplerValue = textureValue;

            sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToShaderString(), GetVariableNameForSlot(OutputSlotId));
            sb.AppendLine("{0}(TEXTURE2D_ARGS({1}.tex, {2}.samplerstate), {1}.GetTransformedUV({3}), {4}, {5}, {6});", GetFunctionName(), textureValue, samplerValue, uvValue, offsetValue, strengthValue, outputValue);
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.ProvideFunction(GetFunctionName(), s =>
            {
                s.AppendLine("void {0}(TEXTURE2D_PARAM(Texture, Sampler), {1} UV, {2} Offset, {3} Strength, out {4} Out)",
                    GetFunctionName(),
                    FindInputSlot<MaterialSlot>(UVInputId).concreteValueType.ToShaderString(),
                    FindInputSlot<MaterialSlot>(OffsetInputId).concreteValueType.ToShaderString(),
                    FindInputSlot<MaterialSlot>(StrengthInputId).concreteValueType.ToShaderString(),
                    FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToShaderString());
                using (s.BlockScope())
                {
                    s.AppendLine("Offset = pow(Offset, 3) * 0.1;");
                    s.AppendLine("$precision2 offsetU = $precision2(UV.x + Offset, UV.y);");
                    s.AppendLine("$precision2 offsetV = $precision2(UV.x, UV.y + Offset);");

                    s.AppendLine("$precision normalSample = SAMPLE_TEXTURE2D(Texture, Sampler, UV).r;");
                    s.AppendLine("$precision uSample = SAMPLE_TEXTURE2D(Texture, Sampler, offsetU).r;");
                    s.AppendLine("$precision vSample = SAMPLE_TEXTURE2D(Texture, Sampler, offsetV).r;");

                    s.AppendLine("$precision3 va = $precision3(1, 0, (uSample - normalSample) * Strength);");
                    s.AppendLine("$precision3 vb = $precision3(0, 1, (vSample - normalSample) * Strength);");
                    s.AppendLine("Out = normalize(cross(va, vb));");
                }
            });
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            foreach (var slot in this.GetInputSlots<MaterialSlot>().OfType<IMayRequireMeshUV>())
            {
                if (slot.RequiresMeshUV(channel))
                    return true;
            }
            return false;
        }
    }
}
