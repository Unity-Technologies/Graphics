using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

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
        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }

        public TriplanarNode()
        {
            name = "Triplanar";
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
            AddSlot(new PositionMaterialSlot(PositionInputId, kPositionInputName, kPositionInputName, CoordinateSpace.World));
            AddSlot(new NormalMaterialSlot(NormalInputId, kNormalInputName, kNormalInputName, CoordinateSpace.World));
            AddSlot(new Vector1MaterialSlot(TileInputId, kTileInputName, kTileInputName, SlotType.Input, 1));
            AddSlot(new Vector1MaterialSlot(BlendInputId, kBlendInputName, kBlendInputName, SlotType.Input, 1));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId, TextureInputId, SamplerInputId, PositionInputId, NormalInputId, TileInputId, BlendInputId });
        }

        public override void ValidateNode()
        {
            var textureSlot = FindInputSlot<Texture2DInputMaterialSlot>(TextureInputId);
            textureSlot.defaultType = (textureType == TextureType.Normal ? TextureShaderProperty.DefaultType.Bump : TextureShaderProperty.DefaultType.White);

            base.ValidateNode();
        }

        // Node generations
        public virtual void GenerateNodeCode(ShaderStringBuilder sb, GraphContext graphContext, GenerationMode generationMode)
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
                    sb.AppendLine("$precision3 {0}_Blend = max(pow(abs({1}), {2}), 0);"
                        , GetVariableNameForNode()
                        , GetSlotValue(NormalInputId, generationMode)
                        , GetSlotValue(BlendInputId, generationMode));
                    sb.AppendLine("{0}_Blend /= ({0}_Blend.x + {0}_Blend.y + {0}_Blend.z ).xxx;", GetVariableNameForNode());

                    sb.AppendLine("$precision3 {0}_X = UnpackNormalmapRGorAG(SAMPLE_TEXTURE2D({1}, {2}, {0}_UV.zy));"
                        , GetVariableNameForNode()
                        , id
                        , edgesSampler.Any() ? GetSlotValue(SamplerInputId, generationMode) : "sampler" + id);

                    sb.AppendLine("$precision3 {0}_Y = UnpackNormalmapRGorAG(SAMPLE_TEXTURE2D({1}, {2}, {0}_UV.xz));"
                        , GetVariableNameForNode()
                        , id
                        , edgesSampler.Any() ? GetSlotValue(SamplerInputId, generationMode) : "sampler" + id);

                    sb.AppendLine("$precision3 {0}_Z = UnpackNormalmapRGorAG(SAMPLE_TEXTURE2D({1}, {2}, {0}_UV.xy));"
                        , GetVariableNameForNode()
                        , id
                        , edgesSampler.Any() ? GetSlotValue(SamplerInputId, generationMode) : "sampler" + id);

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
                    sb.AppendLine("$precision3 {0}_Blend = pow(abs({1}), {2});"
                        , GetVariableNameForNode()
                        , GetSlotValue(NormalInputId, generationMode)
                        , GetSlotValue(BlendInputId, generationMode));
                    sb.AppendLine("{0}_Blend /= dot({0}_Blend, 1.0);", GetVariableNameForNode());
                    sb.AppendLine("$precision4 {0}_X = SAMPLE_TEXTURE2D({1}, {2}, {0}_UV.zy);"
                        , GetVariableNameForNode()
                        , id
                        , edgesSampler.Any() ? GetSlotValue(SamplerInputId, generationMode) : "sampler" + id);

                    sb.AppendLine("$precision4 {0}_Y = SAMPLE_TEXTURE2D({1}, {2}, {0}_UV.xz);"
                        , GetVariableNameForNode()
                        , id
                        , edgesSampler.Any() ? GetSlotValue(SamplerInputId, generationMode) : "sampler" + id);

                    sb.AppendLine("$precision4 {0}_Z = SAMPLE_TEXTURE2D({1}, {2}, {0}_UV.xy);"
                        , GetVariableNameForNode()
                        , id
                        , edgesSampler.Any() ? GetSlotValue(SamplerInputId, generationMode) : "sampler" + id);

                    sb.AppendLine("$precision4 {0} = {1}_X * {1}_Blend.x + {1}_Y * {1}_Blend.y + {1}_Z * {1}_Blend.z;"
                        , GetVariableNameForSlot(OutputSlotId)
                        , GetVariableNameForNode());
                    break;
            }
        }

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability)
        {
            return CoordinateSpace.World.ToNeededCoordinateSpace();
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
