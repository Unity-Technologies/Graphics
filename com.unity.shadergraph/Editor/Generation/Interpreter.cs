using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderFoundry;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.Registry;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Generation
{
    public static class Interpreter
    {

        public static Shader GetShaderForNode(INodeReader node, IGraphHandler graph, Registry.Registry registry)
        {
            void GetBlock(ShaderContainer container, CustomizationPoint vertexCP, CustomizationPoint surfaceCP, out CustomizationPointDescriptor vertexCPDesc, out CustomizationPointDescriptor surfaceCPDesc)
            {
                var block = EvaluateGraphAndPopulateDescriptors(node, graph, container, registry);
                vertexCPDesc = CustomizationPointDescriptor.Invalid;

                var surfaceDescBuilder = new CustomizationPointDescriptor.Builder(surfaceCP);
                var blockDescBuilder = new BlockDescriptor.Builder(block);
                var blockDesc = blockDescBuilder.Build(container);
                surfaceDescBuilder.BlockDescriptors.Add(blockDesc);
                surfaceCPDesc = surfaceDescBuilder.Build(container);
            }

            var builder = new ShaderBuilder();
            SimpleSampleBuilder.Build(new ShaderContainer(), SimpleSampleBuilder.GetTarget(), "Test", GetBlock, builder);
            return ShaderUtil.CreateShaderAsset(builder.ToString());
        }

        internal static Block EvaluateGraphAndPopulateDescriptors(INodeReader rootNode, IGraphHandler shaderGraph, ShaderContainer container, Registry.Registry registry)
        {
            const string BlockName = "ShaderGraphBlock";
            var blockBuilder = new Block.Builder(BlockName);

            var inputVariables = new List<BlockVariable>();
            var outputVariables = new List<BlockVariable>();

            var colorOutBuilder = new BlockVariable.Builder();
            colorOutBuilder.ReferenceName = "BaseColor";
            colorOutBuilder.Type = container._float3;
            var colorOut = colorOutBuilder.Build(container);
            outputVariables.Add(colorOut);

            var outputType = SimpleSampleBuilder.BuildStructFromVariables(container, $"{BlockName}Output", outputVariables);
            var mainBodyFunctionBuilder = new ShaderFunction.Builder($"{rootNode.GetName()}Main", outputType);

            foreach(var node in GatherTreeLeafFirst(rootNode))
            {
                ProcessNode(node, ref container, ref inputVariables, ref outputVariables, ref blockBuilder, ref mainBodyFunctionBuilder, registry);
            }




            //if(rootNode.IsContextNode())
            //{
            //    //iterate through inputs and do passthrough to outputs, adding the output variables

            //}
            //else
            //{
            //    //add BaseColor output and cast first output of root node to float3 and assign
            //}


            var inputType = SimpleSampleBuilder.BuildStructFromVariables(container, $"{BlockName}Input", inputVariables);


            mainBodyFunctionBuilder.AddLine($"{outputType.Name} output;");
            mainBodyFunctionBuilder.AddLine($"output.{colorOut.ReferenceName} = {rootNode.GetName()}_{rootNode.GetOutputPorts().First().GetName()};");
            mainBodyFunctionBuilder.AddLine("return output;");


            // Setup the block from the inputs, outputs, types, functions
            foreach (var variable in inputVariables)
                blockBuilder.AddInput(variable);
            foreach (var variable in outputVariables)
                blockBuilder.AddOutput(variable);
            blockBuilder.AddType(inputType);
            blockBuilder.AddType(outputType);
            mainBodyFunctionBuilder.AddInput(inputType, "In");
            blockBuilder.SetEntryPointFunction(mainBodyFunctionBuilder.Build(container));
            return blockBuilder.Build(container);
        }

        private static void ProcessNode(INodeReader node, ref ShaderContainer container, ref List<BlockVariable> inputVariables, ref List<BlockVariable> outputVariables, ref Block.Builder blockBuilder, ref ShaderFunction.Builder mainBodyFunctionBuilder, Registry.Registry registry)
        {
            var func = registry.GetNodeBuilder(node.GetRegistryKey()).GetShaderFunction(node, container, registry);
            blockBuilder.AddFunction(func);
            string arguments = "";
            foreach (var param in func.Parameters)
            {
                if (node.TryGetPort(param.Name, out var port))
                {
                    string argument = "";
                    if (!port.IsHorizontal())
                        continue;
                    if (port.IsInput())
                    {
                        var connectedPort = port.GetConnectedPorts().FirstOrDefault();
                        if (connectedPort != null) // connected input port-
                        {
                            var connectedNode = connectedPort.GetNode();
                            argument = $"{connectedNode.GetName()}_{connectedPort.GetName()}";
                        }
                        else // not connected.
                        {
                            // get the inlined port value as an initializer from the definition-- since there was no connection).
                            argument = registry.GetTypeBuilder(port.GetRegistryKey()).GetInitializerList((GraphDelta.IFieldReader)port, registry);
                        }
                    }
                    else // this is an output port.
                    {
                        argument = $"{node.GetName()}_{port.GetName()}"; // add to the arguments for the function call.
                        // default initialize this before our function call.
                        var initValue = registry.GetTypeBuilder(port.GetRegistryKey()).GetInitializerList((GraphDelta.IFieldReader)port, registry);
                        mainBodyFunctionBuilder.AddLine($"{param.Type.Name} {argument} = {initValue};");
                    }
                    arguments += argument + ", ";
                }
            }
            if (arguments.Length != 0)
                arguments = arguments.Remove(arguments.Length - 2, 2); // trim the trailing ", "
            mainBodyFunctionBuilder.AddLine($"{func.Name}({arguments});"); // add our node's function call to the body we're building out.

        }

        private static IEnumerable<INodeReader> GatherTreeLeafFirst(INodeReader rootNode)
        {
            Stack<INodeReader> stack = new Stack<INodeReader>();
            HashSet<INodeReader> visited = new HashSet<INodeReader>();
            stack.Push(rootNode);
            while(stack.Count > 0)
            {
                INodeReader check = stack.Peek();
                bool isLeaf = true;
                foreach(IPortReader port in check.GetInputPorts())
                {
                    if(port.IsHorizontal())
                    {
                        foreach(INodeReader connected in GetConnectedNodes(port))
                        {
                            if (!visited.Contains(connected))
                            {
                                visited.Add(connected);
                                isLeaf = false;
                                stack.Push(connected);
                            }
                        }
                    }
                }
                if(isLeaf)
                {
                    yield return stack.Pop();
                }
            }
        }

        private static IEnumerable<INodeReader> GetConnectedNodes(IPortReader port)
        {
            foreach(var connected in port.GetConnectedPorts())
            {
                yield return connected.GetNode();
            }
        }
    }
}
