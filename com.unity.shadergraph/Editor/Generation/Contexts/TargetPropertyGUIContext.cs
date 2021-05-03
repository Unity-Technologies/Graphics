using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal class TargetPropertyGUIContext : VisualElement
    {
        const int kIndentWidthInPixel = 15;
        const int kExposeBoxWidthInPixel = 55;

        public int globalIndentLevel {get; set;} = 0;
        internal bool supportsExposableProperties = false;

        public TargetPropertyGUIContext()
        {
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

        public void AddProperty<T>(string label, int indentLevel, BaseField<T> field, EventCallback<ChangeEvent<T>> evt, VisualElement exposed = null)
        {
            AddProperty<T>(label, string.Empty, indentLevel, field, evt, exposed);
        }

        public void AddProperty<T>(string label, string tooltip, int indentLevel, BaseField<T> field, EventCallback<ChangeEvent<T>> evt, VisualElement exposed = null)
        {
            if (field is INotifyValueChanged<T> notifyValueChanged)
                notifyValueChanged.RegisterValueChangedCallback(evt);

            if (supportsExposableProperties)
            {
                if (exposed != null)
                    exposed.name = "expose";
                else
                {
                    // Create an empty element to fill space
                    exposed = new VisualElement();
                    exposed.style.minWidth = kExposeBoxWidthInPixel;
                }
            }
            else if (exposed != null)
            {
                exposed = null;
                UnityEngine.Debug.LogError("This target doesn't support exposable properties. Consider setting 'supportsExposableProperties' to true.");
            }

            var propertyRow = new PropertyRow(new Label(label) { tooltip = tooltip }, exposed);

            ApplyPadding(propertyRow, indentLevel);
            propertyRow.Add(field);
            this.hierarchy.Add(propertyRow);
        }

        public void AddLabel(string label, int indentLevel)
        {
            var propertyRow = new PropertyRow(new Label(label));
            ApplyPadding(propertyRow, indentLevel, supportsExposableProperties);
            this.hierarchy.Add(propertyRow);
        }

        public void AddHelpBox(MessageType messageType, string messageText)
        {
            var helpBox = new HelpBoxRow(messageType);
            helpBox.Add(new Label(messageText));
            this.hierarchy.Add(helpBox);
        }

        void ApplyPadding(PropertyRow row, int indentLevel, bool exposeBoxPadding = false)
        {
            row.Q(className: "unity-label").style.marginLeft = (globalIndentLevel + indentLevel) * kIndentWidthInPixel + (exposeBoxPadding ? kExposeBoxWidthInPixel : 0);
        }
    }
}
