using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// A model that represents a subgraph node in a graph.
    /// </summary>
    [Serializable]
    public class SubgraphNodeModel : NodeModel, ISubgraphNodeModel
    {
        [SerializeReference]
        Subgraph m_Subgraph;

        /// <inheritdoc />
        public override string Title => m_Subgraph.Title;

        /// <inheritdoc />
        public IGraphModel SubgraphModel
        {
            get => m_Subgraph?.GraphModel;

            set
            {
                if (value is GraphModel graphModel)
                {
                    m_Subgraph ??= new Subgraph();
                    m_Subgraph.GraphModel = graphModel;
                    DefineNode();
                }
                else
                {
                    m_Subgraph = null;
                }
            }
        }

        /// <inheritdoc />
        public string SubgraphGuid => m_Subgraph.AssetGuid;

        /// <inheritdoc />
        public Dictionary<IPortModel, IVariableDeclarationModel> DataInputPortToVariableDeclarationDictionary { get; } = new Dictionary<IPortModel, IVariableDeclarationModel>();

        /// <inheritdoc />
        public Dictionary<IPortModel, IVariableDeclarationModel> DataOutputPortToVariableDeclarationDictionary { get; } = new Dictionary<IPortModel, IVariableDeclarationModel>();

        /// <inheritdoc />
        public Dictionary<IPortModel, IVariableDeclarationModel> ExecutionInputPortToVariableDeclarationDictionary { get; } = new Dictionary<IPortModel, IVariableDeclarationModel>();

        /// <inheritdoc />
        public Dictionary<IPortModel, IVariableDeclarationModel> ExecutionOutputPortToVariableDeclarationDictionary { get; } = new Dictionary<IPortModel, IVariableDeclarationModel>();

        /// <inheritdoc />
        public void Update()
        {
            DefineNode();
        }

        /// <inheritdoc />
        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            DataInputPortToVariableDeclarationDictionary.Clear();
            DataOutputPortToVariableDeclarationDictionary.Clear();
            ExecutionInputPortToVariableDeclarationDictionary.Clear();
            ExecutionOutputPortToVariableDeclarationDictionary.Clear();

            ProcessVariables();
        }

        /// <inheritdoc />
        protected override void DisconnectPort(IPortModel portModel)
        {}

        void ProcessVariables()
        {
            if (SubgraphModel == null)
                return;

            foreach (var variableDeclaration in GetInputOutputVariables())
                AddPort(variableDeclaration, variableDeclaration.Guid.ToString(), variableDeclaration.Modifiers == ModifierFlags.Read, !variableDeclaration.IsInputOrOutputTrigger());
        }

        List<IVariableDeclarationModel> GetInputOutputVariables()
        {
            var inputOutputVariableDeclarations = new List<IVariableDeclarationModel>();

            // Get the input/output variable declarations from the section models to preserve their displayed order in the Blackboard
            foreach (var section in SubgraphModel.SectionModels)
                GetInputOutputVariable(section, ref inputOutputVariableDeclarations);

            return inputOutputVariableDeclarations;
        }

        void GetInputOutputVariable(IGroupItemModel groupItem, ref List<IVariableDeclarationModel> inputOutputVariables)
        {
            if (groupItem is IVariableDeclarationModel variable && variable.IsInputOrOutput())
            {
                inputOutputVariables.Add(variable);
            }
            else if (groupItem is IGroupModel groupModel)
            {
                foreach (var item in groupModel.Items)
                    GetInputOutputVariable(item, ref inputOutputVariables);
            }
        }

        void AddPort(IVariableDeclarationModel variableDeclaration, string portId, bool isInput, bool isData)
        {
            if (isInput)
            {
                if (isData)
                    DataInputPortToVariableDeclarationDictionary[this.AddDataInputPort(variableDeclaration.Title, variableDeclaration.DataType, portId, options: PortModelOptions.NoEmbeddedConstant)] = variableDeclaration;
                else
                    ExecutionInputPortToVariableDeclarationDictionary[this.AddExecutionInputPort(variableDeclaration.Title, portId)] = variableDeclaration;
            }
            else
            {
                if (isData)
                    DataOutputPortToVariableDeclarationDictionary[this.AddDataOutputPort(variableDeclaration.Title, variableDeclaration.DataType, portId, options: PortModelOptions.NoEmbeddedConstant)] = variableDeclaration;
                else
                    ExecutionOutputPortToVariableDeclarationDictionary[this.AddExecutionOutputPort(variableDeclaration.Title, portId)] = variableDeclaration;
            }
        }
    }
}
