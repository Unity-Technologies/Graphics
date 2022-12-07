namespace UnityEngine.Rendering
{
    /// <summary>
    /// Base of resources assets in SRP
    /// </summary>
    [Icon("Packages/com.unity.render-pipelines.core/Editor/Icons/Processed/RenderPipelineResources Icon.asset")]
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
