using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.ShaderGraph.GraphUI.DataModel
{
    public class ShaderGraphModel : GraphModel
    {
        protected override Type GetEdgeType(IPortModel toPort, IPortModel fromPort)
        {
            return toPort.PortDataType == fromPort.PortDataType ? typeof(EdgeModel) : typeof(ConversionEdgeModel);
        }
    }
}
