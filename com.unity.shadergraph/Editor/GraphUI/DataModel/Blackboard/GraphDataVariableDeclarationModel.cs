using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.Registry;
using UnityEditor.ShaderGraph.Registry.Types;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class GraphDataVariableDeclarationModel : VariableDeclarationModel
    {
        [SerializeField]
        string m_ContextNodeName;

        /// <summary>
        // Name of the context node that owns the entry for this Variable
        /// </summary>
        public string contextNodeName
        {
            get => m_ContextNodeName;
            set => m_ContextNodeName = value;
        }

        [SerializeField]
        string m_GraphDataName;

        /// <summary>
        // Name of the port on the Context Node that owns the entry for this Variable
        /// </summary>
        public string graphDataName
        {
            get => m_GraphDataName;
            set => m_GraphDataName = value;
        }

        public GraphDataVariableDeclarationModel()
        {

        }

        ShaderGraphModel shaderGraphModel => GraphModel as ShaderGraphModel;

        public override void CreateInitializationValue()
        {
            if (GraphModel.Stencil.GetConstantNodeValueType(DataType) != null)
            {
                InitializationModel = GraphModel.Stencil.CreateConstantValue(DataType);
                if (InitializationModel is ICLDSConstant cldsConstant)
                {
                    Debug.Log("WARNING: GraphDataVariableDeclarationModel.CreateInitializationValue(): \n CLDSConstants are currently being initialized with dummy info, need to have ability to add properties to GraphDelta");

                    // Is the node name going to be the name of the context node?
                    // The port name is probably the graphDataName cause they are port entries on the context node
                    cldsConstant.Initialize(shaderGraphModel.GraphHandler, contextNodeName, graphDataName);
                }
            }
        }

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
    }
}
