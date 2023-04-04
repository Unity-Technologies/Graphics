using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Drawing.Controls;


namespace UnityEditor.ShaderGraph
{
    enum BillboardType
    {
        Spherical,
        Cylindrical
    }
    [Title("Input", "Mesh Deformation", "Billboard")]
    class BillboardNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireTransform, IMayRequirePosition, IMayRequireNormal, IMayRequireTangent
    {
        private const int PositionOutputSlotId = 0;
        private const int NormalOutputSlotId = 1;
        private const int TangentOutputSlotId = 2;
        private const int PositionSlotId = 3;
        private const int NormalSlotId = 4;
        private const int TangentSlotId = 5;

        public const string kPositionOutputSlotName = "Billboard Position";
        public const string kNormalOutputSlotName = "Billboard Normal";
        public const string kTangentOutputSlotName = "Billboard Tangent";
        public const string kSlotPositionName = "Position";
        public const string kSlotNormalName = "Normal";
        public const string kSlotTangentnName = "Tangent";

        public BillboardNode()
        {
            name = "Billboard";
            precision = Precision.Single;
            synonyms = new string[] { "" };
            UpdateNodeAfterDeserialization();
        }

        [SerializeField]
        private BillboardType m_BillboardType = BillboardType.Spherical;

        [EnumControl("Mode")]
        public BillboardType type
        {
            get { return m_BillboardType; }
            set
            {
                if (m_BillboardType == value)
                    return;

                m_BillboardType = value;
                Dirty(ModificationScope.Graph);
            }
        }
        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(PositionOutputSlotId, kPositionOutputSlotName, kPositionOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(NormalOutputSlotId, kNormalOutputSlotName, kNormalOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(TangentOutputSlotId, kTangentOutputSlotName, kTangentOutputSlotName, SlotType.Output, Vector3.zero));

            AddSlot(new PositionMaterialSlot(PositionSlotId, kSlotPositionName, kSlotPositionName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
            AddSlot(new NormalMaterialSlot(NormalSlotId, kSlotNormalName, kSlotNormalName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
            AddSlot(new TangentMaterialSlot(TangentSlotId, kSlotTangentnName, kSlotTangentnName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
            RemoveSlotsNameNotMatching(new[] { PositionOutputSlotId, TangentOutputSlotId, NormalOutputSlotId, PositionSlotId, TangentSlotId, NormalSlotId });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            sb.AppendLine(@"$precision3 _Object_Scale = mul($precision3(1,1,1), UNITY_MATRIX_M); ");
            if (m_BillboardType == BillboardType.Spherical)
            {
                sb.AppendLine("$precision4x4 rotationCamMatrix = UNITY_MATRIX_I_V;");
            }
            else
            {
                sb.AppendLine("$precision4x4 rotationCamMatrix = $precision4x4( UNITY_MATRIX_I_V[0], $precision4( 0, 1, 0, 0), UNITY_MATRIX_I_V[2], UNITY_MATRIX_I_V[3]);");
            }
            sb.AppendLine(string.Format("$precision4 temp= mul(rotationCamMatrix, $precision4 ({0} * _Object_Scale, 0));", GetSlotValue(PositionSlotId, generationMode)));

            sb.AppendLine(string.Format("$precision3 {0} = TransformWorldToObject(SHADERGRAPH_OBJECT_POSITION + $precision3(0, .5, 0) +temp.xyz);", GetVariableNameForSlot(PositionOutputSlotId)));
            sb.AppendLine(string.Format("$precision3 {0} = TransformWorldToObject(mul(rotationCamMatrix, $precision4({1}, 0)).xyz);", GetVariableNameForSlot(TangentOutputSlotId), GetSlotValue(TangentSlotId, generationMode)));
            sb.AppendLine(string.Format("$precision3 {0} = TransformWorldToObject(mul(rotationCamMatrix, $precision4({1}, 0)).xyz);", GetVariableNameForSlot(NormalOutputSlotId), GetSlotValue(NormalSlotId, generationMode)));
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
