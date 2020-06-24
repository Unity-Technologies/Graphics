using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEngine.VFX.Utility
{
    /// <summary>
    /// Base Class to derive in order to Write Visual Effect Binders
    /// </summary>
    [ExecuteInEditMode, RequireComponent(typeof(VFXPropertyBinder))]
    public abstract class VFXBinderBase : MonoBehaviour
    {
        /// <summary>
        /// VFXPropertyBinder master behaviour this binder is attached to.
        /// </summary>
        protected VFXPropertyBinder binder;

        /// <summary>
        /// Implement this method to perform validity checks:
        /// - Visual Effect Implements correct Properties
        /// - Objects to get values from are correctly set
        /// </summary>
        /// <param name="component">the Visual Effect Componnent to bind properties to</param>
        /// <returns>Whether the binding can be performed</returns>
        public abstract bool IsValid(VisualEffect component);

        /// <summary>
        /// Optional method allowing potential manual state reset of binder
        /// </summary>
        public virtual void Reset()
        {
        }

        /// <summary>
        /// Awake Message : gets the master VFX Property Binder from this game object.
        /// </summary>
        protected virtual void Awake()
        {
            binder = GetComponent<VFXPropertyBinder>();
        }

        /// <summary>
        /// OnEnable Message : Adds this binding in the VFXPropertyBinder update loop
        /// </summary>
        protected virtual void OnEnable()
        {
            if (!binder.m_Bindings.Contains(this))
                binder.m_Bindings.Add(this);

            hideFlags = HideFlags.HideInInspector; // Comment to debug
        }

        /// <summary>
        /// OnEnable Message : Removes this binding from the VFXPropertyBinder update loop
        /// </summary>
        protected virtual void OnDisable()
        {
            if (binder.m_Bindings.Contains(this))
                binder.m_Bindings.Remove(this);
        }

        /// <summary>
        /// Implement UpdateBinding to perform the actual binding. This method is called by the VFXPropertyBinder if IsValid() returns true;
        /// </summary>
        /// <param name="component">The VisualEffect component to bind to</param>
        public abstract void UpdateBinding(VisualEffect component);

        /// <summary>
        /// Returns a readable string summary of this Binder.
        /// </summary>
        /// <returns>a readable string summary of this Binder</returns>
        public override string ToString()
        {
            return GetType().ToString();
        }
    }
}
