using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace GtfPlayground.DataModel
{
    [SearcherItem(typeof(PlaygroundStencil), SearcherContext.Graph, "No-Op/Float")]
    public class NoOpFloatNodeModel : NodeModel
    {
        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            AddInputPort("In", PortType.Data, TypeHandle.Float);
            AddOutputPort("Out", PortType.Data, TypeHandle.Float);
        }
    }
    
    [SearcherItem(typeof(PlaygroundStencil), SearcherContext.Graph, "No-Op/Two Floats")]
    public class NoOpTwoFloatNodeModel : NodeModel
    {
        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            AddInputPort("In 1", PortType.Data, TypeHandle.Float);
            AddInputPort("In 2", PortType.Data, TypeHandle.Float);
            AddOutputPort("Out 1", PortType.Data, TypeHandle.Float);
            AddOutputPort("Out 2", PortType.Data, TypeHandle.Float);
        }
    }
    
    [SearcherItem(typeof(PlaygroundStencil), SearcherContext.Graph, "No-Op/Vector3")]
    public class NoOpVector3NodeModel : NodeModel
    {
        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            AddInputPort("In", PortType.Data, TypeHandle.Vector3);
            AddOutputPort("Out", PortType.Data, TypeHandle.Vector3);
        }
    }
    
    [SearcherItem(typeof(PlaygroundStencil), SearcherContext.Graph, "No-Op/Execution")]
    public class NoOpExecutionNodeModel : NodeModel
    {
        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            AddInputPort("In", PortType.Data, TypeHandle.ExecutionFlow);
            AddOutputPort("Out", PortType.Data, TypeHandle.ExecutionFlow);
        }
    }
}