using UnityEngine;
using UnityEditor.Graphing;
using System.Linq;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [FormerName("UnityEditor.ShaderGraph.SampleCubemapProjection")]
    [Title("Input", "Texture", "Sample Cubemap Projection Node")]
    class SampleCubemapProjectionNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequirePosition
    {
        public const int    OutputSlotId                = 0;
        public const int    CubemapInputId              = 1;
        public const int    WorldPosInputId             = 2;
        public const int    OriginOffsetId              = 3;
        public const int    OffsetPosInputId            = 4;
        public const int    ProjIntensityInputId        = 5;
        public const int    SamplerInputId              = 6;
        public const int    LODInputId                  = 7;
               const string kOutputSlotName             = "Out";
               const string kCubemapInputName           = "Cubemap";
               const string kWorldPosInputName          = "WorldPosition";
               const string kOriginInputName            = "Origin Offset";
               const string kOffsetPosInputName         = "Position Offset";
               const string kProjIntensityInputName     = "Projection Intensity";
               const string kSamplerInputName           = "Sampler";
               const string kLODInputName               = "LOD";

        public SampleCubemapProjectionNode()
        {
            name = "Sample Cubemap Projection";
            UpdateNodeAfterDeserialization();
        }

        string GetFunctionName()
        {
            return $"Unity_SampleCubemapProjection";
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            AddSlot(new CubemapInputMaterialSlot(CubemapInputId, kCubemapInputName, kCubemapInputName));
            AddSlot(new PositionMaterialSlot(WorldPosInputId, kWorldPosInputName, kWorldPosInputName, CoordinateSpace.AbsoluteWorld));
            AddSlot(new Vector3MaterialSlot(OriginOffsetId, kOriginInputName, kOriginInputName, SlotType.Input, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(OffsetPosInputId, kOffsetPosInputName, kOffsetPosInputName, SlotType.Input, Vector3.zero));
            AddSlot(new Vector1MaterialSlot(ProjIntensityInputId, kProjIntensityInputName, kProjIntensityInputName, SlotType.Input, 1.0f));
            AddSlot(new SamplerStateMaterialSlot(SamplerInputId, kSamplerInputName, kSamplerInputName, SlotType.Input));
            AddSlot(new Vector1MaterialSlot(LODInputId, kLODInputName, kLODInputName, SlotType.Input, 0));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId, CubemapInputId, WorldPosInputId, OriginOffsetId, OffsetPosInputId, ProjIntensityInputId, SamplerInputId, LODInputId });
        }

        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability)
        {
            return NeededCoordinateSpace.AbsoluteWorld;
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.ProvideFunction(GetFunctionName(), s =>
                {
                    s.AppendLine("$precision3 Unity_SampleCubemapProjection($precision3 worldPos, $precision3 originOffset, $precision3 positionOffset, $precision projIntensity)");
                    using (s.BlockScope())
                    {
                        s.AppendLines(@"
$precision3 delta = worldPos - (SHADERGRAPH_OBJECT_POSITION + originOffset);
delta += projIntensity*SafeNormalize(originOffset - positionOffset);

return delta;");
                    }
                });
        }

        // Node generations
        public virtual void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            //Sampler input slot

            var samplerSlot = FindInputSlot<MaterialSlot>(SamplerInputId);
            var edgesSampler = owner.GetEdges(samplerSlot.slotReference);
            var id = GetSlotValue(CubemapInputId, generationMode);
            string result = string.Format("" +
                "$precision4 {0} = SAMPLE_TEXTURECUBE_LOD({1}, {2}, Unity_SampleCubemapProjection({3}, {4}, {5}, {6}), {7});"
                    , GetVariableNameForSlot(OutputSlotId)
                    , id
                    , edgesSampler.Any() ? GetSlotValue(SamplerInputId, generationMode) : "sampler" + id
                    , GetSlotValue(WorldPosInputId, generationMode)
                    , GetSlotValue(OriginOffsetId, generationMode)
                    , GetSlotValue(OffsetPosInputId, generationMode)
                    , GetSlotValue(ProjIntensityInputId, generationMode)
                    , GetSlotValue(LODInputId, generationMode));

            sb.AppendLine(result);
        }
    }
}
