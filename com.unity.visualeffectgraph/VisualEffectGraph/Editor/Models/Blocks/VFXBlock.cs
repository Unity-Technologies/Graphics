using UnityEngine;
using System.Collections.Generic;
using Type = System.Type;
using System.Reflection;

namespace UnityEditor.VFX
{
    abstract class VFXBlock : VFXSlotContainerModel<VFXContext, VFXModel>
    {
        [SerializeField]
        protected bool m_Disabled = false;

        public bool enabled
        {
            get { return !m_Disabled; }
            set
            {
                m_Disabled = !value;
                Invalidate(this, InvalidationCause.kStructureChanged);
            }
        }
        public abstract VFXContextType compatibleContexts { get; }
    }
}
