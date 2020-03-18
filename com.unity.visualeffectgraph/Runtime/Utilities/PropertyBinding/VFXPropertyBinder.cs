using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEngine.VFX.Utility
{
    [RequireComponent(typeof(VisualEffect))]
    [DefaultExecutionOrder(1)]
    [ExecuteInEditMode]
    public class VFXPropertyBinder : MonoBehaviour
    {
        [SerializeField]
        protected bool m_ExecuteInEditor = true;
        public List<VFXBinderBase> m_Bindings = new List<VFXBinderBase>();
        [SerializeField]
        protected VisualEffect m_VisualEffect;

        private void OnEnable()
        {
            m_VisualEffect = GetComponent<VisualEffect>();
        }

        void Update()
        {
            if (!m_ExecuteInEditor && Application.isEditor && !Application.isPlaying) return;

            for (int i = 0; i < m_Bindings.Count; i++ )
            {
                var binding = m_Bindings[i];

                if(binding == null)
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

        public T AddParameterBinder<T>() where T : VFXBinderBase
        {
            return gameObject.AddComponent<T>();
        }

        public void ClearParameterBinders()
        {
            var allBinders = GetComponents<VFXBinderBase>();
            foreach (var binder in allBinders) Destroy(binder);
        }

        public void RemoveParameterBinder(VFXBinderBase binder)
        {
            if (binder.gameObject == this.gameObject) Destroy(binder);
        }

        public void RemoveParameterBinders<T>() where T : VFXBinderBase
        {
            var allBinders = GetComponents<VFXBinderBase>();
            foreach (var binder in allBinders)
                if (binder is T) Destroy(binder);
        }

        public IEnumerable<T> GetParameterBinders<T>() where T : VFXBinderBase
        {
            foreach (var binding in m_Bindings)
            {
                if (binding is T) yield return binding as T;
            }
        }
    }
}
