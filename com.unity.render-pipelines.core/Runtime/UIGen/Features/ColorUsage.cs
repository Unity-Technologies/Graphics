using System;

namespace UnityEngine.Rendering.UIGen
{
    public struct ColorUsage : UIDefinition.IFeatureParameter
    {
        public readonly bool showAlpha;
        public readonly bool hdr;
        public bool Mutate(ref UIImplementationIntermediateDocuments result, out Exception error)
        {
            throw new NotImplementedException();
        }
    }
}
