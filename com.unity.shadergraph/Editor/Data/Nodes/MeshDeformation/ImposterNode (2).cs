using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Drawing.Controls;


namespace UnityEditor.ShaderGraph
{
    enum SampleMode
    {
        OneFrame,
        ThreeFrames
    }
    [Title("Input", "Mesh Deformation", "Imposter2")]
    class Imposter2Node : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireTransform, IMayRequirePosition, IMayRequireNormal, IMayRequireTangent, IGeneratesFunction
    {
        private const int PositionOutputSlotId = 0;
        //private const int NormalOutputSlotId = 1;
        private const int ColorOutputSlotId = 2;
        //private const int AlphaOutputSlotId = 5;

        private const int PositionSlotId = 3;
        private const int UVSlotId = 4;
        private const int SamplerSlotId = 9;
        private const int FrameSlotId = 10;
        private const int TextureAlbedoSlotId = 11;
        private const int TextureNormalSlotId = 12;
        private const int LODSlotId = 15;

        public const string kPositionOutputSlotName = "Imposter Position";
        //public const string kNormalOutputSlotName = "Out Normal";
        public const string kColorOutputSlotName = "RGBA";
        //public const string kAlphaOutputSlotName = "Alpha";

        public const string kPositionSlotName = "Position";
        public const string kSlotUVName = "UV";
        public const string kSlotSamplerName = "Sampler State";
        public const string kSlotFrameName = "Frame";
        public const string kSlotTextureAlbedoName = "Texture";
        public const string kSlotTextureNormalName = "Depth";
        //public const string kSlotTextureSpecularName = "Specular & Smoothness";
        //public const string kSlotTextureEmissionName = "Emission & Occlusion";
        public const string kSlotLODName = "LOD";

        public Imposter2Node()
        {
            name = "Imposter2";
            precision = Precision.Single;
            synonyms = new string[] { "" };
            UpdateNodeAfterDeserialization();
        }
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

        [SerializeField]
        private SampleMode m_SampleMode = SampleMode.ThreeFrames;

        [EnumControl("Sample Mode")]
        public SampleMode type
        {
            get { return m_SampleMode; }
            set
            {
                if (m_SampleMode == value)
                    return;

                m_SampleMode = value;
                Dirty(ModificationScope.Graph);
            }
        }
        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(PositionOutputSlotId, kPositionOutputSlotName, kPositionOutputSlotName, SlotType.Output, Vector3.zero));
            //AddSlot(new Vector3MaterialSlot(NormalOutputSlotId, kNormalOutputSlotName, kNormalOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector4MaterialSlot(ColorOutputSlotId, kColorOutputSlotName, kColorOutputSlotName, SlotType.Output, Vector3.zero));
            //AddSlot(new Vector1MaterialSlot(AlphaOutputSlotId, kAlphaOutputSlotName, kAlphaOutputSlotName, SlotType.Output, 0));



            AddSlot(new PositionMaterialSlot(PositionSlotId, kPositionSlotName, kPositionSlotName, CoordinateSpace.Object));
            AddSlot(new UVMaterialSlot(UVSlotId, kSlotUVName, kSlotUVName, UVChannel.UV0));
            AddSlot(new SamplerStateMaterialSlot(SamplerSlotId, kSlotSamplerName, kSlotSamplerName, SlotType.Input));
            AddSlot(new Vector1MaterialSlot(FrameSlotId, kSlotFrameName, kSlotFrameName, SlotType.Input, 12));
            AddSlot(new Texture2DInputMaterialSlot(TextureAlbedoSlotId, kSlotTextureAlbedoName, kSlotTextureAlbedoName));
            AddSlot(new Texture2DInputMaterialSlot(TextureNormalSlotId, kSlotTextureNormalName, kSlotTextureNormalName));
            AddSlot(new Vector1MaterialSlot(LODSlotId, kSlotLODName, kSlotLODName, SlotType.Input, 0f));

            RemoveSlotsNameNotMatching(new[] { FrameSlotId, TextureAlbedoSlotId, TextureNormalSlotId, LODSlotId, SamplerSlotId, PositionOutputSlotId, UVSlotId, PositionSlotId, ColorOutputSlotId });
        }
        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.RequiresIncludePath("Packages/com.unity.shadergraph/ShaderGraphLibrary/Imposter_Update.hlsl");
        }
        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var PositionSlotValue = GetSlotValue(PositionSlotId, generationMode);
            var UVSlotValue = GetSlotValue(UVSlotId, generationMode);
            var OutPosition = GetVariableNameForSlot(PositionOutputSlotId);

            var SamplerSlotValue = GetSlotValue(SamplerSlotId, generationMode);
            var LODSlotValue = GetSlotValue(LODSlotId, generationMode);
            var TextureAlbedoSlotValue = GetSlotValue(TextureAlbedoSlotId, generationMode);
            var TextureNormalSlotValue = GetSlotValue(TextureNormalSlotId, generationMode);
            var OutColor = GetVariableNameForSlot(ColorOutputSlotId);
            //var OutNormal = GetVariableNameForSlot(NormalOutputSlotId);
            //var OutAlpha = GetVariableNameForSlot(AlphaOutputSlotId);

            var result = @$"
$precision3 {OutPosition};
$precision4 {OutColor};
bool check = true;
$precision4 Albedo_texelSize = {TextureAlbedoSlotValue}.texelSize;
Texture2D Albedo_tex = {TextureAlbedoSlotValue}.tex;
Texture2D Normal_tex = {TextureNormalSlotValue}.tex;

";
            if (!m_Value)
            {
                result += $@"check = false;";
            }

            if (m_SampleMode == SampleMode.OneFrame)
            {
                result += $@"Imposter_Current(check,{PositionSlotValue}, {UVSlotValue}, {SamplerSlotValue}.samplerstate, Albedo_texelSize, Albedo_tex, Normal_tex, {LODSlotValue},
    {OutPosition},{OutColor});";
            }
            else
            {
                result += $@"Imposter(check,{PositionSlotValue}, {UVSlotValue}, {SamplerSlotValue}.samplerstate, Albedo_texelSize, Albedo_tex, Normal_tex, {LODSlotValue},
    {OutPosition},{OutColor});";
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
