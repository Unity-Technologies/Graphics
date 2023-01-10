using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Display stats panel
    /// </summary>
    /// <typeparam name="TProfileId">Type of ProfileId the pipeline uses</typeparam>
    public class DebugDisplaySettingsStats<TProfileId> : IDebugDisplaySettingsData
        where TProfileId : Enum
    {
        /// <summary>Current display stats</summary>
        public DebugDisplayStats<TProfileId> debugDisplayStats { get; }

        /// <summary>
        /// Display stats panel constructor with settings
        /// </summary>
        /// <param name="debugDisplayStats"></param>
        public DebugDisplaySettingsStats(DebugDisplayStats<TProfileId> debugDisplayStats)
        {
            this.debugDisplayStats = debugDisplayStats;
        }

        [DisplayInfo(name = "Display Stats", order = int.MinValue)]
        private class StatsPanel : DebugDisplaySettingsPanel
        {
            readonly DebugDisplaySettingsStats<TProfileId> m_Data;

            public override DebugUI.Flags Flags => DebugUI.Flags.RuntimeOnly;

            public StatsPanel(DebugDisplaySettingsStats<TProfileId> displaySettingsStats)
            {
                m_Data = displaySettingsStats;

                m_Data.debugDisplayStats.EnableProfilingRecorders();

                var list = new List<DebugUI.Widget>();
                m_Data.debugDisplayStats.RegisterDebugUI(list);

                foreach (var w in list)
                    AddWidget(w);
            }

            public override void Dispose()
            {
                m_Data.debugDisplayStats.DisableProfilingRecorders();
                base.Dispose();
            }
        }

        /// <inheritdoc/>
        public bool AreAnySettingsActive => false;

        /// <inheritdoc/>
        public IDebugDisplaySettingsPanelDisposable CreatePanel()
        {
            return new StatsPanel(this);
        }
    }
}
