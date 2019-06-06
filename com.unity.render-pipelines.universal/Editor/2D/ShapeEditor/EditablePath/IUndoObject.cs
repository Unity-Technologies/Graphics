using UnityEngine;

namespace UnityEditor.Experimental.Rendering.Univerasl.Path2D
{
    internal interface IUndoObject
    {
        void RegisterUndo(string name);
    }
}
