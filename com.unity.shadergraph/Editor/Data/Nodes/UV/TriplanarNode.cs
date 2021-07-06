using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("UV", "Triplanar")]
    class TriplanarNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequirePosition, IMayRequireNormal, IMayRequireTangent, IMayRequireBitangent
    {
        public const int OutputSlotId = 0;
        public const int TextureInputId = 1;
        public const int SamplerInputId = 2;
        public const int PositionInputId = 3;
        public const int NormalInputId = 4;
        public const int TileInputId = 5;
        public const int BlendInputId = 6;
        const string kOutputSlotName = "Out";
        const string kTextureInputName = "Texture";
        const string kSamplerInputName = "Sampler";
        const string kPositionInputName = "Position";
        const string kNormalInputName = "Normal";
        const string kTileInputName = "Tile";
        const string kBlendInputName = "Blend";

        public override bool hasPreview { get { return true; } }

        public TriplanarNode()
        {
            name = "Triplanar";
            m_PreviewMode = PreviewMode.Preview3D;
            UpdateNodeAfterDeserialization();
        }

        [SerializeField]
        private TextureType m_TextureType = TextureType.Default;

        [EnumControl("Type")]
        public TextureType textureType
        {
            get { return m_TextureType; }
            set
            {
                if (m_TextureType == value)
                    return;

                m_TextureType = value;
                Dirty(ModificationScope.Graph);

                ValidateNode();
            }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero, ShaderStageCapability.Fragment));
            AddSlot(new Texture2DInputMaterialSlot(TextureInputId, kTextureInputName, kTextureInputName));
            AddSlot(new SamplerStateMaterialSlot(SamplerInputId, kSamplerInputName, kSamplerInputName, SlotType.Input));
            AddSlot(new PositionMaterialSlot(PositionInputId, kPositionInputName, kPositionInputName, CoordinateSpace.AbsoluteWorld));
            AddSlot(new NormalMaterialSlot(NormalInputId, kNormalInputName, kNormalInputName, CoordinateSpace.World));
            AddSlot(new Vector1MaterialSlot(TileInputId, kTileInputName, kTileInputName, SlotType.Input, 1));
            AddSlot(new Vector1MaterialSlot(BlendInputId, kBlendInputName, kBlendInputName, SlotType.Input, 1));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId, TextureInputId, SamplerInputId, PositionInputId, NormalInputId, TileInputId, BlendInputId });
        }

        public override void Setup()
        {
            base.Setup();
            var textureSlot = FindInputSlot<Texture2DInputMaterialSlot>(TextureInputId);
            textureSlot.defaultType = (textureType == TextureType.Normal ? Texture2DShaderProperty.DefaultType.NormalMap : Texture2DShaderProperty.DefaultType.White);
        }

        // Node generations
        public virtual void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            sb.AppendLine("$precision3 {0}_UV = {1} * {2};", GetVariableNameForNode(),
                GetSlotValue(PositionInputId, generationMode), GetSlotValue(TileInputId, generationMode));

            //Sampler input slot
            var samplerSlot = FindInputSlot<MaterialSlot>(SamplerInputId);
            var edgesSampler = owner.GetEdges(samplerSlot.slotReference);
            var id = GetSlotValue(TextureInputId, generationMode);

            switch (textureType)
            {
                // Whiteout blend method
                // https://medium.com/@bgolus/normal-mapping-for-a-triplanar-shader-10bf39dca05a
                case TextureType.Normal:
                    // See comment for default case.
                    sb.AppendLine("$precision3 {0}_Blend = SafePositivePow_$precision({1}, min({2}, floor(log2(Min_$precision())/log2(1/sqrt(3)))) );"
                        , GetVariableNameForNode()
                        , GetSlotValue(NormalInputId, generationMode)
                        , GetSlotValue(BlendInputId, generationMode));
                    sb.AppendLine("{0}_Blend /= ({0}_Blend.x + {0}_Blend.y + {0}_Blend.z ).xxx;", GetVariableNameForNode());

                    sb.AppendLine("$precision3 {0}_X = UnpackNormal(SAMPLE_TEXTURE2D({1}.tex, {2}.samplerstate, {0}_UV.zy));"
                        , GetVariableNameForNode()
                        , id
                        , edgesSampler.Any() ? GetSlotValue(SamplerInputId, generationMode) : id);

                    sb.AppendLine("$precision3 {0}_Y = UnpackNormal(SAMPLE_TEXTURE2D({1}.tex, {2}.samplerstate, {0}_UV.xz));"
                        , GetVariableNameForNode()
                        , id
                        , edgesSampler.Any() ? GetSlotValue(SamplerInputId, generationMode) : id);

                    sb.AppendLine("$precision3 {0}_Z = UnpackNormal(SAMPLE_TEXTURE2D({1}.tex, {2}.samplerstate, {0}_UV.xy));"
                        , GetVariableNameForNode()
                        , id
                        , edgesSampler.Any() ? GetSlotValue(SamplerInputId, generationMode) : id);

                    sb.AppendLine("{0}_X = $precision3({0}_X.xy + {1}.zy, abs({0}_X.z) * {1}.x);"
                        , GetVariableNameForNode()
                        , GetSlotValue(NormalInputId, generationMode));

                    sb.AppendLine("{0}_Y = $precision3({0}_Y.xy + {1}.xz, abs({0}_Y.z) * {1}.y);"
                        , GetVariableNameForNode()
                        , GetSlotValue(NormalInputId, generationMode));

                    sb.AppendLine("{0}_Z = $precision3({0}_Z.xy + {1}.xy, abs({0}_Z.z) * {1}.z);"
                        , GetVariableNameForNode()
                        , GetSlotValue(NormalInputId, generationMode));

                    sb.AppendLine("$precision4 {0} = $precision4(normalize({1}_X.zyx * {1}_Blend.x + {1}_Y.xzy * {1}_Blend.y + {1}_Z.xyz * {1}_Blend.z), 1);"
                        , GetVariableNameForSlot(OutputSlotId)
                        , GetVariableNameForNode());
                    sb.AppendLine("$precision3x3 {0}_Transform = $precision3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);", GetVariableNameForNode());
                    sb.AppendLine("{0}.rgb = TransformWorldToTangent({0}.rgb, {1}_Transform);"
                        , GetVariableNameForSlot(OutputSlotId)
                        , GetVariableNameForNode());
                    break;
                default:
                    // We want the sum of the 3 blend weights (by which we normalize them) to be > 0.
                    // Max safe exponent is log2(REAL_MIN)/log2(1/sqrt(3)):
                    // Take the set of all possible normalized vectors, make a set from selecting the maximum component of each 3-vectors from the previous set,
                    // the minimum (:= min_of_max) of that new set is 1/sqrt(3) (by the fact vectors are normalized).
                    // We then want a maximum exponent such that
                    //                            precision_min < min_of_max^exponent_max
                    // where exponent_max is blend,
                    //                       log(precision_min) < log(min_of_max) * exponent_max
                    //     log(precision_min) / log(min_of_max) > exponent_max
                    sb.AppendLine("$precision3 {0}_Blend = SafePositivePow_$precision({1}, min({2}, floor(log2(Min_$precision())/log2(1/sqrt(3)))) );"
                        , GetVariableNameForNode()
                        , GetSlotValue(NormalInputId, generationMode)
                        , GetSlotValue(BlendInputId, generationMode));
                    sb.AppendLine("{0}_Blend /= dot({0}_Blend, 1.0);", GetVariableNameForNode());
                    sb.AppendLine("$precision4 {0}_X = SAMPLE_TEXTURE2D({1}.tex, {2}.samplerstate, {0}_UV.zy);"
                        , GetVariableNameForNode()
                        , id
                        , edgesSampler.Any() ? GetSlotValue(SamplerInputId, generationMode) : id);

                    sb.AppendLine("$precision4 {0}_Y = SAMPLE_TEXTURE2D({1}.tex, {2}.samplerstate, {0}_UV.xz);"
                        , GetVariableNameForNode()
                        , id
                        , edgesSampler.Any() ? GetSlotValue(SamplerInputId, generationMode) : id);

                    sb.AppendLine("$precision4 {0}_Z = SAMPLE_TEXTURE2D({1}.tex, {2}.samplerstate, {0}_UV.xy);"
                        , GetVariableNameForNode()
                        , id
                        , edgesSampler.Any() ? GetSlotValue(SamplerInputId, generationMode) : id);

                    sb.AppendLine("$precision4 {0} = {1}_X * {1}_Blend.x + {1}_Y * {1}_Blend.y + {1}_Z * {1}_Blend.z;"
                        , GetVariableNameForSlot(OutputSlotId)
                        , GetVariableNameForNode());
                    break;
            }
        }

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability)
        {
            return CoordinateSpace.AbsoluteWorld.ToNeededCoordinateSpace() | CoordinateSpace.World.ToNeededCoordinateSpace();
        }

        public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability)
        {
            return CoordinateSpace.World.ToNeededCoordinateSpace();
        }

        public NeededCoordinateSpace RequiresTangent(ShaderStageCapability stageCapability)
        {
            switch (m_TextureType)
            {
                case TextureType.Normal:
                    return CoordinateSpace.World.ToNeededCoordinateSpace();
                default:
                    return NeededCoordinateSpace.None;
            }
        }

        public NeededCoordinateSpace RequiresBitangent(ShaderStageCapability stageCapability)
        {
            switch (m_TextureType)
            {
                case TextureType.Normal:
                    return CoordinateSpace.World.ToNeededCoordinateSpace();
                default:
                    return NeededCoordinateSpace.None;
            }
        }
    }
}
