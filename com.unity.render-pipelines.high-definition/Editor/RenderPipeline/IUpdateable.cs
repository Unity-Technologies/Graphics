using UnityEditor.AnimatedValues;
using UnityEngine.Events;

namespace UnityEditor.Rendering.HighDefinition
{
    interface IUpdateable<T>
    {
        void Update(T v);
    }
}
