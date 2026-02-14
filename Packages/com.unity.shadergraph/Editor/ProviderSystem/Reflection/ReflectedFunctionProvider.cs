
using System;
using UnityEditor.ShaderApiReflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    [Serializable]
    internal class ReflectedFunctionProvider : IProvider<IShaderFunction>
    {
        public string ProviderKey => m_providerKey;
        public GUID AssetID => m_sourceAssetId;
        public IShaderFunction Definition
        {
            get
            {
                if (m_definition == null || !m_definition.IsValid)
                    Reload();
                return m_definition;
            }
        }

        [SerializeField]
        string m_providerKey;

        [SerializeField]
        GUID m_sourceAssetId;

        [NonSerialized]
        IShaderFunction m_definition;

        internal ReflectedFunctionProvider(GUID assetId, IShaderFunction definition)
        {
            m_sourceAssetId = assetId;
            m_definition = definition;

            if (definition.IsValid)
            {
                m_providerKey = ShaderObjectUtils.EvaluateProviderKey(definition);
            }
        }

        internal ReflectedFunctionProvider(GUID assetId, ReflectedFunction function)
            : this(assetId, ShaderReflectionUtils.ToShaderFunction(function))
        {
        }

        // Should be called during hot reload
        public void Reload()
        {
            if (ShaderReflectionUtils.TryResolve(m_providerKey, m_sourceAssetId, out var function))
                m_definition = ShaderReflectionUtils.ToShaderFunction(function);
            else m_definition = null;
        }

        public IProvider Clone()
            => new ReflectedFunctionProvider(this.AssetID, this.Definition);
        
    }
}
