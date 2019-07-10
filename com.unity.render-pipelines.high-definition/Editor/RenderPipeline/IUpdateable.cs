using UnityEditor.AnimatedValues;
using UnityEngine.Events;

namespace UnityEditor.Rendering.HighDefinition
{
    public interface IUpdateable<T>
    {
        void Update(T v);
    }
}
