using System;

namespace UnityEngine.Rendering.UIGen
{
    public struct ColorUsage : UIDefinition.IFeatureParameter
    {
        public readonly bool showAlpha;
        public readonly bool hdr;
    }
}
