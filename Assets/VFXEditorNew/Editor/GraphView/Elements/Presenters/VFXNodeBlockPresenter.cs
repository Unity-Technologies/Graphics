using System;
using RMGUI.GraphView;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.VFX.UI
{
    class VFXNodeBlockPresenter : GraphElementPresenter
    {
        public static VFXNodeBlockUI Create(VFXNodeBlockPresenter nodeblock)
        {
            return new VFXNodeBlockUI(nodeblock);
        }

        public VFXNodeBlockPresenter()
        {
            capabilities |= Capabilities.Selectable;
        }

        public VFXBlock Model
        {
            get { return m_Model; }
            set { m_Model = value; }
        }

        [SerializeField]
        private VFXBlock m_Model;
    }
}
