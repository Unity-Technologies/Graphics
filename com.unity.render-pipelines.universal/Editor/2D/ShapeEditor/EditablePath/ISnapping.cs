using UnityEngine;
using UnityEditor;

namespace UnityEditor.Experimental.Rendering.Univerasl.Path2D
{
    internal interface ISnapping<T>
    {
        T Snap(T value);
    }
}
