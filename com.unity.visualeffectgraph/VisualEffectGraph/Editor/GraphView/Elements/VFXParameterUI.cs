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

        VisualElement m_ExposedLabel;
        VisualElement m_ExposedNameLabel;
        public VFXParameterUI()
        {
            m_Exposed = new Toggle(ToggleExposed);
            m_ExposedName = new TextField();

            m_ExposedName.RegisterCallback<ChangeEvent<string>>(OnNameChanged);
            m_ExposedName.AddToClassList("value");

            m_ExposedLabel = new VisualElement();
            m_ExposedLabel.text = "exposed";
            m_ExposedLabel.AddToClassList("label");
            m_ExposedNameLabel = new VisualElement();
            m_ExposedNameLabel.text = "name";
            m_ExposedNameLabel.AddToClassList("label");

            m_ExposedContainer = new VisualElement();
            VisualElement exposedNameContainer = new VisualElement();

            m_ExposedContainer.Add(m_ExposedLabel);
            m_ExposedContainer.Add(m_Exposed);

            m_ExposedContainer.name = "exposedContainer";
            exposedNameContainer.name = "exposedNameContainer";

            exposedNameContainer.Add(m_ExposedNameLabel);
            exposedNameContainer.Add(m_ExposedName);


            inputContainer.Add(exposedNameContainer);
            inputContainer.Add(m_ExposedContainer);
        }

        protected override void OnStyleResolved(ICustomStyle style)
        {
            base.OnStyleResolved(style);

            float labelWidth = 70;
            float controlWidth = 120;

            var properties = inputContainer.Query().OfType<PropertyRM>().ToList();

            foreach (var port in properties)
            {
                float portLabelWidth = port.GetPreferredLabelWidth();
                float portControlWidth = port.GetPreferredControlWidth();

                if (labelWidth < portLabelWidth)
                {
                    labelWidth = portLabelWidth;
                }
                if (controlWidth < portControlWidth)
                {
                    controlWidth = portControlWidth;
                }
            }

            foreach (var port in properties)
            {
                port.SetLabelWidth(labelWidth);
            }

            m_ExposedLabel.style.width = labelWidth + 16;
            m_ExposedNameLabel.style.width = labelWidth + 16;

            inputContainer.style.width = labelWidth + controlWidth;
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

            if (m_Property == null)
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
