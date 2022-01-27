using System;

namespace UnityEngine.Rendering.UIGen
{
    public struct Max<T> : UIDefinition.IFeatureParameter
    {
        public readonly T value;
    }
}
