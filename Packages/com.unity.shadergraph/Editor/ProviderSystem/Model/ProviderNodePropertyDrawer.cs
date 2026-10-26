using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.ProviderSystem;
using Unity.GraphAuthoring.Editor.ProviderSystem;
using System.Text;

namespace UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    [SGPropertyDrawer(typeof(ProviderNode))]
    class ProviderNodeNodePropertyDrawer : AbstractMaterialNodePropertyDrawer
    {
        private ProviderNode node;

        internal override void AddCustomNodeProperties(VisualElement parentElement, AbstractMaterialNode nodeBase, Action setNodesAsDirtyCallback, Action updateNodeViewsCallback)
        {
            node = nodeBase as ProviderNode;
            var provider = node.Provider;
            bool hasAssetSource = provider.AssetID != default;
            string sourcePath  = hasAssetSource ? AssetDatabase.GUIDToAssetPath(provider.AssetID) : null;
            string providerKey = provider.ProviderKey;

            if (!provider.IsValid)
            {
                var errorMessage = hasAssetSource
                    ? $"Could not find '{providerKey}' in '{sourcePath}'."
                    : $"Could not find '{providerKey}'.";
                parentElement.Add(new HelpBoxRow(errorMessage, MessageType.Error));
                return;
            }

            var versionCandidates = node.Header?.versionCandidates;
            if (versionCandidates != null && versionCandidates.Length >= 1)
            {
                string currentVersion = node.Header.version;
                var choices = new List<string>(versionCandidates.Length);
                int currentIdx = 0;
                for (int i = 0; i < versionCandidates.Length; i++)
                {
                    choices.Add(versionCandidates[i].version);
                    if (versionCandidates[i].version == currentVersion)
                        currentIdx = i;
                }

                var versionDropdown = new DropdownField("Version", choices, currentIdx);
                versionDropdown.RegisterValueChangedCallback(_ =>
                {
                    int newIdx = versionDropdown.index;
                    if (versionCandidates[newIdx].version == node.Header.version)
                        return;

                    setNodesAsDirtyCallback?.Invoke();
                    node.owner.owner.RegisterCompleteObjectUndo($"Change {node.name} Version");
                    node.InitializeFromProvider(versionCandidates[newIdx].provider);
                    node.ValidateNode();
                    node.Dirty(ModificationScope.Graph);
                    updateNodeViewsCallback?.Invoke();
                    inspectorUpdateDelegate?.Invoke();
                });
                parentElement.Add(versionDropdown);
            }

            if (hasAssetSource)
            {
                parentElement.Add(new Label("Provider Key"));
                parentElement.Add(new HelpBoxRow(providerKey, MessageType.None));

                parentElement.Add(new Label("Source Path"));
                parentElement.Add(new HelpBoxRow(sourcePath, MessageType.None));

                string qualifiedSignature = ShaderObjectUtils.QualifySignature(node.Provider.Definition);
                parentElement.Add(new Label("Qualified Signature"));
                parentElement.Add(new HelpBoxRow(qualifiedSignature, MessageType.None));

                string code = ShaderObjectUtils.GenerateCode(provider.Definition, false, false);
                parentElement.Add(new Label("Expected Definition"));
                parentElement.Add(new HelpBoxRow(code, MessageType.None));
            }

            var messages = node.Header?.messages;
            StringBuilder sb = new();
            for (int i = 0; messages != null && i < messages.Count;)
            {
                var subject = messages[i].subject;
                var worstSeverity = MessageType.Warning;
                sb.Clear();
                for (; i < messages.Count && messages[i].subject == subject; i++)
                {
                    sb.AppendLine(messages[i].text);
                    if (messages[i].severity > worstSeverity) worstSeverity = messages[i].severity;
                }
                parentElement.Add(new Label(subject == null ? "Function Hint Messages" : $"Parameter '{subject}' Hint Messages"));
                parentElement.Add(new HelpBoxRow(sb.ToString(), worstSeverity));
            }
        }
    }
}
