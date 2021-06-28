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
            var previewField = new EnumField(node.m_TessellationOption);
            var propertyRow = new PropertyRow(new Label("Tessellation"));
            propertyRow.Add(previewField, (field) =>
            {
                field.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue.Equals(node.m_TessellationOption))
                        return;

                    setNodesAsDirtyCallback?.Invoke();
                    node.owner.owner.RegisterCompleteObjectUndo("Change tessellation option");
                    node.m_TessellationOption = (UnityEditor.ShaderGraph.Internal.TessellationOption)evt.newValue;
                    updateNodeViewsCallback?.Invoke();
                    node.Dirty(ModificationScope.Graph);
                });
            });
            if (node is Serialization.MultiJsonInternal.UnknownNodeType)
                previewField.SetEnabled(false);
            parentElement.Add(propertyRow);
        }
    }
}
