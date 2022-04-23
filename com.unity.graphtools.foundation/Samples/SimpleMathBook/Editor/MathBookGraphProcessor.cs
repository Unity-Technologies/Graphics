using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using static System.IO.Path;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    public class MathBookProcessingResults : GraphProcessingResult
    {
        public string EvaluationResult { get; set; }
    }

    /// <summary>
    /// Evaluate graph at edit time and generate is equivalent in C#.
    /// </summary>
    public class MathBookGraphProcessor : IGraphProcessor
    {
        static readonly Regex k_CodifyRegex = new Regex("[^a-zA-Z0-9]", RegexOptions.Compiled);

        int m_VarCount;
        public List<string> Statements { get; private set; } = new List<string>();
        public MathBookProcessingResults ProcessingResult { get; private set; }

        Stack<(int, List<string>, MathBookProcessingResults)> m_Contexts = new Stack<(int, List<string>, MathBookProcessingResults)>();

        Dictionary<(IGraphModel, IVariableDeclarationModel), string> m_SubgraphGeneratedCode = new Dictionary<(IGraphModel, IVariableDeclarationModel), string>();

        void Initialize()
        {
            m_VarCount = 0;
            ProcessingResult = new MathBookProcessingResults();
        }

        public bool HasCodeForSubgraph(IGraphModel graphModel, IVariableDeclarationModel outputVariable) =>
            m_SubgraphGeneratedCode.ContainsKey((graphModel, outputVariable));

        public void AddCodeForSubgraph(IGraphModel graphModel, IVariableDeclarationModel outputVariable, string code)
        {
            m_SubgraphGeneratedCode[(graphModel, outputVariable)] = code;
        }

        public void PushContext()
        {
            m_Contexts.Push((m_VarCount, Statements, ProcessingResult));
            Initialize();
        }

        public void PopContext()
        {
            (m_VarCount, Statements, ProcessingResult) = m_Contexts.Pop();
        }

        public static string CodifyString(string str)
        {
            return k_CodifyRegex.Replace(str, "_");
        }

        /// <summary>
        /// Gets a unique variable name.
        /// </summary>
        /// <returns>The variable name.</returns>
        public string GetVariableName()
        {
            return "v_" + m_VarCount++;
        }

        /// <summary>
        /// Generate code to declare a new variable.
        /// </summary>
        /// <param name="t">The variable type.</param>
        /// <param name="comment">A comment to add before the declaration.</param>
        /// <returns>The variable name.</returns>
        public string DeclareVariable(TypeHandle t, string comment)
        {
            var v = $"{GetVariableName()}";
            if (!string.IsNullOrEmpty(comment))
                Statements.Add($"/* {comment} */");
            Statements.Add( $"{t.Resolve().FullName} {v}");
            return v;
        }

        /// <summary>
        /// Generate code to evaluate a port.
        /// </summary>
        /// <param name="portModel">The port to evaluate.</param>
        /// <returns>The variable holding the value for the port.</returns>
        public string GenerateCodeForPort(IPortModel portModel)
        {
            var nodeName = portModel.NodeModel is IHasTitle titled ? titled.DisplayTitle : portModel.NodeModel.Guid.ToString();
            var comment = $"Node: {nodeName}; port: {portModel.UniqueName}";
            var variable = DeclareVariable(portModel.DataTypeHandle, comment);

            if (portModel.GetConnectedPorts().FirstOrDefault()?.NodeModel is MathNode connectedNode)
            {
                var resultVar = connectedNode.CompileToCSharp(this);
                Statements.Add($"{variable} = {resultVar}");
            }
            else if (portModel.GetConnectedPorts().FirstOrDefault()?.NodeModel is MathSubgraphNode subgraphNode)
            {
                var resultVar = subgraphNode.CompileToCSharp(this, portModel.GetConnectedPorts().FirstOrDefault());
                Statements.Add($"{variable} = {resultVar}");
            }
            else if (portModel.GetConnectedPorts().FirstOrDefault()?.NodeModel is VariableNodeModel variableNode &&
                variableNode.VariableDeclarationModel.Modifiers == ModifierFlags.Read)
            {
                Statements.Add($"{variable} = {CodifyString(variableNode.VariableDeclarationModel.GetVariableName())}");
            }
            else if (portModel.GetConnectedPorts().FirstOrDefault()?.NodeModel is ConstantNodeModel constantNode)
            {
                Statements.Add($"{variable} = {ConstantToString(constantNode, constantNode.Value)}");
            }
            else if (portModel.EmbeddedValue != null)
            {
                Statements.Add($"{variable} = {ConstantToString(portModel.NodeModel, portModel.EmbeddedValue)}");
            }
            else
            {
                Statements.Add($"{variable} = default");
            }

            return variable;
        }

        string ConstantToString(INodeModel ownerNode, IConstant c)
        {
            switch (c)
            {
                case Constant<bool> b:
                    return b.Value.ToString().ToLower();
                case Constant<int> b:
                    return b.Value.ToString();
                case Constant<float> b:
                    return $"{b.Value}f";
                case Constant<Vector2> b:
                    var v2 = b.Value;
                    return $"new Vector2({v2.x}f, {v2.y}f)";
                case Constant<Vector3> b:
                    var v3 = b.Value;
                    return $"new Vector3({v3.x}f, {v3.y}f, {v3.z}f)";
            }

            ProcessingResult.AddError("Unsupported data type.", ownerNode);
            return "default";
        }

        void DetectLoopsRecursive(IGraphModel graphModel)
        {
            var uniqueGraphs = new HashSet<IGraphModel>();
            var graphsToCheck = new Queue<IGraphModel>();
            uniqueGraphs.Add(graphModel);
            graphsToCheck.Enqueue(graphModel);

            while (graphsToCheck.Count > 0)
            {
                var graphToCheck = graphsToCheck.Dequeue();
                foreach (var subGraphNode in graphToCheck.NodeModels.OfType<ISubgraphNodeModel>())
                {
                    var subGraph = subGraphNode.SubgraphModel;
                    if (uniqueGraphs.Add(subGraph))
                    {
                        graphsToCheck.Enqueue(subGraph);
                    }
                }

                if (DetectLoops(graphToCheck))
                {
                    ProcessingResult.AddError($"Loop detected in graph {graphToCheck.Name}.");
                }
            }

            static bool DetectLoops(IGraphModel graphModel)
            {
                // A topological sort (CommandStateObserver.ObserverManager.SortObservers), but we don't keep the sorted nodes.

                var nodes = graphModel.NodeModels.ToList();
                var allOutputPorts = nodes.SelectMany(node => (node as IInputOutputPortsNodeModel)?.OutputsByDisplayOrder);
                var allConnectedOutputPorts = allOutputPorts.SelectMany(port => port.GetConnectedPorts());
                var allConnectedOutputNodes = allConnectedOutputPorts.Select(port => port.NodeModel).ToList();

                var cycleDetected = false;
                while (nodes.Count > 0 && !cycleDetected)
                {
                    var remainingNodeCount = nodes.Count;
                    for (var index = nodes.Count - 1; index >= 0; index--)
                    {
                        var node = nodes[index];

                        var inputPorts = (node as IInputOutputPortsNodeModel)?.InputsByDisplayOrder ?? Enumerable.Empty<IPortModel>();
                        var connectedInputPorts = inputPorts.SelectMany(port => port.GetConnectedPorts());
                        var connectedInputNodes = connectedInputPorts.Select(port => port.NodeModel);

                        if (connectedInputNodes.Any(inputNode => allConnectedOutputNodes.Contains(inputNode)))
                        {
                            remainingNodeCount--;
                        }
                        else
                        {
                            var outputPorts = (node as IInputOutputPortsNodeModel)?.OutputsByDisplayOrder ?? Enumerable.Empty<IPortModel>();
                            var connectedOutputPorts = outputPorts.SelectMany(port => port.GetConnectedPorts());
                            var connectedOutputNodes = connectedOutputPorts.Select(port => port.NodeModel);

                            foreach (var outputNode in connectedOutputNodes)
                            {
                                allConnectedOutputNodes.Remove(outputNode);
                            }

                            nodes.RemoveAt(index);
                        }
                    }

                    cycleDetected = remainingNodeCount == 0;
                }

                return nodes.Count > 0;
            }
        }

        /// <inheritdoc />
        public GraphProcessingResult ProcessGraph(IGraphModel graphModel, GraphChangeDescription changes)
        {
            var doProcessing = changes == null ||
                               changes.NewModels.Any(m => !(m is IPlacematModel) && !(m is IStickyNoteModel)) ||
                               changes.DeletedModels.Any(m => !(m is IPlacematModel) && !(m is IStickyNoteModel)) ||
                               changes.ChangedModels.Any(m =>
                                   !(m.Key is IPlacematModel) &&
                                   !(m.Key is IStickyNoteModel) &&
                                   m.Value.HasAnyChange(ChangeHint.Data, ChangeHint.GraphTopology));

            if (ProcessingResult == null)
                Initialize();

            if (!doProcessing)
                return ProcessingResult;

            Statements = new List<string>();

            if (!(graphModel is MathBook mathBook))
            {
                ProcessingResult.AddError("Bad graph type.");
                return ProcessingResult;
            }

            foreach (var mathNode in graphModel.NodeModels.OfType<MathNode>())
            {
                if (!mathNode.CheckInputs(out var errorMessage))
                {
                    ProcessingResult.AddError(errorMessage, mathNode);
                }
            }

            var resultNodes = mathBook.NodeModels.OfType<MathResult>().ToList();
            if (resultNodes.Count == 0)
            {
                return ProcessingResult;
            }

            if (resultNodes.Count > 1)
            {
                foreach (var resultNode in resultNodes)
                {
                    ProcessingResult.AddError($"Too many {nameof(MathResult)} nodes.", resultNode,
                        new QuickFix("Delete Node",
                            target => target.Dispatch(new DeleteElementsCommand(resultNode))));
                }

                return ProcessingResult;
            }

            DetectLoopsRecursive(mathBook);

            if (ProcessingResult.Status == GraphProcessingStatuses.Failed)
                return ProcessingResult;

            try
            {
                mathBook.EvaluationContext = this;
                var result = resultNodes.FirstOrDefault()?.Evaluate().ToString() ?? "<Unknown>";
                mathBook.EvaluationContext = null;
                ProcessingResult.EvaluationResult = result;
            }
            catch (InvalidDataException) { }
            catch (ArgumentOutOfRangeException) { }

            resultNodes.First().CompileToCSharp(this);

            string path = null;
            if (graphModel.Asset is ISerializedGraphAsset serializedGraphAsset)
            {
                path = serializedGraphAsset.FilePath;
                path = GetDirectoryName(path);
            }
            if (string.IsNullOrEmpty(path))
            {
                path = "Assets/";
            }

            var graphName = CodifyString(graphModel.Name);
            if (string.IsNullOrEmpty(graphName))
                graphName = "unnamed_graph";

            var returnType = resultNodes.First().DataIn0.DataTypeHandle.Resolve().FullName;

            var code = "";
            code += "using UnityEngine;\n";
            code += "namespace MathBook {\n";
            code += $"class {graphName} {{\n";

            foreach (var function in m_SubgraphGeneratedCode)
            {
                code += function.Value;
                code += "\n\n";
            }

            code += $"public static {returnType} Evaluate() {{\n";
            code += string.Join(";\n", Statements) + ";";
            code += "\n}\n}\n}\n";

            path = Combine(path, graphName + ".cs");
            File.WriteAllText(path, code);

            // Do not refresh the asset database as it
            // is really annoying to have to recompile the
            // generated code every time...

            return ProcessingResult;
        }
    }
}
