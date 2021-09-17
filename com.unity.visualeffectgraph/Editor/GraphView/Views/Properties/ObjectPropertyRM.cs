using UnityEngine;
using UnityEngine.UIElements;

using UnityObject = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    class ObjectPropertyRM : PropertyRM<UnityObject>
    {
        readonly TextField m_TextField;
        readonly Image m_ValueIcon;

        public ObjectPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            styleSheets.Add(VFXView.LoadStyleSheet("ObjectPropertyRM"));

            m_TextField = new TextField { name = "PickLabel", isReadOnly = true };
            var button = new Button { name = "PickButton" };
            var icon  = new VisualElement { name = "PickIcon" };
            m_ValueIcon = new Image { name = "TextureIcon" };

            button.clicked += OnPickObject;
            button.Add(icon);
            m_TextField.Add(m_ValueIcon);
            m_TextField.Add(button);
            Add(m_TextField);
        }

        public override float GetPreferredControlWidth() => 120;

        public override void UpdateGUI(bool force)
        {
            if (force)
            {
                NotifyValueChanged();
            }
        }

        public override void SetValue(object obj) // object setvalue should accept null
        {
            try
            {
                m_Value = (Object)obj;
                m_ValueIcon.image = obj != null
                    ? AssetPreview.GetMiniTypeThumbnail(m_Value)
                    : AssetPreview.GetMiniTypeThumbnail(m_Provider.portType);
                m_TextField.value = m_Value?.name ?? $"None ({m_Provider.portType.Name})";
            }
            catch (System.Exception)
            {
                Debug.Log($"Error Trying to convert {obj?.GetType().Name ?? "null"} to {nameof(Object)}");
            }

            UpdateGUI(!object.ReferenceEquals(m_Value, obj));
        }

        public override bool showsEverything => true;

        protected override void UpdateEnabled() => SetEnabled(propertyEnabled);

        protected override void UpdateIndeterminate() => visible = !indeterminate;

        void OnPickObject() => CustomObjectPicker.Pick(m_Provider.portType, SelectHandler);

        void SelectHandler(Object obj, bool isCanceled)
        {
            if (!isCanceled)
            {
                SetValue(obj);
                NotifyValueChanged();
            }
        }
    }
}
