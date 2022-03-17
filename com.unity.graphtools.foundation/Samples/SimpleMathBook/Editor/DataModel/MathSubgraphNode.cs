using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class MathSubgraphNode : SubgraphNodeModel
    {
        public Value Evaluate(IPortModel dataOutputPort)
        {
            SetDataInputValuesInSubgraph();
            return GetDataOutputNodeInSubgraph(dataOutputPort).InputPort.GetValue();
        }

        void SetDataInputValuesInSubgraph()
        {
            // for each data input
            foreach (var kv in DataInputPortToVariableDeclarationDictionary.Where(pair => pair.Key.GetConnectedEdges().Any()))
            {
                // get their value in current graph
                var value = GetValue(kv.Key.GetValue());

                // get their reference data input in subgraph
                var referenceDataInputs = SubgraphAssetModel.GraphModel.FindReferencesInGraph<IVariableNodeModel>(kv.Value);

                // assign their initialization in subgraph
                foreach (var referenceDataInput in referenceDataInputs)
                {
                    var portType = referenceDataInput.VariableDeclarationModel.InitializationModel.ObjectValue.GetType();
                    var valueType = value.GetType();

                    if (portType != valueType)
                    {
                        var processingResult = (GraphModel as MathBook)?.EvaluationContext?.ProcessingResult;
                        var name = (kv.Key as IHasTitle)?.DisplayTitle ?? kv.Key.UniqueName;
                        processingResult?.AddError($"Type mismatch on port {name} of type {portType.Name}. The incoming type is {valueType.Name}.", this);

                        referenceDataInput.VariableDeclarationModel.InitializationModel.ObjectValue =
                            referenceDataInput.VariableDeclarationModel.InitializationModel.DefaultValue;
                    }
                    else
                    {
                        referenceDataInput.VariableDeclarationModel.InitializationModel.ObjectValue = value;
                    }
                }
            }
        }

        IVariableNodeModel GetDataOutputNodeInSubgraph(IPortModel dataOutputPort)
        {
            var variableNodeModel = DataOutputPortToVariableDeclarationDictionary[dataOutputPort];
            return SubgraphAssetModel.GraphModel.FindReferencesInGraph<IVariableNodeModel>(variableNodeModel).FirstOrDefault();
        }

        static object GetValue(Value value)
        {
            if (value.Type == TypeHandle.Bool)
                return value.Bool;
            if (value.Type == TypeHandle.Float)
                return value.Float;
            if (value.Type == TypeHandle.Int)
                return value.Int;
            if (value.Type == TypeHandle.String)
                return value.String;
            if (value.Type == TypeHandle.Vector2)
                return value.Vector2;
            if (value.Type == TypeHandle.Vector3)
                return value.Vector3;
            return value.Float;
        }

        public string CompileToCSharp(MathBookGraphProcessor context, IPortModel outputPort)
        {
            var inputVariables = new Dictionary<string, string>();
            foreach (var dataInput in DataInputPortToVariableDeclarationDictionary)
            {
                var variable = context.GenerateCodeForPort(dataInput.Key);
                var paramName = dataInput.Value.GetVariableName();
                inputVariables[paramName] = variable;
            }

            var result = context.DeclareVariable(outputPort.DataTypeHandle, "");

            var outputVariableDeclaration = DataOutputPortToVariableDeclarationDictionary[outputPort];
            GenerateSubGraphCode(context, SubgraphAssetModel.GraphModel, outputVariableDeclaration);

            var graphName = MathBookGraphProcessor.CodifyString(SubgraphAssetModel.GraphModel.Name);
            var portName = MathBookGraphProcessor.CodifyString(outputVariableDeclaration.GetVariableName());
            var subgraphParams = inputVariables.Select(kv => $"{kv.Key}: {kv.Value}");
            context.Statements.Add($"{result} = Evaluate_{graphName}_{portName}({string.Join(", ", subgraphParams)})");
            return result;
        }

        void GenerateSubGraphCode(MathBookGraphProcessor context, IGraphModel graphModel, IVariableDeclarationModel outputVariable)
        {
            if (context.HasCodeForSubgraph(graphModel, outputVariable))
                return;

            context.PushContext();

            var outputNodes = graphModel.FindReferencesInGraph<IVariableNodeModel>(outputVariable);
            var result = context.GenerateCodeForPort(outputNodes.First().InputPort);
            context.Statements.Add($"return {result}");

            var returnType = outputNodes.First().InputPort.DataTypeHandle.Resolve().FullName;
            var graphName = MathBookGraphProcessor.CodifyString(graphModel.Name);
            var portName = MathBookGraphProcessor.CodifyString(outputVariable.GetVariableName());

            var inputVariables = graphModel.VariableDeclarations.Where(v => v.Modifiers == ModifierFlags.Read);
            var subgraphParams = inputVariables.Select(v => $"{v.DataType.Resolve().FullName} {MathBookGraphProcessor.CodifyString(v.GetVariableName())} = default");

            var code = "";
            code += $"public static {returnType} Evaluate_{graphName}_{portName}({string.Join(", ", subgraphParams)}) {{\n";
            code += string.Join(";\n", context.Statements) + ";";
            code += "\n}\n";

            context.PopContext();

            context.AddCodeForSubgraph(graphModel, outputVariable, code);
        }
    }
}
