using UnityEngine;
using UnityEngine.UIElements;

using UnityObject = UnityEngine.Object;
using ObjectField = UnityEditor.VFX.UI.VFXLabeledField<UnityEditor.UIElements.ObjectField, UnityEngine.Object>;

namespace UnityEditor.VFX.UI
{
    class ObjectPropertyRM : PropertyRM<UnityObject>
    {
        readonly ObjectField m_ObjectField;
        readonly VisualElement m_Container;
        readonly TextField m_TextField;
        readonly Image m_TextureIcon;

        public ObjectPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {

            m_Container = new VisualElement();
            m_Container.styleSheets.Add(VFXView.LoadStyleSheet("ObjectPropertyRM"));

            if (typeof(Texture).IsAssignableFrom(m_Provider.portType))
            {
                m_Container.style.flexDirection = FlexDirection.Row;

                m_TextField = new TextField { name = "PickLabel", isReadOnly = true, value = m_Value?.name ?? "None (Texture)"};
                var button = new Button { name = "PickButton" };
                var icon  = new VisualElement { name = "PickIcon" };
                m_TextureIcon = new Image { name = "TextureIcon" };

                button.clicked += OnPickObject;
                button.Add(icon);
                m_TextField.Add(m_TextureIcon);
                m_TextField.Add(button);
                m_Container.Add(m_TextField);
            }
            else
            {
                m_ObjectField = new ObjectField(m_Label);
                m_ObjectField.control.objectType = controller.portType;
                m_ObjectField.control.allowSceneObjects = false;
                m_ObjectField.style.flexGrow = 1f;
                m_ObjectField.style.flexShrink = 1f;
                RegisterCallback<KeyDownEvent>(StopKeyPropagation);
                m_Container.Add(m_ObjectField);
            }

            Add(m_Container);
        }

        public override float GetPreferredControlWidth()
        {
            return 120;
        }

        public override void UpdateGUI(bool force)
        {
            if (m_ObjectField != null)
            {
                if (force)
                    m_ObjectField.SetValueWithoutNotify(null);
                m_ObjectField.SetValueWithoutNotify(m_Value);
            }
        }

        public override void SetValue(object obj) // object setvalue should accept null
        {
            try
            {
                m_Value = (UnityObject)obj;
                SelectHandler(m_Value as Texture, false);
            }
            catch (System.Exception)
            {
                Debug.Log("Error Trying to convert" + (obj != null ? obj.GetType().Name : "null") + " to " + typeof(UnityObject).Name);
            }

            UpdateGUI(!object.ReferenceEquals(m_Value, obj));
        }

        public override bool showsEverything { get { return true; } }

        protected override void UpdateEnabled()
        {
            m_Container.SetEnabled(propertyEnabled);
        }

        protected override void UpdateIndeterminate()
        {
            m_Container.visible = !indeterminate;
        }

        void StopKeyPropagation(KeyDownEvent e)
        {
            e.StopPropagation();
        }

        void OnPickObject()
        {
            TexturePicker.Pick(m_Provider.portType, SelectHandler);
        }

        void SelectHandler(Texture texture, bool isCanceled)
        {
            if (!isCanceled)
            {
                m_Value = texture;
                m_TextureIcon.image = texture != null
                    ? AssetPreview.GetMiniTypeThumbnail(texture)
                    : AssetPreview.GetMiniTypeThumbnail(typeof(Texture));
                m_TextField.value = m_Value?.name ?? "None (Texture)";
                NotifyValueChanged();
            }
        }
    }
}
