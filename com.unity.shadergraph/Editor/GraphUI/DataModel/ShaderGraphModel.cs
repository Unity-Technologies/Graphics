using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.Registry;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI.DataModel
{
    public class ShaderGraphModel : GraphModel
    {
        IGraphHandler m_GraphHandler;

        // TODO: This will eventually delegate to real serialized data. For now, it's kept here ephemerally. Until
        //       then, (at least) the following features are broken as a result: reloading assembly, saving, loading.
        public IGraphHandler GraphHandler => m_GraphHandler ??= GraphUtil.CreateGraph();

        protected override Type GetEdgeType(IPortModel toPort, IPortModel fromPort)
        {
            return toPort.PortDataType == fromPort.PortDataType ? typeof(EdgeModel) : typeof(ConversionEdgeModel);
        }

        protected override bool IsCompatiblePort(IPortModel startPortModel, IPortModel compatiblePortModel)
        {
            var srcIsGraphData = startPortModel is GraphDataPortModel;
            var dstIsGraphData = compatiblePortModel is GraphDataPortModel;

            if (!srcIsGraphData && !dstIsGraphData) return base.IsCompatiblePort(startPortModel, compatiblePortModel);

            // For now, don't mix data-backed ports with viewmodel-only ones.
            // TODO: Redirects will presumably change this.
            if (srcIsGraphData != dstIsGraphData) return false;

            var srcPort = (GraphDataPortModel) startPortModel;
            var dstPort = (GraphDataPortModel) compatiblePortModel;

            return GraphHandler.TestConnection(dstPort.graphDataNodeModel.graphDataName,
                dstPort.graphDataName, srcPort.graphDataNodeModel.graphDataName,
                srcPort.graphDataName, ((ShaderGraphStencil) Stencil).GetRegistry());
        }
    }
}
