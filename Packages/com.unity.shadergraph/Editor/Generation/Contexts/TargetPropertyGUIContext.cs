using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal class TargetPropertyGUIContext : VisualElement
    {
        const int kIndentWidthInPixel = 15;

        public int globalIndentLevel { get; set; } = 0;

        public readonly Action graphValidation;

        public TargetPropertyGUIContext(Action graphValidationCallback)
        {
            graphValidation = graphValidationCallback;
        }

        public void AddProperty<T>(string label, BaseField<T> field, bool condition, EventCallback<ChangeEvent<T>> evt)
        {
            if (condition == true)
            {
                AddProperty<T>(label, field, evt);
            }
        }

        public void AddProperty<T>(string label, int indentLevel, BaseField<T> field, bool condition, EventCallback<ChangeEvent<T>> evt)
        {
            if (condition == true)
            {
                AddProperty<T>(label, indentLevel, field, evt);
            }
        }

        public void AddProperty<T>(string label, BaseField<T> field, EventCallback<ChangeEvent<T>> evt)
        {
            AddProperty<T>(label, 0, field, evt);
        }

        public void AddProperty<T>(string label, int indentLevel, BaseField<T> field, EventCallback<ChangeEvent<T>> evt)
        {
            AddProperty<T>(label, string.Empty, indentLevel, field, evt);
        }

        public void AddProperty<T>(string label, string tooltip, int indentLevel, BaseField<T> field, EventCallback<ChangeEvent<T>> evt)
        {
            if (field is INotifyValueChanged<T> notifyValueChanged)
            {
                notifyValueChanged.RegisterValueChangedCallback(evt);
            }

            var propertyLabel = new Label(label);
            propertyLabel.tooltip = tooltip;
            var propertyRow = new PropertyRow(propertyLabel);

            ApplyPadding(propertyRow, indentLevel);
            propertyRow.Add(field);
            this.hierarchy.Add(propertyRow);
        }

        public void AddLabel(string label, int indentLevel)
        {
            var propertyRow = new PropertyRow(new Label(label));
            ApplyPadding(propertyRow, indentLevel);
            this.hierarchy.Add(propertyRow);
        }

        public void AddHelpBox(MessageType messageType, string messageText)
        {
            var helpBox = new HelpBoxRow(messageType);
            helpBox.Add(new Label(messageText));
            this.hierarchy.Add(helpBox);
        }

        void ApplyPadding(PropertyRow row, int indentLevel)
        {
            row.Q(className: "unity-label").style.marginLeft = (globalIndentLevel + indentLevel) * kIndentWidthInPixel;
        }
    }
}
