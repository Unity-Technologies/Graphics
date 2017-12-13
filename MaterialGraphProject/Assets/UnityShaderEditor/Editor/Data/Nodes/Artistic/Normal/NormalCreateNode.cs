using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Title("Artistic", "Normal", "Normal Create")]
    public class NormalCreateNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IGenerateProperties, IMayRequireMeshUV
    {
        public const int TextureInputId = 0;
        public const int UVInputId = 1;
        public const int SamplerInputId = 2;
        public const int OffsetInputId = 3;
        public const int StrengthInputId = 4;
        public const int OutputSlotId = 5;

        const string kTextureInputName = "Texture";
        const string kUVInputName = "UV";
        const string kSamplerInputName = "Sampler";
        const string kOffsetInputName = "Offset";
        const string kStrengthInputName = "Strength";
        const string kOutputSlotName = "Out";

        public NormalCreateNode()
        {
            name = "Normal Create";
            UpdateNodeAfterDeserialization();
        }

        string GetFunctionName()
        {
            return string.Format("Unity_NormalCreate_{0}", precision);
        }

        public override bool hasPreview { get { return true; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Texture2DInputMaterialSlot(TextureInputId, kTextureInputName, kTextureInputName));
            AddSlot(new UVMaterialSlot(UVInputId, kUVInputName, kUVInputName, UVChannel.uv0));
            AddSlot(new SamplerStateMaterialSlot(SamplerInputId, kSamplerInputName, kSamplerInputName, SlotType.Input));
            AddSlot(new Vector1MaterialSlot(OffsetInputId, kOffsetInputName, kOffsetInputName, SlotType.Input, 0.5f));
            AddSlot(new Vector1MaterialSlot(StrengthInputId, kStrengthInputName, kStrengthInputName, SlotType.Input, 8f));
            AddSlot(new Vector3MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector3.zero));
            RemoveSlotsNameNotMatching(new[] { TextureInputId, UVInputId, SamplerInputId, OffsetInputId, StrengthInputId, OutputSlotId });
        }

        string GetFunctionPrototype(string textureIn, string samplerIn, string uvIn, string offsetIn, string strengthIn, string argOut)
        {
            return string.Format("void {0} ({1} {2}, {3} {4}, {5} {6}, {7} {8}, {9} {10}, out {11} {12})", GetFunctionName(),
                ConvertConcreteSlotValueTypeToString(precision, FindInputSlot<MaterialSlot>(TextureInputId).concreteValueType), textureIn,
                ConvertConcreteSlotValueTypeToString(precision, FindInputSlot<MaterialSlot>(SamplerInputId).concreteValueType), samplerIn,
                ConvertConcreteSlotValueTypeToString(precision, FindInputSlot<MaterialSlot>(UVInputId).concreteValueType), uvIn,
                ConvertConcreteSlotValueTypeToString(precision, FindInputSlot<MaterialSlot>(OffsetInputId).concreteValueType), offsetIn,
                ConvertConcreteSlotValueTypeToString(precision, FindInputSlot<MaterialSlot>(StrengthInputId).concreteValueType), strengthIn,
                ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType), argOut);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            string textureValue = GetSlotValue(TextureInputId, generationMode);
            string uvValue = GetSlotValue(UVInputId, generationMode);
            string offsetValue = GetSlotValue(OffsetInputId, generationMode);
            string strengthValue = GetSlotValue(StrengthInputId, generationMode);
            string outputValue = GetSlotValue(OutputSlotId, generationMode);

            var samplerSlot = FindInputSlot<MaterialSlot>(SamplerInputId);
            var edgesSampler = owner.GetEdges(samplerSlot.slotReference);
            string samplerValue;
            if (edgesSampler.Any())
                samplerValue = GetSlotValue(SamplerInputId, generationMode);
            else
                samplerValue = string.Format("sampler{0}", GetSlotValue(TextureInputId, generationMode));

            visitor.AddShaderChunk(string.Format("{0} {1};", ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType), GetVariableNameForSlot(OutputSlotId)), true);
            visitor.AddShaderChunk(GetFunctionCallBody(textureValue, samplerValue, uvValue, offsetValue, strengthValue, outputValue), true);
        }

        string GetFunctionCallBody(string textureValue, string samplerValue, string uvValue, string offsetValue, string strengthValue, string outputValue)
        {
            return GetFunctionName() + " (" + textureValue + ", " + samplerValue + ", " + uvValue + ", " + offsetValue + ", " + strengthValue + ", " + outputValue + ");";
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var sg = new ShaderGenerator();
            sg.AddShaderChunk(GetFunctionPrototype("Texture", "Sampler", "UV", "Offset", "Strength", "Out"), false);
            sg.AddShaderChunk("{", false);
            sg.Indent();

            sg.AddShaderChunk("Offset = pow(Offset, 3) * 0.1;", false);
            sg.AddShaderChunk(string.Format("{0}2 offsetU = float2(UV.x + Offset, UV.y);", precision), false);
            sg.AddShaderChunk(string.Format("{0}2 offsetV = float2(UV.x, UV.y + Offset);", precision), false);

            sg.AddShaderChunk(string.Format("{0} normalSample = Texture.Sample(Sampler, UV);", precision), false);
            sg.AddShaderChunk(string.Format("{0} uSample = Texture.Sample(Sampler, offsetU);", precision), false);
            sg.AddShaderChunk(string.Format("{0} vSample = Texture.Sample(Sampler, offsetV);", precision), false);

            sg.AddShaderChunk(string.Format("{0}3 va = float3(1, 0, (uSample - normalSample) * Strength);", precision), false);
            sg.AddShaderChunk(string.Format("{0}3 vb = float3(0, 1, (vSample - normalSample) * Strength);", precision), false);
            sg.AddShaderChunk("Out = normalize(cross(va, vb));", false);

            sg.Deindent();
            sg.AddShaderChunk("}", false);

            visitor.AddShaderChunk(sg.GetShaderString(0), true);
        }

        public bool RequiresMeshUV(UVChannel channel)
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
