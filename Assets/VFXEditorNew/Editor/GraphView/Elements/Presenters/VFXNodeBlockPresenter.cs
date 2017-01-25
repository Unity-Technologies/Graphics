using System;
using RMGUI.GraphView;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.VFX.UI
{
    class VFXNodeBlockPresenter : IMGUIPresenter
    {
		protected new void OnEnable()
		{
			capabilities |= Capabilities.Selectable | Capabilities.Droppable | Capabilities.Movable;
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
