namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Used to build UI from model. See <see cref="GraphViewFactoryExtensions"/>.
    /// </summary>
    public class ElementBuilder
    {
        public IModelView View { get; set; }
        public IUIContext Context { get; set; }
    }
}
