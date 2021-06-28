using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace GtfPlayground
{
    public class PlaygroundStencil : Stencil
    {
        public const string Name = "Playground";

        public override string ToolName => Name;

        public override IBlackboardGraphModel CreateBlackboardGraphModel(IGraphAssetModel graphAssetModel) =>
            new PlaygroundBlackboardGraphModel(graphAssetModel);

        public override Type GetConstantNodeValueType(TypeHandle typeHandle)
        {
            return typeHandle == PlaygroundTypes.DayOfWeek
                ? typeof(DayOfWeekConstant)
                : TypeToConstantMapper.GetConstantNodeType(typeHandle);
        }
    }
}
