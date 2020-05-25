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
        public TargetPropertyGUIContext()
        {
            
        }

        public void AddProperty<T>(string label, BaseField<T> field, bool condition, EventCallback<ChangeEvent<T>> evt)
        {
            if(condition == true)
            {
                AddProperty<T>(label, field, evt);
            }
        }

        public void AddProperty<T>(string label, int indentLevel, BaseField<T> field, bool condition, EventCallback<ChangeEvent<T>> evt)
        {
            if(condition == true)
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
            if(field is INotifyValueChanged<T> notifyValueChanged)
            {
                notifyValueChanged.RegisterValueChangedCallback(evt);
            }

            string labelText = "";
            for (var i = 0; i < indentLevel; i++)
            {
                labelText += "    ";
            }
            labelText += label;

            var propertyRow = new PropertyRow(new Label(labelText));
            propertyRow.Add(field);
            this.hierarchy.Add(propertyRow);
        }

        public void AddLabel(string label, int indentLevel)
        {
            string labelText = "";
            for (var i = 0; i < indentLevel; i++)
            {
                labelText += "    ";
            }
            labelText += label;

            var propertyRow = new PropertyRow(new Label(labelText));
            this.hierarchy.Add(propertyRow);
        }
    }
}
