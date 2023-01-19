using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.GraphToolsFoundation.Editor;
using Unity.GraphToolsFoundation;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// Represents an instance of a blackboard property/keyword on the graph
    /// </summary>
    [Serializable]
    class SGVariableNodeModel : VariableNodeModel, IGraphDataOwner<SGVariableNodeModel>
    {
        [SerializeField]
        RegistryKey m_RegistryKey;

        [SerializeField]
        string m_GraphDataName;

        /// <summary>
        /// The <see cref="IGraphDataOwner{T}"/> interface for this object.
        /// </summary>
        IGraphDataOwner<SGVariableNodeModel> graphDataOwner => this;

        /// <summary>
        /// The identifier/unique name used to represent this entity and retrieve info. regarding it from CLDS.
        /// </summary>
        public string graphDataName
        {
            get => m_GraphDataName;
            set => m_GraphDataName = value;
        }

        /// <summary>
        /// The <see cref="RegistryKey"/> that represents the concrete type within the Registry, of this object.
        /// </summary>
        public RegistryKey registryKey
        {
            get
            {
                if (!m_RegistryKey.Valid() && graphDataOwner.TryGetNodeHandler(out var reader))
                {
                    m_RegistryKey = reader.GetRegistryKey();
                }

                return m_RegistryKey;
            }
        }

        public SGPortModel outputPortModel => (SGPortModel)m_MainPortModel;

        protected override void OnDefineNode()
        {
            m_MainPortModel = this.AddDataOutputPort(null, this.GetDataType(), ReferenceNodeBuilder.kOutput);
        }

        /// <inheritdoc />
        public override void OnDuplicateNode(AbstractNodeModel sourceNode)
        {
            if (DeclarationModel is SGVariableDeclarationModel declarationModel)
            {
                // if the blackboard property/keyword this variable node is referencing
                // doesn't exist in the graph, it has probably been copied from another graph
                if (!GraphModel.VariableDeclarations.Contains(declarationModel))
                {
                    // Search for the equivalent property/keyword that GTF code
                    // will have created to replace the missing reference
                    DeclarationModel = GraphModel.VariableDeclarations.FirstOrDefault(model => model.Guid == declarationModel.Guid);

                    // Restore the Guid from its graph data name (as currently we need to align the Guids and graph data names)
                    graphDataName = new SerializableGUID(graphDataName.Replace("_", String.Empty)).ToString();

                    // Make sure this reference is up to date
                    declarationModel = (SGVariableDeclarationModel)DeclarationModel;
                }
                else
                    graphDataName = Guid.ToString();

                // Every time a variable node is duplicated, add a reference node pointing back
                // to the property/keyword that is wrapped by the VariableDeclarationModel, on the CLDS level
                (GraphModel as SGGraphModel).GraphHandler.AddReferenceNode(graphDataName, declarationModel.contextNodeName, declarationModel.graphDataName);
            }

            base.OnDuplicateNode(sourceNode);
        }

        protected override PortModel CreatePort(PortDirection direction, PortOrientation orientation, string portName,
            PortType portType,
            TypeHandle dataType, string portId, PortModelOptions options)
        {
            return new SGPortModel(this, direction, orientation, portName ?? "", portType, dataType, portId, options);
        }

        /// <inheritdoc />
        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();

            if (graphDataOwner.TryGetNodeHandler(out var reader))
            {
                m_RegistryKey = reader.GetRegistryKey();
            }
        }
    }
}
