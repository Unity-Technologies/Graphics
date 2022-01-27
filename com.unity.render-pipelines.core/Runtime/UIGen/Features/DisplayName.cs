using System;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.Rendering.UIGen
{
    public struct DisplayName : UIDefinition.IFeatureParameter
    {
        public readonly UIDefinition.PropertyName name;

        public DisplayName(UIDefinition.PropertyName name) {
            this.name = name;
        }
    }
}
