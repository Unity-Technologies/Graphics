using System;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.ProviderSystem;
using UnityEngine;

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
            var definition = node.Provider.Definition;
            string sourcePath = AssetDatabase.GUIDToAssetPath(node.Provider.AssetID);

            string providerKey = provider.ProviderKey;
            string qualifiedSignature = ShaderObjectUtils.QualifySignature(node.Provider.Definition, true, true);
            bool hasProviderKey = definition?.Hints?.ContainsKey(Hints.Func.kProviderKey) ?? false;

            if (provider == null)
            {
                parentElement.Add(new HelpBoxRow("Provider node is in an invalid and irrecoverable state.", MessageType.Error));
                return;
            }

            bool isAnAsset = provider.AssetID != default;

            if (!isAnAsset)
                return;

            else if (!provider.IsValid)
            {
                parentElement.Add(new HelpBoxRow($"Could not find '{providerKey}' in '{sourcePath}'.", MessageType.Error));
            }
            else if (!hasProviderKey)
            {
                parentElement.Add(new HelpBoxRow($"'{providerKey}' in '{sourcePath}' does not have a 'ProviderKey' hint. If the namespace, name, or parameter list changes- this node instance will become invalidated.", MessageType.Warning));
            }
            else
            {
                parentElement.Add(new HelpBoxRow($"Provider Key '{providerKey}' for \n" +
                                                 $"Function Signature '{qualifiedSignature}' found in \n" +
                                                 $"File Path '{sourcePath}'.", MessageType.Info));
            }
        }
    }
}
