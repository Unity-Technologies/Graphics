using System;
using System.Collections.Generic;
using System.Linq;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.Experimental.RMGUI;
using UnityEngine.Experimental.RMGUI.StyleEnums;
using UnityEngine.Experimental.RMGUI.StyleSheets;

namespace UnityEditor.VFX.UI
{
    class VFXParameterUI : Node
    {
        public VFXParameterUI()
        {
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();
            var presenter = GetPresenter<VFXParameterPresenter>();
            if (presenter == null || presenter.node == null)
                return;

            presenter.node.position = presenter.position.position;
            presenter.node.collapsed = !presenter.expanded;
        }
    }
}