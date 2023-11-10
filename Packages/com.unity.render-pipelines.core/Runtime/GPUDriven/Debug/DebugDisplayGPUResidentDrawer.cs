using static UnityEngine.Rendering.DebugUI.Widget;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// GPU Resident Drawer Rendering Debugger settings.
    /// </summary>
    public class DebugDisplayGPUResidentDrawer : IDebugDisplaySettingsData
    {
        const string k_FormatString = "{0}";
        const float k_RefreshRate = 1f / 5f;
        const int k_MaxViewCount = 32;

        private bool displayBatcherStats
        {
            get
            {
                return GPUResidentDrawer.GetDebugStats()?.enabled ?? false;
            }
            set
            {
                DebugRendererBatcherStats debugStats = GPUResidentDrawer.GetDebugStats();
                if (debugStats != null)
                    debugStats.enabled = value;
            }
        }

        private static InstanceCullerViewStats GetInstanceCullerViewStats(int viewIndex)
        {
            DebugRendererBatcherStats debugStats = GPUResidentDrawer.GetDebugStats();
            if (debugStats != null && viewIndex < debugStats.instanceCullerStats.Length)
                return debugStats.instanceCullerStats[viewIndex];
            else
                return new InstanceCullerViewStats();
        }

        static class Strings
        {
            public const string drawerSettingsContainerName = "GPU Resident Drawer Settings";
            public static readonly NameAndTooltip displayBatcherStats = new() { name = "Display Culling Stats", tooltip = "Enable the checkbox to display stats for instance culling." };
        }

        private static int GetInstanceCullerViewCount()
        {
            return GPUResidentDrawer.GetDebugStats()?.instanceCullerStats.Length ?? 0;
        }

        private static DebugUI.Table.Row AddInstanceCullerViewDataRow(int viewIndex)
        {
            return new DebugUI.Table.Row
            {
                displayName = "",
                opened = true,
                isHiddenCallback = () => { return viewIndex >= GetInstanceCullerViewCount(); },
                children =
                {
                    new DebugUI.Value { displayName = "View Type",                       refreshRate = k_RefreshRate, formatString = k_FormatString, getter = () => $"{GetInstanceCullerViewStats(viewIndex).splitID.viewType}" },
                    new DebugUI.Value { displayName = "Subview Index",                   refreshRate = k_RefreshRate, formatString = k_FormatString, getter = () => GetInstanceCullerViewStats(viewIndex).splitID.splitIndex },
                    new DebugUI.Value { displayName = "Visible Instances",               refreshRate = k_RefreshRate, formatString = k_FormatString, getter = () => GetInstanceCullerViewStats(viewIndex).visibleInstances },
                    new DebugUI.Value { displayName = "Draw Commands",                   refreshRate = k_RefreshRate, formatString = k_FormatString, getter = () => GetInstanceCullerViewStats(viewIndex).drawCommands },
                }
            };
        }

        [DisplayInfo(name = "GPU Resident Drawer", order = 5)]
        private class SettingsPanel : DebugDisplaySettingsPanel
        {
            public override string PanelName => "GPU Resident Drawer";

            public override DebugUI.Flags Flags => DebugUI.Flags.EditorForceUpdate;

            public SettingsPanel(DebugDisplayGPUResidentDrawer data)
            {
                AddWidget(new DebugUI.Container()
                {
                    displayName = Strings.drawerSettingsContainerName,
                    children =
                    {
                        new DebugUI.BoolField { nameAndTooltip = Strings.displayBatcherStats, getter = () => data.displayBatcherStats, setter = value => data.displayBatcherStats = value},
                    }
                });

                AddInstanceCullingStatsWidget(data);
            }

            private void AddInstanceCullingStatsWidget(DebugDisplayGPUResidentDrawer data)
            {
                var instanceCullerStats = new DebugUI.Foldout
                {
                    displayName = "Instance Culler Stats",
                    isHeader = true,
                    opened = true,
                    isHiddenCallback = () => !data.displayBatcherStats
                };

                instanceCullerStats.children.Add(new DebugUI.ValueTuple()
                {
                    displayName = "View Count",
                    values = new[]
                    {
                        new DebugUI.Value { refreshRate = k_RefreshRate, formatString = k_FormatString, getter = () => GetInstanceCullerViewCount() }
                    }
                });

                DebugUI.Table viewTable = new DebugUI.Table
                {
                    displayName = "",   // First column is empty because its content needs to change dynamically
                    isReadOnly = true
                };

                // Always add all possible rows, they are dynamically hidden based on actual data
                for (int i = 0; i < k_MaxViewCount; i++)
                    viewTable.children.Add(AddInstanceCullerViewDataRow(i));

                var perViewStats = new DebugUI.Foldout
                {
                    displayName = "Per View Stats",
                    isHeader = true,
                    opened = false,
                    isHiddenCallback = () => !data.displayBatcherStats
                };
                perViewStats.children.Add(viewTable);

                instanceCullerStats.children.Add(perViewStats);

                AddWidget(instanceCullerStats);
            }
        }

        #region IDebugDisplaySettingsQuery

        /// <inheritdoc/>
        public bool AreAnySettingsActive => displayBatcherStats;

        /// <inheritdoc/>
        public bool IsPostProcessingAllowed => true;

        /// <inheritdoc/>
        public bool IsLightingActive => true;

        /// <inheritdoc/>
        public bool TryGetScreenClearColor(ref Color color)
        {
            return false;
        }

        /// <inheritdoc/>
        IDebugDisplaySettingsPanelDisposable IDebugDisplaySettingsData.CreatePanel()
        {
            return new SettingsPanel(this);
        }

        #endregion
    }
}
