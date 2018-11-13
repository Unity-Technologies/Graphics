namespace UnityEditor.ShaderGraph
{
    public interface IShaderNodeType
    {
        void Setup(ref NodeSetupContext context);
        void OnChange(ref NodeTypeChangeContext context);
    }
}
