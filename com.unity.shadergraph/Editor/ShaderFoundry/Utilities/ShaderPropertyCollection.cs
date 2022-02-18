using System;
using System.Collections.Generic;
using UnityEngine;
using BlockProperty = UnityEditor.ShaderFoundry.BlockVariable;

namespace UnityEditor.ShaderFoundry
{
    internal class ShaderPropertyCollection
    {
        Dictionary<string, BlockProperty> visitedProperties = new Dictionary<string, BlockProperty>();
        List<BlockProperty> shaderProperties = new List<BlockProperty>();

        public bool ReadOnly { get; private set; }
        public IEnumerable<BlockProperty> Properties => shaderProperties;

        public void Add(BlockProperty property)
        {
            if (ReadOnly)
            {
                Debug.LogError("ERROR: attempting to add property to readonly collection");
                return;
            }

            if (visitedProperties.TryGetValue(property.Name, out var existingProperty))
            {
                ValidateAreEquivalent(property, existingProperty);
                return;
            }

            visitedProperties.Add(property.Name, property);
            shaderProperties.Add(property);
        }

        public void AddRange(IEnumerable<BlockProperty> properties)
        {
            foreach (var property in properties)
                Add(property);
        }

        public void SetReadOnly()
        {
            ReadOnly = true;
        }

        static bool ValidateAreEquivalent(BlockProperty newProperty, BlockProperty oldProperty)
        {
            if (newProperty.Type != oldProperty.Type)
            {
                ErrorHandling.ReportError($"Uniform '{newProperty.Name}' is being declared with two conflicting types: '{newProperty.Type.Name}' and '{oldProperty.Type.Name}'");
                return false;
            }
            // TODO SHADER: This will need to check more, mainly in the attributes.
            // This probably needs to check the [Property] attribute as well as the extra shaderlab attributes such as [Gamma].
            return true;
        }
    }
}
