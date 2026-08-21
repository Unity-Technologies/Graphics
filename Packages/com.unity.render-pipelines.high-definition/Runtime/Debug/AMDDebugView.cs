using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
#if ENABLE_AMD && ENABLE_AMD_MODULE
using static UnityEngine.AMD.GraphicsDevice;
#endif

#if ENABLE_AMD && ENABLE_AMD_MODULE
namespace UnityEngine.AMD
{
    internal class DebugView
    {
        #region Main Internal Methods

        internal void Reset() { InternalReset(); }

        internal void Update() { InternalUpdate(); }

        internal DebugUI.Widget CreateWidget() { return InternalCreateWidget(); }

        #endregion

        #region Private implementation

        private enum DeviceState
        {
            Unknown,
            MissingPluginDLL,
            DeviceCreationFailed,
            Active
        }

        private GraphicsDeviceDebugView m_DebugView = null;

        private class Container<T> where T : struct
        {
            public T data = new T();
        }

        private class Data
        {
            public DeviceState deviceState = DeviceState.Unknown;
            public bool fsr4UpscaleSupported = false;
            public bool fsr3UpscaleSupported = false;
            public bool fsr2UpscaleSupported = false;
            public Container<FSRUpscaleFeatureDebugInfo>[] fsrUpscaleFeatureInfos = null;
        }

        private Data m_Data = new Data();

        private void InternalReset()
        {
            GraphicsDevice device = AMD.GraphicsDevice.device;
            if (device != null && m_DebugView != null)
            {
                device.DeleteDebugView(m_DebugView);
            }

            m_DebugView = null;
        }

        private void InternalUpdate()
        {
            GraphicsDevice device = AMD.GraphicsDevice.device;
            bool panelIsOpen = DebugManager.instance.displayRuntimeUI || DebugManager.instance.displayEditorUI;
            if (device != null)
            {
                if (panelIsOpen && m_DebugView == null)
                {
                    m_DebugView = device.CreateDebugView();
                }
                else if (!panelIsOpen && m_DebugView != null)
                {
                    device.DeleteDebugView(m_DebugView);
                    m_DebugView = null;
                }
            }

            if (device != null)
            {
                if (m_DebugView != null)
                {
                    m_Data.deviceState = DeviceState.Active;
                    m_Data.fsr4UpscaleSupported = device.IsFeatureAvailable(UnityEngine.AMD.GraphicsDeviceFeature.FSRUpscale4);
                    m_Data.fsr3UpscaleSupported = device.IsFeatureAvailable(UnityEngine.AMD.GraphicsDeviceFeature.FSRUpscale3);
                    m_Data.fsr2UpscaleSupported = device.IsFeatureAvailable(UnityEngine.AMD.GraphicsDeviceFeature.FSRUpscale2);
                    device.UpdateDebugView(m_DebugView);
                    TranslateFidelityFXFeatureArray(m_Data.fsrUpscaleFeatureInfos, m_DebugView);
                }
                else
                {
                    m_Data.deviceState = DeviceState.Unknown;
                }
            }
            else if (device == null)
            {
                bool isPluginLoaded = AMDUnityPlugin.IsLoaded();
                m_Data.deviceState = isPluginLoaded ? DeviceState.DeviceCreationFailed : DeviceState.MissingPluginDLL;
                m_Data.fsr4UpscaleSupported = false;
                m_Data.fsr3UpscaleSupported = false;
                m_Data.fsr2UpscaleSupported = false;
                ClearFeatureStateContainer(m_Data.fsrUpscaleFeatureInfos);
            }

            UpdateDebugUITable();
        }

        private static void ClearFeatureStateContainer(Container<FSRUpscaleFeatureDebugInfo>[] containerArray)
        {
            for (int i = 0; i < containerArray.Length; ++i)
            {
                containerArray[i].data = new FSRUpscaleFeatureDebugInfo();
            }
        }

        private static void TranslateFidelityFXFeatureArray(Container<FSRUpscaleFeatureDebugInfo>[] containerArray, in GraphicsDeviceDebugView debugView)
        {
            // Clear all entries first
            for (int i = 0; i < containerArray.Length; ++i)
            {
                containerArray[i].data = new FSRUpscaleFeatureDebugInfo();
            }

            if (debugView.upscaleFeatureInfoCount == 0)
                return;

            // Copy data over (zero-allocation)
            int maxIndex = Math.Min(containerArray.Length, (int)debugView.upscaleFeatureInfoCount);
            var srcSpan = debugView.upscaleFeatureInfoSpan;
            for (int i = 0; i < maxIndex; ++i)
            {
                containerArray[i].data = srcSpan[i];
            }
        }

        #endregion

        #region Debug User Interface

        private const int MaxDebugRows = 4;
        private DebugUI.Container m_DebugWidget = null;
        private DebugUI.Table.Row[] m_fsrUpscaleViewStateTableRows = null;
        private DebugUI.Container m_fsrUpscaleViewStateTableHeader = null;
        private DebugUI.Table m_FsrUpscaleViewStateTable = null;

        private static string ConvertQualityModeToString(uint qualityMode)
        {
            switch (qualityMode)
            {
                case 1: return "Native AA";
                case 2: return "Quality";
                case 3: return "Balanced";
                case 4: return "Performance";
                case 5: return "Ultra Performance";
                default: return "Unknown";
            }
        }

        private static string GetResolutionModeString(FSRUpscaleFeatureDebugInfo info)
        {
            // DRS mode - quality mode is irrelevant, just show "DRS"
            bool enableDRS = (info.initData.ffxFsrFlags & FfxFsr2InitializationFlags.EnableDynamicResolution) > 0;
            if (enableDRS)
                return "DRS";

            // Fixed resolution mode - show inferred quality or custom percentage
            uint qualityMode = info.qualityMode;
            if (qualityMode != 0)
            {
                // FSR2 doesn't support Native AA - show as Custom 100% instead
                if (info.majorVersion == 2 && qualityMode == 1)
                {
                    return "Custom 100%";
                }
                return ConvertQualityModeToString(qualityMode);
            }

            // Custom resolution - calculate percentage
            float ratio = (float)info.initData.displaySizeWidth / (float)info.renderWidth;
            int percent = (int)(100.0f / ratio);
            return $"Custom {percent}%";
        }
        private DebugUI.Widget InternalCreateWidget()
        {
            if (m_DebugWidget != null)
                return m_DebugWidget;

            m_fsrUpscaleViewStateTableHeader = new DebugUI.Table.Row()
            {
                displayName = "",
                isHiddenCallback = () => true,  // Hide this row - it's only used for header generation
                children =
                {
                    new DebugUI.Container() {
                        displayName = "Status",
                    },
                    new DebugUI.Container() {
                        displayName = "Feature Version",
                    },
                    new DebugUI.Container() {
                        displayName = "Input resolution",
                    },
                    new DebugUI.Container() {
                        displayName = "Output resolution",
                    },
                    new DebugUI.Container() {
                        displayName = "Resolution Mode",
                    }
                }
            };

            m_FsrUpscaleViewStateTable = new DebugUI.Table()
            {
                displayName = "",  // Empty to avoid header duplication
                isReadOnly = true
            };

            m_FsrUpscaleViewStateTable.children.Add(m_fsrUpscaleViewStateTableHeader);

            m_DebugWidget = new DebugUI.Foldout()
            {
                displayName = "AMD Device settings",
                children =
                {
                    new DebugUI.Value()
                    {
                        displayName = "AMDUnityPlugin Version",
                        getter = () => m_DebugView == null ? "-" : m_DebugView.deviceVersion.ToString("X2"),
                    },
                    new DebugUI.Value()
                    {
                        displayName = "FidelityFX SDK Version",
                        getter = () => m_DebugView == null ? "-" : $"{m_DebugView.fidelityFxSdkVersionMajor}.{m_DebugView.fidelityFxSdkVersionMinor}.{m_DebugView.fidelityFxSdkVersionPatch}",
                    },
                    new DebugUI.Value()
                    {
                        displayName = "Device Status",
                        getter = () => m_Data.deviceState.ToString(),
                    },
                    new DebugUI.Value()
                    {
                        displayName = "FSR 2 Upscale",
                        getter = () => {
                            if (!m_Data.fsr2UpscaleSupported) return "Not Available";
                            bool valid = GraphicsDevice.GetFeatureVersion(GraphicsDeviceFeature.FSRUpscale2, out uint major, out uint minor, out uint patch);
                            return !valid ? "Not Available" : $"{major}.{minor}.{patch}";
                        },
                    },
                    new DebugUI.Value()
                    {
                        displayName = "FSR 3 Upscale",
                        getter = () => {
                            if (!m_Data.fsr3UpscaleSupported) return "Not Available";
                            bool valid = GraphicsDevice.GetFeatureVersion(GraphicsDeviceFeature.FSRUpscale3, out uint major, out uint minor, out uint patch);
                            return !valid ? "Not Available" : $"{major}.{minor}.{patch}";
                        },
                    },
                    new DebugUI.Value()
                    {
                        displayName = "FSR 4 Upscale",
                        getter = () => {
                            if (!m_Data.fsr4UpscaleSupported) return "Not Available";
                            bool valid = GraphicsDevice.GetFeatureVersion(GraphicsDeviceFeature.FSRUpscale4, out uint major, out uint minor, out uint patch);
                            return !valid ? "Not Available" : $"{major}.{minor}.{patch}";
                        },
                    },
                    m_FsrUpscaleViewStateTable
                }
            };

            m_Data.fsrUpscaleFeatureInfos = new Container<FSRUpscaleFeatureDebugInfo>[MaxDebugRows];
            m_fsrUpscaleViewStateTableRows = new DebugUI.Table.Row[m_Data.fsrUpscaleFeatureInfos.Length];

            String resToString(uint a, uint b)
            {
                return "" + a + "x" + b;
            }

            for (int r = 0; r < m_Data.fsrUpscaleFeatureInfos.Length; ++r)
            {
                var c = new Container<FSRUpscaleFeatureDebugInfo>()
                {
                    data = new FSRUpscaleFeatureDebugInfo()
                };
                m_Data.fsrUpscaleFeatureInfos[r] = c;

                var fsrUpscaleStateRow = new DebugUI.Table.Row()
                {
                    children =
                    {
                        new DebugUI.Value()
                        {
                            getter = () => c.data.validFeature ? "Valid" : "-"
                        },
                        new DebugUI.Value()
                        {
                            getter = () => c.data.validFeature ? $"{c.data.majorVersion}.{c.data.minorVersion}.{c.data.patchVersion}" : "-"
                        },
                        new DebugUI.Value()
                        {
                            getter = () => c.data.validFeature ? resToString(c.data.renderWidth, c.data.renderHeight) : "-"
                        },
                        new DebugUI.Value()
                        {
                            getter = () => c.data.validFeature ? resToString(c.data.initData.displaySizeWidth, c.data.initData.displaySizeHeight) : "-"
                        },
                        new DebugUI.Value()
                        {
                            getter = () => c.data.validFeature ? GetResolutionModeString(c.data) : "-"
                        }
                    }
                };
                fsrUpscaleStateRow.isHiddenCallback = () => !c.data.validFeature;
                m_fsrUpscaleViewStateTableRows[r] = fsrUpscaleStateRow;
            }
            m_FsrUpscaleViewStateTable.children.Add(m_fsrUpscaleViewStateTableRows);
            m_FsrUpscaleViewStateTable.isHiddenCallback = () =>
            {
                foreach (var row in m_fsrUpscaleViewStateTableRows)
                {
                    if (!row.isHidden)
                        return false;
                }

                return true;
            };

            return m_DebugWidget;
        }

        private void UpdateDebugUITable()
        {
            for (int row = 0; row < m_fsrUpscaleViewStateTableRows.Length; ++row)
            {
                var data = m_Data.fsrUpscaleFeatureInfos[row].data;
                m_fsrUpscaleViewStateTableRows[row].displayName = data.validFeature ? data.featureSlot.ToString() : "";
            }
        }

        #endregion
    }
}
#endif
