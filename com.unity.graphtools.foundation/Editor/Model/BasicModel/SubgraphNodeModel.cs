using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// A model that represents a subgraph node in a graph.
    /// </summary>
    [Serializable]
    public class SubgraphNodeModel : NodeModel, ISubgraphNodeModel
    {
        [SerializeField]
        GraphAssetModel m_ReferenceGraphAssetModel;

        /// <inheritdoc />
        public override string Title => m_ReferenceGraphAssetModel != null ? m_ReferenceGraphAssetModel.Name : "<Missing Subgraph>";

        /// <inheritdoc />
        public GraphAssetModel ReferenceGraphAssetModel
        {
            get => m_ReferenceGraphAssetModel;
            set
            {
                m_ReferenceGraphAssetModel = value;
                DefineNode();
            }
        }

        /// <inheritdoc />
        public Dictionary<IPortModel, IVariableDeclarationModel> DataInputPortToVariableDeclarationDictionary { get; } = new Dictionary<IPortModel, IVariableDeclarationModel>();

        /// <inheritdoc />
        public Dictionary<IPortModel, IVariableDeclarationModel> DataOutputPortToVariableDeclarationDictionary { get; } = new Dictionary<IPortModel, IVariableDeclarationModel>();

        /// <inheritdoc />
        public Dictionary<IPortModel, IVariableDeclarationModel> ExecutionInputPortToVariableDeclarationDictionary { get; } = new Dictionary<IPortModel, IVariableDeclarationModel>();

        /// <inheritdoc />
        public Dictionary<IPortModel, IVariableDeclarationModel> ExecutionOutputPortToVariableDeclarationDictionary { get; } = new Dictionary<IPortModel, IVariableDeclarationModel>();

        /// <inheritdoc />
        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            ProcessVariables();
        }

        void ProcessVariables()
        {
            foreach (var variableDeclaration in m_ReferenceGraphAssetModel.GraphModel.VariableDeclarations.Where(v => v.IsInputOrOutput()))
            {
                var isData = !variableDeclaration.IsInputOrOutputTrigger();

                if (variableDeclaration.Modifiers == ModifierFlags.ReadOnly)
                {
                    if (isData)
                    {
                        DataInputPortToVariableDeclarationDictionary[this.AddDataInputPort(variableDeclaration.Title, variableDeclaration.DataType, options: PortModelOptions.NoEmbeddedConstant)] = variableDeclaration;
                    }
                    else
                    {
                        ExecutionInputPortToVariableDeclarationDictionary[this.AddExecutionInputPort(variableDeclaration.Title)] = variableDeclaration;
                    }
                }
                else
                {
                    if (isData)
                    {
                        DataOutputPortToVariableDeclarationDictionary[this.AddDataOutputPort(variableDeclaration.Title, variableDeclaration.DataType)] = variableDeclaration;
                    }
                    else
                    {
                        ExecutionOutputPortToVariableDeclarationDictionary[this.AddExecutionOutputPort(variableDeclaration.Title)] = variableDeclaration;
                    }
                }
            }
        }
    }
}
