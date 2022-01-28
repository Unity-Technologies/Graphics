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

        public bool Mutate(UIDefinition.Property property, ref UIImplementationIntermediateDocuments result, out Exception error)
        {
            var displayName = (string)name;
            if (string.IsNullOrEmpty(displayName))
            {
                if (!GeneratorUtility.ExtractAndNicifyName((string)property.propertyPath, out displayName, out error))
                    return false;
            }

            result.propertyUxml.SetAttributeValue("label", displayName);
            error = default;
            return true;
        }
    }
}
