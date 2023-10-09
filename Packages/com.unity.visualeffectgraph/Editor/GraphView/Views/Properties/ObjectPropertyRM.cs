using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    class ObjectPropertyRM : PropertyRM<UnityObject>
    {
        readonly ObjectField m_ObjectField;

        public ObjectPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            styleSheets.Add(VFXView.LoadStyleSheet("ObjectPropertyRM"));


            m_ObjectField = new ObjectField { objectType = m_Provider.portType };
            m_ObjectField.RegisterCallback<ChangeEvent<UnityObject>>(OnValueChanged);
            Add(m_ObjectField);
        }

        public override float GetPreferredControlWidth() => 120;

        public override void UpdateGUI(bool force)
        {
            m_ObjectField.value = m_Value;
        }

        public override void SetValue(object obj)
        {
            try
            {
                m_Value = (UnityObject)obj;
            }
            catch (Exception)
            {
                Debug.Log($"Error Trying to convert {obj?.GetType().Name ?? "null"} to Object");
            }

            UpdateGUI(!ReferenceEquals(m_Value, obj));
        }

        public override bool showsEverything => true;

        protected override void UpdateEnabled() => m_ObjectField.SetEnabled(propertyEnabled);

        protected override void UpdateIndeterminate() => visible = !indeterminate;

        private void OnValueChanged(ChangeEvent<UnityObject> evt)
        {
            SetValue(evt.newValue);
            NotifyValueChanged();
        }
    }
}
