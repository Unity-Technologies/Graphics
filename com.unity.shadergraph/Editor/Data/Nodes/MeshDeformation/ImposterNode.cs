using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Drawing.Controls;


namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Mesh Deformation", "Imposter")]
    class ImposterNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireTransform, IMayRequirePosition, IMayRequireNormal, IMayRequireTangent, IGeneratesFunction
    {
        private const int PositionOutputSlotId = 0;
        private const int NormalOutputSlotId = 1;
        private const int ColorOutputSlotId = 2;
        private const int AlphaOutputSlotId = 5;

        private const int PositionSlotId = 3;
        private const int UVSlotId = 4;
        private const int NormalSlotId = 6;
        private const int TangentSlotId = 7;
        private const int BiTangentSlotId = 8;
        private const int SamplerSlotId = 9;

        public const string kPositionOutputSlotName = "Out Position";
        public const string kNormalOutputSlotName = "Out Normal";
        public const string kColorOutputSlotName = "Color";
        public const string kAlphaOutputSlotName = "Alpha";

        public const string kPositionSlotName = "In Position";
        public const string kSlotUVName = "UV";
        public const string kSlotBiTangentName = "BiTangent";
        public const string kSlotNormalName = "Normal";
        public const string kSlotTangentnName = "Tangent";
        public const string kSlotSamplerName = "Sampler State";

        public ImposterNode()
        {
            name = "Imposter";
            precision = Precision.Single;
            synonyms = new string[] { "" };
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(PositionOutputSlotId, kPositionOutputSlotName, kPositionOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(NormalOutputSlotId, kNormalOutputSlotName, kNormalOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(ColorOutputSlotId, kColorOutputSlotName, kColorOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector1MaterialSlot(AlphaOutputSlotId, kAlphaOutputSlotName, kAlphaOutputSlotName, SlotType.Output, 0));

            AddSlot(new PositionMaterialSlot(PositionSlotId, kPositionSlotName, kPositionSlotName, CoordinateSpace.Object));
            AddSlot(new NormalMaterialSlot(NormalSlotId, kSlotNormalName, kSlotNormalName, CoordinateSpace.World));
            AddSlot(new TangentMaterialSlot(TangentSlotId, kSlotTangentnName, kSlotTangentnName, CoordinateSpace.World));
            AddSlot(new BitangentMaterialSlot(BiTangentSlotId, kSlotBiTangentName, kSlotBiTangentName, CoordinateSpace.World));
            AddSlot(new UVMaterialSlot(UVSlotId, kSlotUVName, kSlotUVName, UVChannel.UV0));
            AddSlot(new SamplerStateMaterialSlot(SamplerSlotId, kSlotSamplerName, kSlotSamplerName, SlotType.Input));
            RemoveSlotsNameNotMatching(new[] { SamplerSlotId, PositionOutputSlotId, UVSlotId, NormalOutputSlotId, PositionSlotId, TangentSlotId, NormalSlotId, BiTangentSlotId, ColorOutputSlotId, AlphaOutputSlotId });
        }
        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.RequiresIncludePath("Packages/com.unity.shadergraph/ShaderGraphLibrary/Imposter.hlsl");
        }
        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            //string ImposterInclude = @"#include""Assets/External/URPIMP-master/Imposter.hlsl""";

            // get the values into named variables
            var PositionSlotValue = GetSlotValue(PositionSlotId, generationMode);
            var UVSlotValue = GetSlotValue(UVSlotId, generationMode);
            var OutPosition = GetVariableNameForSlot(PositionOutputSlotId);

            var TangentSlotValue = GetSlotValue(TangentSlotId, generationMode);
            var BiTangentSlotValue = GetSlotValue(BiTangentSlotId, generationMode);
            var NormalSlotValue = GetSlotValue(NormalSlotId, generationMode);
            var SamplerSlotValue = GetSlotValue(SamplerSlotId, generationMode);
            var OutColor = GetVariableNameForSlot(ColorOutputSlotId);
            var OutNormal = GetVariableNameForSlot(NormalOutputSlotId);
            var OutAlpha = GetVariableNameForSlot(AlphaOutputSlotId);

            var result = @$"
$precision4 UVGrid;
$precision4 Plane0;
$precision4 Plane1;
$precision4 Plane2;
SamplerState ss;
$precision3 {OutPosition};
$precision3 {OutColor};
$precision3 {OutNormal};
$precision {OutAlpha};

Vertex_half({PositionSlotValue}, {UVSlotValue}, {OutPosition}, UVGrid, Plane0, Plane1, Plane2);
Fragment_half({NormalSlotValue}, {TangentSlotValue}, {BiTangentSlotValue},UVGrid, Plane0, Plane1, Plane2, {SamplerSlotValue}.samplerstate, {OutColor}, {OutNormal}, {OutAlpha});
";
            //sb.AppendLine(ImposterInclude);
            sb.AppendLine(result);
        }

        public NeededTransform[] RequiresTransform(ShaderStageCapability stageCapability = ShaderStageCapability.All) => new[] { NeededTransform.ObjectToWorld };
        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            if (stageCapability == ShaderStageCapability.Vertex || stageCapability == ShaderStageCapability.All)
                return NeededCoordinateSpace.Object;
            else
                return NeededCoordinateSpace.None;
        }
        public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            if (stageCapability == ShaderStageCapability.Vertex || stageCapability == ShaderStageCapability.All)
                return NeededCoordinateSpace.Object;
            else
                return NeededCoordinateSpace.None;
        }

        public NeededCoordinateSpace RequiresTangent(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            if (stageCapability == ShaderStageCapability.Vertex || stageCapability == ShaderStageCapability.All)
                return NeededCoordinateSpace.Object;
            else
                return NeededCoordinateSpace.None;
        }

    }
}
