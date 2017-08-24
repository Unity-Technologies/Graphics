using System.Linq;
using System.Reflection;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Texture/Sample 2D")]
    public class Sample2DTexture : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireMeshUV
    {
        public const int OutputSlotId = 0;
        public const int TextureInputId = 1;
        public const int UVInput = 2;
        public const int SamplerInput = 3;

        private const string kOutputSlotName = "rgba";
        private const string kTextureInputName = "Tex";
        private const string kUVInputName = "UV";
        private const string kSamplerInputName = "Sampler";

        public override bool hasPreview { get { return true; } }

        public Sample2DTexture()
        {
            name = "Sample2DTexture";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, SlotValueType.Vector4, Vector4.zero));
            AddSlot(new MaterialSlot(TextureInputId, kTextureInputName, kTextureInputName, SlotType.Input, SlotValueType.Texture2D, Vector4.zero));
            AddSlot(new MaterialSlot(UVInput, kUVInputName, kUVInputName, SlotType.Input, SlotValueType.Vector2, Vector4.zero));
            AddSlot(new MaterialSlot(SamplerInput, kSamplerInputName, kSamplerInputName, SlotType.Input, SlotValueType.SamplerState, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId, TextureInputId, UVInput, SamplerInput });
        }

        // Node generations
        public virtual void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            //Texture input slot
            var textureSlot = FindInputSlot<MaterialSlot>(TextureInputId);
            var edgesTexture = owner.GetEdges(textureSlot.slotReference);
            // if no texture connected return nothing
            if (!edgesTexture.Any())
            {
                visitor.AddShaderChunk(precision + "4 " + GetVariableNameForSlot(OutputSlotId) + " = " + precision + "4(0,0,0,0);", true);
                return;
            }

            //UV input slot
            var uvSlot = FindInputSlot<MaterialSlot>(UVInput);
            var uvName = string.Format("{0}.xy", UVChannel.uv0.GetUVName());
            var edgesUV = owner.GetEdges(uvSlot.slotReference);
            if (edgesUV.Any())
                uvName = GetSlotValue(UVInput, generationMode);

            //Sampler input slot
            var samplerSlot = FindInputSlot<MaterialSlot>(SamplerInput);
            var edgesSampler = owner.GetEdges(samplerSlot.slotReference);

            string result;
            if (edgesSampler.Any())
            {
                result = string.Format(@"
#ifdef UNITY_COMPILER_HLSL
{0}4 {1} = {2}.Sample({3}, {4});
#else
{0}4 {1} = UNITY_SAMPLE_TEX2D({2}, {4});
#endif"
                        , precision
                        , GetVariableNameForSlot(OutputSlotId)
                        , GetSlotValue(TextureInputId, generationMode)
                        , GetSlotValue(SamplerInput, generationMode)
                        , uvName);
            }
            else
            {
                result = string.Format("{0}4 {1} = UNITY_SAMPLE_TEX2D({2},{3});"
                        , precision
                        , GetVariableNameForSlot(OutputSlotId)
                        , GetSlotValue(TextureInputId, generationMode)
                        , uvName);
            }
            visitor.AddShaderChunk(result, true);
        }

        public bool RequiresMeshUV(UVChannel channel)
        {
            if (channel != UVChannel.uv0)
            {
                return false;
            }

            var uvSlot = FindInputSlot<MaterialSlot>(UVInput);
            if (uvSlot == null)
                return true;

            var edges = owner.GetEdges(uvSlot.slotReference).ToList();
            return edges.Count == 0;
        }
    }
}
