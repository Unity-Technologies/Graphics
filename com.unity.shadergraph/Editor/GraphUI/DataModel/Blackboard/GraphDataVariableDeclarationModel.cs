using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.Registry;
using UnityEditor.ShaderGraph.Registry.Types;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class GraphDataVariableDeclarationModel : VariableDeclarationModel
    {
        // TODO (Joe): When possible, assign these fields such that they point to a valid reference. It's possible
        //  only one name will be needed. If so, delete the other.
        string m_NodeName;
        string m_PortName;

        public string NodeName => m_NodeName;
        public string PortName => m_PortName;

        // Really hacky shim to create a valid target if one is needed for testing. Uncomment and use to get a node
        // and port name that can be used temporarily in CreateInitializationValue.
        // TODO (Joe): Delete this as soon as it's no longer needed, nothing like this should exist here

        // (string nodeName, string portName) MakeTempBackingConnection()
        // {
        //     var nodeName = "BLACKBOARD_CONTEXT_PLACEHOLDER_" + Guid;
        //     var refName = "BLACKBOARD_REF_PLACEHOLDER_" + Guid;
        //     var portName = "BLACKBOARD_PORT_PLACEHOLDER_" + Guid;
        //
        //     var model = (ShaderGraphModel)GraphModel;
        //     var stencil = (ShaderGraphStencil)model.Stencil;
        //     var graphHandler = model.GraphHandler;
        //     var registry = model.RegistryInstance;
        //
        //     graphHandler.AddReferenceNode(nodeName, refName, registry);
        //     var writer = graphHandler.GetNodeWriter(nodeName);
        //     var reader = graphHandler.GetNodeReader(nodeName);
        //
        //     if (!writer.TryGetPort(portName, out _))
        //     {
        //         var w = writer.AddPort(reader, portName, false, GraphType.kRegistryKey, registry);
        //         w.SetField(GraphType.kPrecision, GraphType.Precision.Fixed);
        //
        //         if (DataType == TypeHandle.Float)
        //         {
        //             w.SetField(GraphType.kLength, GraphType.Length.One);
        //             w.SetField(GraphType.kHeight, GraphType.Height.One);
        //             w.SetField(GraphType.kPrimitive, GraphType.Primitive.Float);
        //         }
        //         else if (DataType == TypeHandle.Bool)
        //         {
        //             w.SetField(GraphType.kLength, GraphType.Length.One);
        //             w.SetField(GraphType.kHeight, GraphType.Height.One);
        //             w.SetField(GraphType.kPrimitive, GraphType.Primitive.Bool);
        //             w.SetField(GraphType.kPrecision, GraphType.Precision.Fixed);
        //         }
        //         else if (DataType == TypeHandle.Vector4)
        //         {
        //             w.SetField(GraphType.kLength, GraphType.Length.Three);
        //             w.SetField(GraphType.kHeight, GraphType.Height.One);
        //             w.SetField(GraphType.kPrimitive, GraphType.Primitive.Bool);
        //             w.SetField(GraphType.kPrecision, GraphType.Precision.Fixed);
        //         }
        //
        //         // ...etc, add cases for whatever other fields you want to test
        //     }
        //
        //     return (nodeName, portName);
        // }

        // TODO: Ensure this is called at deserialization.
        public override void CreateInitializationValue()
        {
            var model = (ShaderGraphModel)GraphModel;
            var stencil = (ShaderGraphStencil)model.Stencil;

            Debug.LogWarning("UNIMPLEMENTED: Data connection for GraphDataVariableDeclarationModel. Field will act like a Vector4 regardless of type.");

            InitializationModel = stencil.CreateConstantValue(DataType);
            if (InitializationModel is ICLDSConstant cldsConstant)
            {
                // var (nodeName, portName) = MakeTempBackingConnection();
                cldsConstant.Initialize(model.GraphHandler, m_NodeName, m_PortName);
            }
        }
    }
}
