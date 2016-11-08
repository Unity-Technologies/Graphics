using System.Collections.Generic;

namespace RMGUI.GraphView
{
    public interface IGraphElementDataSource
    {
        IEnumerable<GraphElementData> elements { get; }
        void AddElement(GraphElementData element);
        void RemoveElement(GraphElementData element);
    }
}
