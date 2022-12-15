using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.VFX;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.Testing.VisualEffectGraph
{
    public class VFXPrefabReferenceTest : MonoBehaviour
    {
        public GameObject PrefabReference;
        public VisualEffectAsset VfxReference;

        private static readonly float kWaitTime = 0.5f;
        private float m_Wait = kWaitTime;
        private uint m_Index;

        private void Update()
        {
            if (VfxReference == null)
                return;

            if (PrefabReference == null)
                return;

            m_Wait -= Time.deltaTime;
            if (m_Wait < 0.0f)
            {
                m_Wait = kWaitTime;

                if (m_Index < 6)
                {
                    PrefabReference.GetComponent<VisualEffect>().SetFloat("hue", (float)m_Index / 6.0f);
                    var newVFX = GameObject.Instantiate(PrefabReference);
                    newVFX.transform.eulerAngles = new Vector3(0, 0, 60 * m_Index);
                    m_Index++;
                }
            }

            var backupPrefab = PrefabReference;
            PrefabReference = null;

            VFXBatchedEffectInfo batchEffectInfos = UnityEngine.VFX.VFXManager.GetBatchedEffectInfo(VfxReference);
            if (batchEffectInfos.activeInstanceCount != m_Index)
                throw new InvalidOperationException("Unexpected activeInstanceCount: " + batchEffectInfos.activeInstanceCount);

            if (m_Index > 0)
            {
                if (batchEffectInfos.activeBatchCount != 1)
                    throw new InvalidOperationException("Unexpected batchEffectCount: " + batchEffectInfos.activeBatchCount);

                var batchInfo = UnityEngine.VFX.VFXManager.GetBatchInfo(VfxReference, 0);
                if (batchInfo.activeInstanceCount != m_Index)
                    throw new InvalidOperationException("Unexpected totalActiveInstanceCount: " + batchInfo.activeInstanceCount);

                if (batchInfo.capacity < 6)
                    throw new InvalidOperationException("Unexpected capacity: " + batchInfo.capacity);
            }

            PrefabReference = backupPrefab;
        }
    }
}
