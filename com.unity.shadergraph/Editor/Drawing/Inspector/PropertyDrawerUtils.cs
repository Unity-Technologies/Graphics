using System;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEditor.Graphing.Util;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.Drawing.Inspector
{
    public static class PropertyDrawerUtils
    {
        public static Label CreateLabel(string text, int indentLevel = 0, FontStyle fontStyle = FontStyle.Normal)
        {
            string label = new string(' ', indentLevel * 4);
            var labelVisualElement = new Label(label + text);
            labelVisualElement.style.unityFontStyleAndWeight = fontStyle;
            return labelVisualElement;
        }

        internal static void AddDefaultNodeProperties(VisualElement parentElement, AbstractMaterialNode node, Action setNodesAsDirtyCallback, Action updateNodeViewsCallback)
        {
            EnumField precisionField = null;
            if (node.canSetPrecision)
            {
                precisionField = new EnumField(node.precision);
                var propertyRow = new PropertyRow(new Label("Precision"));
                propertyRow.Add(precisionField, (field) =>
                {
                    field.RegisterValueChangedCallback(evt =>
                    {
                        if (evt.newValue.Equals(node.precision))
                            return;

                        setNodesAsDirtyCallback?.Invoke();
                        node.owner.owner.RegisterCompleteObjectUndo("Change precision");
                        node.precision = (Precision)evt.newValue;
                        node.owner.ValidateGraph();
                        updateNodeViewsCallback?.Invoke();
                        node.Dirty(ModificationScope.Graph);
                    });
                });
                if (node is Serialization.MultiJsonInternal.UnknownNodeType)
                    precisionField.SetEnabled(false);
                parentElement.Add(propertyRow);
            }

            EnumField previewField = null;
            if (node.hasPreview)
            {
                previewField = new EnumField(node.m_PreviewMode);
                var propertyRow = new PropertyRow(new Label("Preview"));
                propertyRow.Add(previewField, (field) =>
                {
                    field.RegisterValueChangedCallback(evt =>
                    {
                        if (evt.newValue.Equals(node.m_PreviewMode))
                            return;

                        setNodesAsDirtyCallback?.Invoke();
                        node.owner.owner.RegisterCompleteObjectUndo("Change preview");
                        node.m_PreviewMode = (PreviewMode)evt.newValue;
                        updateNodeViewsCallback?.Invoke();
                        node.Dirty(ModificationScope.Graph);
                    });
                });
                if (node is Serialization.MultiJsonInternal.UnknownNodeType)
                    previewField.SetEnabled(false);
                parentElement.Add(propertyRow);
            }

            if (node is BlockNode bnode)
            {
                AddCustomInterpolatorProperties(parentElement, bnode, setNodesAsDirtyCallback, updateNodeViewsCallback);
            }
        }

        internal static void AddCustomInterpolatorProperties(VisualElement parentElement, BlockNode node, Action setNodesAsDirtyCallback, Action updateNodeViewsCallback)
        {
            if (!node.isCustomBlock)
                return;

            TextField textField = null;
            {
                textField = new TextField { value = node.customBlockName, multiline = false };
                var propertyRow = new PropertyRow(new Label("Name"));
                propertyRow.Add(textField, (field) =>
                {
                    field.RegisterCallback<FocusOutEvent>(evt =>
                    {
                        HashSet<string> usedNames = new HashSet<string>();
                        foreach (var other in node.contextData.blocks) if(other != node) usedNames.Add(other.value.descriptor.displayName);
                        field.value = node.customBlockName = GraphUtil.SanitizeName(usedNames, "{0}_{1}", NodeUtils.ConvertToValidHLSLIdentifier(field.value));
                        
                        setNodesAsDirtyCallback?.Invoke();
                        node.owner.owner.RegisterCompleteObjectUndo("Change Block Name");
                        node.RenewCustomBlockFieldDescriptor();
                        updateNodeViewsCallback?.Invoke();
                        node.Dirty(ModificationScope.Graph);
                    });
                });
                parentElement.Add(propertyRow);  
            }

            EnumField typeField = null;
            {
                typeField = new EnumField(node.customBlockType);
                var propertyRow = new PropertyRow(new Label("Type"));
                propertyRow.Add(typeField, (field) =>
                {
                    field.RegisterValueChangedCallback(evt =>
                    {
                        if (evt.newValue.Equals(node.customBlockType))
                            return;

                        setNodesAsDirtyCallback?.Invoke();
                        node.owner.owner.RegisterCompleteObjectUndo("Change Block Type");
                        node.customBlockType = (BlockNode.CustomBlockType)evt.newValue;
                        node.RenewCustomBlockFieldDescriptor(); // Dirty
                        updateNodeViewsCallback?.Invoke();
                        node.Dirty(ModificationScope.Graph);
                    }); 
                });
                parentElement.Add(propertyRow);
            }
        }
    }
}
