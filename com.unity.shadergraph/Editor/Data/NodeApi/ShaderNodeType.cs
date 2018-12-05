namespace UnityEditor.ShaderGraph
{
    public abstract class ShaderNodeType
    {
        public virtual void Setup(NodeSetupContext context) { }

        public abstract void OnNodeAdded(NodeChangeContext context, ShaderNode node);

        public virtual void OnNodeModified(NodeChangeContext context, ShaderNode node) { }
    }
}
