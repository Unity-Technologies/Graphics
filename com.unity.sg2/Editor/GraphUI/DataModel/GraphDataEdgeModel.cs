


using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class GraphDataEdgeModel : EdgeModel
    {
    //    public override void SetPorts(IPortModel toPortModel, IPortModel fromPortModel)
    //    {
    //        var model = toPortModel?.AssetModel ?? fromPortModel?.AssetModel;

    //        InitAssetModel(model);

    //        base.SetPorts(toPortModel, fromPortModel);
    //    }

    //    public override IPortModel FromPort
    //    {
    //        get
    //        {
    //            if (GraphModel != null)
    //            {
    //                GraphModel.TryGetModelFromGuid(FromNodeGuid, out var el);
    //                var node = el as NodeModel;
    //                if (node == null)
    //                    return m_FromPortModelCache = null;
    //                return node.Ports.First(p => p.UniqueName == FromPortId);
    //            }
    //            return null;
    //        }
    //        set => base.FromPort = value;
    //    }
    //    public override IPortModel ToPort
    //    {
    //        get
    //        {
    //            if (GraphModel != null)
    //            {
    //                GraphModel.TryGetModelFromGuid(ToNodeGuid, out var el);
    //                var node = el as NodeModel;
    //                if (node == null)
    //                    return m_ToPortModelCache = null;
    //                return node.Ports.First(p => p.UniqueName == ToPortId);
    //            }
    //            return null;
    //        }
    //        set => base.ToPort = value;
    //    }
    }
}

//set
//{
//    var oldPort = FromPort;
//    m_FromPortReference.Assign(value);
//    m_FromPortModelCache = value;
//    OnPortChanged(oldPort, value);
//}
