using UnityEngine;

namespace UnityEditor.Rendering.Universal.Path2D
{
    internal interface IUndoObject
    {
        void RegisterUndo(string name);
    }
}
