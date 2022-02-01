namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for plugins.
    /// </summary>
    public interface IPluginHandler
    {
        void Register(GraphViewEditorWindow window);
        void Unregister();

        void OptionsMenu(GenericMenu menu);
    }
}
