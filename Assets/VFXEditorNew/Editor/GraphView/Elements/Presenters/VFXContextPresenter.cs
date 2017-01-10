using System;
using RMGUI.GraphView;
using UnityEngine;

namespace UnityEditor.VFX.UI
{
    class VFXContextPresenter : GraphElementPresenter
    {
        protected new void OnEnable()
        {
            base.OnEnable();
            capabilities |= Capabilities.Deletable;
        }

        public VFXContext Model
        {
            get { return m_Model; }
            set { m_Model = value; }
        }

        private VFXContext m_Model;
    }
}
