using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Base class for Rendering Debugger Display Stats.
    /// </summary>
    /// <typeparam name="TProfileId">Type of ProfileId the pipeline uses</typeparam>
    public abstract class DebugDisplayStats<TProfileId> where TProfileId : Enum
    {
        // Accumulate values to avg over one second.
        private class AccumulatedTiming
        {
            public float accumulatedValue = 0;
            public float lastAverage = 0;

            internal void UpdateLastAverage(int frameCount)
            {
                lastAverage = accumulatedValue / frameCount;
                accumulatedValue = 0.0f;
            }
        }

        private enum DebugProfilingType
        {
            CPU,
            InlineCPU,
            GPU
        }

        /// <summary>
        /// Enable profiling recorders.
        /// </summary>
        public abstract void EnableProfilingRecorders();

        /// <summary>
        /// Disable all active profiling recorders.
        /// </summary>
        public abstract void DisableProfilingRecorders();

        /// <summary>
        /// Add display stats widgets to the list provided.
        /// </summary>
        /// <param name="list">List to add the widgets to.</param>
        public abstract void RegisterDebugUI(List<DebugUI.Widget> list);

        /// <summary>
        /// Update the timing data displayed in Display Stats panel.
        /// </summary>
        public abstract void Update();

        /// <summary>
        /// Helper function to get all TProfilerId values of a given type to show in Detailed Stats section.
        /// </summary>
        /// <returns>List of TProfileId values excluding ones marked with [HideInDebugUI]</returns>
        protected List<TProfileId> GetProfilerIdsToDisplay()
        {
            List<TProfileId> ids = new();
            var type = typeof(TProfileId);

            var enumValues = Enum.GetValues(type);
            foreach (var enumValue in enumValues)
            {
                var memberInfos = type.GetMember(enumValue.ToString());
                var enumValueMemberInfo = memberInfos.First(m => m.DeclaringType == type);
                var hiddenAttribute = Attribute.GetCustomAttribute(enumValueMemberInfo, typeof(HideInDebugUIAttribute));
                if (hiddenAttribute == null)
                    ids.Add((TProfileId)enumValue);
            }

            return ids;
        }

        /// <summary>
        /// Update the detailed stats
        /// </summary>
        /// <param name="samplers">List of samplers to update</param>
        protected void UpdateDetailedStats(List<TProfileId> samplers)
        {
            m_HiddenProfileIds.Clear();

            m_TimeSinceLastAvgValue += Time.unscaledDeltaTime;
            m_AccumulatedFrames++;
            bool needUpdatingAverages = m_TimeSinceLastAvgValue >= k_AccumulationTimeInSeconds;

            UpdateListOfAveragedProfilerTimings(needUpdatingAverages, samplers);

            if (needUpdatingAverages)
            {
                m_TimeSinceLastAvgValue = 0.0f;
                m_AccumulatedFrames = 0;
            }
        }

        private static readonly string[] k_DetailedStatsColumnLabels = {"CPU", "CPUInline", "GPU"};
        private Dictionary<TProfileId, AccumulatedTiming>[] m_AccumulatedTiming = { new(), new(), new() };
        private float m_TimeSinceLastAvgValue = 0.0f;
        private int m_AccumulatedFrames = 0;
        private HashSet<TProfileId> m_HiddenProfileIds = new();

        private const float k_AccumulationTimeInSeconds = 1.0f;

        /// <summary> Whether to display timings averaged over a second instead of updating every frame. </summary>
        protected bool averageProfilerTimingsOverASecond = false;

        /// <summary> Whether to hide empty scopes from UI. </summary>
        protected bool hideEmptyScopes = true;

        /// <summary>
        /// Helper function to build a list of sampler widgets for display stats
        /// </summary>
        /// <param name="title">Title for the stats list foldout</param>
        /// <param name="samplers">List of samplers to display</param>
        /// <returns>Foldout containing the list of sampler widgets</returns>
        protected DebugUI.Widget BuildDetailedStatsList(string title, List<TProfileId> samplers)
        {
            var foldout = new DebugUI.Foldout(title, BuildProfilingSamplerWidgetList(samplers), k_DetailedStatsColumnLabels);
            foldout.opened = true;
            return foldout;
        }

        private void UpdateListOfAveragedProfilerTimings(bool needUpdatingAverages, List<TProfileId> samplers)
        {
            foreach (var samplerId in samplers)
            {
                var sampler = ProfilingSampler.Get(samplerId);

                // Accumulate.
                bool allValuesZero = true;
                if (m_AccumulatedTiming[(int) DebugProfilingType.CPU].TryGetValue(samplerId, out var accCPUTiming))
                {
                    accCPUTiming.accumulatedValue += sampler.cpuElapsedTime;
                    allValuesZero &= accCPUTiming.accumulatedValue == 0;
                }

                if (m_AccumulatedTiming[(int)DebugProfilingType.InlineCPU].TryGetValue(samplerId, out var accInlineCPUTiming))
                {
                    accInlineCPUTiming.accumulatedValue += sampler.inlineCpuElapsedTime;
                    allValuesZero &= accInlineCPUTiming.accumulatedValue == 0;
                }

                if (m_AccumulatedTiming[(int)DebugProfilingType.GPU].TryGetValue(samplerId, out var accGPUTiming))
                {
                    accGPUTiming.accumulatedValue += sampler.gpuElapsedTime;
                    allValuesZero &= accGPUTiming.accumulatedValue == 0;
                }

                if (needUpdatingAverages)
                {
                    accCPUTiming?.UpdateLastAverage(m_AccumulatedFrames);
                    accInlineCPUTiming?.UpdateLastAverage(m_AccumulatedFrames);
                    accGPUTiming?.UpdateLastAverage(m_AccumulatedFrames);
                }

                // Update visibility status based on whether each accumulated value of this scope is zero
                if (allValuesZero)
                    m_HiddenProfileIds.Add(samplerId);
            }
        }

        private float GetSamplerTiming(TProfileId samplerId, ProfilingSampler sampler, DebugProfilingType type)
        {
            if (averageProfilerTimingsOverASecond)
            {
                // Find the right accumulated dictionary
                if (m_AccumulatedTiming[(int)type].TryGetValue(samplerId, out AccumulatedTiming accTiming))
                    return accTiming.lastAverage;
            }

            return (type == DebugProfilingType.CPU)
                ? sampler.cpuElapsedTime
                : ((type == DebugProfilingType.GPU) ? sampler.gpuElapsedTime : sampler.inlineCpuElapsedTime);
        }

        private ObservableList<DebugUI.Widget> BuildProfilingSamplerWidgetList(IEnumerable<TProfileId> samplers)
        {
            var result = new ObservableList<DebugUI.Widget>();

            DebugUI.Value CreateWidgetForSampler(TProfileId samplerId, ProfilingSampler sampler,
                DebugProfilingType type)
            {
                // Find the right accumulated dictionary and add it there if not existing yet.
                var accumulatedDictionary = m_AccumulatedTiming[(int)type];
                if (!accumulatedDictionary.ContainsKey(samplerId))
                {
                    accumulatedDictionary.Add(samplerId, new AccumulatedTiming());
                }

                return new()
                {
                    formatString = "{0:F2}ms",
                    refreshRate = 1.0f / 5.0f,
                    getter = () => GetSamplerTiming(samplerId, sampler, type)
                };
            }

            foreach (var samplerId in samplers)
            {
                var sampler = ProfilingSampler.Get(samplerId);

                // In non-dev build ProfilingSampler.Get always returns null.
                if (sampler == null)
                    continue;

                sampler.enableRecording = true;

                result.Add(new DebugUI.ValueTuple
                {
                    displayName = sampler.name,
                    isHiddenCallback = () =>
                    {
                        if (hideEmptyScopes && m_HiddenProfileIds.Contains(samplerId))
                            return true;
                        return false;
                    },
                    values = Enum.GetValues(typeof(DebugProfilingType)).Cast<DebugProfilingType>()
                        .Select(e => CreateWidgetForSampler(samplerId, sampler, e)).ToArray()
                });
            }

            return result;
        }
    }
}
