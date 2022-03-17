namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Used to build UI from model. See <see cref="GraphViewFactoryExtensions"/>.
    /// </summary>
    public class ElementBuilder
    {
        public IRootView View { get; set; }
        public IViewContext Context { get; set; }
    }
}
