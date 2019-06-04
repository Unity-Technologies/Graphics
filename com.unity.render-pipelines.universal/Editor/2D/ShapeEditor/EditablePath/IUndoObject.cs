using UnityEngine;

namespace UnityEditor.Experimental.Rendering.LWRP.Path2D
{
    internal interface IUndoObject
    {
        void RegisterUndo(string name);
    }
}
