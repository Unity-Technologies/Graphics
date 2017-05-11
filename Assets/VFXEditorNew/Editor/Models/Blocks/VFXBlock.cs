using UnityEngine;
using System.Collections.Generic;
using Type = System.Type;
using System.Reflection;

namespace UnityEditor.VFX
{
    abstract class VFXBlock : VFXSlotContainerModel<VFXContext, VFXModel>
    {
        [SerializeField]
        protected bool m_Enabled = true;

        public bool enabled
        {
            get { return m_Enabled;}
            set {
                m_Enabled = value;
                Invalidate(this,InvalidationCause.kStructureChanged);
            }
        }
        public abstract VFXContextType compatibleContexts { get; }
    }
}
