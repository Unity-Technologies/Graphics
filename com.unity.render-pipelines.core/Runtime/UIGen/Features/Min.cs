using System;

namespace UnityEngine.Rendering.UIGen
{
    public struct Min<T> : UIDefinition.IFeatureParameter
    {
        public readonly T value;
    }
}
