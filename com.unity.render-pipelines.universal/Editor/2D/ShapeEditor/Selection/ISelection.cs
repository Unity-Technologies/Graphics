using System.Collections.Generic;

namespace UnityEditor.Experimental.Rendering.Universal.Path2D
{
    internal interface ISelection<T>
    {
        int Count { get; }
        T activeElement { get; set; }
        T[] elements { get; set; }
        void Clear();
        void BeginSelection();
        void EndSelection(bool select);
        bool Select(T element, bool select);
        bool Contains(T element);
    }
}
