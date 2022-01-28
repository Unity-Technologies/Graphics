using System;

namespace UnityEngine.Rendering.UIGen
{
    public struct Min<T> : UIDefinition.IFeatureParameter
    {
        public readonly T value;

        public Min(T value)
            => this.value = value;

        public bool Mutate(ref UIImplementationIntermediateDocuments result, out Exception error)
        {
            throw new NotImplementedException();
        }
    }
}
