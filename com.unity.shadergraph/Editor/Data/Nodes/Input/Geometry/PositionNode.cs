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

        public List<TessellationOption> validTessellationOptions => new List<TessellationOption> { TessellationOption.Default, TessellationOption.Predisplacement };

        [SerializeField]
        private TessellationOption m_TessellationOption = TessellationOption.Default;

        [PopupControl("Tessellation")]
        public PopupList tessellationPopup
        {
            get
            {
                var names = validTessellationOptions.Select(cs => cs.ToString().PascalToLabel()).ToArray();
                return new PopupList(names, (int)m_TessellationOption);
            }
            set
            {
                if (m_TessellationOption == (TessellationOption)value.selectedEntry)
                    return;

                m_TessellationOption = (TessellationOption)value.selectedEntry;
                Dirty(ModificationScope.Graph);
            }
        }
        public TessellationOption tessellationOption => m_TessellationOption;

        public PositionNode()
        {
            name = "Position";
            precision = Precision.Single;
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
            if (RequiresPredisplacement(ShaderStageCapability.All))
            {
                name += TessellationOption.Predisplacement.ToString();
            }
            return name;
        }

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability)
        {
            return space.ToNeededCoordinateSpace();
        }

        public bool RequiresPredisplacement(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            return tessellationOption == TessellationOption.Predisplacement;
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
