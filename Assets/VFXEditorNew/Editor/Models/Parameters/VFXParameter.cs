using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.VFX
{
    class VFXParameter : VFXSlotContainerModel<VFXModel, VFXModel>
    {
        protected VFXParameter()
        {
            m_exposedName = "exposedName";
            m_exposed = false;
        }

        [SerializeField]
        private string m_exposedName;
        [SerializeField]
        private bool m_exposed;

        public string exposedName
        {
            get { return m_exposedName; }
            set
            {
                if (m_exposedName != value)
                {
                    m_exposedName = value;
                    Invalidate(InvalidationCause.kParamChanged);
                }
            }
        }

        public bool exposed
        {
            get { return m_exposed; }
            set
            {
                if (m_exposed != value)
                {
                    m_exposed = value;
                    Invalidate(InvalidationCause.kParamChanged);
                }
            }
        }

        public void Init(Type _type)
        {
            if (_type != null && outputSlots.Count == 0)
            {
                AddSlot(VFXSlot.Create(new VFXProperty(_type, "o"), VFXSlot.Direction.kOutput));
            }
            else
            {
                throw new InvalidOperationException("Cannot init VFXParameter");
            }
        }
    }
}