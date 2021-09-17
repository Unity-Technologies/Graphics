using UnityEngine;
using UnityEngine.UIElements;

using UnityObject = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    class ObjectPropertyRM : PropertyRM<UnityObject>
    {
        readonly VisualElement m_Container;
        readonly TextField m_TextField;
        readonly Image m_ValueIcon;

        public ObjectPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            m_Container = new VisualElement();
            m_Container.styleSheets.Add(VFXView.LoadStyleSheet("ObjectPropertyRM"));

            m_Container.style.flexDirection = FlexDirection.Row;

            m_TextField = new TextField { name = "PickLabel", isReadOnly = true, value = m_Value?.name ?? "None (Texture)"};
            var button = new Button { name = "PickButton" };
            var icon  = new VisualElement { name = "PickIcon" };
            m_ValueIcon = new Image { name = "TextureIcon" };

            button.clicked += OnPickObject;
            button.Add(icon);
            m_TextField.Add(m_ValueIcon);
            m_TextField.Add(button);
            m_Container.Add(m_TextField);

            Add(m_Container);
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
                Debug.Log("Error Trying to convert" + (obj != null ? obj.GetType().Name : "null") + " to " + typeof(UnityObject).Name);
            }

            UpdateGUI(!object.ReferenceEquals(m_Value, obj));
        }

        public override bool showsEverything => true;

        protected override void UpdateEnabled() => m_Container.SetEnabled(propertyEnabled);

        protected override void UpdateIndeterminate() => m_Container.visible = !indeterminate;

        void OnPickObject() => TexturePicker.Pick(m_Provider.portType, SelectHandler);

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
