using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.ProviderSystem;
using Unity.GraphAuthoring.Editor.ProviderSystem;

namespace UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    [SGPropertyDrawer(typeof(ProviderNode))]
    class ProviderNodePropertyDrawer : AbstractProviderNodePropertyDrawer<IShaderFunction>
    {
        private protected override void AddDescriptionValues(VisualElement section, AbstractProviderNode<IShaderFunction> node)
        {
            if (node.Provider.AssetID != default)
                ProviderInspectorUtils.AddValue(section, "Source", AssetDatabase.GUIDToAssetPath(node.Provider.AssetID));
        }

        private protected override void AddDescriptionBlocks(VisualElement section, AbstractProviderNode<IShaderFunction> node)
        {
            if (node.Provider.AssetID != default)
                ProviderInspectorUtils.AddBlock(section, "Definition", ShaderObjectUtils.GenerateCode(node.Provider.Definition, false, false));
        }

        private protected override void AddSettings(VisualElement section, AbstractProviderNode<IShaderFunction> nodeBase, Action setNodesAsDirtyCallback, Action updateNodeViewsCallback)
        {
            var node = (ProviderNode)nodeBase;
            var versionCandidates = node.Header?.versionCandidates;
            if (versionCandidates == null || versionCandidates.Length < 1)
                return;

            string currentVersion = node.Header.version;
            var choices = new List<string>(versionCandidates.Length);
            int currentIdx = 0;
            for (int i = 0; i < versionCandidates.Length; i++)
            {
                choices.Add(versionCandidates[i].version);
                if (versionCandidates[i].version == currentVersion)
                    currentIdx = i;
            }

            var versionDropdown = new DropdownField(choices, currentIdx);
            versionDropdown.RegisterValueChangedCallback(_ =>
            {
                int newIdx = versionDropdown.index;
                if (versionCandidates[newIdx].version == node.Header.version)
                    return;

                setNodesAsDirtyCallback?.Invoke();
                node.owner.owner.RegisterCompleteObjectUndo($"Change {node.name} Version");
                node.InitializeFromProvider(versionCandidates[newIdx].provider);
                node.owner.ValidateGraph();
                updateNodeViewsCallback?.Invoke();
                inspectorUpdateDelegate?.Invoke();
            });
            ProviderInspectorUtils.AddSetting(section, "Version", null, versionDropdown);
        }
    }
}
