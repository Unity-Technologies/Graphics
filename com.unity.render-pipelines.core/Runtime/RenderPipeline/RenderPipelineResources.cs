namespace UnityEngine.Rendering
{
    public abstract class RenderPipelineResources : ScriptableObject
    {
        protected abstract string packagePath { get; }
        internal string packagePath_Internal => packagePath;
    }
}
