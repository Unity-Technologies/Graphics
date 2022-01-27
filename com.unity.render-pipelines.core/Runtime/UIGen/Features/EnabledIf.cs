using System;

namespace UnityEngine.Rendering.UIGen
{
    public struct EnabledIf : UIDefinition.IFeatureParameter
    {
        public readonly Func<bool> isEnabled;
    }
}
