using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Rendering.Universal
{
    class DebugDisplayStats : IDebugDisplaySettingsData
    {
        DebugFrameTiming m_DebugFrameTiming = new DebugFrameTiming();

        private class StatsPanel : DebugDisplaySettingsPanel
        {
            public override string PanelName => "Display Stats";
            public override DebugUI.Flags Flags => DebugUI.Flags.RuntimeOnly;

            public StatsPanel(DebugFrameTiming frameTiming)
            {
                var list = new List<DebugUI.Widget>();
                frameTiming.RegisterDebugUI(list);

                foreach (var w in list)
                    AddWidget(w);
            }
        }

        public void UpdateFrameTiming()
        {
            m_DebugFrameTiming.UpdateFrameTiming();
        }

        #region IDebugDisplaySettingsData

        /// <inheritdoc/>
        public bool AreAnySettingsActive => false;

        /// <inheritdoc/>
        public bool IsPostProcessingAllowed => true;

        /// <inheritdoc/>
        public bool IsLightingActive => true;

        /// <inheritdoc/>
        public bool TryGetScreenClearColor(ref Color _) => false;

        /// <inheritdoc/>
        public IDebugDisplaySettingsPanelDisposable CreatePanel()
        {
            return new StatsPanel(m_DebugFrameTiming);
        }

        #endregion
    }
}
