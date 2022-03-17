using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface ISubgraphNodeModel : IInputOutputPortsNodeModel, IHasTitle
    {
        /// <summary>
        /// The graph asset referenced by the subgraph node.
        /// </summary>
        IGraphAssetModel SubgraphAssetModel { get; set; }
        /// <summary>
        /// The guid of the graph asset referenced by the subgraph node.
        /// </summary>
        string SubgraphGuid { get; }
        /// <summary>
        /// The data input port models on the subgraph node with their corresponding variable declaration models.
        /// </summary>
        Dictionary<IPortModel, IVariableDeclarationModel> DataInputPortToVariableDeclarationDictionary { get; }
        /// <summary>
        /// The data output port models on the subgraph node with their corresponding variable declaration models.
        /// </summary>
        Dictionary<IPortModel, IVariableDeclarationModel> DataOutputPortToVariableDeclarationDictionary { get; }
        /// <summary>
        /// The execution input port models on the subgraph node with their corresponding variable declaration models.
        /// </summary>
        Dictionary<IPortModel, IVariableDeclarationModel> ExecutionInputPortToVariableDeclarationDictionary { get; }
        /// <summary>
        /// The execution output port models on the subgraph node with their corresponding variable declaration models.
        /// </summary>
        Dictionary<IPortModel, IVariableDeclarationModel> ExecutionOutputPortToVariableDeclarationDictionary { get; }

        /// <summary>
        /// Updates the subgraph node.
        /// </summary>
        void Update();
    }
}
