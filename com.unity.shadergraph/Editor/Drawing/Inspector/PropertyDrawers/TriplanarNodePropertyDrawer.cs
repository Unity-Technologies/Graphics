using System;
using UnityEditor.UIElements;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    [SGPropertyDrawer(typeof(TriplanarNode))]
    class TriplanarNodePropertyDrawer : AbstractMaterialNodePropertyDrawer
    {
        TriplanarNode node;
        PropertyRow inputPropRow;
        PropertyRow normalOutputPropRow;

        void UpdateVisibility()
        {
            normalOutputPropRow.visible = (node.textureType == TextureType.Normal);
        }

        // when node is modified we want to update the visibility
        void OnNodeModified(AbstractMaterialNode node, ModificationScope scope)
        {
            UpdateVisibility();
        }

        internal override void AddCustomNodeProperties(VisualElement parentElement, AbstractMaterialNode nodeBase, Action setNodesAsDirtyCallback, Action updateNodeViewsCallback)
        {
            node = nodeBase as TriplanarNode;

            var previewField = new EnumField(node.inputSpace);
            inputPropRow = new PropertyRow(new Label("Input Space"));
            inputPropRow.Add(previewField, (field) =>
            {
                field.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue.Equals(node.inputSpace))
                        return;

                    setNodesAsDirtyCallback?.Invoke();
                    node.owner.owner.RegisterCompleteObjectUndo("Change normal input space");
                    node.inputSpace = (CoordinateSpace)evt.newValue;
                    updateNodeViewsCallback?.Invoke();
                    node.Dirty(ModificationScope.Graph);
                });
            });
            parentElement.Add(inputPropRow);

            previewField = new EnumField(node.normalOutputSpace);
            normalOutputPropRow = new PropertyRow(new Label("Normal Output Space"));
            normalOutputPropRow.Add(previewField, (field) =>
            {
                field.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue.Equals(node.normalOutputSpace))
                        return;

                    setNodesAsDirtyCallback?.Invoke();
                    node.owner.owner.RegisterCompleteObjectUndo("Change normal output space");
                    node.normalOutputSpace = (CoordinateSpace)evt.newValue;
                    updateNodeViewsCallback?.Invoke();
                    node.Dirty(ModificationScope.Graph);
                });
            });
            parentElement.Add(normalOutputPropRow);

            UpdateVisibility();

            node.RegisterCallback(OnNodeModified);
        }

        internal override void DisposePropertyDrawer()
        {
            node.UnregisterCallback(OnNodeModified);
        }
    }
}
