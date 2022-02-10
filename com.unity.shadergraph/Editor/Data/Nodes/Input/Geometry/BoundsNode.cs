using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    // TODO: disable this node for bultin
    [Title("Input", "Geometry", "Bounds")]
    class BoundsNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireTangent, IMayRequireBitangent, IMayRequireNormal, IMayRequireTransform
    {
        public BoundsNode()
        {
            name = "Bounds";
            UpdateNodeAfterDeserialization();
        }

        [SerializeField]
        CoordinateSpace m_Space;

        [EnumControl]
        public CoordinateSpace space
        {
            get { return m_Space; }
            set
            {
                if (Equals(m_Space, value))
                    return;
                m_Space = value;
                Dirty(ModificationScope.Graph);
            }
        }

        const int kBoundsMinOutputSlotId = 0;
        const int kBoundsMaxOutputSlotId = 1;
        const int kBoundsSizeOutputSlotId = 2;
        const string kBoundsMinOutputSlotName = "Min";
        const string kBoundsMaxOutputSlotName = "Max";
        const string kBoundsSizeOutputSlotName = "Size";

        public override bool hasPreview { get { return false; } }

        internal SpaceTransform spaceTransform => new SpaceTransform(CoordinateSpace.AbsoluteWorld, space, ConversionType.Position, false, sgVersion);

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(kBoundsMinOutputSlotId, kBoundsMinOutputSlotName, kBoundsMinOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(kBoundsMaxOutputSlotId, kBoundsMaxOutputSlotName, kBoundsMaxOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(kBoundsSizeOutputSlotId, kBoundsSizeOutputSlotName, kBoundsSizeOutputSlotName, SlotType.Output, Vector3.zero));

            RemoveSlotsNameNotMatching(new[]
            {
                kBoundsMinOutputSlotId, kBoundsMaxOutputSlotId, kBoundsSizeOutputSlotId
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.ForReals)
            {
                if (IsSlotConnected(kBoundsMinOutputSlotId))
                {
                    string slot = GetSlotValue(kBoundsMinOutputSlotId, generationMode);
                    sb.AddLine($"$precision3 {slot};");
                    SpaceTransformUtil.GenerateTransformCodeStatement(spaceTransform, "unity_RendererBounds_Min.xyz", slot, sb);
                }
                if (IsSlotConnected(kBoundsMaxOutputSlotId))
                {
                    string slot = GetSlotValue(kBoundsMaxOutputSlotId, generationMode);
                    sb.AddLine($"$precision3 {slot};");
                    SpaceTransformUtil.GenerateTransformCodeStatement(spaceTransform, "unity_RendererBounds_Max.xyz", slot, sb);
                }
                if (IsSlotConnected(kBoundsSizeOutputSlotId))
                {
                    string slot = GetSlotValue(kBoundsSizeOutputSlotId, generationMode);
                    sb.AddLine($"$precision3 {slot} = (unity_RendererBounds_Max - unity_RendererBounds_Min).xyz;");
                }
            }
            else
            {
                if (IsSlotConnected(kBoundsMinOutputSlotId))
                    sb.AddLine($"$precision3 {GetSlotValue(kBoundsMinOutputSlotId, generationMode)} = 0;");
                if (IsSlotConnected(kBoundsMaxOutputSlotId))
                    sb.AddLine($"$precision3 {GetSlotValue(kBoundsMaxOutputSlotId, generationMode)} = 0;");
                if (IsSlotConnected(kBoundsSizeOutputSlotId))
                    sb.AddLine($"$precision3 {GetSlotValue(kBoundsSizeOutputSlotId, generationMode)} = 0;");
            }
        }
        public NeededCoordinateSpace RequiresTangent(ShaderStageCapability stageCapability)
            => spaceTransform.RequiresTangent;
        public NeededCoordinateSpace RequiresBitangent(ShaderStageCapability stageCapability)
            => spaceTransform.RequiresBitangent;
        public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability)
            => spaceTransform.RequiresNormal;
        public NeededTransform[] RequiresTransform(ShaderStageCapability stageCapability)
            => spaceTransform.RequiresTransform.ToArray();
    }
}
