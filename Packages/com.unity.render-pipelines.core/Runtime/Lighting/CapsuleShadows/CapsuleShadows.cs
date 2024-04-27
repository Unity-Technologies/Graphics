
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Rendering
{
    [ExecuteAlways]
    public class CapsuleShadows : MonoBehaviour
    {
        public bool m_KeepAxisAligned = true;
        public float m_MinimumLength = 0.15f;
        public float m_RadiusScale = 0.8f;
        public CapsuleOccluderLightLayer m_LightLayerMask = CapsuleOccluderLightLayer.LightLayerDefault;

        [HideInInspector] public const int MINIMUM_VERTICES_FOR_PCA = 16;

        public SkinnedMeshRenderer[] MeshRenderers { get; private set; }
        public List<CapsuleOccluder> CapsuleOccluders { get; private set; }

        public bool IsValid { get; private set; }

        private void OnValidate()
        {
            RefreshOccluders();
            MeshRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            IsValid = ( MeshRenderers?.Length != 0);

            m_MinimumLength = Mathf.Max(m_MinimumLength, 0.0f);
            m_RadiusScale = Mathf.Max(m_RadiusScale, 0.0f);
        }

        private void OnDisable()
        {
            if (CapsuleOccluders == null || CapsuleOccluders.Count == 0)
            {
                return;
            }

            foreach (var occluder in CapsuleOccluders)
            {
                if (occluder != null)
                {
                    occluder.enabled = false;
                }
            }
        }

        private void OnEnable()
        {
            RefreshOccluders();
            if (CapsuleOccluders == null || CapsuleOccluders.Count == 0)
            {
                return;
            }

            foreach (var occluder in CapsuleOccluders)
            {
                if (occluder != null)
                {
                    occluder.enabled = true;
                }
            }
        }

        public void RefreshOccluders()
        {
            CapsuleOccluders = new List<CapsuleOccluder>();
            CapsuleOccluders = gameObject.GetComponentsInChildren<CapsuleOccluder>().ToList();
            MeshRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            IsValid = ( MeshRenderers?.Length != 0);
        }
    }
}
