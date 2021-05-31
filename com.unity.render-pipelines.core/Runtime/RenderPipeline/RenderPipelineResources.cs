namespace UnityEngine.Rendering
{
    /// <summary>
    /// Base of resources assets in SRP
    /// </summary>
    public abstract class RenderPipelineResources : ScriptableObject
    {
        /// <summary>
        /// Utility to add Reload All button at the end of your asset inspector.
        /// It will provide your package path that you misu override in child class.
        /// </summary>
        protected virtual string packagePath => null;
        internal string packagePath_Internal => packagePath;
    }
}
