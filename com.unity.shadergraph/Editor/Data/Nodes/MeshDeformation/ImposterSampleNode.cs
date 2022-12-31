using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Drawing.Controls;


namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Mesh Deformation", "ImposterSample")]
    class ImposterSampleNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireTransform, IMayRequirePosition, IMayRequireNormal, IMayRequireTangent, IGeneratesFunction
    {
        private const int RGBAOutputSlotId = 0;

        private const int SamplerSlotId = 1;
        private const int Frame0SlotId = 2;
        private const int Frame1SlotId = 3;
        private const int Frame2SlotId = 4;
        private const int WeightsSlotId = 6;
        private const int TextureSlotId = 5;

        public const string kRGBAOutputSlotName = "RGBA";

        public const string kWeightsSlotName = "Weights";
        public const string kFrame0SlotName = "UV0";
        public const string kFrame1SlotName = "UV1";
        public const string kFrame2SlotName = "UV2";
        public const string kSlotTextureName = "Texture";
        public const string kSlotSamplerName = "Sampler State";


        public ImposterSampleNode()
        {
            name = "ImposterSample";
            precision = Precision.Single;
            synonyms = new string[] { "" };
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(RGBAOutputSlotId, kRGBAOutputSlotName, kRGBAOutputSlotName, SlotType.Output, Vector4.zero));

            AddSlot(new Texture2DInputMaterialSlot(TextureSlotId, kSlotTextureName, kSlotTextureName));
            AddSlot(new SamplerStateMaterialSlot(SamplerSlotId, kSlotSamplerName, kSlotSamplerName, SlotType.Input));
            AddSlot(new Vector2MaterialSlot(Frame0SlotId, kFrame0SlotName, kFrame0SlotName, SlotType.Input, Vector4.zero));
            AddSlot(new Vector2MaterialSlot(Frame1SlotId, kFrame1SlotName, kFrame1SlotName, SlotType.Input, Vector4.zero));
            AddSlot(new Vector2MaterialSlot(Frame2SlotId, kFrame2SlotName, kFrame2SlotName, SlotType.Input, Vector4.zero));
            AddSlot(new Vector3MaterialSlot(WeightsSlotId, kWeightsSlotName, kWeightsSlotName, SlotType.Input, Vector3.zero));

            RemoveSlotsNameNotMatching(new[] { RGBAOutputSlotId, TextureSlotId, SamplerSlotId, WeightsSlotId, Frame0SlotId, Frame1SlotId, Frame2SlotId});
        }
        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.RequiresIncludePath("Packages/com.unity.shadergraph/ShaderGraphLibrary/Imposter_saperated.hlsl");
        }
        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var RGBA = GetSlotValue(RGBAOutputSlotId, generationMode);
            var Weights = GetSlotValue(WeightsSlotId, generationMode);            
            var Frame0 = GetSlotValue(Frame0SlotId, generationMode);
            var Frame1 = GetSlotValue(Frame1SlotId, generationMode);
            var Frame2 = GetSlotValue(Frame2SlotId, generationMode);
            var Texture = GetSlotValue(TextureSlotId, generationMode);
            var ss = GetSlotValue(SamplerSlotId, generationMode);

            var result = @$"
$precision4 ddxy = $precision4(ddx(imp.uv), ddy(imp.uv));
{RGBA} = ImposterBlendWeights({Texture}.tex, {ss}, {Frame0}, {Frame1}, {Frame2}, {Weights},ddxy);
";
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
