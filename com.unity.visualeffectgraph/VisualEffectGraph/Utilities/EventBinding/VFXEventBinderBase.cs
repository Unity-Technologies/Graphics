using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Experimental.VFX;

namespace UnityEngine.Experimental.VFX.Utility
{
    public abstract class VFXEventBinderBase : MonoBehaviour
    {
        [SerializeField]
        protected List<VisualEffect> m_targets = new List<VisualEffect>();
        public string EventName = "Event";

        [SerializeField, HideInInspector]
        protected List<VFXEventAttribute> m_EventAttributes = new List<VFXEventAttribute>();

        private void OnValidate()
        {
            m_EventAttributes.Clear();
            foreach (var target in m_targets)
                m_EventAttributes.Add(target.CreateVFXEventAttribute());
        }

        public void AddVisualEffect(VisualEffect target)
        {
            if(target != null && !m_targets.Contains(target))
            {
                m_targets.Add(target);
                m_EventAttributes.Add(target.CreateVFXEventAttribute());
            }
        }

        public void RemoveVisualEffect(VisualEffect target)
        {
            int index = m_targets.IndexOf(target);
            if(index >= 0)
            {
                m_targets.RemoveAt(index);
                m_EventAttributes.RemoveAt(index);
            }
        }

        public void ClearVisualEffects()
        {
            m_targets.Clear();
            m_EventAttributes.Clear();
        }

        protected abstract void SetEventAttribute(VisualEffect target, VFXEventAttribute attribute, Object[] parameters = null);

        protected void SendEventToVisualEffects(params Object[] parameters)
        {
            for (int i = 0; i < m_targets.Count; i++)
            {
                SetEventAttribute(m_targets[i], m_EventAttributes[i], parameters);
                m_targets[i].SendEvent(EventName, m_EventAttributes[i]);
            }
        }
    }
}


