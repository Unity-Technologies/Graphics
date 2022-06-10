using System.Collections.Generic;
using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    internal class BasePropertyBlockBuilder
    {
        public string BlockName;
        public string FieldName = "MyField";
        public PropertyAttributeData PropertyAttribute;

        public string GetUniformName() => string.IsNullOrEmpty(PropertyAttribute?.UniformName) ? FieldName : PropertyAttribute.UniformName;
        public string GetDisplayName() => string.IsNullOrEmpty(PropertyAttribute?.DisplayName) ? FieldName : PropertyAttribute.DisplayName;
    }
}
