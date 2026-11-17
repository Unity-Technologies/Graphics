using System;
using Unity.GraphAuthoring.Editor.ProviderSystem;
using UnityEditor.ShaderGraph.ProviderSystem;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    [SGPropertyDrawer(typeof(AbstractVirtualFunctionNode))]
    class VirtualFunctionNodePropertyDrawer : AbstractProviderNodePropertyDrawer<IVirtualFunction>
    {
        private protected override void AddDescriptionValues(VisualElement section, AbstractProviderNode<IVirtualFunction> node)
            => ProviderInspectorUtils.AddValue(section, "Context", AbstractVirtualFunctionNode.ContextLabel(AbstractVirtualFunctionNode.ContextOf(node.Provider.Definition)));

        private protected override void AddSettings(VisualElement section, AbstractProviderNode<IVirtualFunction> node, Action setNodesAsDirtyCallback, Action updateNodeViewsCallback)
        {
            var provider = node.Provider;
            var definition = provider.Definition;
            bool hasBase = AbstractVirtualFunctionNode.HasBase(definition);
            var toggle = new Toggle { value = definition.EnsureBaseCall };
            toggle.SetEnabled(hasBase);
            toggle.RegisterValueChangedCallback(evt =>
            {
                setNodesAsDirtyCallback?.Invoke();
                node.owner.owner?.RegisterCompleteObjectUndo(evt.newValue ? "Ensure Base Call" : "Don't Ensure Base Call");
                node.owner.SetEnsureBaseCall(provider.Info.ProviderKey, evt.newValue);
                updateNodeViewsCallback?.Invoke();
                inspectorUpdateDelegate?.Invoke();
            });

            var tooltip = hasBase
                ? "Calls the base implementation first, unless a Base node already calls it."
                : "A pure virtual function has no base implementation to call.";
            ProviderInspectorUtils.AddSetting(section, "Ensure Base Call", tooltip, toggle);
        }
    }
}
