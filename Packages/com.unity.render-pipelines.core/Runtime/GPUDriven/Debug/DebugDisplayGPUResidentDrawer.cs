using System;
using System.Collections.Generic;
using Unity.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using static UnityEngine.Rendering.DebugUI;
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
        const int k_MaxOcclusionPassCount = 32;
        const int k_MaxContextCount = 16;

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

        /// <summary>Returns the view instances id for the selected occluder debug view index, or 0 if not valid.</summary>
        internal bool GetOccluderViewInstanceID(out int viewInstanceID)
        {
            DebugRendererBatcherStats debugStats = GPUResidentDrawer.GetDebugStats();
            if (debugStats != null)
            {
                if (occluderDebugViewIndex >= 0 && occluderDebugViewIndex < debugStats.occluderStats.Length)
                {
                    viewInstanceID = debugStats.occluderStats[occluderDebugViewIndex].viewInstanceID;
                    return true;
                }
            }

            viewInstanceID = 0;
            return false;
        }

        /// <summary>Returns if the occlusion test heatmap debug overlay is enabled.</summary>
        internal bool occlusionTestOverlayEnable
        {
            get { return GPUResidentDrawer.GetDebugStats()?.occlusionOverlayEnabled ?? false; }
            set
            {
                DebugRendererBatcherStats debugStats = GPUResidentDrawer.GetDebugStats();
                if (debugStats != null)
                    debugStats.occlusionOverlayEnabled = value;
            }
        }

        private bool occlusionTestOverlayCountVisible
        {
            get { return GPUResidentDrawer.GetDebugStats()?.occlusionOverlayCountVisible ?? false; }
            set
            {
                DebugRendererBatcherStats debugStats = GPUResidentDrawer.GetDebugStats();
                if (debugStats != null)
                    debugStats.occlusionOverlayCountVisible = value;
            }
        }

        private bool overrideOcclusionTestToAlwaysPass
        {
            get { return GPUResidentDrawer.GetDebugStats()?.overrideOcclusionTestToAlwaysPass ?? false; }
            set
            {
                DebugRendererBatcherStats debugStats = GPUResidentDrawer.GetDebugStats();
                if (debugStats != null)
                    debugStats.overrideOcclusionTestToAlwaysPass = value;
            }
        }

        /// <summary>Returns true if the occluder debug overlay is enabled.</summary>
        public bool occluderDebugViewEnable = false;

        internal bool occluderContextStats = false;
        internal Vector2 occluderDebugViewRange = new Vector2(0.0f, 1.0f);
        internal int occluderDebugViewIndex = 0;

        private static InstanceCullerViewStats GetInstanceCullerViewStats(int viewIndex)
        {
            DebugRendererBatcherStats debugStats = GPUResidentDrawer.GetDebugStats();
            if (debugStats != null && viewIndex < debugStats.instanceCullerStats.Length)
                return debugStats.instanceCullerStats[viewIndex];
            else
                return new InstanceCullerViewStats();
        }

        private static InstanceOcclusionEventStats GetInstanceOcclusionEventStats(int passIndex)
        {
            DebugRendererBatcherStats debugStats = GPUResidentDrawer.GetDebugStats();
            if (debugStats != null && passIndex < debugStats.instanceOcclusionEventStats.Length)
                return debugStats.instanceOcclusionEventStats[passIndex];
            else
                return new InstanceOcclusionEventStats();
        }

        static class Strings
        {
            public const string drawerSettingsContainerName = "GPU Resident Drawer Settings";
            public static readonly NameAndTooltip displayBatcherStats = new() { name = "Display Culling Stats", tooltip = "Enable the checkbox to display stats for instance culling." };
            public const string occlusionCullingTitle = "Occlusion Culling";
            public static readonly NameAndTooltip occlusionTestOverlayEnable = new() { name = "Occlusion Test Overlay", tooltip = "Occlusion test visualisation." };
            public static readonly NameAndTooltip occlusionTestOverlayCountVisible = new() { name = "Occlusion Test Overlay Count Visible", tooltip = "Occlusion test visualisation should count visible instances instead of occluded instances." };
            public static readonly NameAndTooltip overrideOcclusionTestToAlwaysPass = new() { name = "Override Occlusion Test To Always Pass", tooltip = "Occlusion test always passes." };
            public static readonly NameAndTooltip occluderContextStats = new() { name = "Occluder Context Stats", tooltip = "Show all the active occluder context textures." };
            public static readonly NameAndTooltip occluderDebugViewEnable = new() { name = "Occluder Debug View", tooltip = "Debug view of occluder texture." };
            public static readonly NameAndTooltip occluderDebugViewIndex = new() { name = "Occluder Debug View Index", tooltip = "Index of the view for which the occluder texture is displayed. Use the Occlusion Test Context Stats for a list of the views." };
            public static readonly NameAndTooltip occluderDebugViewRangeMin = new() { name = "Occluder Debug View Range Min", tooltip = "Range in which the occluder debug texture are displayed." };
            public static readonly NameAndTooltip occluderDebugViewRangeMax = new() { name = "Occluder Debug View Range Max", tooltip = "Range in which the occluder debug texture are displayed." };
        }

        private static DebugOccluderStats GetOccluderStats(int occluderIndex)
        {
            DebugRendererBatcherStats debugStats = GPUResidentDrawer.GetDebugStats();
            if (debugStats != null && occluderIndex < debugStats.occluderStats.Length)
                return debugStats.occluderStats[occluderIndex];
            else
                return new DebugOccluderStats();
        }

        private static int GetOcclusionContextsCounts()
        {
            return GPUResidentDrawer.GetDebugStats()?.occluderStats.Length ?? 0;
        }

        private static int GetInstanceCullerViewCount()
        {
            return GPUResidentDrawer.GetDebugStats()?.instanceCullerStats.Length ?? 0;
        }

        private static int GetInstanceOcclusionEventCount()
        {
            return GPUResidentDrawer.GetDebugStats()?.instanceOcclusionEventStats.Length ?? 0;
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
                    new DebugUI.Value { displayName = "View Type",          refreshRate = k_RefreshRate, formatString = k_FormatString, getter = () => GetInstanceCullerViewStats(viewIndex).viewType },
                    new DebugUI.Value { displayName = "View Instance ID",   refreshRate = k_RefreshRate, formatString = k_FormatString, getter = () =>
                        {
                            var viewStats = GetInstanceCullerViewStats(viewIndex);
#if UNITY_EDITOR
                            Object view = EditorUtility.InstanceIDToObject(viewStats.viewInstanceID);
                            if (view)
                            {
                                return $"{viewStats.viewInstanceID} ({view.name})";
                            }
#endif
                            return viewStats.viewInstanceID;
                        }
                    },
                    new DebugUI.Value { displayName = "Split Index",        refreshRate = k_RefreshRate, formatString = k_FormatString, getter = () => GetInstanceCullerViewStats(viewIndex).splitIndex },
                    new DebugUI.Value { displayName = "Visible Instances CPU | GPU", tooltip = "Visible instances after CPU culling and after GPU culling.", refreshRate = k_RefreshRate, formatString = k_FormatString, getter = () =>
                        {
                            var viewStats = GetInstanceCullerViewStats(viewIndex);
                            return $"{viewStats.visibleInstancesOnCPU} | {viewStats.visibleInstancesOnGPU}";
                        }
                    },
                    new DebugUI.Value { displayName = "Visible Primitives CPU | GPU", tooltip = "Visible primitives after CPU culling and after GPU culling.", refreshRate = k_RefreshRate, formatString = k_FormatString, getter = () =>
                        {
                            var viewStats = GetInstanceCullerViewStats(viewIndex);
                            return $"{viewStats.visiblePrimitivesOnCPU} | {viewStats.visiblePrimitivesOnGPU}";
                        }
                    },
                    new DebugUI.Value { displayName = "Draw Commands",      refreshRate = k_RefreshRate, formatString = k_FormatString, getter = () => GetInstanceCullerViewStats(viewIndex).drawCommands },
                }
            };
        }

        private static object OccluderVersionString(in InstanceOcclusionEventStats stats)
        {
            return (stats.eventType == InstanceOcclusionEventType.OccluderUpdate || stats.occlusionTest != OcclusionTest.None) ? stats.occluderVersion : "-";
        }

        private static object OcclusionTestString(in InstanceOcclusionEventStats stats)
        {
            return (stats.eventType == InstanceOcclusionEventType.OcclusionTest) ? stats.occlusionTest : "-";
        }

        private static object VisibleInstancesString(in InstanceOcclusionEventStats stats)
        {
            return (stats.eventType == InstanceOcclusionEventType.OcclusionTest) ? stats.visibleInstances : "-";
        }

        private static object CulledInstancesString(in InstanceOcclusionEventStats stats)
        {
            return (stats.eventType == InstanceOcclusionEventType.OcclusionTest) ? stats.culledInstances : "-";
        }

        private static object VisiblePrimitivesString(in InstanceOcclusionEventStats stats)
        {
            return (stats.eventType == InstanceOcclusionEventType.OcclusionTest) ? stats.visiblePrimitives : "-";
        }

        private static object CulledPrimitivesString(in InstanceOcclusionEventStats stats)
        {
            return (stats.eventType == InstanceOcclusionEventType.OcclusionTest) ? stats.culledPrimitives : "-";
        }

        private static DebugUI.Table.Row AddInstanceOcclusionPassDataRow(int eventIndex)
        {
            return new DebugUI.Table.Row
            {
                displayName = "",
                opened = true,
                isHiddenCallback = () => { return eventIndex >= GetInstanceOcclusionEventCount(); },
                children =
                {
                    new DebugUI.Value { displayName = "View Instance ID",   refreshRate = k_RefreshRate, formatString = k_FormatString, getter = () =>
                        {
                            var eventStats = GetInstanceOcclusionEventStats(eventIndex);
#if UNITY_EDITOR
                            Object view = EditorUtility.InstanceIDToObject(eventStats.viewInstanceID);
                            if (view)
                            {
                                return $"{eventStats.viewInstanceID} ({view.name})";
                            }
#endif
                            return eventStats.viewInstanceID;
                        }
                    },
                    new DebugUI.Value { displayName = "Event Type",         refreshRate = k_RefreshRate, formatString = k_FormatString, getter = () => $"{GetInstanceOcclusionEventStats(eventIndex).eventType}" },
                    new DebugUI.Value { displayName = "Occluder Version",   refreshRate = k_RefreshRate, formatString = k_FormatString, getter = () => OccluderVersionString(GetInstanceOcclusionEventStats(eventIndex)) },
                    new DebugUI.Value { displayName = "Subview Mask",       refreshRate = k_RefreshRate, formatString = k_FormatString, getter = () => $"0x{GetInstanceOcclusionEventStats(eventIndex).subviewMask:X}" },
                    new DebugUI.Value { displayName = "Occlusion Test",     refreshRate = k_RefreshRate, formatString = k_FormatString, getter = () => $"{OcclusionTestString(GetInstanceOcclusionEventStats(eventIndex))}" },
                    new DebugUI.Value { displayName = "Visible Instances",  refreshRate = k_RefreshRate, formatString = k_FormatString, getter = () => VisibleInstancesString(GetInstanceOcclusionEventStats(eventIndex)) },
                    new DebugUI.Value { displayName = "Culled Instances",   refreshRate = k_RefreshRate, formatString = k_FormatString, getter = () => CulledInstancesString(GetInstanceOcclusionEventStats(eventIndex)) },
                    new DebugUI.Value { displayName = "Visible Primitives",  refreshRate = k_RefreshRate, formatString = k_FormatString, getter = () => VisiblePrimitivesString(GetInstanceOcclusionEventStats(eventIndex)) },
                    new DebugUI.Value { displayName = "Culled Primitives",   refreshRate = k_RefreshRate, formatString = k_FormatString, getter = () => CulledPrimitivesString(GetInstanceOcclusionEventStats(eventIndex)) },
                }
            };
        }

        private static DebugUI.Table.Row AddOcclusionContextDataRow(int index)
        {
            return new DebugUI.Table.Row
            {
                displayName = "",
                opened = true,
                isHiddenCallback = () => index >= GetOcclusionContextsCounts(),
                children =
                {
                    new DebugUI.Value { displayName = "View Instance ID",   refreshRate = k_RefreshRate, formatString = k_FormatString, getter = () => GetOccluderStats(index).viewInstanceID },
                    new DebugUI.Value { displayName = "Subview Count",      refreshRate = k_RefreshRate, formatString = k_FormatString, getter = () => GetOccluderStats(index).subviewCount },
                    new DebugUI.Value { displayName = "Size Per Subview",       refreshRate = k_RefreshRate, formatString = k_FormatString, getter =
                    () =>
                    {
                        Vector2Int size = GetOccluderStats(index).occluderMipLayoutSize;
                        return $"{size.x}x{size.y}";
                    }},
                }
            };
        }


        [DisplayInfo(name = "GPU Resident Drawer", order = 5)]
        [CurrentPipelineHelpURL("gpu-resident-drawer")]
        private class SettingsPanel : DebugDisplaySettingsPanel
        {
            public override string PanelName => "GPU Resident Drawer";

            public override DebugUI.Flags Flags => DebugUI.Flags.EditorForceUpdate;

            public SettingsPanel(DebugDisplayGPUResidentDrawer data)
            {
                var helpBox = new DebugUI.MessageBox()
                {
                    displayName = "Not Supported",
                    style = MessageBox.Style.Warning,
                    messageCallback = () =>
                    {
                        var settings = GPUResidentDrawer.GetGlobalSettingsFromRPAsset();
                        return GPUResidentDrawer.IsGPUResidentDrawerSupportedBySRP(settings, out var msg, out var _) ? string.Empty : msg;
                    },
                    isHiddenCallback = () => GPUResidentDrawer.IsEnabled()
                };

                AddWidget(helpBox);

                AddWidget(new Container()
                {
                    displayName = Strings.occlusionCullingTitle,
                    isHiddenCallback = () => !GPUResidentDrawer.IsEnabled(),
                    children =
                    {
                        new DebugUI.BoolField { nameAndTooltip = Strings.occlusionTestOverlayEnable, getter = () => data.occlusionTestOverlayEnable, setter = value => data.occlusionTestOverlayEnable = value},
                        new DebugUI.BoolField { nameAndTooltip = Strings.occlusionTestOverlayCountVisible, getter = () => data.occlusionTestOverlayCountVisible, setter = value => data.occlusionTestOverlayCountVisible = value},
                        new DebugUI.BoolField { nameAndTooltip = Strings.overrideOcclusionTestToAlwaysPass, getter = () => data.overrideOcclusionTestToAlwaysPass, setter = value => data.overrideOcclusionTestToAlwaysPass = value},
                        new DebugUI.BoolField { nameAndTooltip = Strings.occluderContextStats, getter = () => data.occluderContextStats, setter = value => data.occluderContextStats = value},
                        new DebugUI.BoolField { nameAndTooltip = Strings.occluderDebugViewEnable, getter = () => data.occluderDebugViewEnable, setter = value => data.occluderDebugViewEnable = value},
                        new DebugUI.IntField { nameAndTooltip = Strings.occluderDebugViewIndex, getter = () => data.occluderDebugViewIndex, setter = value => data.occluderDebugViewIndex = value, isHiddenCallback = () => !data.occluderDebugViewEnable, min = () => 0, max = () => Math.Max(GetOcclusionContextsCounts() - 1, 0) },
                        new DebugUI.FloatField {nameAndTooltip = Strings.occluderDebugViewRangeMin, getter = () => data.occluderDebugViewRange.x, setter = value => data.occluderDebugViewRange.x = value, isHiddenCallback = () => !data.occluderDebugViewEnable},
                        new DebugUI.FloatField {nameAndTooltip = Strings.occluderDebugViewRangeMax, getter = () => data.occluderDebugViewRange.y, setter = value => data.occluderDebugViewRange.y = value, isHiddenCallback = () => !data.occluderDebugViewEnable}
                    }
                });
                AddOcclusionContextStatsWidget(data);

                AddWidget(new DebugUI.Container()
                {
                    displayName = Strings.drawerSettingsContainerName,
                    isHiddenCallback = () => !GPUResidentDrawer.IsEnabled(),
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

                instanceCullerStats.children.Add(new DebugUI.ValueTuple()
                {
                    displayName = "Total Visible Instances (Cameras | Lights | Both)",
                    values = new[]
                    {
                        new DebugUI.Value { refreshRate = k_RefreshRate, formatString = k_FormatString, getter = () =>
                            {
                                int totalGRDInstances = 0;

                                for (int viewIndex = 0; viewIndex < GetInstanceCullerViewCount(); viewIndex++)
                                {
                                    var viewStats = GetInstanceCullerViewStats(viewIndex);
                                    if (viewStats.viewType == BatchCullingViewType.Camera)
                                        totalGRDInstances += viewStats.visibleInstancesOnGPU;
                                }
                                return totalGRDInstances;
                            }
                        },
                        new DebugUI.Value { refreshRate = k_RefreshRate, formatString = k_FormatString, getter = () =>
                            {
                                int totalGRDInstances = 0;

                                for (int viewIndex = 0; viewIndex < GetInstanceCullerViewCount(); viewIndex++)
                                {
                                    var viewStats = GetInstanceCullerViewStats(viewIndex);
                                    if (viewStats.viewType == BatchCullingViewType.Light)
                                        totalGRDInstances += viewStats.visibleInstancesOnGPU;
                                }
                                return totalGRDInstances;
                            }
                        },
                        new DebugUI.Value { refreshRate = k_RefreshRate, formatString = k_FormatString, getter = () =>
                        {
                            int totalGRDInstances = 0;

                            for (int viewIndex = 0; viewIndex < GetInstanceCullerViewCount(); viewIndex++)
                            {
                                var viewStats = GetInstanceCullerViewStats(viewIndex);
                                if (viewStats.viewType != BatchCullingViewType.Filtering
                                    && viewStats.viewType != BatchCullingViewType.Picking
                                    && viewStats.viewType != BatchCullingViewType.SelectionOutline)
                                    totalGRDInstances += viewStats.visibleInstancesOnGPU;
                            }
                            return totalGRDInstances;
                        }
                        },
                    }
                });

                instanceCullerStats.children.Add(new DebugUI.ValueTuple()
                {
                    displayName = "Total Visible Primitives (Cameras | Lights | Both)",
                    values = new[]
                    {
                        new DebugUI.Value { refreshRate = k_RefreshRate, formatString = k_FormatString, getter = () =>
                            {
                                int totalGRDPrimitives = 0;

                                for (int viewIndex = 0; viewIndex < GetInstanceCullerViewCount(); viewIndex++)
                                {
                                    var viewStats = GetInstanceCullerViewStats(viewIndex);
                                    if (viewStats.viewType == BatchCullingViewType.Camera)
                                        totalGRDPrimitives += viewStats.visiblePrimitivesOnGPU;
                                }
                                return totalGRDPrimitives;
                            }
                        },
                        new DebugUI.Value { refreshRate = k_RefreshRate, formatString = k_FormatString, getter = () =>
                            {
                                int totalGRDPrimitives = 0;

                                for (int viewIndex = 0; viewIndex < GetInstanceCullerViewCount(); viewIndex++)
                                {
                                    var viewStats = GetInstanceCullerViewStats(viewIndex);
                                    if (viewStats.viewType == BatchCullingViewType.Light)
                                        totalGRDPrimitives += viewStats.visiblePrimitivesOnGPU;
                                }
                                return totalGRDPrimitives;
                            }
                        },
                        new DebugUI.Value { refreshRate = k_RefreshRate, formatString = k_FormatString, getter = () =>
                            {
                                int totalGRDPrimitives = 0;

                                for (int viewIndex = 0; viewIndex < GetInstanceCullerViewCount(); viewIndex++)
                                {
                                    var viewStats = GetInstanceCullerViewStats(viewIndex);
                                    if (viewStats.viewType != BatchCullingViewType.Filtering
                                        && viewStats.viewType != BatchCullingViewType.Picking
                                        && viewStats.viewType != BatchCullingViewType.SelectionOutline)
                                        totalGRDPrimitives += viewStats.visiblePrimitivesOnGPU;
                                }
                                return totalGRDPrimitives;
                            }
                        },
                    }
                });

                DebugUI.Table viewTable = new DebugUI.Table
                {
                    displayName = "",
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


                DebugUI.Table eventTable = new DebugUI.Table
                {
                    displayName = "",   // First column is empty because its content needs to change dynamically
                    isReadOnly = true
                };

                // Always add all possible rows, they are dynamically hidden based on actual data
                for (int i = 0; i < k_MaxOcclusionPassCount; i++)
                    eventTable.children.Add(AddInstanceOcclusionPassDataRow(i));

                var perEventStats = new DebugUI.Foldout
                {
                    displayName = "Occlusion Culling Events",
                    isHeader = true,
                    opened = false,
                    isHiddenCallback = () => !data.displayBatcherStats
                };
                perEventStats.children.Add(eventTable);

                instanceCullerStats.children.Add(perEventStats);


                AddWidget(instanceCullerStats);
            }
            private void AddOcclusionContextStatsWidget(DebugDisplayGPUResidentDrawer data)
            {
                var visibilityStats = new DebugUI.Foldout
                {
                    displayName = "Occlusion Context Stats",
                    isHeader = true,
                    opened = true,
                    isHiddenCallback = () => !data.occluderContextStats
                };

                visibilityStats.children.Add(new DebugUI.ValueTuple()
                {
                    displayName = "Active Occlusion Contexts",
                    values = new[]
                    {
                        new DebugUI.Value { refreshRate = k_RefreshRate, formatString = k_FormatString, getter = () => GetOcclusionContextsCounts() }
                    }
                });

                DebugUI.Table viewTable = new DebugUI.Table
                {
                    displayName = "",   // First column is empty because its content needs to change dynamically
                    isReadOnly = true
                };

                // Always add all possible rows, they are dynamically hidden based on actual data
                for (int i = 0; i < k_MaxContextCount; i++)
                    viewTable.children.Add(AddOcclusionContextDataRow(i));

                visibilityStats.children.Add(viewTable);

                AddWidget(visibilityStats);
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
