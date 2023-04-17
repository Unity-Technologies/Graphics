using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Drawing.Controls;


namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Mesh Deformation", "ImposterUV")]
    class ImposterUVNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireTransform, IMayRequirePosition, IMayRequireNormal, IMayRequireTangent, IGeneratesFunction
    {
        private const int UV0OutputSlotId = 0;
        private const int UV1OutputSlotId = 1;
        private const int UV2OutputSlotId = 2;
        private const int ImposterPosOutputSlotId = 3;
        private const int UVGridOutputSlotId = 4;

        private const int PositionInputSlotId = 5;
        private const int UVInputSlotId = 6;
        private const int ImposterFramesInputSlotId = 7;
        private const int ImposterOffsetInputSlotId = 8;
        private const int ImposterSizeInputSlotId = 9;
        private const int ImposterHemiCheckInputSlotId = 10;

        public const string kUV0OutputSlotName = "UV0";
        public const string kUV1OutputSlotName = "UV1";
        public const string kUV2OutputSlotName = "UV2";
        public const string kImposterPosOutputSlotName = "Imposter Position";
        public const string kUVGridOutputSlotName = "UV Grid";

        public const string kPositionInputSlotName = "Position";
        public const string kUVInputSlotName = "UV";
        public const string kImposterFramesInputSlotName = "Imposter Frames";
        public const string kImposterOffsetInputSlotName = "Imposter Offset";
        public const string kImposterSizeInputSlotName = "Imposter Size";
        public const string kImposterHemiCheckInputSlotName = "Hemi Sphere";


        public ImposterUVNode()
        {
            name = "ImposterUV";
            precision = Precision.Single;
            synonyms = new string[] { "" };
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(ImposterPosOutputSlotId, kImposterPosOutputSlotName, kImposterPosOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector4MaterialSlot(UV0OutputSlotId, kUV0OutputSlotName, kUV0OutputSlotName, SlotType.Output, Vector4.zero));
            AddSlot(new Vector4MaterialSlot(UV1OutputSlotId, kUV1OutputSlotName, kUV1OutputSlotName, SlotType.Output, Vector4.zero));
            AddSlot(new Vector4MaterialSlot(UV2OutputSlotId, kUV2OutputSlotName, kUV2OutputSlotName, SlotType.Output, Vector4.zero));
            AddSlot(new Vector4MaterialSlot(UVGridOutputSlotId, kUVGridOutputSlotName, kUVGridOutputSlotName, SlotType.Output, Vector4.zero));

            AddSlot(new Vector3MaterialSlot(PositionInputSlotId, kPositionInputSlotName, kPositionInputSlotName, SlotType.Input, Vector3.zero));
            AddSlot(new Vector4MaterialSlot(UVInputSlotId, kUVInputSlotName, kUVInputSlotName, SlotType.Input, Vector4.zero));
            AddSlot(new Vector1MaterialSlot(ImposterFramesInputSlotId, kImposterFramesInputSlotName, kImposterFramesInputSlotName, SlotType.Input, 16));
            AddSlot(new Vector3MaterialSlot(ImposterOffsetInputSlotId, kImposterOffsetInputSlotName, kImposterOffsetInputSlotName, SlotType.Input, Vector3.zero));
            AddSlot(new Vector1MaterialSlot(ImposterSizeInputSlotId, kImposterSizeInputSlotName, kImposterSizeInputSlotName, SlotType.Input, 1));
            AddSlot(new BooleanMaterialSlot(ImposterHemiCheckInputSlotId, kImposterHemiCheckInputSlotName, kImposterHemiCheckInputSlotName, SlotType.Input, false));

            RemoveSlotsNameNotMatching(new[] { ImposterHemiCheckInputSlotId, ImposterPosOutputSlotId, UV0OutputSlotId, UV1OutputSlotId, UV2OutputSlotId, UVGridOutputSlotId, PositionInputSlotId, UVInputSlotId, ImposterFramesInputSlotId, ImposterOffsetInputSlotId, ImposterSizeInputSlotId });
        }
        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.RequiresIncludePath("Packages/com.unity.shadergraph/ShaderGraphLibrary/Imposter_2Nodes.hlsl");
        }
        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var UV0 = GetVariableNameForSlot(UV0OutputSlotId);
            var UV1 = GetVariableNameForSlot(UV1OutputSlotId);
            var UV2 = GetVariableNameForSlot(UV2OutputSlotId);
            var outPos = GetVariableNameForSlot(ImposterPosOutputSlotId);
            var outUVGrid = GetVariableNameForSlot(UVGridOutputSlotId);

            var inPos = GetSlotValue(PositionInputSlotId, generationMode);
            var inUV = GetSlotValue(UVInputSlotId, generationMode);
            var imposterFrame = GetSlotValue(ImposterFramesInputSlotId, generationMode);
            var imposterOffset = GetSlotValue(ImposterOffsetInputSlotId, generationMode);
            var imposterSize = GetSlotValue(ImposterSizeInputSlotId, generationMode);
            var hemiCheck = GetSlotValue(ImposterHemiCheckInputSlotId, generationMode);

            var result = @$"
$precision4 {UV0};
$precision4 {UV1};
$precision4 {UV2};
$precision3 {outPos};
$precision4 {outUVGrid};
";


            //TODO: change it to call ImposterUV_Parallax
            result += $@"ImposterUV({inPos}, {inUV}, {imposterFrame}, {imposterOffset}, {imposterSize}, {hemiCheck},
                        {outPos}, {outUVGrid}, {UV0}, {UV1}, {UV2});";

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
