using System.Collections;
using System.Collections.Generic;
using UnityEngine.VFX;

namespace UnityEngine.VFX.Utility
{
    abstract class VFXEventBinderBase : MonoBehaviour
    {
        [SerializeField]
        protected VisualEffect target;
        public string EventName = "Event";

        [SerializeField, HideInInspector]
        protected VFXEventAttribute eventAttribute;

        protected virtual void OnEnable()
        {
            UpdateCacheEventAttribute();
        }

        private void OnValidate()
        {
            UpdateCacheEventAttribute();
        }

        private void UpdateCacheEventAttribute()
        {
            if (target != null)
                eventAttribute = target.CreateVFXEventAttribute();
            else
                eventAttribute = null;
        }

        protected abstract void SetEventAttribute(object[] parameters = null);

        protected void SendEventToVisualEffect(params object[] parameters)
        {
            if (target != null)
            {
                SetEventAttribute(parameters);
                target.SendEvent(EventName, eventAttribute);
            }
        }
    }
}
