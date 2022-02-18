using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderFoundry
{
    internal class ShaderUniformCollection
    {
        Dictionary<string, UniformDeclarationData> visitedUniforms = new Dictionary<string, UniformDeclarationData>();
        List<UniformDeclarationData> shaderUniforms = new List<UniformDeclarationData>();

        public bool ReadOnly { get; private set; }
        public IEnumerable<UniformDeclarationData> Uniforms => shaderUniforms;

        public void Add(UniformDeclarationData uniform)
        {
            if (ReadOnly)
            {
                Debug.LogError("ERROR: attempting to add uniform to readonly collection");
                return;
            }

            if (visitedUniforms.TryGetValue(uniform.Name, out var existingUniform))
            {
                ValidateAreEquivalent(uniform, existingUniform);
                return;
            }
            visitedUniforms.Add(uniform.Name, uniform);
            shaderUniforms.Add(uniform);
        }

        public void AddRange(IEnumerable<UniformDeclarationData> uniforms)
        {
            foreach (var uniform in uniforms)
                Add(uniform);
        }

        public void Add(ShaderPropertyCollection shaderProperties)
        {
            foreach (var property in shaderProperties.Properties)
            {
                var propertyData = PropertyDeclarations.Extract(property.Type, property.Name, property.Attributes);
                if (propertyData != null && propertyData.UniformDeclarations != null)
                    AddRange(propertyData.UniformDeclarations);
            }
        }

        public void SetReadOnly()
        {
            ReadOnly = true;
        }

        public bool HasDotsProperties()
        {
            foreach (var uniformDeclaration in Uniforms)
            {
                if (uniformDeclaration.DataSource == UniformDataSource.PerInstance)
                    return true;
            }
            return false;
        }

        static bool ValidateAreEquivalent(UniformDeclarationData newUniform, UniformDeclarationData oldUniform)
        {
            if (newUniform.Type != oldUniform.Type)
            {
                ErrorHandling.ReportError($"Uniform '{newUniform.Name}' is being declared with two conflicting types: '{newUniform.Type.Name}' and '{oldUniform.Type.Name}'");
                return false;
            }

            if (newUniform.DataSource != oldUniform.DataSource)
            {
                ErrorHandling.ReportError($"Uniform '{newUniform.Name}' is being declared with two conflicting data sources: '{newUniform.DataSource}' and '{oldUniform.DataSource}'");
                return false;
            }

            if (newUniform.DeclarationOverride != oldUniform.DeclarationOverride)
            {
                ErrorHandling.ReportError($"Uniform '{newUniform.Name}' is being declared with two conflicting declaration overrides: '{newUniform.DeclarationOverride}' and '{oldUniform.DeclarationOverride}'");
                return false;
            }
            return true;
        }
    }
}
