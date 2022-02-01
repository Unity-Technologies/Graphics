namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    public static class NodeDataCreationExtension
    {
        public static INodeModel CreateMathSubgraphNode(this IGraphNodeCreationData data, GraphAssetModel referenceGraphAsset)
        {
            return data.GraphModel.CreateNode<MathSubgraphNode>(referenceGraphAsset.Name, data.Position,
                initializationCallback: v => { v.ReferenceGraphAssetModel = referenceGraphAsset; }, spawnFlags: data.SpawnFlags);
        }
    }
}
