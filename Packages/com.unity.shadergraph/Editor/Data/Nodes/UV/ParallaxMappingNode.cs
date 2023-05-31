using UnityEngine;
using UnityEditor.Graphing;
using System;
using System.Linq;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Drawing.Controls;


namespace UnityEditor.ShaderGraph
{
    enum Channel
    {
        Red = 0,
        Green = 1,
        Blue = 2,
        Alpha = 3,
    }
    [Title("UV", "Parallax Mapping")]
    class ParallaxMappingNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequireViewDirection, IMayRequireMeshUV
    {
        public ParallaxMappingNode()
        {
            name = "Parallax Mapping";
            synonyms = new string[] { "offset mapping" };
            UpdateNodeAfterDeserialization();
        }

        // Input slots
        private const int kHeightmapSlotId = 1;
        private const string kHeightmapSlotName = "Heightmap";
        private const int kHeightmapSamplerSlotId = 2;
        private const string kHeightmapSamplerSlotName = "HeightmapSampler";
        private const int kAmplitudeSlotId = 3;
        private const string kAmplitudeSlotName = "Amplitude";
        private const int kUVsSlotId = 4;
        private const string kUVsSlotName = "UVs";


        // Output slots
        private const int kParallaxUVsOutputSlotId = 0;
        private const string kParallaxUVsOutputSlotName = "ParallaxUVs";

        public override bool hasPreview { get { return false; } }

        [SerializeField]
        private Channel m_Channel = Channel.Green;

        [EnumControl("Heightmap Sample Channel")]
        public Channel channel
        {
            get { return m_Channel; }
            set
            {
                if (m_Channel == value)
                    return;

                m_Channel = value;
                Dirty(ModificationScope.Graph);
            }
        }
        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Texture2DInputMaterialSlot(kHeightmapSlotId, kHeightmapSlotName, kHeightmapSlotName, ShaderStageCapability.Fragment));
            AddSlot(new SamplerStateMaterialSlot(kHeightmapSamplerSlotId, kHeightmapSamplerSlotName, kHeightmapSamplerSlotName, SlotType.Input));
            AddSlot(new Vector1MaterialSlot(kAmplitudeSlotId, kAmplitudeSlotName, kAmplitudeSlotName, SlotType.Input, 1, ShaderStageCapability.Fragment));
            AddSlot(new UVMaterialSlot(kUVsSlotId, kUVsSlotName, kUVsSlotName, UVChannel.UV0, ShaderStageCapability.Fragment));

            AddSlot(new Vector2MaterialSlot(kParallaxUVsOutputSlotId, kParallaxUVsOutputSlotName, kParallaxUVsOutputSlotName, SlotType.Output, Vector2.zero, ShaderStageCapability.Fragment));
            RemoveSlotsNameNotMatching(new[]
            {
                kParallaxUVsOutputSlotId,
                kHeightmapSlotId,
                kHeightmapSamplerSlotId,
                kAmplitudeSlotId,
                kUVsSlotId,
            });
        }

        public override void Setup()
        {
            base.Setup();
            var textureSlot = FindInputSlot<Texture2DInputMaterialSlot>(kHeightmapSlotId);
            textureSlot.defaultType = Texture2DShaderProperty.DefaultType.Black;
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.RequiresIncludePath("Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl");
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var heightmap = GetSlotValue(kHeightmapSlotId, generationMode);
            var samplerSlot = FindInputSlot<MaterialSlot>(kHeightmapSamplerSlotId);
            var edgesSampler = owner.GetEdges(samplerSlot.slotReference);
            var amplitude = GetSlotValue(kAmplitudeSlotId, generationMode);
            var uvs = GetSlotValue(kUVsSlotId, generationMode);

            sb.AppendLines(String.Format(@"$precision2 {5} = {0}.GetTransformedUV({4}) + ParallaxMappingChannel(TEXTURE2D_ARGS({0}.tex, {1}.samplerstate), IN.{2}, {3} * 0.01, {0}.GetTransformedUV({4}), {6});",
                heightmap,
                edgesSampler.Any() ? GetSlotValue(kHeightmapSamplerSlotId, generationMode) : heightmap,
                CoordinateSpace.Tangent.ToVariableName(InterpolatorType.ViewDirection),
                amplitude, // cm in the interface so we multiply by 0.01 in the shader to convert in meter
                uvs,
                GetSlotValue(kParallaxUVsOutputSlotId, generationMode),
                (int)channel
            ));
        }

        public NeededCoordinateSpace RequiresViewDirection(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            return NeededCoordinateSpace.Tangent;
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            using (var tempSlots = PooledList<MaterialSlot>.Get())
            {
                GetInputSlots(tempSlots);
                var result = false;
                foreach (var slot in tempSlots)
                {
                    if (slot.RequiresMeshUV(channel))
                    {
                        result = true;
                        break;
                    }
                }

                tempSlots.Clear();
                return result;
            }
        }
    }
}
