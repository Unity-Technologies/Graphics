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
        private const int WeightOutputSlotId = 9;

        private const int SamplerSlotId = 12;
        private const int Frame0SlotId = 3;
        private const int Frame1SlotId = 4;
        private const int Frame2SlotId = 5;
        private const int GridSlotId = 6;
        private const int TexelSizeSlotId = 7;
        private const int FramesSlotId = 8;
        private const int DepthTextureSlotId = 10;
        private const int BorderClampSlotId = 11;

        public const string kUV0OutputSlotName = "UV0";
        public const string kUV1OutputSlotName = "UV1";
        public const string kUV2OutputSlotName = "UV2";
        public const string kWeightOutputSlotName = "Weights";

        public const string kGridSlotName = "Grid";
        public const string kFrame0SlotName = "Frame0";
        public const string kFrame1SlotName = "Frame1";
        public const string kFrame2SlotName = "Frame2";
        public const string kTexelSizeSlotName = "Texel Size";
        public const string kSlotFramesName = "Frame Amount";
        public const string kSlotDepthTextureName = "Depth Texture";
        public const string kSlotBorderClampName = "Border";
        public const string kSlotSamplerName = "Sampler State";

        [SerializeField]
        internal bool m_Value = true;

        [ToggleControl("Parallax")]
        public ToggleData value
        {
            get { return new ToggleData(m_Value); }
            set
            {
                if (m_Value == value.isOn)
                    return;
                m_Value = value.isOn;
                Dirty(ModificationScope.Node);
            }
        }

        public ImposterUVNode()
        {
            name = "ImposterUV";
            precision = Precision.Single;
            synonyms = new string[] { "" };
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector2MaterialSlot(UV0OutputSlotId, kUV0OutputSlotName, kUV0OutputSlotName, SlotType.Output, Vector2.zero));
            AddSlot(new Vector2MaterialSlot(UV1OutputSlotId, kUV1OutputSlotName, kUV1OutputSlotName, SlotType.Output, Vector2.zero));
            AddSlot(new Vector2MaterialSlot(UV2OutputSlotId, kUV2OutputSlotName, kUV2OutputSlotName, SlotType.Output, Vector2.zero));
            AddSlot(new Vector4MaterialSlot(WeightOutputSlotId, kWeightOutputSlotName, kWeightOutputSlotName, SlotType.Output, Vector4.zero));

            AddSlot(new Texture2DInputMaterialSlot(DepthTextureSlotId, kSlotDepthTextureName, kSlotDepthTextureName));
            AddSlot(new SamplerStateMaterialSlot(SamplerSlotId, kSlotSamplerName, kSlotSamplerName, SlotType.Input));
            AddSlot(new Vector4MaterialSlot(Frame0SlotId, kFrame0SlotName, kFrame0SlotName, SlotType.Input, Vector4.zero));
            AddSlot(new Vector4MaterialSlot(Frame1SlotId, kFrame1SlotName, kFrame1SlotName, SlotType.Input, Vector4.zero));
            AddSlot(new Vector4MaterialSlot(Frame2SlotId, kFrame2SlotName, kFrame2SlotName, SlotType.Input, Vector4.zero));
            AddSlot(new Vector1MaterialSlot(FramesSlotId, kSlotFramesName, kSlotFramesName, SlotType.Input, 0));
            AddSlot(new Vector1MaterialSlot(TexelSizeSlotId, kTexelSizeSlotName, kTexelSizeSlotName, SlotType.Input, 0));
            AddSlot(new Vector2MaterialSlot(GridSlotId, kGridSlotName, kGridSlotName, SlotType.Input,   Vector2.zero));
            AddSlot(new Vector1MaterialSlot(BorderClampSlotId, kSlotBorderClampName, kSlotBorderClampName, SlotType.Input, 0));

            RemoveSlotsNameNotMatching(new[] { WeightOutputSlotId, GridSlotId, UV0OutputSlotId, UV1OutputSlotId, UV2OutputSlotId, Frame0SlotId, Frame1SlotId, Frame2SlotId, FramesSlotId, TexelSizeSlotId});
        }
        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.RequiresIncludePath("Packages/com.unity.shadergraph/ShaderGraphLibrary/Imposter_saperated.hlsl");
        }
        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var UV0 = GetSlotValue(UV0OutputSlotId, generationMode);
            var UV1 = GetSlotValue(UV1OutputSlotId, generationMode);
            var UV2 = GetSlotValue(UV2OutputSlotId, generationMode);
            var Weights = GetSlotValue(WeightOutputSlotId, generationMode);            
            var Grid = GetSlotValue(GridSlotId, generationMode);
            var Frame0 = GetSlotValue(Frame0SlotId, generationMode);
            var Frame1 = GetSlotValue(Frame1SlotId, generationMode);
            var Frame2 = GetSlotValue(Frame2SlotId, generationMode);
            var Frames = GetSlotValue(FramesSlotId, generationMode);
            var TexelSize = GetSlotValue(TexelSizeSlotId, generationMode);
            var DepthTexture = GetSlotValue(DepthTextureSlotId, generationMode);
            var border = GetSlotValue(BorderClampSlotId, generationMode);
            var SamplerSlotValue = GetSlotValue(SamplerSlotId, generationMode);

            var result = @$"
$precision2 {UV0};
$precision2 {UV1};
$precision2 {UV2};
$precision4 {Weights};
$precision4 texelSize = {DepthTexture}.texelSize;
Texture2D depthTex = {DepthTexture}.tex;
";
            if (m_Value)
            {
                result += $@"ImposterUV_Parallax({Grid},{Frame0},{Frame1}, {Frame2}, {TexelSize}, {Frames},
            {DepthTexture}, {border},{SamplerSlotValue},{UV0},{UV1}, {UV2}, {Weights});";
            }
            else
            {
                result += $@"ImposterUV({Grid},{Frame0},{Frame1}, {Frame2}, {TexelSize}, {Frames},
            {DepthTexture}, {border},{SamplerSlotValue},{UV0},{UV1}, {UV2}, {Weights});";

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
