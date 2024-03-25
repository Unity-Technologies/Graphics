using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    /// <summary>
    /// Vector2 properties can represent a Min-Max range. This custom PropertyRM allows to display it as a MinMaxSlider
    /// </summary>
    class Vector2PropertyRM : VectorPropertyRM<VFXVector2Field, Vector2>
    {
        public Vector2PropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override float GetPreferredControlWidth() => 120;

        public override INotifyValueChanged<Vector2> CreateField()
        {
            var label = new Label(ObjectNames.NicifyVariableName(provider.name));
            label.AddToClassList("label");
            Add(label);
            return new VFXVector2Field();
        }
    }
}
