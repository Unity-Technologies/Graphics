using System;

namespace UnityEngine.Rendering.UIGen
{
    public struct EnabledIf : UIDefinition.IFeatureParameter
    {
        public readonly Func<bool> isEnabled;
        public bool Mutate(ref UIImplementationIntermediateDocuments result, out Exception error)
        {
            throw new NotImplementedException();
        }
    }
}
