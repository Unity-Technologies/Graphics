using System;
using System.Collections.Generic;
using Unity.GraphAuthoring.Editor.ProviderSystem;
using UnityEditor.ShaderGraph.ProviderSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    // Serialize Shader Interface mode as a data extension so that the mode doesn't leak into assets that don't use the feature.
    [Serializable]
    [HasDependencies(typeof(ShaderInterfaceGraphData))]
    sealed class ShaderInterfaceGraphData : AbstractShaderGraphDataExtension, IHasDependencies, IHasAssetDependencies
    {
        [SerializeReference]
        IProvider<IShaderInterface> m_ShaderInterface;

        [SerializeField]
        List<VirtualFunctionStackData> m_Stacks = new();

        internal IProvider<IShaderInterface> shaderInterface
        {
            get => m_ShaderInterface;
            set => m_ShaderInterface = (IProvider<IShaderInterface>)value?.Clone();
        }

        internal IReadOnlyList<VirtualFunctionStackData> stacks => m_Stacks;

        internal override string displayName => "Shader Interface";

        internal bool SetStacks(List<VirtualFunctionStackData> stacks)
        {
            bool same = stacks.Count == m_Stacks.Count;
            for (int i = 0; same && i < stacks.Count; ++i)
                same = m_Stacks[i].Matches(stacks[i]);
            if (same)
                return false;

            m_Stacks = stacks;
            return true;
        }

        internal bool RemoveFromStack(string providerKey)
        {
            var stack = m_Stacks.Find(s => s.Holds(providerKey));
            if (stack == null)
                return false;
            stack.Remove(providerKey);
            if (stack.providerKeys.Count == 0)
                m_Stacks.Remove(stack);
            return true;
        }

        internal override void OnPropertiesGUI(VisualElement context, Action onChange, Action<string> registerUndo, GraphData owner)
        {
        }

        public bool Reload(HashSet<string> changedAssetGuids)
        {
            if (m_ShaderInterface == null || !changedAssetGuids.Contains(m_ShaderInterface.AssetID.ToString()))
                return false;

            m_ShaderInterface.Reload();
            return true;
        }

        public void GetSourceAssetDependencies(AssetCollection assetCollection)
        {
            if (m_ShaderInterface == null || m_ShaderInterface.AssetID == default)
                return;

            assetCollection.AddAssetDependency(m_ShaderInterface.AssetID,
                AssetCollection.Flags.SourceDependency
                | AssetCollection.Flags.ArtifactDependency
                | AssetCollection.Flags.IncludeInExportPackage);
        }
    }
}
