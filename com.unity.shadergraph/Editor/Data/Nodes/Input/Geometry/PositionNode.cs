using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [FormerName("UnityEngine.MaterialGraph.WorldPosNode")]
    [Title("Input", "Geometry", "Position")]
    class PositionNode : GeometryNode, IMayRequirePosition, IMayRequirePositionPredisplacement
    {
        public override int latestVersion => 1;
        private const int kOutputSlotId = 0;
        public const string kOutputSlotName = "Out";
        public override List<CoordinateSpace> validSpaces => new List<CoordinateSpace> { CoordinateSpace.Object, CoordinateSpace.View, CoordinateSpace.World, CoordinateSpace.Tangent, CoordinateSpace.AbsoluteWorld };
        [SerializeField]
        internal PositionSource m_PositionSource = PositionSource.Default;

        public PositionNode()
        {
            name = "Position";
            precision = Precision.Single;
            synonyms = new string[] { "location" };
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(
                kOutputSlotId,
                kOutputSlotName,
                kOutputSlotName,
                SlotType.Output,
                Vector3.zero));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            var name = string.Format("IN.{0}", space.ToVariableName(InterpolatorType.Position));
            if (RequiresPositionPredisplacement(ShaderStageCapability.All) != NeededCoordinateSpace.None)
            {
                name += PositionSource.Predisplacement.ToString();
            }
            return name;
        }

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability)
        {
            return space.ToNeededCoordinateSpace();
        }

        public NeededCoordinateSpace RequiresPositionPredisplacement(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            return m_PositionSource == PositionSource.Predisplacement ? space.ToNeededCoordinateSpace() : NeededCoordinateSpace.None;
        }

        public override void OnAfterMultiDeserialize(string json)
        {
            base.OnAfterMultiDeserialize(json);
            //required update
            if (sgVersion < 1)
            {
                ChangeVersion(1);
            }
        }
    }
}
