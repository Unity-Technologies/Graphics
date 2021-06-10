using UnityEngine;
using UnityEditor;

namespace UnityEditor.Rendering.Universal.Path2D
{
    internal interface ISnapping<T>
    {
        T Snap(T value);
    }
}
