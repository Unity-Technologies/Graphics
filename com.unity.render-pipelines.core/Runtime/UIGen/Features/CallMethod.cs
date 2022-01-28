using System;

namespace UnityEngine.Rendering.UIGen
{
    public struct CallMethod : UIDefinition.IFeatureParameter
    {
        public readonly Action call;
        public bool Mutate(UIDefinition.Property property, ref UIImplementationIntermediateDocuments result, out Exception error)
        {
            throw new NotImplementedException();
        }
    }
}
