using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Texture", "Sample Cubemap")]
    class SampleRawCubemapNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireNormal
    {
        public const int OutputSlotId = 0;
        public const int CubemapInputId = 1;
        public const int NormalInputId = 2;
        public const int SamplerInputId = 3;
        public const int LODInputId = 4;
        const string kOutputSlotName = "Out";
        const string kCubemapInputName = "Cube";
        const string kNormalInputName = "Dir";
        const string kSamplerInputName = "Sampler";
        const string kLODInputName = "LOD";

        public override bool hasPreview { get { return true; } }

        public SampleRawCubemapNode()
        {
            name = "Sample Cubemap";
            UpdateNodeAfterDeserialization();
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            AddSlot(new CubemapInputMaterialSlot(CubemapInputId, kCubemapInputName, kCubemapInputName));
            AddSlot(new NormalMaterialSlot(NormalInputId, kNormalInputName, kNormalInputName, CoordinateSpace.World));
            AddSlot(new SamplerStateMaterialSlot(SamplerInputId, kSamplerInputName, kSamplerInputName, SlotType.Input));
            AddSlot(new Vector1MaterialSlot(LODInputId, kLODInputName, kLODInputName, SlotType.Input, 0));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId, CubemapInputId, NormalInputId, SamplerInputId, LODInputId });
        }

        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }

        // Node generations
        public virtual void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            //Sampler input slot
            var samplerSlot = FindInputSlot<MaterialSlot>(SamplerInputId);
            var edgesSampler = owner.GetEdges(samplerSlot.slotReference);

            var id = GetSlotValue(CubemapInputId, generationMode);
            string result = string.Format("$precision4 {0} = SAMPLE_TEXTURECUBE_LOD({1}, {2}, {3}, {4});"
                    , GetVariableNameForSlot(OutputSlotId)
                    , id
                    , edgesSampler.Any() ? GetSlotValue(SamplerInputId, generationMode) : "sampler" + id
                    , GetSlotValue(NormalInputId, generationMode)
                    , GetSlotValue(LODInputId, generationMode));

            sb.AppendLine(result);
        }

        public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability)
        {
            var normalSlot = FindInputSlot<MaterialSlot>(NormalInputId);
            var edgesNormal = owner.GetEdges(normalSlot.slotReference);
            if (!edgesNormal.Any())
                return CoordinateSpace.Object.ToNeededCoordinateSpace();
            else
                return NeededCoordinateSpace.None;
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            name = "Sample Cubemap";
        }
    }
}
