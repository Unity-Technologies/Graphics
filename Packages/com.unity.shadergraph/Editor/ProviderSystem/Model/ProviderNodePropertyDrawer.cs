using System;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.ProviderSystem;
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
            string sourcePath = AssetDatabase.GUIDToAssetPath(node.Provider.AssetID);
            string providerKey = provider.ProviderKey;

            if (!provider.IsValid)
            {
                parentElement.Add(new HelpBoxRow($"Could not find '{providerKey}' in '{sourcePath}'.", MessageType.Error));
                // TODO: When this happens, we should offer to update the AssetID if it's found elsewhere in the provider library.
                // This should be a consideration for API Management Hints (SVFXG-865).
                return;
            }

            // TODO: We could show this for all ProviderNodes, but the inspector isn't currently refreshed on topological changes.
            if (provider.AssetID != default)
            {
                parentElement.Add(new Label("Provider Key"));
                parentElement.Add(new HelpBoxRow(providerKey, MessageType.None));

                parentElement.Add(new Label("Source Path"));
                parentElement.Add(new HelpBoxRow(sourcePath, MessageType.None));

                string qualifiedSignature = ShaderObjectUtils.QualifySignature(node.Provider.Definition, true, true);
                parentElement.Add(new Label("Qualified Signature"));
                parentElement.Add(new HelpBoxRow(qualifiedSignature, MessageType.None));

                string code = ShaderObjectUtils.GenerateCode(provider.Definition, false, false, false);
                parentElement.Add(new Label("Expected Definition"));
                parentElement.Add(new HelpBoxRow(code, MessageType.None));
            }

            StringBuilder sb = new();
            bool hasMsg = false;

            // Function
            foreach (var msg in node.Header.Messages)
            {
                hasMsg = true;
                sb.AppendLine(msg);
            }
            if (hasMsg)
            {
                parentElement.Add(new Label("Function Hint Messages"));
                parentElement.Add(new HelpBoxRow(sb.ToString(), MessageType.Warning));
            }

            // Parameters
            foreach (var paramHeader in node.ParamHeaders.Values)
            {
                hasMsg = false;
                sb.Clear();
                foreach(var msg in paramHeader.Messages)
                {
                    hasMsg = true;
                    sb.AppendLine(msg);
                }
                if (hasMsg)
                {
                    parentElement.Add(new Label($"Parameter '{paramHeader.referenceName}' Hint Messages"));
                    parentElement.Add(new HelpBoxRow(sb.ToString(), MessageType.Warning));
                }
            }
        }
    }
}
