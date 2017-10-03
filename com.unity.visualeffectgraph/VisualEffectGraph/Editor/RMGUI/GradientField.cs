using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;


namespace UnityEditor.VFX.UIElements
{
    class GradientField : ValueControl<Gradient>
    {
        public GradientField(string label) : base(label)
        {
        }

        public GradientField(VisualElement existingLabel) : base(existingLabel)
        {
            this.AddManipulator(new Clickable(OnClick));
        }

        void OnClick()
        {
            GradientPicker.Show(GetValue(), true, OnGradientChanged);
        }

        void OnGradientChanged(Gradient gradient)
        {
            Gradient copy = new Gradient();

            copy.alphaKeys = gradient.alphaKeys;
            copy.colorKeys = gradient.colorKeys;
            copy.mode = gradient.mode;

            SetValue(copy);

            if (OnValueChanged != null)
                OnValueChanged();

            Dirty(ChangeType.Repaint);
        }

        Mesh m_GradientMesh;

        protected override void ValueToGUI()
        {
            Gradient gradient = GetValue();

            // Instantiate because GetGradientPreview returns a temporary;
            Texture2D gradientTexture = UnityEditorInternal.GradientPreviewCache.GenerateGradientPreview(gradient, style.backgroundImage.value);

            style.backgroundImage = gradientTexture;
        }
    }
}
