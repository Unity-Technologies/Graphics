using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    class Vector3PropertyRM : VectorPropertyRM<VFXVector3Field, Vector3>
    {
        private VFXColorField m_ColorField;

        public Vector3PropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            var isColor = m_Provider.attributes.Is(VFXPropertyAttributes.Type.Color);

            if (isColor)
            {
                var mainContainer = new VisualElement { name = "mainContainer" };
                mainContainer.AddToClassList("maincontainer");

                m_ColorField = new VFXColorField((Label)null);
                m_ColorField.OnValueChanged = OnColorValueChanged;
                m_ColorField.showAlpha = false;
                var vector3Field = this.Q<VFXVector3Field>();
                mainContainer.Add(m_ColorField);
                mainContainer.Add(vector3Field);
                Add(mainContainer);
            }
        }

        public override float GetPreferredControlWidth() => 170;

        public override INotifyValueChanged<Vector3> CreateField()
        {
            var label = new Label(ObjectNames.NicifyVariableName(provider.name));
            label.AddToClassList("label");
            Add(label);
            return new VFXVector3Field();
        }

        public override void UpdateGUI(bool force)
        {
            if (m_ColorField != null)
                m_ColorField.value = new Color(m_Value.x, m_Value.y, m_Value.z);

            base.UpdateGUI(force);
        }

        void OnColorValueChanged()
        {
            m_Value = new Vector3(m_ColorField.value.r, m_ColorField.value.g, m_ColorField.value.b);

            NotifyValueChanged();
        }

        protected override void UpdateEnabled()
        {
            fieldControl.SetEnabled(propertyEnabled);
            if (m_ColorField != null)
                m_ColorField.SetEnabled(propertyEnabled);
        }

        protected override void UpdateIndeterminate()
        {
            fieldControl.indeterminate = indeterminate;
            if (m_ColorField != null)
                m_ColorField.indeterminate = indeterminate;
        }

        public override bool IsCompatible(IPropertyRMProvider providerParam)
        {
            if (!base.IsCompatible(providerParam)) return false;

            bool isColor = provider.attributes.Is(VFXPropertyAttributes.Type.Color);

            return isColor == (m_ColorField != null);
        }

        public override bool showsEverything => true;
    }
}
