using System;
using UnityEditor.UIElements;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    [SGPropertyDrawer(typeof(PositionNode))]
    class PositionNodePropertyDrawer : AbstractMaterialNodePropertyDrawer
    {
        internal override void AddCustomNodeProperties(VisualElement parentElement, AbstractMaterialNode nodeBase, Action setNodesAsDirtyCallback, Action updateNodeViewsCallback)
        {
            var node = nodeBase as PositionNode;
            var previewField = new EnumField(node.m_PositionSource);
            var propertyRow = new PropertyRow(new Label("Source"));
            propertyRow.Add(previewField, (field) =>
            {
                field.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue.Equals(node.m_PositionSource))
                        return;

                    setNodesAsDirtyCallback?.Invoke();
                    node.owner.owner.RegisterCompleteObjectUndo("Change position source");
                    node.m_PositionSource = (UnityEditor.ShaderGraph.Internal.PositionSource)evt.newValue;
                    updateNodeViewsCallback?.Invoke();
                    node.Dirty(ModificationScope.Graph);
                });
            });
            parentElement.Add(propertyRow);
        }
    }
}
