using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal class TargetPropertyGUIContext : VisualElement
    {
        const int kIndentWidthInPixel = 15;

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
                    exposed.style.minWidth = 55;
                }
            }
            else if (exposed != null)
            {
                exposed = null;
                UnityEngine.Debug.LogError("This target doesn't support exposable properties. Consider setting 'supportsExposableProperties' to true.");
            }

            var propertyRow = new PropertyRow(new Label(label), exposed);
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
