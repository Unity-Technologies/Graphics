using System;

namespace UnityEngine.Rendering.UIGen
{
    public struct Max<T> : UIDefinition.IFeatureParameter
    {
        public readonly T value;

        public Max(T value)
            => this.value = value;

        // TODO: [Fred] idea a feature to mutate another feature (ex easier to mutate assign code of databind)
        public bool Mutate(UIDefinition.Property property, ref UIImplementationIntermediateDocuments result, out Exception error)
        {
            throw new NotImplementedException();
        }
    }
}
