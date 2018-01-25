using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    [Title("UV", "Triplanar")]
    public class TriplanarNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequirePosition, IMayRequireNormal
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
            }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            AddSlot(new Texture2DInputMaterialSlot(TextureInputId, kTextureInputName, kTextureInputName));
            AddSlot(new SamplerStateMaterialSlot(SamplerInputId, kSamplerInputName, kSamplerInputName, SlotType.Input));
            AddSlot(new PositionMaterialSlot(PositionInputId, kPositionInputName, kPositionInputName, CoordinateSpace.World));
            AddSlot(new NormalMaterialSlot(NormalInputId, kNormalInputName, kNormalInputName, CoordinateSpace.World));
            AddSlot(new Vector1MaterialSlot(TileInputId, kTileInputName, kTileInputName, SlotType.Input, 1));
            AddSlot(new Vector1MaterialSlot(BlendInputId, kBlendInputName, kBlendInputName, SlotType.Input, 1));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId, TextureInputId, SamplerInputId, PositionInputId, NormalInputId, TileInputId, BlendInputId });
        }

        // Node generations
        public virtual void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var sb = new ShaderStringBuilder();
            sb.AppendLine("{0}3 {1}_UV = {2} * {3};", precision, GetVariableNameForNode(), 
                GetSlotValue(PositionInputId, generationMode), GetSlotValue(TileInputId, generationMode));

            sb.AppendLine("{0}3 {1}_Blend = pow(abs({2}), {3});", precision, GetVariableNameForNode(),
                GetSlotValue(NormalInputId, generationMode), GetSlotValue(BlendInputId, generationMode));

            sb.AppendLine("{0}_Blend /= dot({0}_Blend, 1.0);", GetVariableNameForNode());

            //Sampler input slot
            var samplerSlot = FindInputSlot<MaterialSlot>(SamplerInputId);
            var edgesSampler = owner.GetEdges(samplerSlot.slotReference);

            var id = GetSlotValue(TextureInputId, generationMode);
            sb.AppendLine("{0}4 {1}_X = SAMPLE_TEXTURE2D({2}, {3}, {1}_UV.yz);"
                , precision
                , GetVariableNameForNode()
                , id
                , edgesSampler.Any() ? GetSlotValue(SamplerInputId, generationMode) : "sampler" + id);

            sb.AppendLine("{0}4 {1}_Y = SAMPLE_TEXTURE2D({2}, {3}, {1}_UV.xz);"
                , precision
                , GetVariableNameForNode()
                , id
                , edgesSampler.Any() ? GetSlotValue(SamplerInputId, generationMode) : "sampler" + id);

            sb.AppendLine("{0}4 {1}_Z = SAMPLE_TEXTURE2D({2}, {3}, {1}_UV.xy);"
                , precision
                , GetVariableNameForNode()
                , id
                , edgesSampler.Any() ? GetSlotValue(SamplerInputId, generationMode) : "sampler" + id);

            sb.AppendLine("{0}4 {1} = {2}_X * {2}_Blend.x + {2}_Y * {2}_Blend.y + {2}_Z * {2}_Blend.z;"
                , precision
                , GetVariableNameForSlot(OutputSlotId)
                , GetVariableNameForNode());

            if (textureType == TextureType.Normal)
                sb.AppendLine("{0}.rgb = UnpackNormalmapRGorAG({0});", GetVariableNameForSlot(OutputSlotId));

            visitor.AddShaderChunk(sb.ToString(), false);
        }

        public NeededCoordinateSpace RequiresPosition()
        {
            return CoordinateSpace.World.ToNeededCoordinateSpace();
        }

        public NeededCoordinateSpace RequiresNormal()
        {
            return CoordinateSpace.World.ToNeededCoordinateSpace();
        }
    }
}
