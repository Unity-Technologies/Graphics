namespace UnityEditor.ShaderGraph
{
    public abstract class ShaderNodeType
    {
        public abstract void Setup(ref NodeSetupContext context);

        public abstract void OnNodeAdded(NodeChangeContext context, NodeRef node);

        public virtual void OnNodeModified(NodeChangeContext context, NodeRef node)
        {
        }
    }
}
