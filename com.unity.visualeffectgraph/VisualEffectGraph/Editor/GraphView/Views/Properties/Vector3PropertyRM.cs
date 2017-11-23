using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;
using UnityEditor.VFX;
using UnityEditor.VFX.UIElements;
using Object = UnityEngine.Object;
using Type = System.Type;

using Vector3Field = UnityEditor.VFX.UIElements.Vector3Field;
using FloatField = UnityEditor.Experimental.UIElements.FloatField;
using ColorField = UnityEditor.VFX.UIElements.ColorField;

namespace UnityEditor.VFX.UI
{
    class Vector3PropertyRM : PropertyRM<Vector3>
    {
        ColorField m_ColorField;

        Vector3Field m_VectorField;

        public Vector3PropertyRM(IPropertyRMProvider presenter, float labelWidth) : base(presenter, labelWidth)
        {
            bool isColor = VFXPropertyAttribute.IsColor(m_Provider.attributes);

            if (isColor)
            {
                m_ColorField = new ColorField(m_Label);
                m_ColorField.OnValueChanged = OnColorValueChanged;
                m_VectorField = new Vector3Field();
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
                var labeledField = new LabeledField<Vector3Field, Vector3>(m_Label);
                m_VectorField = labeledField.control;
                Add(labeledField);
                labeledField.AddToClassList("fieldContainer");
            }
        }

        public override void UpdateGUI()
        {
            if (m_ColorField != null)
                m_ColorField.value = new Color(m_Value.x, m_Value.y, m_Value.z);

            m_VectorField.value = m_Value;
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

        protected override void UpdateEnabled()
        {
            m_VectorField.SetEnabled(propertyEnabled);
            if (m_ColorField != null)
                m_ColorField.SetEnabled(propertyEnabled);
        }

        public override float GetPreferredControlWidth()
        {
            return 195;
        }

        public override bool showsEverything { get { return true; } }
    }
}
