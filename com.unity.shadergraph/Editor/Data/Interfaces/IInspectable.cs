namespace UnityEditor.ShaderGraph.Drawing
{
    interface IInspectable
    {
        string displayName { get; }
        PropertySheet GetInspectorContent();
    }
}
