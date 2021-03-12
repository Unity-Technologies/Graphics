using System;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEditor.Graphing.Util;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph.Drawing.Inspector
{
    public static class PropertyDrawerUtils
    {
        public static Label CreateLabel(string text, int indentLevel = 0, FontStyle fontStyle = FontStyle.Normal)
        {
            string label = new string(' ', indentLevel * 4);
            var labelVisualElement = new Label(label + text);
            labelVisualElement.name = "header";
            labelVisualElement.style.unityFontStyleAndWeight = fontStyle;
            return labelVisualElement;
        }

        public static Label CreateLabel(string text, int indentLevel = 0)
        {
            string label = new string(' ', indentLevel * 4);
            var labelVisualElement = new Label(label + text);
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
        }
    }
}
