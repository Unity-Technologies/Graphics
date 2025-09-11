using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering
{
    [ExecuteInEditMode]
    internal class DisallowGPUDrivenRendering : MonoBehaviour
    {
        private bool m_AppliedRecursively;

        [FormerlySerializedAs("applyToChildrenRecursively")]
        public bool m_applyToChildrenRecursively;

        public bool applyToChildrenRecursively
        {
            get => m_applyToChildrenRecursively;
            set
            {
                m_applyToChildrenRecursively = value;
                OnDisable();
                OnEnable();
            }
        }

        private void OnEnable()
        {
            m_AppliedRecursively = applyToChildrenRecursively;

            if (applyToChildrenRecursively)
                AllowGPUDrivenRenderingRecursively(transform, false);
            else
                AllowGPUDrivenRendering(transform, false);
        }

        private void OnDisable()
        {
            if (m_AppliedRecursively)
                AllowGPUDrivenRenderingRecursively(transform, true);
            else
                AllowGPUDrivenRendering(transform, true);
        }

        private static void AllowGPUDrivenRendering(Transform transform, bool allow)
        {
            var renderer = transform.GetComponent<MeshRenderer>();

            if (renderer)
                renderer.allowGPUDrivenRendering = allow;
        }

        private static void AllowGPUDrivenRenderingRecursively(Transform transform, bool allow)
        {
            AllowGPUDrivenRendering(transform, allow);

            foreach (Transform child in transform)
            {
                if (!child.GetComponent<DisallowGPUDrivenRendering>())
                    AllowGPUDrivenRenderingRecursively(child, allow);
            }
        }

        private void OnValidate()
        {
            OnDisable();
            if (enabled)
                OnEnable();
        }
    }
}
