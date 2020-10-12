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
    class PositionNode : GeometryNode, IMayRequirePosition
    {
        public override int latestVersion => 1;
        private const int kOutputSlotId = 0;
        public const string kOutputSlotName = "Out";
        public override List<CoordinateSpace> validSpaces => new List<CoordinateSpace> {CoordinateSpace.Object, CoordinateSpace.View, CoordinateSpace.World, CoordinateSpace.Tangent, CoordinateSpace.AbsoluteWorld};

        public PositionNode()
        {
            name = "Position";
            precision = Precision.Single;
            UpdateNodeAfterDeserialization();
            onBeforeVersionChange += UpgradeNodeWithVersion;
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



        public void UpgradeNodeWithVersion(int newVersion)
        {
            if (sgVersion == 0 && newVersion > 0 && space == CoordinateSpace.World)
            {
                var names = validSpaces.Select(cs => cs.ToString().PascalToLabel()).ToArray();
                spacePopup = new PopupList(names, (int)CoordinateSpace.AbsoluteWorld);
            }
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return string.Format("IN.{0}", space.ToVariableName(InterpolatorType.Position));
        }

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability)
        {
            return space.ToNeededCoordinateSpace();
        }

        public override void OnAfterMultiDeserialize(string json)
        {
            base.OnAfterMultiDeserialize(json);
            //required update
            if(sgVersion < 1)
            {
                ChangeVersion(1);
            }
        }
    }
}
