using System;
using RMGUI.GraphView;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.VFX.UI
{
    public class VFXNodeBlockPresenter : GraphElementPresenter
    {
        public static VFXNodeBlockUI Create(VFXNodeBlockPresenter nodeblock)
        {
            return new VFXNodeBlockUI(nodeblock);
        }

        public VFXNodeBlockPresenter()
        {
            capabilities |= Capabilities.Selectable;
        }
    }
}
