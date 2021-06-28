using System;
using GtfPlayground.DataModel;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace GtfPlayground
{
    public class PlaygroundGraphModel : GraphModel
    {
        protected override Type GetEdgeType(IPortModel toPort, IPortModel fromPort)
        {
            return toPort.PortDataType == fromPort.PortDataType ? typeof(EdgeModel) : typeof(ConversionEdgeModel);
        }
    }
}