using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    //public interface IRegistry
    //{
    //    IEnumerable<RegistryKey> BrowseRegistryKeys();
    //    Defs.INodeDefinitionBuilder GetBuilder(RegistryKey key);
    //    RegistryFlags GetFlags(RegistryKey key);
    //    GraphDelta.INodeReader GetDefaultTopology(RegistryKey key);
    //    bool RegisterNodeBuilder<T>() where T : Defs.INodeDefinitionBuilder;
    //    Defs.INodeDefinitionBuilder ResolveBuilder<T>() where T : Defs.INodeDefinitionBuilder;
    //    RegistryKey ResolveKey<T>() where T : Defs.IRegistryEntry;
    //    RegistryFlags ResolveFlags<T>() where T : Defs.IRegistryEntry;
    //}

    // TODO (Brett) I think we can remove this class.

    public static class GraphDeltaExtensions
    {

        public static IEnumerable<PortHandler> GetConnectedPorts(this PortHandler port)
        {
            // TODO (Liz) Implement
            throw new System.NotImplementedException();
        }

        private const string kRegistryKeyName = "_RegistryKey";
        public static RegistryKey GetRegistryKey(this NodeHandler node)
        {
            return node.GetMetadata<RegistryKey>(kRegistryKeyName);
        }

        public static RegistryKey GetRegistryKey(this PortHandler port)
        {
            return port.GetTypeField().GetMetadata<RegistryKey>(kRegistryKeyName);
        }

        public static RegistryKey GetRegistryKey(this FieldHandler field)
        {
            return field.GetMetadata<RegistryKey>(kRegistryKeyName);
        }

        public static void AddReferenceNode(this GraphHandler handler, string nodeName, string referenceName, Registry registry)
        {
            var node = handler.AddNode<ReferenceNodeBuilder>(nodeName, registry);
            node.SetMetadata("_referenceName", referenceName);
            // reference nodes have some weird rules, in that they can't really fetch or achieve any sort of identity until they are connected downstream to a context node.
            // We need stronger rules around references-- namely that a reference type must be consistent across all instances of that reference within however many context nodes.

            // This is funny though-- if you change a reference's type, that means _all_ reference handles that are represented by a context input port and all reference nodes and...
            // all of their downstream nodes get propogated-- and then upstream node connections can be disrupted if the type every changes.
        }

        internal static void SetupContext(this GraphHandler handler, IEnumerable<IContextDescriptor> contexts, Registry registry)
        {
            // only safe to call right now.
            NodeHandler previousContextNode = null;
            foreach(var context in contexts)
            {
                // Initialize the Context Node with information from the ContextDescriptor
                var name = context.GetRegistryKey().Name + "_Context"; // Not like this...
                var currentContextNode = handler.AddNode<ContextBuilder>(name, registry);
                currentContextNode.SetMetadata("_contextDescriptor", context.GetRegistryKey()); // initialize the node w/a reference to the context descriptor (so it can build itself).
                if(previousContextNode != null)
                {
                    // Create the monadic connection if it should exist.
                    var outPort = previousContextNode.AddPort("Out", false, false);
                    var inPort  = currentContextNode.AddPort("In", true, false);
                    handler.AddEdge(outPort.ID, inPort.ID);
                }
                handler.ReconcretizeNode(name, registry);
                previousContextNode = currentContextNode;
            }

            var entryPoint = handler.AddNode<ContextBuilder>("EntryPoint", registry);
            var toEntry = previousContextNode.AddPort("Out", false, false);
            var inEntry = entryPoint.AddPort("In", true, false);
            handler.AddEdge(toEntry.ID, inEntry.ID);
            handler.ReconcretizeNode("EntryPoint", registry);
        }

        //public static void ProcessGraph(this GraphDelta.IGraphHandler handler, ShaderFoundry.ShaderContainer container, Registry registry)
        //{
        //    // if we walk the vertical/input output relationship here, we will get all of our context nodes.
        //    // Each context node is processed by flattening the node i/o in capturing local variables, and passing those along, applying casts where appropriate.
        //    var entryPoint = handler.GetNodeReader("EntryPoint");
        //    ProcessContextNode(entryPoint, handler, container, registry);
        //}

        //private static ShaderFoundry.Block ProcessContextNode(GraphDelta.INodeReader contextNode, GraphDelta.IGraphHandler handler, ShaderFoundry.ShaderContainer container, Registry registry)
        //{
        //    // can't handle duplicate contexts
        //    contextNode.GetField<RegistryKey>("_contextDescriptor", out var contextKey);

        //    var blockbuilder = new ShaderFoundry.Block.Builder(contextKey.Name);
        //    var funcbuilder = new ShaderFoundry.ShaderFunction.Builder(contextKey.Name + "_func");
        //    var outtypebuilder = new ShaderFoundry.ShaderType.StructBuilder(contextKey.Name + "_out");
        //    ShaderFoundry.ShaderType inputType = default;

        //    // Find our input struct -->
        //    // This gets more awkward/interesting with reference nodes-- it's unclear to me now how they should be handled.
        //    // Is pass-through interpolation working?
        //    foreach (var port in contextNode.GetPorts().Where(e => e.IsInput() && !e.IsHorizontal()))
        //    {
        //        var connectedPort = port.GetConnectedPorts().FirstOrDefault();
        //        if (connectedPort != null)
        //        {
        //            var connectedNode = handler.GetNodeByPort(connectedPort);
        //            var previousBlock = ProcessContextNode(connectedNode, handler, container, registry);
        //            if (previousBlock.Inputs.Any())
        //                inputType = previousBlock.EntryPointFunction.Parameters.Where(e => e.Name == "Out").FirstOrDefault().Type;
        //        }
        //    }

        //    // process our body and initialize our output struct accordingly.
        //    var visitedList = new HashSet<string>();
        //    foreach (var port in contextNode.GetPorts().Where(e => e.IsInput() && e.IsHorizontal()))
        //    {
        //        // get the type of our port, so we can add the correct type of field to our output struct.
        //        var shaderType = registry.GetTypeBuilder(port.GetRegistryKey()).GetShaderType((GraphDelta.IFieldReader)port, container, registry);
        //        outtypebuilder.AddField(shaderType, port.GetName());
        //        var connectedPort = port.GetConnectedPorts().FirstOrDefault();
        //        if(connectedPort != null)
        //        {
        //            var connectedNode = handler.GetNodeByPort(connectedPort);
        //            if (connectedNode.GetField("_referenceName", out string referenceName))
        //            {
        //                // reference nodes aren't functions, but are scoped to the input structure of the context node.
        //                // unclear if passthrough interpolation is setup or not-- if it isn't, this won't work for many cases.
        //                // will need to add walk up the dependencies and provide extra fields that need to be inlined into their i/o structures.
        //                funcbuilder.AddLine($"Out.{port.GetName()} = In.{referenceName};");
        //                continue;
        //            }
        //            if (!visitedList.Contains(connectedNode.GetName()))
        //            {
        //                // recursively build out each input connection's body code (output variable initializations-- visited list prevents dupes).
        //                ProcessFuncNode(connectedNode, handler, visitedList, funcbuilder, container, registry);
        //            }
        //            // TODO: CAST
        //            funcbuilder.AddLine($"Out.{port.GetName()} = {connectedNode.GetName()}_{connectedPort.GetName()};");
        //        }
        //        else
        //        {
        //            var init = registry.GetTypeBuilder(port.GetRegistryKey()).GetInitializerList((GraphDelta.IFieldReader)port, registry);
        //            funcbuilder.AddLine($"Out.{port.GetName()} = {init};");
        //        }
        //    }


        //    // copy the fields from our output struct to the block's outputs
        //    var outType = outtypebuilder.Build(container);
        //    foreach (var outField in outType.StructFields)
        //    {
        //        var blockVarBuilder = new ShaderFoundry.BlockVariable.Builder();
        //        blockVarBuilder.ReferenceName = outField.Name;
        //        blockVarBuilder.Type = outField.Type;
        //        var blockVar = blockVarBuilder.Build(container);
        //        blockbuilder.AddOutput(blockVar);
        //    }

        //    // copy the input fields from our input struct to the block's inputs
        //    if (inputType.IsValid) foreach (var inField in inputType.StructFields)
        //    {
        //        var blockVarBuilder = new ShaderFoundry.BlockVariable.Builder();
        //        blockVarBuilder.ReferenceName = inField.Name;
        //        blockVarBuilder.Type = inField.Type;
        //        var blockVar = blockVarBuilder.Build(container);
        //        blockbuilder.AddInput(blockVar);
        //    }

        //    // finalize our function and entry point
        //    funcbuilder.AddOutput(outType, "Out");
        //    var func = funcbuilder.Build(container);
        //    blockbuilder.SetEntryPointFunction(func);

        //    // done with the block?
        //    return blockbuilder.Build(container);
        //}

        //private static void ProcessFuncNode(GraphDelta.INodeReader node, GraphDelta.IGraphHandler handler, HashSet<string> visitedList, ShaderFoundry.ShaderFunction.Builder funcBuilder, ShaderFoundry.ShaderContainer container, Registry registry)
        //{
        //    var func = registry.GetNodeBuilder(node.GetRegistryKey()).GetShaderFunction(node, container, registry);
        //    string arguments = "";
        //    foreach (var param in func.Parameters)
        //    {
        //        if(node.TryGetPort(param.Name, out var port))
        //        {
        //            string argument = "";
        //            if (!port.IsHorizontal()) continue;
        //            if(port.IsInput())
        //            {
        //                var connectedPort = port.GetConnectedPorts().FirstOrDefault();
        //                if (connectedPort != null) // connected input port-
        //                {
        //                    var connectedNode = handler.GetNodeByPort(connectedPort);
        //                    if (!visitedList.Contains(connectedNode.GetName()))
        //                    {
        //                        // This will roll out its output vars as well as the call to initialize them.
        //                        // visitedList protects from duplication, as we know the func's outputs have already been initialized.
        //                        // (note that ShaderFoundry.Container should handle deduplications).
        //                        ProcessFuncNode(node, handler, visitedList, funcBuilder, container, registry);
        //                    }
        //                    // TODO: CAST
        //                    argument = $"{connectedNode.GetName()}_{connectedPort.GetName()}";
        //                }
        //                else // not connected.
        //                {
        //                    // get the inlined port value as an initializer from the definition-- since there was no connection).
        //                    argument = registry.GetTypeBuilder(port.GetRegistryKey()).GetInitializerList((GraphDelta.IFieldReader)port, registry);
        //                }
        //            }
        //            else // this is an output port.
        //            {
        //                argument = $"{node.GetName()}_{port.GetName()}"; // add to the arguments for the function call.
        //                // default initialize this before our function call.
        //                var initValue = registry.GetTypeBuilder(port.GetRegistryKey()).GetInitializerList((GraphDelta.IFieldReader)port, registry);
        //                funcBuilder.AddLine($"{param.Type.Name} {argument} = {initValue};");
        //            }
        //            arguments += argument + ", ";
        //        }
        //    }
        //    if (arguments.Length != 0)
        //        arguments.Remove(arguments.Length - 3, 2); // trim the trailing ", "
        //    funcBuilder.AddLine($"{func.Name}({arguments});"); // add our node's function call to the body we're building out.
        //}

        public static bool TestConnection(this GraphHandler handler, string srcNode, string srcPort, string dstNode, string dstPort, Registry registry)
        {
            var dstNodeHandler = handler.GetNode(dstNode);
            var dstPortHandler = dstNodeHandler.GetPort(dstPort);
            var srcPortHandler = handler.GetNode(srcNode).GetPort(srcPort);
            return registry.CastExists(dstPortHandler.GetTypeField().GetRegistryKey(), srcPortHandler.GetTypeField().GetRegistryKey());
        }

        public static bool TryConnect(this GraphHandler handler, string srcNode, string srcPort, string dstNode, string dstPort, Registry registry)
        {
            var dstNodeHandler = handler.GetNode(dstNode);
            var dstPortHandler = dstNodeHandler.GetPort(dstPort);
            var srcPortHandler = handler.GetNode(srcNode).GetPort(srcPort);
            return handler.AddEdge(srcPortHandler.ID, dstPortHandler.ID) != null;
        }


        //public static void SetPortField<T>(this NodeHandler node, string portName, string fieldName, T value)
        //{
        //    var pw = node.GetPort(portName);
        //    if(pw == null)
        //    {
        //        pw = node.AddPort(portName, true, true);
        //    }
        //    pw.SetField(fieldName, value);
        //}

        //internal static void SetField<T> (GraphDataHandler handler, string fieldName, T value)
        //{
        //    FieldHandler<T> fieldWriter = handler.GetField<T>(fieldName);
        //    if (fieldWriter == null)
        //    {
        //        handler.AddField(fieldName, value);
        //    }
        //    else
        //    {
        //        fieldWriter.SetData(value);
        //    }

        //}

        //public static void SetField<T>(this NodeHandler node, string fieldName, T value) => SetField(node as GraphDataHandler, fieldName, value);
        //public static void SetField<T>(this PortHandler port, string fieldName, T value) => SetField(port as GraphDataHandler, fieldName, value);
        //public static void SetField<T>(this FieldHandler field, string fieldName, T value) => SetField(field as GraphDataHandler, fieldName, value);

        public static T GetField<T>(this NodeHandler node, string fieldName)
        {
            return node.GetField<T>(fieldName).GetData();
        }
        public static T GetField<T>(this PortHandler port, string fieldName)
        {
            return port.GetField<T>(fieldName).GetData();
        }
        public static T GetField<T>(this FieldHandler field, string fieldName)
        {
            return field.GetSubField<T>(fieldName).GetData();
        }

        public static bool TryGetField<T>(this NodeHandler node, string fieldName, out T data)
        {
            var field = node.GetField<T>(fieldName);
            if(field != null)
            {
                data = field.GetData();
                return true;
            }
            data = default;
            return false;
        }

        public static void GetField<T>(this PortHandler port, string fieldName, out T data)
        {
            data = port.GetField<T>(fieldName).GetData();
        }
        public static void GetField<T>(this FieldHandler field, string fieldName, out T data)
        {
            data = field.GetSubField<T>(fieldName).GetData();
        }

        public static void TryGetPort(this NodeHandler node, string portName, out PortHandler port)
        {
            port = node.GetPort(portName);
        }

        internal static PortHandler AddPort<T>(this NodeHandler node, string name, bool isInput, Registry registry) where T : ITypeDefinitionBuilder
        {
            return AddPort(node, name, isInput, Registry.ResolveKey<T>(), registry);
        }

        public static PortHandler AddPort(this NodeHandler node, string name, bool isInput, RegistryKey key, Registry registry)
        {
            var port = node.AddPort(name, isInput, true);
            port.AddTypeField().SetMetadata(kRegistryKeyName, key);

            var builder = registry.GetTypeBuilder(key);

            builder.BuildType(port.GetTypeField(), registry);
            return port;
        }
    }
}
