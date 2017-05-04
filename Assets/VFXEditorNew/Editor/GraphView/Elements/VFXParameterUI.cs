using System;
using System.Collections.Generic;
using System.Linq;
using UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.VFX.UI
{
    class VFXNodeUI : Node
    {
        public VFXNodeUI()
        {
            clipChildren = false;
            inputContainer.clipChildren = false;
            mainContainer.clipChildren = false;
            leftContainer.clipChildren = false;
            rightContainer.clipChildren = false;
            outputContainer.clipChildren = false;
            AddToClassList("VFXNodeUI");
        }

        public override NodeAnchor InstantiateNodeAnchor(NodeAnchorPresenter presenter)
        {
            if (presenter.direction == Direction.Input)
            {
                return VFXEditableDataAnchor.Create<VFXDataEdgePresenter>(presenter as VFXDataAnchorPresenter);
            }
            else
            {
                return VFXOutputDataAnchor.Create<VFXDataEdgePresenter>(presenter as VFXDataAnchorPresenter);
            }
        }
    }

    class VFXBuiltInParameterUI : VFXNodeUI
    {
    }

    class VFXAttributeParameterUI : VFXNodeUI
    {
    }

    class VFXParameterUI : VFXNodeUI
    {
        private TextField m_ExposedName;
        private Toggle m_Exposed;
        VisualContainer m_ExposedContainer;

        public void OnNameChanged()
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

        public VFXParameterUI()
        {
            m_Exposed = new Toggle(ToggleExposed);
            m_ExposedName = new TextField();

            m_ExposedName.onTextChanged += OnNameChanged;
            m_ExposedName.AddToClassList("value");

            VisualElement exposedLabel = new VisualElement();
            exposedLabel.text = "exposed";
            exposedLabel.AddToClassList("label");
            VisualElement exposedNameLabel = new VisualElement();
            exposedNameLabel.text = "name";
            exposedNameLabel.AddToClassList("label");

            m_ExposedContainer = new VisualContainer();
            VisualContainer exposedNameContainer = new VisualContainer();

            m_ExposedContainer.AddChild(exposedLabel);
            m_ExposedContainer.AddChild(m_Exposed);

            m_ExposedContainer.name = "exposedContainer";
            exposedNameContainer.name = "exposedNameContainer";

            exposedNameContainer.AddChild(exposedNameLabel);
            exposedNameContainer.AddChild(m_ExposedName);


            inputContainer.AddChild(exposedNameContainer);
            inputContainer.AddChild(m_ExposedContainer);
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();
            var presenter = GetPresenter<VFXParameterPresenter>();
            if (presenter == null)
                return;

            m_ExposedName.height = 24.0f;
            m_Exposed.height = 24.0f;
            m_ExposedName.text = presenter.exposedName == null ? "" : presenter.exposedName;
            m_Exposed.on = presenter.exposed;

            if (m_Property == null)
            {
                m_Property = PropertyRM.Create(presenter);

                inputContainer.AddChild(m_Property);
            }
            if( m_Property!= null )
                m_Property.Update();
        }
    }
}
