using System;
using UnityEditor.SearchService;
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


            if (m_Provider.portType.IsSubclassOf(typeof(Texture)))
            {
                m_ObjectField = new ObjectField { objectType = typeof(Texture), allowSceneObjects = false };
                m_ObjectField.onObjectSelectorShow += OnShowObjectSelector;
            }
            else
            {
                m_ObjectField = new ObjectField { objectType = m_Provider.portType, allowSceneObjects = false };
            }

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
            var newValueType = evt.newValue != null ? evt.newValue.GetType() : null;
            if (newValueType != null && newValueType != m_Provider.portType && (newValueType != typeof(RenderTexture) || m_Provider.portType == typeof(CubemapArray)))
            {
                m_ObjectField.SetValueWithoutNotify(evt.previousValue);
            }
            else
            {
                SetValue(evt.newValue);
                NotifyValueChanged();
            }
        }

        private void OnShowObjectSelector()
        {
            var isAdvancedSearch = ObjectSelectorSearch.HasEngineOverride();
            var searchFilter = $"t:{m_Provider.portType.Name}";
            if (isAdvancedSearch)
                searchFilter = "(" + searchFilter;
            if (m_Provider.portType != typeof(CubemapArray))
            {
                searchFilter += isAdvancedSearch ? " or t:RenderTexture)" : " t:RenderTexture";
            }

            ObjectSelector.get.searchFilter = searchFilter;
        }
    }
}
