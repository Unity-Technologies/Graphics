using UnityEditor.GraphToolsFoundation.Overdrive;

namespace UnityEngine.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for graph processors.
    /// </summary>
    public interface IGraphProcessor
    {
        GraphProcessingResult ProcessGraph(IGraphModel graphModel);
    }
}
