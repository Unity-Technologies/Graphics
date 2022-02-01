using System;
using UnityEditor.GraphToolsFoundation.Overdrive;

namespace UnityEngine.GraphToolsFoundation.Overdrive
{
    class NoOpGraphProcessor : IGraphProcessor
    {
        public GraphProcessingResult ProcessGraph(IGraphModel graphModel)
        {
            return new GraphProcessingResult();
        }
    }
}
