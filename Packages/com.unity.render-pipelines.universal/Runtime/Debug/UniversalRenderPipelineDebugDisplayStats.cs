using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// URP Rendering Debugger Display Stats.
    /// </summary>
    class UniversalRenderPipelineDebugDisplayStats : DebugDisplayStats<URPProfileId>
    {
        private DebugFrameTiming m_DebugFrameTiming = new();

        private List<URPProfileId> m_RecordedSamplers = new();

        /// <inheritdoc/>
        public override void EnableProfilingRecorders()
        {
            Debug.Assert(m_RecordedSamplers.Count == 0);
            m_RecordedSamplers = GetProfilerIdsToDisplay();
        }

        /// <inheritdoc/>
        public override void DisableProfilingRecorders()
        {
            foreach (var sampler in m_RecordedSamplers)
                ProfilingSampler.Get(sampler).enableRecording = false;

            m_RecordedSamplers.Clear();
        }

        /// <inheritdoc/>
        public override void RegisterDebugUI(List<DebugUI.Widget> list)
        {
#if UNITY_ANDROID || UNITY_IPHONE || UNITY_TVOS
            list.Add(new DebugUI.MessageBox
            {
                displayName = "Warning: GPU timings may not be accurate on mobile devices that have tile-based architectures.",
                style = DebugUI.MessageBox.Style.Warning
            });
#endif

            m_DebugFrameTiming.RegisterDebugUI(list);

            var detailedStatsFoldout = new DebugUI.Foldout
            {
                displayName = "Detailed Stats",
                opened = false,
                children =
                {
                    new DebugUI.BoolField
                    {
                        displayName = "Update every second with average",
                        getter = () => averageProfilerTimingsOverASecond,
                        setter = value => averageProfilerTimingsOverASecond = value
                    },
                    new DebugUI.BoolField
                    {
                        displayName = "Hide empty scopes",
                        tooltip = "Hide profiling scopes where elapsed time in each category is zero",
                        getter = () => hideEmptyScopes,
                        setter = value => hideEmptyScopes = value
                    }
                }
            };
            detailedStatsFoldout.children.Add(BuildDetailedStatsList("Profiling Scopes", m_RecordedSamplers));
            list.Add(detailedStatsFoldout);
        }

        /// <inheritdoc/>
        public override void Update()
        {
            m_DebugFrameTiming.UpdateFrameTiming();
            UpdateDetailedStats(m_RecordedSamplers);
        }
    }
}
