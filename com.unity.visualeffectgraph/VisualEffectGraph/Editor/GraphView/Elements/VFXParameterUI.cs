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
    class VFXParameterUI : VFXNodeUI
    {
        Image m_Icon;
        public VFXParameterUI()
        {
            RemoveFromClassList("VFXNodeUI");
            AddStyleSheetPath("VFXParameter");

            m_CollapseButton.RemoveFromHierarchy();
            m_Icon = new Image() { name = "exposed-icon" };
            titleContainer.Insert(0, m_Icon);
        }

        public new VFXParameterNodeController controller
        {
            get { return base.controller as VFXParameterNodeController; }
        }

        protected override bool syncInput
        {
            get { return false; }
        }
        protected override void SelfChange()
        {
            base.SelfChange();

            VisualElement contents = mainContainer.Q("contents");
            VisualElement divider = contents.Q("divider");

            const string k_HiddenClassList = "hidden";

            divider.visible = false;
            divider.AddToClassList(k_HiddenClassList);

            if (controller.parentController.exposed)
            {
                AddToClassList("exposed");
            }
            else
            {
                RemoveFromClassList("exposed");
            }
        }
    }
}
