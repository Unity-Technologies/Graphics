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
    class VFXParameterUI : VFXStandaloneSlotContainerUI
    {
        PropertyRM m_Property;
        PropertyRM[] m_SubProperties;
        VFXPropertyIM m_PropertyIM;
        IMGUIContainer m_Container;

        public VFXParameterUI()
        {
            VisualElement exposedLabel = new VisualElement();
            exposedLabel.text = "exposed";
            exposedLabel.AddToClassList("label");
            VisualElement exposedNameLabel = new VisualElement();
            exposedNameLabel.text = "name";
            exposedNameLabel.AddToClassList("label");
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
                    m_PropertyIM = VFXPropertyIM.Create(presenter.anchorType, 55);

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
