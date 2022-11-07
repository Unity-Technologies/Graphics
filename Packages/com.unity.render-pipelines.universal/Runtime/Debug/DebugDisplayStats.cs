using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Rendering.Universal
{
    class DebugDisplayStats : IDebugDisplaySettingsData
    {
        DebugFrameTiming m_DebugFrameTiming = new DebugFrameTiming();

        [DisplayInfo(name = "Display Stats", order = int.MinValue)]
        private class StatsPanel : DebugDisplaySettingsPanel
        {
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

        public bool AreAnySettingsActive => false;
        public bool IsPostProcessingAllowed => true;
        public bool IsLightingActive => true;
        public bool TryGetScreenClearColor(ref Color _) => false;

        public IDebugDisplaySettingsPanelDisposable CreatePanel()
        {
            return new StatsPanel(m_DebugFrameTiming);
        }

        #endregion
    }
}
