//#define INTERPRETER_DEBUG
//#define INTERPRETER_V2
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.ContextLayeredDataStorage;
using UnityEditor.ShaderFoundry;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using static UnityEditor.ShaderGraph.GraphDelta.ContextEntryEnumTags;
using PropertyAttribute = UnityEditor.ShaderFoundry.PropertyAttribute;

namespace UnityEditor.ShaderGraph.Generation
{
// TODO: Interpreter should be refactored to cache processing state that is shared across
// many calls to the interpreter. When Async preview goes live especially, there will be a
// lot of repeated work in sorting and processing of nodes, many of which can be cached and
// stitched on demand at various levels. An interpreter that could accept change notifications,
// eg. when topological changes occur and the halo of those changes, it would be possible to even cache
// the ShaderFoundry objects as well.
    public static class Interpreter
    {

        static string defaultSurfaceDescriptionCPName = LegacyCustomizationPoints.SurfaceDescriptionCPName;
        internal class ShaderEvaluationCache
        {
            private class ReferableData : VariableData
            {
                public PropertyBlockUsage propertyBlockUsage;
                public DataSource dataSource;
                public string displayName;
                public string defaultValue;
                public StructField field;
            }

            private class VariableData
            {
                public string desiredVariableName;
                public string actualVariableName;
                public ShaderType shaderType;
                public string initializationString;
                public bool inlineInitialization;
            }

            private struct VariableReference
            {
                private int m_hash;
                public VariableReference(VariableData variable)
                {
                    m_hash = HashCode.Combine(variable.desiredVariableName.GetHashCode(StringComparison.Ordinal),
                                              variable.actualVariableName?.GetHashCode(StringComparison.Ordinal),
                                              variable.shaderType,
                                              variable.initializationString?.GetHashCode(StringComparison.Ordinal));
                }
            }

            private struct FunctionReference
            {
                private int m_hash;
                public FunctionReference(ShaderFunction function)
                {
                    int tmp = 0;
                    foreach(var param in function.Parameters)
                    {
                        tmp += param.Name.GetHashCode(StringComparison.Ordinal) ^ param.Type.Name.GetHashCode(StringComparison.Ordinal);
                    }
                    m_hash = HashCode.Combine(function.Name.GetHashCode(StringComparison.Ordinal),
                                              function.Body.GetHashCode(StringComparison.Ordinal),
                                              tmp);
                }
            }

            public struct ProcessingResult
            {
                public Block.Builder blockBuilder;
            }

            private Dictionary<VariableReference, VariableData> variableReferences;
            private Dictionary<VariableReference, int> variableCollisions;

            private Dictionary<ElementID, VariableReference> portToVariableLookup;
            private HashSet<VariableReference> initialized;

            private Dictionary<FunctionReference, ShaderFunction> functionReferences;
            private HashSet<ShaderFoundry.IncludeDescriptor> includeReferences;
            private HashSet<ShaderType> typeReferences;

            private ShaderFunction.Builder mainFunction;
            private HashSet<VariableReference> inputs;
            private ShaderType.StructBuilder inputBuilder;
            private HashSet<VariableReference> outputs;
            private ShaderType.StructBuilder outputBuilder;

            private Registry registry;
            private ShaderContainer container;
            public ShaderEvaluationCache(ShaderContainer container, Registry registry)
            {
                variableReferences = new Dictionary<VariableReference, VariableData>();
                portToVariableLookup = new Dictionary<ElementID, VariableReference>(new ElementIDComparer());
                initialized = new HashSet<VariableReference>();
                functionReferences = new Dictionary<FunctionReference, ShaderFunction>();
                includeReferences = new HashSet<ShaderFoundry.IncludeDescriptor>();
                typeReferences = new HashSet<ShaderType>();
                variableCollisions = new Dictionary<VariableReference, int>();
                this.registry = registry;
                this.container = container;
            }

            //Should this also take in the customization point? Then we can provide the known customization point inputs to the variable references. We still gather them at a later point anyways, but would
            //largely be a redundancy
            public ProcessingResult Process(NodeHandler node)
            {
                //starting with root node, gather all leaf nodes. //Create iterator of leaf first traversal
                //process each leaf node, setting up input and output variables,gathering any includes, and adding to the main function any initializations //while iterator has values
                //foreach value
                //  ask variable management to setup or verify variable initialization
                //  input variable management output into main function
                //  input function call into main function


                Block.Builder builder = new Block.Builder(container, $"{node.ID.LocalPath}Node");

                inputs = new HashSet<VariableReference>();
                inputBuilder = new ShaderType.StructBuilder(builder, $"ShaderGraph_Block_{node.ID.LocalPath}_Input");
                outputs = new HashSet<VariableReference>();
                outputBuilder = new ShaderType.StructBuilder(builder, $"ShaderGraph_Block_{node.ID.LocalPath}_Output");
                RegisterOutputs(node, builder);
                var outputType = outputBuilder.Build();
                mainFunction = new ShaderFunction.Builder(builder, $"{node.ID.LocalPath}Node_Main", outputType);

                var workset = GatherTreeLeafFirst(node);
                var toPrint = new List<(FunctionReference, IEnumerable<VariableReference>)>();
                foreach(var workunit in workset)
                {
                    var functionToCall = RegisterNodeDependencies(workunit);
                    var variables = RegisterVariables(workunit, functionToCall).ToList();
                    if (functionToCall.HasValue)
                    {
                        toPrint.Add((functionToCall.Value, variables));
                    }
                }
                foreach((var functionToCall, var variables) in toPrint)
                {
                    WriteFunctionCall(functionToCall, variables);
                }
                WriteOutputInitializationAndAssignment(node, outputType);
                mainFunction.AddLine("return Output;");


                var inputType = inputBuilder.Build();
                typeReferences.Add(inputType);
                typeReferences.Add(outputType);
                mainFunction.AddInput(inputType, "In");
                Finalize(ref builder);
                return new ProcessingResult()
                {
                    blockBuilder = builder
                };
            }

            private void Finalize(ref Block.Builder builder)
            {
                foreach (var type in typeReferences)
                {
                    builder.AddType(type);
                }
                foreach (var include in includeReferences)
                {
                    builder.AddInclude(include);
                }
                foreach (var func in functionReferences.Values)
                {
                    builder.AddFunction(func);
                }
                builder.SetEntryPointFunction(mainFunction.Build());

            }

            private void WriteFunctionCall(FunctionReference functionRef, IEnumerable<VariableReference> parameterReferences)
            {
                List<string> paramStrings = new List<string>();
                foreach(var paramRef in parameterReferences)
                {
                    var param = variableReferences[paramRef];
                    if (!param.inlineInitialization)
                    {
                        var s = param.actualVariableName ?? param.desiredVariableName;
                        //small handling case for first variable with variable collisions - basically if this variable had variable collisions but no actual name was assigned,
                        //this it was the first instance of the variable name and we need to create the actual variable name for it
                        if(param.actualVariableName == null && variableCollisions.ContainsKey(paramRef))
                        {
                            s = CreateActualVariableName(param.desiredVariableName, 0);
                        }
                        if (!initialized.Contains(paramRef))
                        {
                            mainFunction.AddVariableDeclarationStatement(param.shaderType, s, param.initializationString);
                            initialized.Add(paramRef);
                        }
                        paramStrings.Add(s);
                        //if this is a reference, we now know we are using it. Make sure it is added to the inputs.
                        if(!inputs.Contains(paramRef) && param is ReferableData referable)
                        {
                            inputs.Add(paramRef);
                            inputBuilder.AddField(referable.field);
                        }
                    }
                    else
                    {
                        paramStrings.Add(param.initializationString);
                    }

                }
                var function = functionReferences[functionRef];
                mainFunction.CallFunction(function, paramStrings.ToArray());
            }

            private void WriteOutputInitializationAndAssignment(NodeHandler node, ShaderType outputType)
            {
                mainFunction.AddLine($"{outputType.Name} Output;");
                foreach(var field in outputType.StructFields)
                {
                    var port = node.GetPort(field.Name);
                    if (port.IsInput)
                    {
                        var connport = port.GetFirstConnectedPort();
                        if (connport != null)
                        {
                            VariableReference varref = portToVariableLookup[connport.ID];
                            VariableData variable = variableReferences[varref];
                            if (!variable.inlineInitialization && initialized.Contains(varref))
                            {
                                mainFunction.AddLine($"Output.{field.Name} = {variable.actualVariableName ?? variable.desiredVariableName};");
                            }
                            else
                            {
                                mainFunction.AddLine($"Output.{field.Name} = {variable.initializationString};");
                            }
                        }
                        else
                        {
                            mainFunction.AddLine($"Output.{field.Name} = {registry.GetTypeBuilder(port.GetTypeField().GetRegistryKey()).GetInitializerList(port.GetTypeField(), registry)};");
                        }

                    }
                    else
                    {
                        if (portToVariableLookup.TryGetValue(port.ID, out VariableReference varref) && variableReferences.TryGetValue(varref, out VariableData variable))
                        {
                            if (!variable.inlineInitialization && initialized.Contains(varref))
                            {
                                mainFunction.AddLine($"Output.{field.Name} = {variable.actualVariableName ?? variable.desiredVariableName};");
                            }
                            else
                            {
                                mainFunction.AddLine($"Output.{field.Name} = {variable.initializationString};");
                            }
                        }
                        else
                        {
                            //this is an error?
                            mainFunction.AddLine($"Output.{field.Name} = {registry.GetTypeBuilder(port.GetTypeField().GetRegistryKey()).GetInitializerList(port.GetTypeField(), registry)};");
                        }
                    }
                }

            }

            #region Variable_Handling
            private IEnumerable<VariableReference> RegisterVariables(NodeHandler workunit, FunctionReference? functionReference)
            {
                //if this is a run of the mill, function generating node. Generate local variables to handle passing around block-local values.
                if (functionReference.HasValue)
                {
                    var function = functionReferences[functionReference.Value];
                    foreach (var param in function.Parameters)
                    {
                        var port = workunit.GetPort(param.Name);
                        if (param.IsInput)
                        {
                            //check and see if this port is already mapped to a variable - I dont think this should ever actually happen
                            if (portToVariableLookup.TryGetValue(port.ID, out VariableReference varref))
                            {
                                //TODO: Remove this line. Theoretically, the below code should handle the case, but its weird it got hit to begin with
                                Debug.LogWarning("If this message shows up, contact shadergraph team");
                                //while the variable reference might already exist, we may still need to cast it to the correct type
                                var data = variableReferences[varref];
                                if (data.shaderType != param.Type)
                                {
                                    //Handle cast and replace variable reference with casted data
                                    VariableData castData = CreateCastVariableData(data, param.Type);
                                    var castRef = new VariableReference(castData);
                                    variableReferences.Add(castRef, castData);
                                    portToVariableLookup.Remove(port.ID);
                                    portToVariableLookup.Add(port.ID, castRef);
                                    yield return castRef;
                                    continue;
                                }
                                else
                                {
                                    yield return varref;
                                    continue;
                                }
                            }
                            //if we are connected, then the variables should have been setup in a previous step. Conditionally cast and return them
                            var connection = port.GetFirstConnectedPort();
                            if (connection != null)
                            {
                                if (!portToVariableLookup.TryGetValue(connection.ID, out VariableReference connref))
                                {
                                    Debug.LogError("Previous node output variable was not setup properly");
                                }
                                else
                                {
                                    var data = variableReferences[connref];
                                    if (data.shaderType != param.Type)
                                    {
                                        VariableData castData = CreateCastVariableData(data, param.Type);
                                        var castRef = new VariableReference(castData);
                                        variableReferences.Add(castRef, castData);
                                        portToVariableLookup.Add(port.ID, castRef);
                                        yield return castRef;
                                        continue;
                                    }
                                    else
                                    {
                                        //May want to promote inline to variable here, since this would indicate the value being used in more than one place
                                        portToVariableLookup.Add(port.ID, connref);
                                        yield return connref;
                                        continue;
                                    }
                                }
                            }
                            //Otherwise, we need to setup a new variable for the port
                            yield return CreateVariableData(port, function, param);
                        }
                        else
                        {
                            yield return CreateVariableData(port, function, param);
                        }
                    }
                }
                //Otherwise this is a node with some sort of data. In terms of interpretation, we only really need to know variables that are being supplied.
                //Is it a context node?
                else if(workunit.HasMetadata("_contextDescriptor"))
                {
                    foreach(var port in workunit.GetPorts())
                    {
                        if(port.IsHorizontal && port.IsInput)
                        {
                            yield return CreateReferableData(port);
                        }
                    }
                }
            }

            private void RegisterOutputs(NodeHandler root, Block.Builder builder)
            {
                bool useNodeInputs = root.HasMetadata("_contextDescriptor");
                foreach(var port in root.GetPorts())
                {
                    if(port.IsHorizontal && (port.IsInput == useNodeInputs))
                    {
                        CreateOutputData(port, builder);
                    }
                }
            }

            private VariableReference CreateOutputData(PortHandler port, Block.Builder builder)
            {
                var name = $"Output.{port.ID.LocalPath}";
                var portTypeField = port.GetTypeField();
                var shaderType = registry.GetTypeBuilder(portTypeField.GetRegistryKey()).GetShaderType(portTypeField, builder.Container, registry);
                ReferableData data = new ReferableData
                {
                    desiredVariableName = name,
                    actualVariableName = name,
                    shaderType = shaderType,
                    inlineInitialization = false,
                    field = new StructField.Builder(builder.Container, port.ID.LocalPath, shaderType).Build()
                };

                VariableReference reference = new VariableReference(data);
                variableReferences.Add(reference, data);
                outputBuilder.AddField(data.field);
                return reference;

            }

            private VariableReference CreateVariableData(PortHandler port, ShaderFunction function, FunctionParameter param)
            {
                string initialization = null;
                //we dont initialize function outputs - the function calls do that
                if (param.IsInput)
                {
                    initialization = registry.GetTypeBuilder(port.GetTypeField().GetRegistryKey()).GetInitializerList(port.GetTypeField(), registry);
                }
                //generally, inlining values makes more readable code unless the same value is being used in multiple places.
                bool inline = port.IsInput;
                string desiredName = $"{function.Name}_{param.Name}";
                VariableData data = new VariableData
                {
                    desiredVariableName = desiredName,
                    initializationString = initialization,
                    inlineInitialization = inline,
                    shaderType = param.Type,
                    actualVariableName = null
                };
                VariableReference reference = new VariableReference(data);
                if (variableReferences.TryGetValue(reference, out var existingData))
                {
                    //there was a variable name collision - need to resolve it
                    ResolveVariableNameCollision(reference, data, existingData, out VariableReference newRef, out VariableData newData);
                    variableReferences.Add(newRef, newData);
                    portToVariableLookup.Add(port.ID, newRef);
                    return newRef;
                }
                else
                {
                    variableReferences.Add(reference, data);
                    portToVariableLookup.Add(port.ID, reference);
                    return reference;
                }
            }

            private VariableReference CreateReferableData(PortHandler port)
            {
                //temp hookup to old system
                var tmpOutputs = new VariableRegistry();
                var tmpInputs = new VariableRegistry();
                var tmpText = new List<(string, Texture)>();
                BuildPropertyAttributes(port, registry, mainFunction.Container, ref tmpOutputs, ref tmpInputs, ref tmpText);

                StructField field;
                var portTypeField = port.GetTypeField();
                var shaderType = registry.GetTypeBuilder(portTypeField.GetRegistryKey()).GetShaderType(portTypeField, container, registry);
                if(tmpInputs.Any())
                {
                    field = tmpInputs.First();
                }
                else
                {
                    field = new StructField.Builder(container, port.ID.LocalPath, shaderType).Build();
                }

                var name = $"In.{field.Name}";


                ReferableData data = new ReferableData
                {
                    desiredVariableName = name,
                    actualVariableName = name,
                    shaderType = shaderType,
                    inlineInitialization = false,
                    field = field
                };

                VariableReference reference = new VariableReference(data);
                variableReferences.Add(reference, data);
                if (!portToVariableLookup.ContainsKey(port.ID))
                {
                    portToVariableLookup.Add(port.ID, reference);
                }
                portToVariableLookup.Add(port.GetNode().GetPort($"out_{port.ID.LocalPath}").ID, reference);
                initialized.Add(reference);
                return reference;
            }

            //Our current naming scheme for variables is {NodeName}_{VariableName}. If there is collision, then we instead do {NodeName}{NodeCount}_{VariableName}. Two
            //instances of node Foo with variable Bar would create variables Foo1_Bar and Foo2_Bar.
            private string CreateActualVariableName(string desiredName, int collisionCount)
            {
                string output = "";
                foreach(char c in desiredName)
                {
                    if(c == '_')
                    {
                        output += collisionCount + 1;
                    }
                    output += c;
                }
                return output;
            }

            //We keep track of the number of collisions for naming conventions
            private void ResolveVariableNameCollision(VariableReference reference, VariableData data, VariableData existingData, out VariableReference newRef, out VariableData newData)
            {
                int currentCollisions;
                if(variableCollisions.TryGetValue(reference, out currentCollisions))
                {
                    currentCollisions++;
                    variableCollisions[reference] = currentCollisions;
                }
                else
                {
                    currentCollisions = 1;
                    variableCollisions[reference] = currentCollisions;
                }

                newData = new VariableData
                {
                    actualVariableName = CreateActualVariableName(data.desiredVariableName, currentCollisions),
                    shaderType = data.shaderType,
                    desiredVariableName = data.desiredVariableName,
                    initializationString = data.initializationString,
                    inlineInitialization = data.inlineInitialization,
                };
                newRef = new VariableReference(newData);
            }

            private VariableData CreateCastVariableData(VariableData data, ShaderType type)
            {
                return new VariableData()
                {
                    desiredVariableName = $"Cast_{data.actualVariableName ?? data.desiredVariableName}",
                    initializationString = "CAST_TODO",
                    shaderType = type,
                    inlineInitialization = true
                };
            }
            #endregion

            #region Dependency_Handling
            private FunctionReference? RegisterNodeDependencies(NodeHandler workunit)
            {
                var nodeBuilder = registry.GetNodeBuilder(workunit.GetRegistryKey());
                List<ShaderFoundry.IncludeDescriptor> localIncludes = new();
                var func = nodeBuilder.GetShaderFunction(workunit, mainFunction.Container, registry, out var dependencies);
                if (dependencies.localFunctions != null)
                {
                    foreach (var f in dependencies.localFunctions)
                    {
                        EnsureFunction(func);
                    }
                }
                if (dependencies.includes != null)
                {
                    foreach (var inc in dependencies.includes)
                    {
                        EnsureInclude(inc);
                    }
                }
                //If the node is not one that generates a function (and function is a struct and cant be null), then we check by seeing if the provided function is valid
                if (func.IsValid)
                {
                    return EnsureFunction(func);
                }
                else
                {
                    return null;
                }
            }

            private void EnsureInclude(ShaderFoundry.IncludeDescriptor include)
            {
                includeReferences.Add(include);
            }

            private FunctionReference EnsureFunction(ShaderFunction function)
            {
                var reference = new FunctionReference(function);
                if(!functionReferences.ContainsKey(reference))
                {
                    functionReferences.Add(reference, function);
                }
                foreach(var param in function.Parameters)
                {
                    typeReferences.Add(param.Type);
                }
                return reference;
            }
            #endregion
        }


        internal static class InterpreterTestStub
        {
            public static void GetShaderFunctionInHumanReadableForm(ref StringBuilder sb, NodeHandler node, Registry reg, RegistryKey key)
            {
                Interpreter.GetShaderFunctionInHumanReadableForm(ref sb, reg.GetNodeBuilder(key).GetShaderFunction(node, new ShaderContainer(), reg, out _));
            }

            public static string GetShaderBlockInHumanReadableForm(NodeHandler node, Registry registry)
            {
                return Interpreter.GetShaderBlockInHumanReadableForm(GetShaderBlockForNode(node, registry, new ShaderContainer()));
            }
        }

        internal static void GetShaderFunctionInHumanReadableForm(ref StringBuilder sb, ShaderFunction function)
        {
            sb.Append(function.ReturnType.Name + " " + function.Name + "(");
            bool any = false;
            foreach (var param in function.Parameters)
            {
                any = true;
                sb.Append($"{param.Type.Name} {param.Name}, ");
            }
            if (any)
            {
                sb.Remove(sb.Length - 2, 2);
            }
            sb.AppendLine(")");
            sb.AppendLine("{");
            sb.Append(function.Body);
            sb.AppendLine("}");
        }

        internal static string GetShaderBlockInHumanReadableForm(BlockSequenceElement block)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("//INCLUDES------");
            foreach(var include in block.Block.Includes)
            {
                sb.AppendLine(include.Value);
            }

            sb.AppendLine();
            sb.AppendLine();

            sb.AppendLine("//FUNCTIONS------");
            foreach(var function in block.Block.Functions)
            {
                if (function != block.Block.EntryPointFunction)
                {
                    GetShaderFunctionInHumanReadableForm(ref sb, function);
                }
            }

            sb.AppendLine();
            sb.AppendLine();

            sb.AppendLine("//BLOCK INPUT------");
            var input = block.Block.EntryPointFunction.Parameters.First();
            var inputFields = input.Type.StructFields;
            sb.AppendLine($"struct {input.Type.Name}");
            sb.AppendLine("{");
            foreach(var f in inputFields)
            {
                sb.AppendLine($"    {f.Type.Name} {f.Name};");
            }
            sb.AppendLine("}");

            sb.AppendLine();

            sb.AppendLine("//BLOCK OUTPUT------");
            var outputType = block.Block.EntryPointFunction.ReturnType;
            var outputFields = outputType.StructFields;
            sb.AppendLine($"struct {outputType.Name}");
            sb.AppendLine("{");
            foreach (var f in outputFields)
            {
                sb.AppendLine($"    {f.Type.Name} {f.Name};");
            }
            sb.AppendLine("}");


            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("//MAIN FUNCTION------");
            GetShaderFunctionInHumanReadableForm(ref sb, block.Block.EntryPointFunction);

            return sb.ToString();

        }

        //THIS IS A REFERENCE FOR LIZ
        //private struct WorkUnit
        //{
        //    //The overarching shader foundary container. Everything gets created and managed through this.
        //    public ShaderContainer container;
        //    //The builder for an individual shader block.
        //    public Block.Builder builder;
        //    //Every shader block needs an "entry point function" - this is the main code for the block
        //    public ShaderFunction.Builder mainBodyFunctionBuilder;
        //    //Still need a cache object of some kind to keep track of functions and variables
        //}

        public static string GetShaderStringForGraph(GraphHandler graph)
        {
            return null;
        }

        internal static string GetShaderStringForNode(NodeHandler node, Registry registry)
        {
            return GetShaderBlockInHumanReadableForm(GetShaderBlockForNode(node, registry, new ShaderContainer()));
        }

        internal static BlockSequenceElement GetShaderBlockForNode(NodeHandler node, Registry registry, ShaderContainer shaderContainer)
        {
            ShaderEvaluationCache sec = new ShaderEvaluationCache(shaderContainer, registry/*any SF stuff needed for creation of block instance*/);
            var workunit = sec.Process(node);
            //builder.AddInput(sec.InputType)
            //builder.AddOutput
            //etc
            var block = workunit.blockBuilder.Build();
            var blockDescBuilder = new BlockSequenceElement.Builder(shaderContainer, block);
            return blockDescBuilder.Build();
        }


        //This will replace the other GatherTreeLeafFirst, but since it makes a slightly different assumption than the initial one leaving both code paths separate for now
        private static IEnumerable<NodeHandler> GatherTreeLeafFirstV2(NodeHandler rootNode)
        {
            Stack<NodeHandler> stack = new();
            HashSet<string> visited = new();


            var cpName = rootNode.GetField<string>("_CustomizationPointName");
            string cpString = defaultSurfaceDescriptionCPName;
            if(cpName != null)
            {
                cpString = cpName.GetData();
            }

            stack.Push(rootNode);
            while (stack.Count > 0)
            {
                NodeHandler check = stack.Peek();
                bool isLeaf = true;

                bool isContext = check.HasMetadata("_contextDescriptor");
                //We may still include a context node as part of the tree IFF the context node belongs to the same customization point
                bool include = false;
                if(isContext)
                {
                    var f = check.GetField<string>("_CustomizationPointName");
                    if(f != null && f.GetData().Equals(cpString, StringComparison.Ordinal))
                    {
                        include = true;
                    }
                }
                if (!isContext || check.ID.Equals(rootNode.ID) || include)
                {
                    foreach (PortHandler port in check.GetPorts())
                    {
                        if (port.IsInput)
                        {
                            foreach (NodeHandler connected in GetConnectedNodes(port))
                            {
                                if (!visited.Contains(connected.ID.FullPath))
                                {
                                    visited.Add(connected.ID.FullPath);
                                    isLeaf = false;
                                    stack.Push(connected);
                                }
                            }
                        }
                    }
                }
                if (isLeaf)
                {
                    yield return stack.Pop();
                }
            }

        }



        private static void GetBlocksV2(ShaderContainer container,
                                        CustomizationPoint vertexCP,
                                        CustomizationPoint surfaceCP,
                                        NodeHandler node,
                                        GraphHandler graph,
                                        Registry registry,
                                        ref List<(string, Texture)> defaultTextures,
                                        out CustomizationPointImplementation vertexCPDesc,
                                        out CustomizationPointImplementation surfaceCPDesc)
        {
            vertexCPDesc = CustomizationPointImplementation.Invalid; // we currently do not use the vertex customization point
            var surfaceDescBuilder = new CustomizationPointImplementation.Builder(container, surfaceCP);
            var vertexDescBuilder = new CustomizationPointImplementation.Builder(container, vertexCP);
            var blockInstance = GetShaderBlockForNode(node, registry, container);
            Debug.Log(GetShaderBlockInHumanReadableForm(blockInstance));
            surfaceDescBuilder.AddBlockSequenceElement(blockInstance);
            surfaceCPDesc = surfaceDescBuilder.Build();
            
        }

        private class ShaderFunctionRegistry : List<ShaderFunction>
        {
            public void EnsureFunctionPresent(ShaderFunction function)
            {
                bool alreadyContained = false;
                foreach(var f in this)
                {
                    if(FunctionsAreEqual(f, function))
                    {
                        alreadyContained = true;
                        break;
                    }
                }
                if(!alreadyContained)
                {
                    Add(function);
                }
            }

            public void EnsureFunctionsPresent(IEnumerable<ShaderFunction> functions)
            {
                foreach(var f in functions)
                {
                    EnsureFunctionPresent(f);
                }
            }

        }

        private class StructFieldEqualityComparer : IEqualityComparer<StructField>
        {
            public bool Equals(StructField x, StructField y)
            {
                return string.CompareOrdinal(x.Name, y.Name) == 0;
            }

            public int GetHashCode(StructField obj)
            {
                return obj.Name.GetHashCode();
            }
        }

        internal class VariableRegistry : IEnumerable<StructField>
        {
            HashSet<StructField> m_set;
            List<StructField> m_list;
            public VariableRegistry() : base()
            {
                m_set = new HashSet<StructField>(new StructFieldEqualityComparer());
                m_list = new List<StructField>();
            }

            public void Add(StructField field)
            {
                if (m_set.Add(field))
                {
                    m_list.Add(field);
                }

#if INTERPRETER_DEBUG
                else
                {
                    Debug.Log($"Rejecting StructField {field.Type.Name} {field.Name}; duplicate");
                }
#endif
            }

            public IEnumerator<StructField> GetEnumerator()
            {
                return m_list.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return m_list.GetEnumerator();
            }

            public StructField this[int i]
            {
                get => m_list[i];
            }
        }
        /// <summary>
        /// There's a collection of required and potentially useful pieces of data
        /// that are cached as a part of the preview management.
        /// Things like the compiled shader, material, the shader code, etc. This
        /// function caches the code local only to the node, whereas the subsequent
        /// function GetBlockCode provides the code for all upstream nodes.
        public static string GetFunctionCode(NodeHandler node, Registry registry)
        {
            var builder = new ShaderBuilder();
            var func = registry.GetNodeBuilder(node.GetRegistryKey()).GetShaderFunction(node, new ShaderContainer(), registry, out var dependencies);
            builder.AddDeclarationString(func);
            if (dependencies.localFunctions != null)
            {
                foreach (var dep in dependencies.localFunctions)
                    builder.AddDeclarationString(dep);
            }
            return builder.ConvertToString();
        }

        public static string GetBlockCode(NodeHandler node, GraphHandler graph, Registry registry, ref List<(string, UnityEngine.Texture)> defaultTextures)
        {
            var container = new ShaderContainer();
            var scpBuilder = new CustomizationPointImplementation.Builder(container, CustomizationPoint.Invalid);
            var vcpBuilder = new CustomizationPointImplementation.Builder(container, CustomizationPoint.Invalid);
            var fragBlockSet = new HashSet<string>();
            var vertBlockSet = new HashSet<string>();
            EvaluateGraphAndPopulateDescriptors(node, graph, container, registry,ref vcpBuilder, ref scpBuilder, ref defaultTextures, ref fragBlockSet, ref vertBlockSet, null, null);
            return GetShaderBlockInHumanReadableForm(GetShaderBlockForNode(node, registry, container));
        }

        private static void GetBlocks(ShaderContainer    container,
                              CustomizationPoint vertexCP,
                              CustomizationPoint surfaceCP,
                              NodeHandler node,
                              GraphHandler graph,
                              Registry registry,
                              ref List<(string, UnityEngine.Texture)> defaultTextures,
                          out CustomizationPointImplementation vertexCPDesc,
                          out CustomizationPointImplementation surfaceCPDesc)
        {
            vertexCPDesc = CustomizationPointImplementation.Invalid; // we currently do not use the vertex customization point

            var surfaceDescBuilder = new CustomizationPointImplementation.Builder(container, surfaceCP);
            var vertexDescBuilder = new CustomizationPointImplementation.Builder(container, vertexCP);
            var fragBlockSet = new HashSet<string>();
            var vertBlockSet = new HashSet<string>();
            EvaluateGraphAndPopulateDescriptors(node, graph, container, registry, ref vertexDescBuilder, ref surfaceDescBuilder, ref defaultTextures, ref fragBlockSet, ref vertBlockSet, vertexCP.Name, surfaceCP.Name);
            surfaceCPDesc = surfaceDescBuilder.Build();
            //vertexCPDesc = vertexDescBuilder.Build();
        }

        // TODO: Passing in the target directly is not what we want to do here, but having it be live gives us a clearer basis
        // to refactor from when we introduce targets/templates and explore whether we should abstract all of this from either one.
        internal static string GetShaderForNode(NodeHandler node, GraphHandler graph, Registry registry, out List<(string, UnityEngine.Texture)> defaultTextures, Target target = null, string shaderName = null)
        {
            List<(string, UnityEngine.Texture)> defaults = new();
            void lambda(ShaderContainer container, CustomizationPoint vertex, CustomizationPoint fragment, out CustomizationPointImplementation vertexCPDesc, out CustomizationPointImplementation fragmentCPDesc)
#if INTERPRETER_V2
                => GetBlocksV2(container, vertex, fragment, node, graph, registry, ref defaults, out vertexCPDesc, out fragmentCPDesc);
#else
                => GetBlocks(container, vertex, fragment, node, graph, registry, ref defaults, out vertexCPDesc, out fragmentCPDesc);
#endif

            var shader = SimpleSampleBuilder.Build(new ShaderContainer(), target ?? SimpleSampleBuilder.GetTarget(), shaderName ?? node.ID.LocalPath, lambda, String.Empty);

            defaultTextures = new();
            defaultTextures.AddRange(defaults);
            return shader.codeString;
        }

        private static ShaderType BuildStructFromVariables(ShaderContainer container, string name, IEnumerable<StructField> variables, Block.Builder blockBuilder)
        {
            var structBuilder = new ShaderType.StructBuilder(blockBuilder, name);

            foreach (var field in variables)
                structBuilder.AddField(field);

            return structBuilder.Build();
        }

        static void EvaluateBlockReferrables(
            NodeHandler rootNode,
            Registry registry,
            ShaderContainer container,
            ref VariableRegistry outputVariables,
            ref VariableRegistry inputVariables,
            ref List<(string, UnityEngine.Texture)> defaultTextures)
        {
            var isContext = rootNode.HasMetadata("_contextDescriptor");
            foreach (var port in rootNode.GetPorts())
            {
                if (port.IsHorizontal && (isContext ? port.IsInput : !port.IsInput))
                {
                    BuildPropertyAttributes(port, registry, container, ref outputVariables, ref inputVariables, ref defaultTextures);
                }
            }
        }

        internal static void BuildPropertyAttributes(
            PortHandler port,
            Registry registry,
            ShaderContainer container,
            ref VariableRegistry outputVariables,
            ref VariableRegistry inputVariables,
            ref List<(string, UnityEngine.Texture)> defaultTextures)
        {
            var name = port.ID.LocalPath;
            var portTypeField = port.GetTypeField();
            var shaderType = registry.GetTypeBuilder(portTypeField.GetRegistryKey()).GetShaderType(portTypeField, container, registry);
            var varOutBuilder = new StructField.Builder(container, name, shaderType);

            // TODO: This entire step should be deferred to the Type to determine how to process the Property rules,
            // and also warn on bad property rules.
            if (port.GetTypeField().GetRegistryKey().Name == SamplerStateType.kRegistryKey.Name)
            {
                var inVar = SamplerStateType.UniformPromotion(port.GetTypeField(), container, registry);
                inputVariables.Add(inVar);
                outputVariables.Add(varOutBuilder.Build());
                return;
            }

            var usage = PropertyBlockUsage.Excluded;
            var usageField = port.GetField <PropertyBlockUsage>(kPropertyBlockUsage);
            if(usageField != null)
            {
                usage = usageField.GetData();
            }
            PropertyAttribute propertyData = null;
            switch (usage)
            {
                case PropertyBlockUsage.Included:
                    propertyData = new PropertyAttribute { DefaultValue = port.GetDefaultValueString(registry, container), DisplayName = port.GetDisplayNameString(), Exposed = true };
                    var varInBuilder = new StructField.Builder(container, name, shaderType);

                    if (portTypeField.GetRegistryKey().Name == GraphType.kRegistryKey.Name)
                    {
                        var isColor = GraphTypeHelpers.GetLength(portTypeField) >= GraphType.Length.Three &&
                            (port.GetField<bool>(kIsColor)?.GetData() ?? false);

                        if (isColor)
                        {
                            varInBuilder.AddAttribute(new ShaderAttribute.Builder(container, "Color").Build());
                            if (port.GetField<bool>(kIsHdr)?.GetData() ?? false)
                            {
                                varInBuilder.AddAttribute(new ShaderAttribute.Builder(container, "HDR").Build());
                            }
                        }
                    }

                    if (portTypeField.GetRegistryKey().Name == BaseTextureType.kRegistryKey.Name)
                    {
                        var useTilingOffset = port.GetField<bool>(kTextureUseTilingOffset)?.GetData() ?? false;
                        varInBuilder.AddAttribute(new ShaderAttribute.Builder(container, useTilingOffset ? "ScaleOffset" : "NoScaleOffset").Build());

                        var isNormalMap = port.GetField<TextureDefaultType>(kTextureDefaultType)?.GetData() is TextureDefaultType.NormalMap;
                        if (isNormalMap)
                        {
                            varInBuilder.AddAttribute(new ShaderAttribute.Builder(container, "Normal").Build());
                        }
                    }

                    SimpleSampleBuilder.MarkAsProperty(container, varInBuilder, propertyData);
                    inputVariables.Add(varInBuilder.Build());
                    break;
                case PropertyBlockUsage.Excluded:
                    var source = DataSource.Constant;
                    var sourceField = port.GetField<DataSource>(kDataSource);
                    if (sourceField != null)
                    {
                        source = sourceField.GetData();
                    }
                    if(source != DataSource.Constant)
                    {
                        propertyData = new PropertyAttribute { DataSource = UniformDataSource.Global, UniformName = port.LocalID, Exposed = false };
                        var varInputBuilder = new StructField.Builder(container, name, shaderType);
                        SimpleSampleBuilder.MarkAsProperty(container, varInputBuilder, propertyData);
                        inputVariables.Add(varInputBuilder.Build());
                    }
                    break;
                default:
                    break;
            }

            if (portTypeField.GetRegistryKey().Name == BaseTextureType.kRegistryKey.Name)
            {
                var fieldHandler = port.GetTypeField();
                var tex = BaseTextureType.GetTextureAsset(fieldHandler);
                if (tex != null && !defaultTextures.Contains((name, tex)))
                    defaultTextures.Add((name, tex));
            }


            outputVariables.Add(varOutBuilder.Build());
        }

        internal static void EvaluateGraphAndPopulateDescriptors(
                NodeHandler rootNode,
                GraphHandler shaderGraph,
                ShaderContainer container,
                Registry registry,
                ref CustomizationPointImplementation.Builder vertexDescBuilder,
                ref CustomizationPointImplementation.Builder surfaceDescBuilder,
                ref List<(string, UnityEngine.Texture)> defaultTextures,
                //TODO: Liz, figure out why we are generating duplicate blocks to begin with
                ref HashSet<string> includedFragBlocks,
                ref HashSet<string> includedVertBlocks,
                string vertexName,
                string surfaceName,
                Dictionary<ElementID, HashSet<ElementID>> depList = null
            )
        {
            depList ??= GraphHandlerUtils.BuildDependencyList(shaderGraph);
            var topoList = GraphHandlerUtils.GetUpstreamNodesTopologically(shaderGraph, rootNode.ID, depList, true).Select(e => new NodeHandler(e, shaderGraph.graphDelta, registry)).ToList();


            /* PSEDUOCODE
             * Given a root node and graph, we need to know if the node isn't a context node.
             *      If the node isnt a context node, then we know we will need a remap block from the
             *      output of the node to "BaseColor" for previews
             *
             * In either case, we then need to create the block outputs for the given node based on its ports
             *
             * We want to gather all upstream nodes from the rootnode, and start evaluating the expression tree
             *
             * If we hit a context node upstream, then we need to recurse on that context node and make sure its outputs
             *   match our expected inputs
             */


            string BlockName = $"ShaderGraphBlock_{rootNode.ID.LocalPath}";
            var blockBuilder = new Block.Builder(container, BlockName);

            var inputVariables = new VariableRegistry();
            var outputVariables = new VariableRegistry();
            bool isContext = rootNode.HasMetadata("_contextDescriptor");
            //Evaluate outputs for this block based on root nodes "outputs/endpoints" (horizontal input ports)
            EvaluateBlockReferrables(rootNode, registry, container, ref outputVariables, ref inputVariables, ref defaultTextures);

            //Create output type from evaluated root node outputs
            var outputType = BuildStructFromVariables(container, $"{BlockName}Output", outputVariables, blockBuilder);


            var mainBodyFunctionBuilder = new ShaderFunction.Builder(container, $"SYNTAX_{rootNode.ID.LocalPath}Main", outputType);

            var shaderFunctions = new ShaderFunctionRegistry();
            var includes = new List<ShaderFoundry.IncludeDescriptor>();
            foreach(var node in topoList)
            {
                //if the node is a context node (and not the root node) we recurse
                if (node.HasMetadata("_contextDescriptor"))
                {
                    if (!node.ID.Equals(rootNode.ID))
                    {
                        //evaluate the upstream context's block
                        EvaluateGraphAndPopulateDescriptors(node, shaderGraph, container, registry, ref vertexDescBuilder, ref surfaceDescBuilder, ref defaultTextures, ref includedFragBlocks, ref includedVertBlocks, vertexName, surfaceName, depList);
                        //create inputs to our block based on the upstream context's outputs
                        foreach (var port in node.GetPorts())
                        {
                            if (port.IsHorizontal && port.IsInput)
                            {
                                var name = port.ID.LocalPath;
                                var type = registry.GetTypeBuilder(port.GetTypeField().GetRegistryKey()).GetShaderType(port.GetTypeField(), container, registry);
                                var varInBuilder = new StructField.Builder(container, name, type);
                                var varIn = varInBuilder.Build();
                                inputVariables.Add(varIn);
                            }
                        }
                    }

                }
                else
                {
                    ProcessNode(node, ref container, ref inputVariables, ref outputVariables, ref defaultTextures, ref blockBuilder, ref mainBodyFunctionBuilder, ref shaderFunctions, ref includes, registry);
                }
            }

            // Should get us every uniquely defined parameter-- not sure how this handles intrinsics- ehh...
            var shaderTypes = shaderFunctions.SelectMany(e => e.Parameters).Select(p => p.Type).ToHashSet();
            foreach(var type in shaderTypes)
            {
                blockBuilder.AddType(type);
            }
             // TODO: https://github.com/Unity-Technologies/Graphics/pull/7079 and following changes in Graphics repo
            // will allow includes to be added directly to ShaderFunctions instead.
            foreach (var include in includes)
            {
                blockBuilder.AddInclude(include);
            }

            var inputType = BuildStructFromVariables(container, $"{BlockName}Input", inputVariables, blockBuilder);

            mainBodyFunctionBuilder.AddLine($"{BlockName}Block::{outputType.Name} output;");
            int varIndex = 0;
            foreach(PortHandler port in rootNode.GetPorts())
            {
                if(port.IsHorizontal)
                {
                    if (port.IsInput && isContext)
                    {
                        //var entry = port.GetTypeField().GetSubField<IContextDescriptor.ContextEntry>(Registry.Types.GraphType.kEntry).GetData();
                        var connectedPort = port.GetConnectedPorts().FirstOrDefault();
                        if (connectedPort != null) // connected input port-
                        {
                            var connectedNode = connectedPort.GetNode();
                            if (connectedNode.HasMetadata("_contextDescriptor"))
                            {
                                mainBodyFunctionBuilder.AddLine($"output.{outputVariables[varIndex++].Name} = In.{connectedPort.ID.LocalPath.Replace("out_","")};");
                            }
                            else
                            {
                                mainBodyFunctionBuilder.AddLine($"output.{outputVariables[varIndex++].Name} = {ApplyConversionAndReturnConvertedVariable(connectedPort, port, registry, container, ref mainBodyFunctionBuilder, port.GetShaderType(registry, container).Name, ref shaderFunctions)};");
                            }
                        }
                        else // not connected.
                        {
                            EvaluateContextOutputInitialization(mainBodyFunctionBuilder, port, outputVariables[varIndex++].Name, registry);
                        }
                    }
                    else if(!port.IsInput && !isContext)
                    {
                        mainBodyFunctionBuilder.AddLine($"output.{outputVariables[varIndex++].Name} = SYNTAX_{rootNode.ID.LocalPath}_{port.ID.LocalPath};");
                    }
                }
            }

            foreach (var func in shaderFunctions)
            {
                blockBuilder.AddFunction(func);
            }

            mainBodyFunctionBuilder.AddLine("return output;");

            // Setup the block from the inputs, outputs, types, functions
            blockBuilder.AddType(inputType);
            blockBuilder.AddType(outputType);
            mainBodyFunctionBuilder.AddInput(inputType, "In");
            blockBuilder.SetEntryPointFunction(mainBodyFunctionBuilder.Build());
            var block = blockBuilder.Build();
            var blockDescBuilder = new BlockSequenceElement.Builder(container, block);
            var blockDesc = blockDescBuilder.Build();
            var cpName = rootNode.GetField<string>("_CustomizationPointName");
            if (!isContext || cpName == null || (cpName != null && cpName.GetData().Equals(surfaceName)))
            {
                if (!includedFragBlocks.Contains(blockDesc.Block.Name))
                {
                    surfaceDescBuilder.AddBlockSequenceElement(blockDesc);
                    includedFragBlocks.Add(blockDesc.Block.Name);
                }
            }
            else if(isContext && cpName != null && cpName.GetData().Equals(vertexName))
            {
                if (!includedVertBlocks.Contains(blockDesc.Block.Name))
                {
                    vertexDescBuilder.AddBlockSequenceElement(blockDesc);
                    includedVertBlocks.Add(blockDesc.Block.Name);
                }
            }

            //if the root node was not a context node, then we need to remap an output to the expected customization point output
            if (!isContext)
            {

                string remapBlockName = $"ShaderGraphBlock_{rootNode.ID.LocalPath}_REMAP";
                var remapBuilder = new Block.Builder(container, remapBlockName);

                var remapFromVariables = outputVariables;
                var remapToVariables = new List<StructField>();


                var colorOutBuilder = new StructField.Builder(container, "BaseColor", container._float3);
                var colorOut = colorOutBuilder.Build();
                remapToVariables.Add(colorOut);

                var remapInputType  = BuildStructFromVariables(container, $"{remapBlockName}Input",  remapFromVariables, remapBuilder);
                var remapOutputType = BuildStructFromVariables(container, $"{remapBlockName}Output", remapToVariables,   remapBuilder);

                var remapMainBodyFunctionBuilder = new ShaderFunction.Builder(container, $"SYNTAX_{remapBlockName}Main", remapOutputType);
                remapMainBodyFunctionBuilder.AddInput(remapInputType, "inputs");
                remapMainBodyFunctionBuilder.AddLine($"{remapBlockName}Block::{remapOutputType.Name} output;");
                var remap = remapFromVariables.FirstOrDefault();
                remapMainBodyFunctionBuilder.AddLine($"output.BaseColor = {ConvertToFloat3(remap.Type, $"inputs.{remap.Name}")};");
                remapMainBodyFunctionBuilder.AddLine("return output;");

                remapBuilder.AddType(remapInputType);
                remapBuilder.AddType(remapOutputType);
                remapBuilder.SetEntryPointFunction(remapMainBodyFunctionBuilder.Build());

                var remapBlock = remapBuilder.Build();
                var remapBlockDescBuilder = new BlockSequenceElement.Builder(container, remapBlock);
                var remapBlockDesc = remapBlockDescBuilder.Build();
                surfaceDescBuilder.AddBlockSequenceElement((remapBlockDesc));

            }
        }

        private static void EvaluateContextOutputInitialization(ShaderFunction.Builder mainBodyFunctionBuilder, PortHandler port, string name, Registry registry)
        {
            var field = port.GetTypeField();

            var source = DataSource.Constant;
            var sourceField = port.GetField<DataSource>(kDataSource);
            if (sourceField != null)
            {
                source = sourceField.GetData();
            }
            // basically, unless this is a completely inlined/constant value, this is just a passthrough from somewhere else.
            if (source == DataSource.Constant)
            {
                mainBodyFunctionBuilder.AddLine($"output.{name} = {registry.GetTypeBuilder(port.GetTypeField().GetRegistryKey()).GetInitializerList(field, registry)};");
            }
            else
            {
                mainBodyFunctionBuilder.AddLine($"output.{name} = In.{name};");
            }

        }

        private static ShaderType EvaluateShaderType(ContextEntry entry, ShaderContainer container)
        {
            //length by height
            string lxh = "";
            if((int)entry.length > 1 || (int)entry.height > 1)
            {
                lxh += (int)entry.length;
            }
            if((int)entry.height > 1)
            {
                lxh += "x" + (int)entry.height;
            }
            switch (entry.primitive)
            {
                case GraphType.Primitive.Bool:
                    return container.GetType($"bool{lxh}");
                case GraphType.Primitive.Int:
                    return container.GetType($"int{lxh}");
                case GraphType.Primitive.Float:
                    if (entry.precision == GraphType.Precision.Single)
                    {
                        return container.GetType($"double{lxh}");
                    }
                    else
                    {
                        return container.GetType($"float{lxh}");
                    }
                default:
                    throw new ArgumentException("unsupported type");
            }
        }

        private static bool FunctionsAreEqual(ShaderFunction a, ShaderFunction b)
        {
            if(a.Name.CompareTo(b.Name) != 0)
            {
                return false;
            }

            var aParams = a.Parameters.ToList();
            var bParams = b.Parameters.ToList();

            if(aParams.Count != bParams.Count)
            {
                return false;
            }

            for(int i = 0; i < aParams.Count(); ++i)
            {
                if(aParams[i].IsInput != bParams[i].IsInput
                || aParams[i].Type    != bParams[i].Type
                || aParams[i].IsValid != bParams[i].IsValid)//does this one need to be checked?
                {
                    return false;
                }
            }
            return true;
        }

        private static string ApplyConversionAndReturnConvertedVariable(PortHandler fromPort,
                                                                        PortHandler toPort,
                                                                        Registry registry,
                                                                        ShaderContainer container,
                                                                        ref ShaderFunction.Builder mainBodyFunctionBuilder,
                                                                        string convertedTypeName,
                                                                        ref ShaderFunctionRegistry shaderFunctions,
                                                                        string toConvertNameOverride = null)
        {
            var connectedNode = fromPort.GetNode();
            var toConvert = $"SYNTAX_{connectedNode.ID.LocalPath}_{fromPort.ID.LocalPath}";
            if(toConvertNameOverride != null)
            {
                toConvert = toConvertNameOverride;
            }
            var converted = $"CONVERT_{toPort.GetNode().ID.LocalPath}_{toPort.ID.LocalPath}";
            var cast = registry.GetCast(fromPort, toPort);
            var castFunction = cast.GetShaderCast(fromPort.GetTypeField(), toPort.GetTypeField(), container, registry);

            mainBodyFunctionBuilder.AddLine($"{convertedTypeName} {converted};");
            mainBodyFunctionBuilder.AddLine($"{castFunction.Name}({toConvert}, {converted});");
            shaderFunctions.EnsureFunctionPresent(castFunction);
            return converted;

        }

        private static void ProcessNode(NodeHandler node,
            ref ShaderContainer container,
            ref VariableRegistry inputVariables,
            ref VariableRegistry outputVariables,
            ref List<(string, UnityEngine.Texture)> defaultTextures, // replace this with a generalized default properties solution.
            ref Block.Builder blockBuilder,
            ref ShaderFunction.Builder mainBodyFunctionBuilder,
            ref ShaderFunctionRegistry shaderFunctions,
            ref List<ShaderFoundry.IncludeDescriptor> includes,
            Registry registry)
        {
            var nodeBuilder = registry.GetNodeBuilder(node.GetRegistryKey());
            List<ShaderFoundry.IncludeDescriptor> localIncludes = new();
            var func = nodeBuilder.GetShaderFunction(node, container, registry, out var dependencies);


            string arguments = "";
            foreach (var param in func.Parameters)
            {
                var port = node.GetPort(param.Name);
                if (port != null)
                {
                    string argument = "";
                    if (!port.IsHorizontal)
                        continue;
                    if (port.IsInput)
                    {
                        PortHandler connectedPort = null;
                        using (var enumerator = port.GetConnectedPorts().GetEnumerator())
                        {
                            if (enumerator.MoveNext())
                            {
                                connectedPort = enumerator.Current;
                            }
                        }
                        if (connectedPort != null) // connected input port-
                        {
                            var connectedNode = connectedPort.GetNode();
                            if (connectedNode.HasMetadata("_contextDescriptor"))
                            {
                                argument = ApplyConversionAndReturnConvertedVariable(connectedPort,
                                                                                     port,
                                                                                     registry,
                                                                                     container,
                                                                                     ref mainBodyFunctionBuilder,
                                                                                     param.Type.Name,
                                                                                     ref shaderFunctions,
                                                                                     $"In.{connectedPort.ID.LocalPath.Replace("out_", "")}");
                            }
                            else
                            {
                                argument = ApplyConversionAndReturnConvertedVariable(connectedPort, port, registry, container, ref mainBodyFunctionBuilder, param.Type.Name, ref shaderFunctions);
                            }
                        }
                        else // not connected.
                        {
                            // get the inlined port value as an initializer from the definition-- since there was no connection).
                            argument = registry.GetTypeBuilder(port.GetTypeField().GetRegistryKey()).GetInitializerList(port.GetTypeField(), registry);

                            // TODO: Property/Uniform promotion should be generalized and also should ensure that all promoted fields/uniforms are unique.
                            if (port.GetTypeField().GetRegistryKey().Name == BaseTextureType.kRegistryKey.Name)
                            {
                                var fieldHandler = port.GetTypeField();
                                var field = BaseTextureType.UniformPromotion(port.GetTypeField(), container, registry);
                                inputVariables.Add(field);

                                var tex = BaseTextureType.GetTextureAsset(fieldHandler);
                                var name = ITypeDefinitionBuilder.GetUniqueUniformName(fieldHandler);
                                if (tex != null && !defaultTextures.Contains((name, tex)))
                                    defaultTextures.Add((name, tex));
                            }

                            if (port.GetTypeField().GetRegistryKey().Name == SamplerStateType.kRegistryKey.Name)
                            {
                                var fieldHandler = port.GetTypeField();
                                var field = SamplerStateType.UniformPromotion(port.GetTypeField(), container, registry);
                                inputVariables.Add(field);
                            }
                        }
                    }
                    else // this is an output port.
                    {
                        argument = $"SYNTAX_{node.ID.LocalPath}_{port.ID.LocalPath}"; // add to the arguments for the function call.
                        // default initialize this before our function call.
                        mainBodyFunctionBuilder.AddLine($"{param.Type.Name} {argument};");
                    }
                    arguments += argument + ", ";
                }
            }
            if (arguments.Length != 0)
                arguments = arguments.Remove(arguments.Length - 2, 2); // trim the trailing ", "
            mainBodyFunctionBuilder.AddLine($"{func.Name}({arguments});"); // add our node's function call to the body we're building out.


            // Process functions and prevent from adding duplicates
            if (dependencies.localFunctions != null)
                shaderFunctions.EnsureFunctionsPresent(dependencies.localFunctions);
            shaderFunctions.EnsureFunctionPresent(func);

            // Process includes and prevent from adding duplicates
            if (dependencies.includes != null)
                localIncludes.AddRange(dependencies.includes);
            foreach (var include in localIncludes)
            {
                bool shouldAdd = true;
                foreach (var existing in includes)
                {
                    if (existing.Value == include.Value)
                    {
                        shouldAdd = false;
                        break;
                    }
                }
                if (shouldAdd)
                {
                    includes.Add(include);
                }
            }
        }

        private static string ConvertToFloat3(ShaderType type, string name)
        {
            return type.VectorDimension switch
            {
                4 => $"{name}.xyz",
                2 => $"float3({name}.x, {name}.y, 0)",
                1 => $"float3({name}, {name}, {name})",
                _ => name,
            };
        }

        private static IEnumerable<NodeHandler> GatherTreeLeafFirst(NodeHandler rootNode)
        {
            Stack<NodeHandler> stack = new();
            HashSet<string> visited = new();
            stack.Push(rootNode);
            while(stack.Count > 0)
            {
                NodeHandler check = stack.Peek();
                bool isLeaf = true;
                //we treat context nodes as leaves to recurse on in processing. If the node is not a context node, or if its the root node, we process normally.
                if (!check.HasMetadata("_contextDescriptor") || check.ID.Equals(rootNode.ID))
                {
                    foreach (PortHandler port in check.GetPorts())
                    {
                        if (port.IsInput)
                        {
                            foreach (NodeHandler connected in GetConnectedNodes(port))
                            {
                                if (!visited.Contains(connected.ID.FullPath))
                                {
                                    isLeaf = false;
                                    stack.Push(connected);
                                }
                            }
                        }
                    }
                }
                if(isLeaf)
                {
                    if (visited.Contains(check.ID.FullPath))
                    {
                        stack.Pop();
                    }
                    else
                    {
                        visited.Add(check.ID.FullPath);
                        yield return stack.Pop();
                    }
                }
            }
        }

        private static IEnumerable<NodeHandler> GetConnectedNodes(PortHandler port)
        {
            foreach(var connected in port.GetConnectedPorts())
            {
                yield return connected.GetNode();
            }
        }


        internal static void GenerateSubgraphBody(
            NodeHandler root,
            ShaderContainer container,
            ref ShaderFunction.Builder builder,
            ref List<ShaderFunction> shaderFunctions,
            ref List<ShaderFoundry.IncludeDescriptor> includes)
        {
            var registry = root.Registry;

            // For each node, we need to add a function call to the body.
            foreach (var node in GatherTreeLeafFirst(root))
            {
                if (node.GetRegistryKey().Name == ContextBuilder.kRegistryKey.Name
                || node.GetRegistryKey().Name == ReferenceNodeBuilder.kRegistryKey.Name)
                    continue;

                var nodeBuilder = registry.GetNodeBuilder(node.GetRegistryKey());
                var func = nodeBuilder.GetShaderFunction(node, container, root.Registry, out var dependencies);

                // build up our dependencies...
                if (dependencies.includes != null)
                    includes.Union(dependencies.includes);
                if (dependencies.localFunctions != null)
                    shaderFunctions.Union(dependencies.localFunctions);
                if (!shaderFunctions.Contains(func))
                    shaderFunctions.Add(func);

                // gather up the arguments for this subnode
                List<string> arguments = new();
                foreach (var parameter in func.Parameters)
                {
                    var port = node.GetPort(parameter.Name);

                    var argument = $"sg_{node.ID.LocalPath}_{port.ID.LocalPath}";
                    string initializer = null;
                    var connectedPort = port.GetConnectedPorts().FirstOrDefault();
                    var connectedNode = connectedPort?.GetNode() ?? null;
                    bool addLocalVariable = true;

                    bool isReference = connectedNode?.GetRegistryKey().Name == ReferenceNodeBuilder.kRegistryKey.Name;
                    bool isContextOut = connectedNode?.GetRegistryKey().Name == ContextBuilder.kRegistryKey.Name;

                    // if it's a reference node, we can use the name of the contextEntry port, which is the name of the input port on the rootnode.
                    if (port.IsInput && isReference)
                    {
                        //root is providing the argument, but still goes through a reference node.
                        argument = connectedNode.GetPort(ReferenceNodeBuilder.kContextEntry).GetConnectedPorts().First().LocalID.Replace("out_", "");
                        addLocalVariable = false;
                    }
                    // a normal connect-- we just use the normally generated argument name, but from the connected node/port.
                    else if (port.IsInput && connectedPort != null)
                    {
                        argument = $"sg_{connectedNode.ID.LocalPath}_{connectedPort.ID.LocalPath}";

                    }
                    // not connected, so we need an initializer to setup the default value.
                    else if (port.IsInput)
                    {
                        initializer = registry.GetTypeBuilder(port.GetTypeField().GetRegistryKey()).GetInitializerList(port.GetTypeField(), registry);
                    }
                    // we are connected to a subgraph output, which means we can just use the connected ports name directly as the argument.
                    else if (!port.IsInput && isContextOut)
                    {
                        argument = "out_" + connectedPort.LocalID;
                        addLocalVariable = false;
                    }

                    arguments.Add(argument);
                    if (addLocalVariable)
                        builder.AddVariableDeclarationStatement(parameter.Type, argument, initializer);
                }
                builder.CallFunction(func, arguments.ToArray());
            }
        }
    }
}
