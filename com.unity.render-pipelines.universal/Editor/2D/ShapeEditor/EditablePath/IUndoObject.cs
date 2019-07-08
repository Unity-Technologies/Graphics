using UnityEngine;

namespace UnityEditor.Experimental.Rendering.Universal.Path2D
{
    internal interface IUndoObject
    {
        void RegisterUndo(string name);
    }
}
