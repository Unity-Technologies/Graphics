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
            UpdateNodeAfterDeserialization();
        }

        string GetFunctionName()
        {
            return $"Unity_NormalFromTexture_{concretePrecision.ToShaderString()}";
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
                samplerValue = string.Format("sampler{0}", GetSlotValue(TextureInputId, generationMode));

            sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToShaderString(), GetVariableNameForSlot(OutputSlotId));
            sb.AppendLine("{0}({1}, {2}, {3}, {4}, {5}, {6});", GetFunctionName(), textureValue, samplerValue, uvValue, offsetValue, strengthValue, outputValue);
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.ProvideFunction(GetFunctionName(), s =>
                {
                    s.AppendLine("void {0}({1} Texture, {2} Sampler, {3} UV, {4} Offset, {5} Strength, out {6} Out)", GetFunctionName(),
                        FindInputSlot<MaterialSlot>(TextureInputId).concreteValueType.ToShaderString(),
                        FindInputSlot<MaterialSlot>(SamplerInputId).concreteValueType.ToShaderString(),
                        FindInputSlot<MaterialSlot>(UVInputId).concreteValueType.ToShaderString(),
                        FindInputSlot<MaterialSlot>(OffsetInputId).concreteValueType.ToShaderString(),
                        FindInputSlot<MaterialSlot>(StrengthInputId).concreteValueType.ToShaderString(),
                        FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToShaderString());
                    using (s.BlockScope())
                    {
                        s.AppendLine("Offset = pow(Offset, 3) * 0.1;");
                        s.AppendLine("$precision2 offsetU = $precision2(UV.x + Offset, UV.y);");
                        s.AppendLine("$precision2 offsetV = $precision2(UV.x, UV.y + Offset);");

                        s.AppendLine("$precision normalSample = Texture.Sample(Sampler, UV);");
                        s.AppendLine("$precision uSample = Texture.Sample(Sampler, offsetU);");
                        s.AppendLine("$precision vSample = Texture.Sample(Sampler, offsetV);");

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
