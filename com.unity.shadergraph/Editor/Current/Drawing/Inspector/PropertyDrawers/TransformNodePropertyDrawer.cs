using System;
using UnityEditor.UIElements;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    [SGPropertyDrawer(typeof(TransformNode))]
    class TransformNodePropertyDrawer : AbstractMaterialNodePropertyDrawer
    {
        TransformNode node;
        PropertyRow normalizePropRow;

        void UpdateVisibility()
        {
            normalizePropRow.visible = (node.sgVersion >= 2) && (node.conversionType != ConversionType.Position);
        }

        internal override void AddCustomNodeProperties(VisualElement parentElement, AbstractMaterialNode nodeBase, Action setNodesAsDirtyCallback, Action updateNodeViewsCallback)
        {
            node = nodeBase as TransformNode;

            var normalizeControl = new Toggle();
            normalizeControl.value = node.normalize;

            normalizePropRow = new PropertyRow(new Label("Normalize Output"));
            normalizePropRow.Add(normalizeControl, (field) =>
            {
                field.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue.Equals(node.normalize))
                        return;

                    setNodesAsDirtyCallback?.Invoke();
                    node.owner.owner.RegisterCompleteObjectUndo("Change normalize");
                    node.normalize = evt.newValue;
                    updateNodeViewsCallback?.Invoke();
                    node.Dirty(ModificationScope.Graph);
                });
            });
            parentElement.Add(normalizePropRow);

            UpdateVisibility();

            node.RegisterCallback(OnNodeModified);
        }

        // when node is modified we want to update the visibility
        void OnNodeModified(AbstractMaterialNode node, ModificationScope scope)
        {
            UpdateVisibility();
        }

        internal override void DisposePropertyDrawer()
        {
            node.UnregisterCallback(OnNodeModified);
        }
    }
}
