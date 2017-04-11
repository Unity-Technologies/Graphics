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
        }
        public override NodeAnchor InstantiateNodeAnchor(NodeAnchorPresenter presenter)
        {
            return VFXDataAnchor.Create<VFXDataEdgePresenter>(presenter as VFXDataAnchorPresenter);
        }
    }
    class VFXParameterUI : VFXNodeUI
    {
        private Button m_ExposedName;
        private Button m_Exposed;

        private void RandomizeName()
        {
            var presenter = GetPresenter<VFXParameterPresenter>();
            string letter = "abcdefghijklmnopqrstuvwxyz";
            string randName = "rand_";
            for (int i=0;i<8;++i)
            {
                randName += letter[UnityEngine.Random.Range(0, letter.Length)];
            }
            presenter.exposedName = randName;
        }

        private void ToggleExposed()
        {
            var presenter = GetPresenter<VFXParameterPresenter>();
            presenter.exposed = !presenter.exposed;
        }

        public VFXParameterUI()
        {
            m_Exposed = new Button(ToggleExposed);
            m_ExposedName = new Button(RandomizeName);

            inputContainer.AddChild(m_Exposed);
            inputContainer.AddChild(m_ExposedName);
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();
            var presenter = GetPresenter<VFXParameterPresenter>();
            if (presenter == null || presenter.parameter == null)
                return;

            presenter.node.position = presenter.position.position;
            presenter.node.collapsed = !presenter.expanded;
            presenter.parameter.exposed = presenter.exposed;
            presenter.parameter.exposedName = presenter.exposedName;

            m_ExposedName.height = 24.0f;
            m_Exposed.height = 24.0f;
            m_ExposedName.text = presenter.exposedName == null ? "" : presenter.exposedName;
            m_Exposed.text = presenter.exposed ? "Exposed" : "Not Exposed";
       }
    }
}