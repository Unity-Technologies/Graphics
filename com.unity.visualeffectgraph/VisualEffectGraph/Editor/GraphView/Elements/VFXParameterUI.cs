using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.VFX.UI
{
    class VFXBuiltInParameterUI : VFXStandaloneSlotContainerUI
    {
    }

    class VFXAttributeParameterUI : VFXStandaloneSlotContainerUI
    {
    }

    class VFXParameterUI : VFXStandaloneSlotContainerUI
    {
        private TextField m_ExposedName;
        private Toggle m_Exposed;
        VisualElement m_ExposedContainer;

        public void OnNameChanged(ChangeEvent<string> e)
        {
            var presenter = GetPresenter<VFXParameterPresenter>();

            presenter.exposedName = m_ExposedName.text;
        }

        private void ToggleExposed()
        {
            var presenter = GetPresenter<VFXParameterPresenter>();
            presenter.exposed = !presenter.exposed;
        }

        PropertyRM m_Property;
        PropertyRM[] m_SubProperties;
        VFXPropertyIM m_PropertyIM;
        IMGUIContainer m_Container;

        public VFXParameterUI()
        {
            m_Exposed = new Toggle(ToggleExposed);
            m_ExposedName = new TextField();

            m_ExposedName.RegisterCallback<ChangeEvent<string>>(OnNameChanged);
            m_ExposedName.AddToClassList("value");

            VisualElement exposedLabel = new VisualElement();
            exposedLabel.text = "exposed";
            exposedLabel.AddToClassList("label");
            VisualElement exposedNameLabel = new VisualElement();
            exposedNameLabel.text = "name";
            exposedNameLabel.AddToClassList("label");

            m_ExposedContainer = new VisualElement();
            VisualElement exposedNameContainer = new VisualElement();

            m_ExposedContainer.Add(exposedLabel);
            m_ExposedContainer.Add(m_Exposed);

            m_ExposedContainer.name = "exposedContainer";
            exposedNameContainer.name = "exposedNameContainer";

            exposedNameContainer.Add(exposedNameLabel);
            exposedNameContainer.Add(m_ExposedName);


            inputContainer.Add(exposedNameContainer);
            inputContainer.Add(m_ExposedContainer);
        }

        void OnGUI()
        {
            if (m_PropertyIM != null)
            {
                var presenter = GetPresenter<VFXParameterPresenter>();
                var all = presenter.allChildren.OfType<VFXDataAnchorPresenter>();
                m_PropertyIM.OnGUI(all.FirstOrDefault());
            }
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();
            var presenter = GetPresenter<VFXParameterPresenter>();
            if (presenter == null)
                return;

            m_ExposedName.style.height = 24.0f;
            m_Exposed.style.height = 24.0f;
            m_ExposedName.text = presenter.exposedName == null ? "" : presenter.exposedName;
            m_Exposed.on = presenter.exposed;

            if (m_Property == null && m_PropertyIM == null)
            {
                m_Property = PropertyRM.Create(presenter, 55);
                if (m_Property != null)
                {
                    inputContainer.Add(m_Property);

                    if (!m_Property.showsEverything)
                    {
                        int count = presenter.CreateSubPresenters();
                        m_SubProperties = new PropertyRM[count];

                        for (int i = 0; i < count; ++i)
                        {
                            m_SubProperties[i] = PropertyRM.Create(presenter.GetSubPresenter(i), 55);
                            inputContainer.Add(m_SubProperties[i]);
                        }
                    }
                }
                else
                {
                    m_PropertyIM = VFXPropertyIM.Create(presenter.portType, 55);

                    m_Container = new IMGUIContainer(OnGUI) { name = "IMGUI" };
                    inputContainer.Add(m_Container);
                }
            }
            if (m_Property != null)
                m_Property.Update();
            if (m_SubProperties != null)
            {
                foreach (var subProp in m_SubProperties)
                {
                    subProp.Update();
                }
            }
        }
    }
}
