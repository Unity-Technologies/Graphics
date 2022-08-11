using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace Unity.Testing.VisualEffectGraph
{
    [ExecuteInEditMode]
    public class DebugBatchInfo : MonoBehaviour
    {
        private List<VFXBatchedEffectInfo> batchedEffectInfos = new List<VFXBatchedEffectInfo>();

        public float logPeriod = 1.0f;
        private float lastLogTime = 0.0f;

        // Start is called before the first frame update
        void Start()
        {
            batchedEffectInfos = new List<VFXBatchedEffectInfo>();
            lastLogTime = 0.0f;
        }

        void Update()
        {
            if (Time.time < lastLogTime + logPeriod)
                return;

            lastLogTime = Time.time;

            VFXManager.GetBatchedEffectInfos(batchedEffectInfos);
            Debug.Log("BATCHED EFFECT INFO count:" + batchedEffectInfos.Count);
            foreach (var info in batchedEffectInfos)
                Debug.Log(string.Format("vfx:{0} activeCount:{1} emptyCount:{2} instanceCount:{3} unbatchedInstance:{4} instanceCapacity:{5} capacityPerBatch:{6} gpuSize:{7} cpuSize:{8}"
                    , info.vfxAsset
                    , info.activeBatchCount
                    , info.inactiveBatchCount
                    , info.activeInstanceCount
                    , info.unbatchedInstanceCount
                    , info.totalInstanceCapacity
                    , info.maxInstancePerBatchCapacity
                    , info.totalGPUSizeInBytes
                    , info.totalCPUSizeInBytes
                ));
        }
    }
}
