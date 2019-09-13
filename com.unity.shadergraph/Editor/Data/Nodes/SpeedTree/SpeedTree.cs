using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
	enum SpeedTreeWindQuality
	{
		None = 0,
		Fastest = 1,
		Fast = 2,
		Better = 3,
		Best = 4,
		Palm = 5
	}

	[Title("SpeedTree", "SpeedTreeWind")]
	class SpeedTreeNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequirePosition, IMayRequireNormal, IMayRequireVertexColor, IMayRequireMeshUV, IGeneratesFunction
	{
		public const int OutPosSlotId = 0;
		private const string kOutPosSlotName = "OutPosition";
        public const int OutNormSlotId = 1;
        private const string kOutNormSlotName = "OutNormal";
        public const int OutUVSlotId = 2;
        private const string kOutUVSlotName = "OutUV0";

        public const int WindQualitySlotId = 3;
        private const string kWindQualitySlotName = "Wind Quality";

        //public const int BillboardInputSlotId = 2;
        //private const string kBillboardInputSlotName = "Billboard";


    [SerializeField]
    private SpeedTreeWindQuality m_WindQuality = SpeedTreeWindQuality.Best;

    [EnumControl("Default Wind Quality")]
    public SpeedTreeWindQuality windQuality
    {
        get { return m_WindQuality; }
        set
        {
            if (m_WindQuality == value)
                return;

            m_WindQuality = value;
            Dirty(ModificationScope.Node);
        }
    }

        public override bool hasPreview { get { return false; } }

		public SpeedTreeNode()
		{
			name = "SpeedTreeWind";
			UpdateNodeAfterDeserialization();
		}

		public override void UpdateNodeAfterDeserialization()
		{
			AddSlot(new Vector3MaterialSlot(OutPosSlotId, kOutPosSlotName, kOutPosSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(OutNormSlotId, kOutNormSlotName, kOutNormSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector2MaterialSlot(OutUVSlotId, kOutUVSlotName, kOutUVSlotName, SlotType.Output, Vector3.zero));

            AddSlot(new Vector1MaterialSlot(WindQualitySlotId, kWindQualitySlotName, kWindQualitySlotName, SlotType.Input, 0));
            //AddSlot(new BooleanMaterialSlot(BillboardInputSlotId, kBillboardInputSlotName, kBillboardInputSlotName, SlotType.Input, false));

            //RemoveSlotsNameNotMatching(new[] { OutputSlotId, WindQualitySlotId, BillboardInputSlotId });
            RemoveSlotsNameNotMatching(new[] { OutPosSlotId, OutNormSlotId, OutUVSlotId, WindQualitySlotId });
        }

		public void GenerateNodeCode(ShaderStringBuilder sb, GraphContext graphContext, GenerationMode generationMode)
		{
            /*
			string windQuality = ((int)m_WindQuality).ToString();
			var windSlot = FindSlot<MaterialSlot>(WindQualitySlotId);
			if ((windSlot != null) && owner.GetEdges(windSlot.slotReference).Any())
			{
				windQuality = GetSlotValue(WindQualitySlotId, generationMode);
			}

			var billboard = GetSlotValue(BillboardInputSlotId, generationMode);

			sb.AppendLine("$precision3 {0} = SpeedTreeWind(IN.{1}, IN.{2}, IN.{3}, IN.{4}, IN.{5}, IN.{6}, (int){7}, {8});",
											GetVariableNameForSlot(OutputSlotId),
											CoordinateSpace.Object.ToVariableName(InterpolatorType.Position),
											CoordinateSpace.Object.ToVariableName(InterpolatorType.Normal),
											UVChannel.UV0.GetUVName(),
											UVChannel.UV1.GetUVName(),
											UVChannel.UV2.GetUVName(),
											UVChannel.UV3.GetUVName(),
											windQuality,
											billboard);
                                            */
            string windQuality = ((int)m_WindQuality).ToString();
            var windSlot = FindSlot<MaterialSlot>(WindQualitySlotId);
            if ((windSlot != null) && owner.GetEdges(windSlot.slotReference).Any())
            {
                windQuality = GetSlotValue(WindQualitySlotId, generationMode);
            }

            sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(OutPosSlotId).concreteValueType.ToShaderString(), GetVariableNameForSlot(OutPosSlotId));
            sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(OutNormSlotId).concreteValueType.ToShaderString(), GetVariableNameForSlot(OutNormSlotId));
            sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(OutUVSlotId).concreteValueType.ToShaderString(), GetVariableNameForSlot(OutUVSlotId));
            sb.AppendLine("ApplyWindTransformation(IN.{0}, IN.{1}, IN.{2}, IN.{3}, IN.{4}, IN.{5}, IN.{6}, (int){7}, {8}, {9}, {10});",
                            CoordinateSpace.Object.ToVariableName(InterpolatorType.Position),
                            CoordinateSpace.Object.ToVariableName(InterpolatorType.Normal),
                            ShaderGeneratorNames.VertexColor,
                            UVChannel.UV0.GetUVName(),
                            UVChannel.UV1.GetUVName(),
                            UVChannel.UV2.GetUVName(),
                            UVChannel.UV3.GetUVName(),
                            windQuality,
                            GetVariableNameForSlot(OutPosSlotId),
                            GetVariableNameForSlot(OutNormSlotId),
                            GetVariableNameForSlot(OutUVSlotId));
        }

		public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability)
		{
			return CoordinateSpace.Object.ToNeededCoordinateSpace();
		}

		public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability)
		{
			return CoordinateSpace.Object.ToNeededCoordinateSpace();
		}

        public bool RequiresVertexColor(ShaderStageCapability stageCapability)
        {
            return true;
        }

		public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
		{
			return true;
		}

		public void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
		{
            // inject SpeedTree wind code at global scope
            //registry.ProvideFunction("SpeedTreeWind", sb => { sb.AppendLines("#include \"Packages/com.unity.shadergraph/ShaderGraphLibrary/SpeedTree.hlsl\""); });
            registry.ProvideFunction("ApplyWindTransformation", sb => { sb.AppendLines("#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTreeCommonWind.hlsl\""); });
		}

	}

}
