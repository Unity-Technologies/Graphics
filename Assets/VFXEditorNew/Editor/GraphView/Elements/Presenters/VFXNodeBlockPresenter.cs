using System;
using RMGUI.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEditor.VFX.UI
{
    class VFXNodeBlockPresenter : GraphElementPresenter
    {
		protected new void OnEnable()
		{
			capabilities |= Capabilities.Selectable | Capabilities.Droppable | Capabilities.Movable;


		}

        public VFXBlock Model
        {
            get { return m_Model; }
            set {

                if (m_Model != value)
                {
                    m_Model = value;
                }
            }
        }

        public Type GetPropertiesType()
        {
            return m_Model.Desc.GetPropertiesType();
        }

        public object GetCurrentProperties()
        {
            return m_Model.GetCurrentProperties();
        }

        [SerializeField]
        private VFXBlock m_Model;
    }
}
