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
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
            RegisterCallback<AttachToPanelEvent>(OnAttach);
        }


        void OnAttach(AttachToPanelEvent e)
        {
            ValueToGUI();
        }

        void OnDetach(DetachFromPanelEvent e)
        {
            if (style.backgroundImage.value != null)
            {
                Object.DestroyImmediate(style.backgroundImage.value);
                style.backgroundImage = null;
            }
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

        protected override void ValueToGUI()
        {
            Gradient gradient = GetValue();

            Texture2D gradientTexture = UnityEditorInternal.GradientPreviewCache.GenerateGradientPreview(gradient, style.backgroundImage.value);
            gradientTexture.hideFlags = HideFlags.HideAndDontSave;

            style.backgroundImage = gradientTexture;
        }
    }
}
