using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEngine.Experimental.VFX.Utility
{
    [ExecuteInEditMode, RequireComponent(typeof(VFXParameterBinder))]
    public abstract class VFXBinderBase : MonoBehaviour
    {
        protected VFXParameterBinder binder;

        public abstract bool IsValid(VisualEffect component);

        public virtual void Reset()
        {
            //Optional method allowing potential manual state reset of binder
        }

        protected virtual void Awake()
        {
            binder = GetComponent<VFXParameterBinder>();
        }

        protected virtual void OnEnable()
        {
            if (!binder.m_Bindings.Contains(this))
                binder.m_Bindings.Add(this);

            hideFlags = HideFlags.HideInInspector; // Comment to debug
        }

        protected virtual void OnDisable()
        {
            if (binder.m_Bindings.Contains(this))
                binder.m_Bindings.Remove(this);
        }

        public abstract void UpdateBinding(VisualEffect component);

        public override string ToString()
        {
            return GetType().ToString();
        }
    }
}
