using UnityEditor.AnimatedValues;
using UnityEngine.Events;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public interface IUpdateable<T>
    {
        void Update(T v);
    }
}
