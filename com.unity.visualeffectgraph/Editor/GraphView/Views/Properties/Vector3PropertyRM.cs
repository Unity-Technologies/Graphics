using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.VFX;
using UnityEditor.VFX.UIElements;
using Object = UnityEngine.Object;
using Type = System.Type;

using VFXVector3Field = UnityEditor.VFX.UI.VFXVector3Field;
using VFXColorField = UnityEditor.VFX.UI.VFXColorField;

namespace UnityEditor.VFX.UI
{
    class Vector3PropertyRM : PropertyRM<Vector3>
    {
        VFXColorField m_ColorField;

        VFXVector3Field m_VectorField;

        public Vector3PropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            bool isColor = m_Provider.attributes.Is(VFXPropertyAttributes.Type.Color);

            if (isColor)
            {
                m_ColorField = new VFXColorField(m_Label);
                m_ColorField.OnValueChanged = OnColorValueChanged;
                m_ColorField.showAlpha = false;
                m_VectorField = new VFXVector3Field();
                m_VectorField.RegisterCallback<ChangeEvent<Vector3>>(OnValueChanged);
                var mainContainer = new VisualElement() { name = "mainContainer" };
                mainContainer.AddToClassList("maincontainer");

                mainContainer.Add(m_ColorField);
                mainContainer.Add(m_VectorField);
                Add(mainContainer);
                m_VectorField.AddToClassList("fieldContainer");
            }
            else
            {
                var labeledField = new VFXLabeledField<VFXVector3Field, Vector3>(m_Label);
                m_VectorField = labeledField.control;
                labeledField.RegisterCallback<ChangeEvent<Vector3>>(OnValueChanged);
                Add(labeledField);
                labeledField.AddToClassList("fieldContainer");
            }

            m_VectorField.onValueDragFinished = ValueDragFinished;
            m_VectorField.onValueDragStarted = ValueDragStarted;
        }

        public override void UpdateGUI(bool force)
        {
            if (m_ColorField != null)
                m_ColorField.value = new Color(m_Value.x, m_Value.y, m_Value.z);

            m_VectorField.SetValueWithoutNotify(m_Value);
            if (force)
                m_VectorField.ForceUpdate();
        }

        void OnColorValueChanged()
        {
            m_Value = new Vector3(m_ColorField.value.r, m_ColorField.value.g, m_ColorField.value.b);

            NotifyValueChanged();
        }

        void OnValueChanged(ChangeEvent<Vector3> e)
        {
            m_Value = m_VectorField.value;

            NotifyValueChanged();
        }

        protected void ValueDragFinished()
        {
            m_Provider.EndLiveModification();
            hasChangeDelayed = false;
            NotifyValueChanged();
        }

        protected void ValueDragStarted()
        {
            m_Provider.StartLiveModification();
        }

        protected override void UpdateEnabled()
        {
            m_VectorField.SetEnabled(propertyEnabled);
            if (m_ColorField != null)
                m_ColorField.SetEnabled(propertyEnabled);
        }

        protected override void UpdateIndeterminate()
        {
            m_VectorField.indeterminate = indeterminate;
            if (m_ColorField != null)
                m_ColorField.indeterminate = indeterminate;
        }

        public override float GetPreferredControlWidth()
        {
            return 170;
        }

        public override bool IsCompatible(IPropertyRMProvider provider)
        {
            if (!base.IsCompatible(provider)) return false;

            bool isColor = provider.attributes.Is(VFXPropertyAttributes.Type.Color);

            return isColor == (m_ColorField != null);
        }

        public override bool showsEverything { get { return true; } }
    }
}
