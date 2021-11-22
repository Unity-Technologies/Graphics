using System;
using System.Reflection;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    [SGPropertyDrawer(typeof(ProceduralVirtualTextureNode))]
    class ProceduralVirtualTextureNodePropertyDrawer : AbstractMaterialNodePropertyDrawer
    {
        internal override void AddCustomNodeProperties(VisualElement parentElement, AbstractMaterialNode nodeBase, Action setNodesAsDirtyCallback, Action updateNodeViewsCallback)
        {
            var node = nodeBase as ProceduralVirtualTextureNode;

            var previewField = new EnumField(node.shaderDeclaration);
            var propertyRow = new PropertyRow(new Label("Shader Declaration"));
            propertyRow.Add(previewField, (field) =>
            {
                field.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue.Equals(node.shaderDeclaration))
                        return;

                    setNodesAsDirtyCallback?.Invoke();
                    node.owner.owner.RegisterCompleteObjectUndo("Change PVT shader declaration");
                    node.shaderDeclaration = (UnityEditor.ShaderGraph.Internal.HLSLDeclaration)evt.newValue;
                    updateNodeViewsCallback?.Invoke();
                    node.Dirty(ModificationScope.Graph);
                });
            });
            parentElement.Add(propertyRow);
        }
    }
}
