using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Drawing.Controls;


namespace UnityEditor.ShaderGraph
{
    enum SampleType
    {
        OneFrame,
        ThreeFrame
    };

    [Title("Input", "Mesh Deformation", "ImposterSample")]
    class ImposterSampleNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireTransform, IMayRequirePosition, IMayRequireNormal, IMayRequireTangent, IGeneratesFunction, IMayRequireViewDirection
    {
        private const int RGBAOutputSlotId = 0;

        private const int SamplerSlotId = 1;
        private const int UV0SlotId = 2;
        private const int UV1SlotId = 3;
        private const int UV2SlotId = 4;
        private const int ImposterFramesSlotId = 6;
        private const int TextureSlotId = 5;
        private const int ParallaxCheckSlotId = 7;
        private const int ImposterBorderClampSlotId = 8;
        private const int UVGridSlotId = 9;
        private const int ViewDirSlotId = 10;
        private const int HeightMapChannelSlotId = 11;

        public const string kRGBAOutputSlotName = "RGBA";

        public const string kSlotSamplerName = "Sampler State";
        public const string kUV0SlotName = "UV0";
        public const string kUV1SlotName = "UV1";
        public const string kUV2SlotName = "UV2";
        public const string kImposterFramesName = "Imposter Frames";
        public const string kSlotTextureName = "Texture";
        public const string kParallaxCheckName = "Parallax";
        public const string kImposterBorderClampName = "Imposter Clip";
        public const string kUVGridName = "UVGrid";
        public const string kViewDirName = "View Dir";
        public const string kHeightMapChannelName = "Height Map Channel";


        public ImposterSampleNode()
        {
            name = "ImposterSample";
            precision = Precision.Single;
            synonyms = new string[] { "" };
            UpdateNodeAfterDeserialization();
        }


        [SerializeField]
        private SampleType m_SampleType = SampleType.ThreeFrame;

        [EnumControl("Sample Type")]
        public SampleType sampleType
        {
            get { return m_SampleType; }
            set
            {
                if (m_SampleType == value)
                    return;

                m_SampleType = value;
                Dirty(ModificationScope.Graph);

                ValidateNode();
            }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(RGBAOutputSlotId, kRGBAOutputSlotName, kRGBAOutputSlotName, SlotType.Output, Vector4.zero));

            AddSlot(new Texture2DInputMaterialSlot(TextureSlotId, kSlotTextureName, kSlotTextureName));
            AddSlot(new SamplerStateMaterialSlot(SamplerSlotId, kSlotSamplerName, kSlotSamplerName, SlotType.Input));
            AddSlot(new Vector4MaterialSlot(UV0SlotId, kUV0SlotName, kUV0SlotName, SlotType.Input, Vector4.zero));
            AddSlot(new Vector4MaterialSlot(UV1SlotId, kUV1SlotName, kUV1SlotName, SlotType.Input, Vector4.zero));
            AddSlot(new Vector4MaterialSlot(UV2SlotId, kUV2SlotName, kUV2SlotName, SlotType.Input, Vector4.zero));
            AddSlot(new Vector4MaterialSlot(UVGridSlotId, kUVGridName, kUVGridName, SlotType.Input, Vector4.zero));
            AddSlot(new Vector1MaterialSlot(ImposterFramesSlotId, kImposterFramesName, kImposterFramesName, SlotType.Input, 16));
            AddSlot(new Vector1MaterialSlot(ImposterBorderClampSlotId, kImposterBorderClampName, kImposterBorderClampName, SlotType.Input, 1));
            AddSlot(new Vector1MaterialSlot(ParallaxCheckSlotId, kParallaxCheckName, kParallaxCheckName, SlotType.Input, 0));
            AddSlot(new Vector1MaterialSlot(HeightMapChannelSlotId, kHeightMapChannelName, kHeightMapChannelName, SlotType.Input, 3));
            AddSlot(new ViewDirectionMaterialSlot(ViewDirSlotId, kViewDirName, kViewDirName, CoordinateSpace.Tangent));


            RemoveSlotsNameNotMatching(new[] { HeightMapChannelSlotId, ViewDirSlotId, ParallaxCheckSlotId, RGBAOutputSlotId, TextureSlotId, SamplerSlotId, UV0SlotId, UV1SlotId, UV2SlotId, ImposterFramesSlotId, ImposterBorderClampSlotId, UVGridSlotId });
        }
        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.RequiresIncludePath("Packages/com.unity.shadergraph/ShaderGraphLibrary/Imposter.hlsl");
        }
        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var RGBA = GetVariableNameForSlot(RGBAOutputSlotId);
            var ImposterFrames = GetSlotValue(ImposterFramesSlotId, generationMode);
            var UV0 = GetSlotValue(UV0SlotId, generationMode);
            var UV1 = GetSlotValue(UV1SlotId, generationMode);
            var UV2 = GetSlotValue(UV2SlotId, generationMode);
            var Texture = GetSlotValue(TextureSlotId, generationMode);
            var ss = GetSlotValue(SamplerSlotId, generationMode);
            var BorderClamp = GetSlotValue(ImposterBorderClampSlotId, generationMode);
            var UVGrid = GetSlotValue(UVGridSlotId, generationMode);
            var parallax = GetSlotValue(ParallaxCheckSlotId, generationMode);
            var viewDir = GetSlotValue(ViewDirSlotId, generationMode);
            var channel = GetSlotValue(HeightMapChannelSlotId, generationMode);

            var result = @$"$precision4 {RGBA};";
            if (m_SampleType == SampleType.ThreeFrame)
            {
                result += $@"ImposterSample({channel},{viewDir}, {parallax}, {ImposterFrames}, {Texture}.tex, {Texture}.texelSize, {BorderClamp},
                                    {UVGrid}, {UV0}, {UV1}, {UV2}, {ss}.samplerstate, {RGBA});";
            }
            else
            {
                result += $@"ImposterSample_oneFrame({channel},{viewDir},{parallax}, {ImposterFrames}, {Texture}.tex, {Texture}.texelSize, {BorderClamp},
                                    {UVGrid}, {UV0}, {ss}.samplerstate, {RGBA});";
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
        public NeededCoordinateSpace RequiresViewDirection(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            return NeededCoordinateSpace.Tangent;
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
