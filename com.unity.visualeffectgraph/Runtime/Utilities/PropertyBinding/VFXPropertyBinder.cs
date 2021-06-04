using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEngine.VFX.Utility
{
    /// <summary>
    /// A Behaviour that controls binding between Visual Effect Properties, and other scene values, through the use of VFXBinderBase
    /// </summary>
    [RequireComponent(typeof(VisualEffect))]
    [DefaultExecutionOrder(1)]
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class VFXPropertyBinder : MonoBehaviour
    {
        /// <summary>
        /// Whether the bindings should be executed in editor (as preview)
        /// </summary>
        [SerializeField]
        protected bool m_ExecuteInEditor = true;

        /// <summary>
        /// The list of all Bindings attached to the binder, these bindings are managed by the VFXPropertyBinder and should be managed using the AddPropertyBinder, ClearPropertyBinders, RemovePropertyBinder, RemovePropertyBinders, and GetPropertyBinders.
        /// </summary>
        public List<VFXBinderBase> m_Bindings = new List<VFXBinderBase>();

        /// <summary>
        /// The Visual Effect component attached to the VFXPropertyBinder
        /// </summary>
        [SerializeField]
        protected VisualEffect m_VisualEffect;

        private void OnEnable()
        {
            Reload();
        }

        private void OnValidate()
        {
            Reload();
        }
        static private void SafeDestroy(Object toDelete)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                Destroy(toDelete); //Undo.DestroyObjectImmediate is needed only for Reset, which can't be called in play mode
            else
                UnityEditor.Undo.DestroyObjectImmediate(toDelete);
#else
            Destroy(toDelete);
#endif
        }

#if UNITY_EDITOR
        List<VFXBinderBase> m_BinderToCopyPaste;
#endif

        private void Reload()
        {
            m_VisualEffect = GetComponent<VisualEffect>();
#if UNITY_EDITOR
            //Handle probable copy/paste component saving list of inappropriate entries.
            m_BinderToCopyPaste = new List<VFXBinderBase>();
            foreach (var bindings in m_Bindings)
            {
                if (bindings != null && bindings.gameObject != gameObject)
                    m_BinderToCopyPaste.Add(bindings);
            }
            if (m_BinderToCopyPaste.Count == 0)
                m_BinderToCopyPaste = null;
#endif
            m_Bindings = new List<VFXBinderBase>();
            m_Bindings.AddRange(gameObject.GetComponents<VFXBinderBase>());
        }

        private void Reset()
        {
            Reload();
            ClearPropertyBinders();
        }

#if UNITY_EDITOR
        void Update()
        {
            if (m_BinderToCopyPaste != null)
            {
                //We can't add a component during a OnInvalidate, restore & copy linked binders (from copy/past) here
                foreach (var copyPaste in m_BinderToCopyPaste)
                {
                    var type = copyPaste.GetType();
                    var newComponent = gameObject.AddComponent(type);
                    UnityEditor.EditorUtility.CopySerialized(copyPaste, newComponent);
                }

                m_BinderToCopyPaste = null;
                Reload();
            }
        }

#endif
        void LateUpdate()
        {
            if (!m_ExecuteInEditor && Application.isEditor && !Application.isPlaying)
                return;

            for (int i = 0; i < m_Bindings.Count; i++)
            {
                var binding = m_Bindings[i];

                if (binding == null)
                {
                    Debug.LogWarning(string.Format("Parameter binder at index {0} of GameObject {1} is null or missing", i, gameObject.name));
                    continue;
                }
                else
                {
                    if (binding.IsValid(m_VisualEffect))
                        binding.UpdateBinding(m_VisualEffect);
                }
            }
        }

        /// <summary>
        /// Adds a new PropertyBinder
        /// </summary>
        /// <typeparam name="T">the Type of Property Binder</typeparam>
        /// <returns>The PropertyBinder newly Created</returns>
        public T AddPropertyBinder<T>() where T : VFXBinderBase
        {
            return gameObject.AddComponent<T>();
        }

        /// <summary>
        /// Adds a new PropertyBinder
        /// </summary>
        /// <typeparam name="T">the Type of Property Binder</typeparam>
        /// <returns>The PropertyBinder newly Created</returns>
        [Obsolete("Use AddPropertyBinder<T>() instead")]
        public T AddParameterBinder<T>() where T : VFXBinderBase
        {
            return AddPropertyBinder<T>();
        }

        /// <summary>
        /// Clears all the Property Binders
        /// </summary>
        public void ClearPropertyBinders()
        {
            var allBinders = GetComponents<VFXBinderBase>();
            foreach (var binder in allBinders)
                SafeDestroy(binder);
        }

        /// <summary>
        /// Clears all the Property Binders
        /// </summary>
        [Obsolete("Please use ClearPropertyBinders() instead")]
        public void ClearParameterBinders()
        {
            ClearPropertyBinders();
        }

        /// <summary>
        /// Removes specified Property Binder
        /// </summary>
        /// <param name="binder">The VFXBinderBase to remove</param>
        public void RemovePropertyBinder(VFXBinderBase binder)
        {
            if (binder.gameObject == this.gameObject)
                SafeDestroy(binder);
        }

        /// <summary>
        /// Removes specified Property Binder
        /// </summary>
        /// <param name="binder">The VFXBinderBase to remove</param>
        [Obsolete("Please use RemovePropertyBinder() instead")]
        public void RemoveParameterBinder(VFXBinderBase binder)
        {
            RemovePropertyBinder(binder);
        }

        /// <summary>
        /// Remove all Property Binders of Given Type
        /// </summary>
        /// <typeparam name="T">Specified VFXBinderBase type</typeparam>
        public void RemovePropertyBinders<T>() where T : VFXBinderBase
        {
            var allBinders = GetComponents<VFXBinderBase>();
            foreach (var binder in allBinders)
                if (binder is T)
                    SafeDestroy(binder);
        }

        /// <summary>
        /// Remove all Property Binders of Given Type
        /// </summary>
        /// <typeparam name="T">Specified VFXBinderBase type</typeparam>
        [Obsolete("Please use RemovePropertyBinders<T>() instead")]
        public void RemoveParameterBinders<T>() where T : VFXBinderBase
        {
            RemovePropertyBinders<T>();
        }

        /// <summary>
        /// Gets all VFXBinderBase of Given Type, attached to this VFXPropertyBinder
        /// </summary>
        /// <typeparam name="T">Specific VFXBinderBase type</typeparam>
        /// <returns>An IEnumerable of all VFXBinderBase</returns>
        public IEnumerable<T> GetPropertyBinders<T>() where T : VFXBinderBase
        {
            foreach (var binding in m_Bindings)
            {
                if (binding is T) yield return binding as T;
            }
        }

        /// <summary>
        /// Gets all VFXBinderBase of Given Type, attached to this VFXPropertyBinder
        /// </summary>
        /// <typeparam name="T">Specific VFXBinderBase type</typeparam>
        /// <returns>An IEnumerable of all VFXBinderBase</returns>
        [Obsolete("Please use GetPropertyBinders<T>() instead")]
        public IEnumerable<T> GetParameterBinders<T>() where T : VFXBinderBase
        {
            return GetPropertyBinders<T>();
        }
    }
}
