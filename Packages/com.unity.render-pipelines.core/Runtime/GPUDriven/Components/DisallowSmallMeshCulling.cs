using System;

namespace UnityEngine.Rendering
{
    [ExecuteInEditMode]
    internal class DisallowSmallMeshCulling : MonoBehaviour
    {
        private bool m_AppliedRecursively;
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
                AllowSmallMeshCullingRecursively(transform, false);
            else
                AllowSmallMeshCulling(transform, false);
        }

        private void OnDisable()
        {
            if (m_AppliedRecursively)
                AllowSmallMeshCullingRecursively(transform, true);
            else
                AllowSmallMeshCulling(transform, true);
        }

        private static void AllowSmallMeshCulling(Transform transform, bool allow)
        {
            var renderer = transform.GetComponent<MeshRenderer>();

            if (renderer)
                renderer.smallMeshCulling = allow;
        }

        private static void AllowSmallMeshCullingRecursively(Transform transform, bool allow)
        {
            AllowSmallMeshCulling(transform, allow);

            foreach (Transform child in transform)
            {
                if (!child.GetComponent<DisallowGPUDrivenRendering>())
                    AllowSmallMeshCullingRecursively(child, allow);
            }
        }

        private void OnValidate()
        {
            OnDisable();
            OnEnable();
        }
    }
}
