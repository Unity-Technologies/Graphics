using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Drawing.Controls;


namespace UnityEditor.ShaderGraph
{
    enum UVMode
    {
        Octahendron,
        HemiOctahendron
    }
    [Title("Input", "Mesh Deformation", "ImposterData")]
    class ImposterDataNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireTransform, IMayRequirePosition, IMayRequireNormal, IMayRequireTangent, IGeneratesFunction
    {
        private const int PositionOutputSlotId = 0;
        private const int UVOutputSlotId = 1;
        private const int GridOutputSlotId = 2;
        private const int Frame0OutputSlotId = 3;
        private const int Frame1OutputSlotId = 4;
        private const int Frame2OutputSlotId = 5;

        private const int PositionSlotId = 6;
        private const int UVSlotId = 7;
        private const int FramesSlotId = 8;
        private const int OffsetSlotId = 9;
        private const int SizeSlotId = 10;
        private const int HemiCheckSlotId = 11;

        public const string kPositionOutputSlotName = "Out Position";
        public const string kUVOutputSlotName = "Out UV";
        public const string kGridOutputSlotName = "Grid";
        public const string kFrame0OutputSlotName = "Frame0";
        public const string kFrame1OutputSlotName = "Frame1";
        public const string kFrame2OutputSlotName = "Frame2";

        public const string kPositionSlotName = "In Position";
        public const string kSlotUVName = "UV";
        public const string kSlotFramesName = "Frame Amount";
        public const string kSlotOffsetName = "Offset";
        public const string kSlotSizeName = "Size";
        public const string kSlotHemiCheckName = "HemiCheck";

        public ImposterDataNode()
        {
            name = "ImposterData";
            precision = Precision.Single;
            synonyms = new string[] { "" };
            UpdateNodeAfterDeserialization();
        }

        [SerializeField]
        private UVMode m_UVMode = UVMode.Octahendron;

        [EnumControl("UV Mode")]
        public UVMode type
        {
            get { return m_UVMode; }
            set
            {
                if (m_UVMode == value)
                    return;

                m_UVMode = value;
                Dirty(ModificationScope.Graph);
            }
        }
        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(PositionOutputSlotId, kPositionOutputSlotName, kPositionOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector2MaterialSlot(UVOutputSlotId, kUVOutputSlotName, kUVOutputSlotName, SlotType.Output, Vector2.zero));
            AddSlot(new Vector2MaterialSlot(GridOutputSlotId, kGridOutputSlotName, kGridOutputSlotName, SlotType.Output, Vector2.zero));
            AddSlot(new Vector4MaterialSlot(Frame0OutputSlotId, kFrame0OutputSlotName, kFrame0OutputSlotName, SlotType.Output, Vector4.zero));
            AddSlot(new Vector4MaterialSlot(Frame1OutputSlotId, kFrame1OutputSlotName, kFrame1OutputSlotName, SlotType.Output, Vector4.zero));
            AddSlot(new Vector4MaterialSlot(Frame2OutputSlotId, kFrame2OutputSlotName, kFrame2OutputSlotName, SlotType.Output, Vector4.zero));
            
            AddSlot(new PositionMaterialSlot(PositionSlotId, kPositionSlotName, kPositionSlotName, CoordinateSpace.Object));
            AddSlot(new UVMaterialSlot(UVSlotId, kSlotUVName, kSlotUVName, UVChannel.UV0));
            AddSlot(new Vector1MaterialSlot(FramesSlotId, kSlotFramesName, kSlotFramesName, SlotType.Input, 0));
            AddSlot(new Vector1MaterialSlot(OffsetSlotId, kSlotOffsetName, kSlotOffsetName, SlotType.Input, 0));
            AddSlot(new Vector1MaterialSlot(SizeSlotId, kSlotSizeName, kSlotSizeName, SlotType.Input, 0));
            AddSlot(new BooleanMaterialSlot(HemiCheckSlotId, kSlotHemiCheckName, kSlotHemiCheckName, SlotType.Input, false));

            RemoveSlotsNameNotMatching(new[] { FramesSlotId, OffsetSlotId, SizeSlotId, PositionOutputSlotId, UVOutputSlotId, GridOutputSlotId, Frame0OutputSlotId, Frame1OutputSlotId, Frame2OutputSlotId, PositionSlotId, UVSlotId });
        }
        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.RequiresIncludePath("Packages/com.unity.shadergraph/ShaderGraphLibrary/Imposter_saperated.hlsl");
        }
        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var PositionSlotValue = GetSlotValue(PositionSlotId, generationMode);
            var UVSlotValue = GetSlotValue(UVSlotId, generationMode);
            var FramesSlotValue = GetSlotValue(FramesSlotId, generationMode);
            var OffsetSlotValue = GetSlotValue(OffsetSlotId, generationMode);
            var SizeSlotValue = GetSlotValue(SizeSlotId, generationMode);
            var HemiCheck = GetSlotValue(HemiCheckSlotId, generationMode);

            var OutPosition = GetVariableNameForSlot(PositionOutputSlotId);
            var OutUV = GetSlotValue(UVOutputSlotId, generationMode);
            var Frame0 = GetSlotValue(Frame0OutputSlotId, generationMode);
            var Frame1 = GetSlotValue(Frame1OutputSlotId, generationMode);
            var Frame2 = GetSlotValue(Frame2OutputSlotId, generationMode);
            var Grid = GetSlotValue(GridOutputSlotId, generationMode);

            var result = @$"
ImposterData imp;
imp.vertex = {PositionSlotValue};
imp.uv = {UVSlotValue}.xy;
ImposterVertex(imp, {FramesSlotValue}, {OffsetSlotValue}, {SizeSlotValue}, {HemiCheck});
$precision3 {OutPosition} = imp.vertex;
$precision2 {OutUV} = imp.uv;
$precision2 {Grid} = imp.grid;
$precision4 {Frame0} = imp.frame0;
$precision4 {Frame1} = imp.frame1;
$precision4 {Frame2} = imp.frame2;
";

            if (m_UVMode == UVMode.HemiOctahendron)
            {
                result += $@"imp.hemiCheck = true;";
            }
            else
            {
                result += $@"imp.hemiCheck = false;";

            }
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
