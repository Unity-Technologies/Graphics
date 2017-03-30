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
        }

        public override void OnEnable()
        {
            base.OnEnable();
            Invalidate(InvalidationCause.kStructureChanged);
        }

        protected override void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            base.OnInvalidate(model, cause);
            if (m_Type != null && outputSlots.Count == 0)
            {
                AddSlot(VFXSlot.Create(new VFXProperty(m_Type, "o"), VFXSlot.Direction.kOutput));
            }
        }

        [SerializeField]
        private bool m_Exposed;
        public bool exposed
        {
            get
            {
                return m_Exposed;
            }
            set
            {
                m_Exposed = value;
            }
        }

        [NonSerialized]
        private Type m_Type;
        public Type type
        {
            get
            {
                return m_Type;
            }
            set
            {
                Debug.AssertFormat(m_Type == null, "Type should be only set once");
                m_Type = value;
                Invalidate(InvalidationCause.kStructureChanged);
            }
        }

    }
}