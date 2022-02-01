using System;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

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
            foreach (var dataInput in DataInputPortToVariableDeclarationDictionary)
            {
                // get their value in current graph
                var value = GetValue(dataInput.Key.GetValue());

                // get their reference data input in subgraph
                var referenceDataInputs = ReferenceGraphAssetModel.GraphModel.FindReferencesInGraph<IVariableNodeModel>(dataInput.Value);

                // assign their initialization in subgraph
                foreach (var referenceDataInput in referenceDataInputs)
                {
                    referenceDataInput.VariableDeclarationModel.InitializationModel.ObjectValue = value;
                }
            }
        }

        IVariableNodeModel GetDataOutputNodeInSubgraph(IPortModel dataOutputPort)
        {
            var variableNodeModel = DataOutputPortToVariableDeclarationDictionary[dataOutputPort];
            return ReferenceGraphAssetModel.GraphModel.FindReferencesInGraph<IVariableNodeModel>(variableNodeModel).FirstOrDefault();
        }

        static object GetValue(Value value)
        {
            switch (value.Type)
            {
                case ValueType.Bool:
                    return value.Bool;
                case ValueType.Float:
                    return value.Float;
                case ValueType.Int:
                    return value.Int;
                case ValueType.String:
                    return value.String;
                case ValueType.Vector2:
                    return value.Vector2;
                case ValueType.Vector3:
                    return value.Vector3;
                default:
                    return value.Float;
            }
        }
    }
}
