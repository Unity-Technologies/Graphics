using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class MathBook : GraphModel
    {
        [SerializeField]
        [Tooltip("The subgraph properties.")]
        SubgraphPropertiesField m_SubgraphPropertiesField = new SubgraphPropertiesField("A graph requires at least one input or output variable declaration to become usable as a subgraph.");
        public SubgraphPropertiesField SubgraphPropertiesField => m_SubgraphPropertiesField;

        public MathBookGraphProcessor EvaluationContext { get; set; } = null;

        public MathBook()
        {
            StencilType = null;
        }

        public override Type DefaultStencilType => typeof(MathBookStencil);

        public override ISubgraphNodeModel CreateSubgraphNode(IGraphAssetModel referenceGraphAsset, Vector2 position, SerializableGUID guid = default, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            if (referenceGraphAsset.IsContainerGraph())
            {
                Debug.LogWarning("Failed to create the subgraph node. Container graphs cannot be referenced by a subgraph node.");
                return null;
            }
            return this.CreateNode<MathSubgraphNode>(referenceGraphAsset.Name, position, guid, v => { v.SubgraphAssetModel = referenceGraphAsset; }, spawnFlags);
        }
    }
}
