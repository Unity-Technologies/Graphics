using RMGUI.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace UnityEditor.VFX.UI
{
    class VFXOperatorPresenter : VFXNodePresenter
    {
        [SerializeField]
        public int m_dirtyHack;

        [SerializeField]
        private object m_settings;
        public object settings { get { return m_settings; } set { m_settings = value; m_dirtyHack++; } }

        public VFXOperator Operator
        {
            get
            {
                return node as VFXOperator;
            }
        }

        protected override void Reset()
        {
            if (Operator != null)
            {
                settings = Operator.settings;
            }
            base.Reset();
        }
    }
}
