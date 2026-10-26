using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Base class for Rendering Debugger Display Stats.
    /// Subclasses provide pipeline-specific lists of <see cref="ProfilingSampler"/> instances
    /// and register the corresponding debug UI widgets.
    /// </summary>
    public abstract class DebugDisplayStats
    {
        List<ProfilingSampler> m_CoreProfilingSamplers = GetProfilingSamplersToDisplay(typeof(CoreProfilingSamplers));

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
        public virtual void EnableProfilingRecorders()
        {
            AddAndEnableProfilingSamplers(m_CoreProfilingSamplers);
        }

        /// <summary>
        /// Add Profiling Samplers to recorded list and enable them.
        /// </summary>
        /// <param name="samplers">List of profiling samplers.</param>
        protected void AddAndEnableProfilingSamplers(List<ProfilingSampler> samplers)
        {
            foreach (var sampler in samplers)
            {
                if (sampler == null) continue;

                m_RecordedSamplers.Add(sampler);
                sampler.enableRecording = true;
            }
        }

        /// <summary>
        /// Disable all active profiling recorders.
        /// </summary>
        public virtual void DisableProfilingRecorders()
        {
            foreach (var sampler in m_RecordedSamplers)
                sampler.enableRecording = false;

            m_RecordedSamplers.Clear();
        }

        /// <summary>
        /// Add display stats widgets to the list provided.
        /// </summary>
        /// <param name="list">List to add the widgets to.</param>
        public virtual void RegisterDebugUI(List<DebugUI.Widget> list)
        {
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
            detailedStatsFoldout.children.Add(BuildDetailedStatsList("Profiling Scopes", m_CoreProfilingSamplers));
            list.Add(detailedStatsFoldout);
        }

        /// <summary>
        /// Update the timing data displayed in Display Stats panel.
        /// </summary>
        public virtual void Update()
        {
            m_DebugFrameTiming.UpdateFrameTiming();
            UpdateDetailedStats(m_RecordedSamplers);
        }

        /// <summary>
        /// Collects all <c>public static readonly ProfilingSampler</c> fields from
        /// <paramref name="markersType"/>, skipping any decorated with
        /// <see cref="HideInDebugUIAttribute"/>.
        /// </summary>
        /// <param name="markersType">A static class containing ProfilingSampler fields
        /// (e.g. <c>typeof(CoreProfilingSamplers)</c>).</param>
        /// <returns>List of ProfilingSampler instances to display.</returns>
        protected static List<ProfilingSampler> GetProfilingSamplersToDisplay(Type markersType)
        {
            var result = new List<ProfilingSampler>();
            foreach (var field in markersType.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.FieldType != typeof(ProfilingSampler))
                    continue;
                if (field.GetCustomAttribute<HideInDebugUIAttribute>() != null)
                    continue;
                if (field.GetValue(null) is ProfilingSampler sampler)
                    result.Add(sampler);
            }
            return result;
        }

        /// <summary>
        /// Update the detailed stats.
        /// </summary>
        /// <param name="samplers">List of samplers to update.</param>
        protected void UpdateDetailedStats(List<ProfilingSampler> samplers)
        {
            m_HiddenSamplers.Clear();

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
        private Dictionary<ProfilingSampler, AccumulatedTiming>[] m_AccumulatedTiming = { new(), new(), new() };
        private float m_TimeSinceLastAvgValue = 0.0f;
        private int m_AccumulatedFrames = 0;
        private HashSet<ProfilingSampler> m_HiddenSamplers = new();

        private const float k_AccumulationTimeInSeconds = 1.0f;

        /// <summary> Whether to display timings averaged over a second instead of updating every frame. </summary>
        protected bool averageProfilerTimingsOverASecond = false;

        /// <summary> Whether to hide empty scopes from UI. </summary>
        protected bool hideEmptyScopes = true;

        /// <summary>
        /// Frame Times measurements used for Display Stats.
        /// </summary>
        readonly DebugFrameTiming m_DebugFrameTiming = new();

        /// <summary>
        /// List of markers for Detailed stats.
        /// </summary>
        readonly List<ProfilingSampler> m_RecordedSamplers = new();

        /// <summary>
        /// Helper function to build a list of sampler widgets for display stats.
        /// </summary>
        /// <param name="title">Title for the stats list foldout.</param>
        /// <param name="samplers">List of samplers to display.</param>
        /// <returns>Foldout containing the list of sampler widgets.</returns>
        protected DebugUI.Widget BuildDetailedStatsList(string title, List<ProfilingSampler> samplers)
        {
            var foldout = new DebugUI.Foldout(title, BuildProfilingSamplerWidgetList(samplers), k_DetailedStatsColumnLabels);
            foldout.opened = true;
            foldout.alternateRowColors = true;
            return foldout;
        }

        private void UpdateListOfAveragedProfilerTimings(bool needUpdatingAverages, List<ProfilingSampler> samplers)
        {
            foreach (var sampler in samplers)
            {
                // Accumulate.
                bool allValuesZero = true;
                if (m_AccumulatedTiming[(int)DebugProfilingType.CPU].TryGetValue(sampler, out var accCPUTiming))
                {
                    accCPUTiming.accumulatedValue += sampler.cpuElapsedTime;
                    allValuesZero &= accCPUTiming.accumulatedValue == 0;
                }

                if (m_AccumulatedTiming[(int)DebugProfilingType.InlineCPU].TryGetValue(sampler, out var accInlineCPUTiming))
                {
                    accInlineCPUTiming.accumulatedValue += sampler.inlineCpuElapsedTime;
                    allValuesZero &= accInlineCPUTiming.accumulatedValue == 0;
                }

                if (m_AccumulatedTiming[(int)DebugProfilingType.GPU].TryGetValue(sampler, out var accGPUTiming))
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
                    m_HiddenSamplers.Add(sampler);
            }
        }

        private float GetSamplerTiming(ProfilingSampler sampler, DebugProfilingType type)
        {
            if (averageProfilerTimingsOverASecond)
            {
                // Find the right accumulated dictionary
                if (m_AccumulatedTiming[(int)type].TryGetValue(sampler, out AccumulatedTiming accTiming))
                    return accTiming.lastAverage;
            }

            return (type == DebugProfilingType.CPU)
                ? sampler.cpuElapsedTime
                : ((type == DebugProfilingType.GPU) ? sampler.gpuElapsedTime : sampler.inlineCpuElapsedTime);
        }

        /// <summary>
        /// Create an observable list of widgets from Profiling Samplers
        /// </summary>
        /// <param name="samplers">Profiling Sampler collection.</param>
        /// <returns>An observable list of widgets.</returns>
        protected ObservableList<DebugUI.Widget> BuildProfilingSamplerWidgetList(IEnumerable<ProfilingSampler> samplers)
        {
            var result = new ObservableList<DebugUI.Widget>();

            DebugUI.Value CreateWidgetForSampler(ProfilingSampler sampler, DebugProfilingType type)
            {
                // Find the right accumulated dictionary and add it there if not existing yet.
                var accumulatedDictionary = m_AccumulatedTiming[(int)type];
                if (!accumulatedDictionary.ContainsKey(sampler))
                {
                    accumulatedDictionary.Add(sampler, new AccumulatedTiming());
                }

                return new()
                {
                    formatString = "{0:F2}ms",
                    refreshRate = 1.0f / 5.0f,
                    getter = () => GetSamplerTiming(sampler, type)
                };
            }

            foreach (var sampler in samplers)
            {
                if (sampler == null) continue;

                result.Add(new DebugUI.ValueTuple
                {
                    displayName = sampler.name,
                    isHiddenCallback = () => hideEmptyScopes && m_HiddenSamplers.Contains(sampler),
                    values = Enum.GetValues(typeof(DebugProfilingType)).Cast<DebugProfilingType>()
                        .Select(e => CreateWidgetForSampler(sampler, e)).ToArray()
                });
            }

            return result;
        }
    }

    /// <summary>
    /// Base class for Rendering Debugger Display Stats.
    /// </summary>
    /// <typeparam name="TProfileId">Type of ProfileId the pipeline uses</typeparam>
    [Obsolete("Use the non-generic DebugDisplayStats base class with ProfilingSampler lists. #from(6000.6)")]
    public abstract class DebugDisplayStats<TProfileId> : DebugDisplayStats where TProfileId : Enum
    {
        /// <summary>
        /// Helper function to get all TProfilerId values of a given type to show in Detailed Stats section.
        /// </summary>
        /// <returns>List of TProfileId values excluding ones marked with [HideInDebugUI]</returns>
        [Obsolete("Use GetProfilingSamplersToDisplay(Type) with a static marker class. #from(6000.6)")]
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
        /// Update the detailed stats.
        /// </summary>
        /// <param name="samplers">List of enum profile IDs to update.</param>
        [Obsolete("Use UpdateDetailedStats(List<ProfilingSampler>) instead. #from(6000.6)")]
        protected void UpdateDetailedStats(List<TProfileId> samplers)
        {
            UpdateDetailedStats(ConvertToSamplers(samplers));
        }

        /// <summary>
        /// Helper function to build a list of sampler widgets for display stats.
        /// </summary>
        /// <param name="title">Title for the stats list foldout.</param>
        /// <param name="samplers">List of enum profile IDs to display.</param>
        /// <returns>Foldout containing the list of sampler widgets.</returns>
        [Obsolete("Use BuildDetailedStatsList(string, List<ProfilingSampler>) instead. #from(6000.6)")]
        protected DebugUI.Widget BuildDetailedStatsList(string title, List<TProfileId> samplers)
        {
            return BuildDetailedStatsList(title, ConvertToSamplers(samplers));
        }

        static List<ProfilingSampler> ConvertToSamplers(List<TProfileId> ids)
        {
            var samplers = new List<ProfilingSampler>(ids.Count);
            foreach (var id in ids)
            {
                var sampler = ProfilingSampler.Get(id);
                if (sampler != null)
                    samplers.Add(sampler);
            }
            return samplers;
        }
    }
}
