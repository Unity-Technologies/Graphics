using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [FormerName("UnityEditor.ShaderGraph.CubemapNode")]
    [Title("Input", "Texture", "Sample Cubemap")]
    class SampleCubemapNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireViewDirection, IMayRequireNormal
    {
        public const int OutputSlotId = 0;
        public const int CubemapInputId = 1;
        public const int ViewDirInputId = 2;
        public const int NormalInputId = 3;
        public const int SamplerInputId = 5;
        public const int LODInputId = 4;
        const string kOutputSlotName = "Out";
        const string kCubemapInputName = "Cube";
        const string kViewDirInputName = "ViewDir";
        const string kNormalInputName = "Normal";
        const string kSamplerInputName = "Sampler";
        const string kLODInputName = "LOD";

        public override bool hasPreview { get { return true; } }

        public SampleCubemapNode()
        {
            name = "Sample Cubemap";
            UpdateNodeAfterDeserialization();
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            AddSlot(new CubemapInputMaterialSlot(CubemapInputId, kCubemapInputName, kCubemapInputName));
            AddSlot(new ViewDirectionMaterialSlot(ViewDirInputId, kViewDirInputName, kViewDirInputName, CoordinateSpace.Object));
            AddSlot(new NormalMaterialSlot(NormalInputId, kNormalInputName, kNormalInputName, CoordinateSpace.Object));
            AddSlot(new SamplerStateMaterialSlot(SamplerInputId, kSamplerInputName, kSamplerInputName, SlotType.Input));
            AddSlot(new Vector1MaterialSlot(LODInputId, kLODInputName, kLODInputName, SlotType.Input, 0));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId, CubemapInputId, ViewDirInputId, NormalInputId, SamplerInputId, LODInputId });
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
            string result = string.Format("$precision4 {0} = SAMPLE_TEXTURECUBE_LOD({1}, {2}, reflect(-{3}, {4}), {5});"
                    , GetVariableNameForSlot(OutputSlotId)
                    , id
                    , edgesSampler.Any() ? GetSlotValue(SamplerInputId, generationMode) : "sampler" + id
                    , GetSlotValue(ViewDirInputId, generationMode)
                    , GetSlotValue(NormalInputId, generationMode)
                    , GetSlotValue(LODInputId, generationMode));

            sb.AppendLine(result);
        }

        public NeededCoordinateSpace RequiresViewDirection(ShaderStageCapability stageCapability)
        {
            var viewDirSlot = FindInputSlot<MaterialSlot>(ViewDirInputId);
            var edgesViewDir = owner.GetEdges(viewDirSlot.slotReference);
            if (!edgesViewDir.Any())
                return CoordinateSpace.Object.ToNeededCoordinateSpace();
            else
                return NeededCoordinateSpace.None;
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
    }
}
