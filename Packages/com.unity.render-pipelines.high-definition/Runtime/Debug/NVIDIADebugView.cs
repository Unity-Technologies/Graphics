using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

#if ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
namespace UnityEngine.NVIDIA
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

        private class Data
        {
            public DeviceState deviceState = DeviceState.Unknown;
            public bool dlssSupported = false;
            public DLSSDebugFeatureInfos[] dlssFeatureInfos = null;
        }
        private Data m_Data = new Data();

        private void InternalReset()
        {
            GraphicsDevice device = NVIDIA.GraphicsDevice.device;
            if (device != null && m_DebugView != null)
            {
                device.DeleteDebugView(m_DebugView);
            }

            m_DebugView = null;
        }

        private void InternalUpdate()
        {
            GraphicsDevice device = NVIDIA.GraphicsDevice.device;
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
                    m_Data.dlssSupported = device.IsFeatureAvailable(UnityEngine.NVIDIA.GraphicsDeviceFeature.DLSS);
                    device.UpdateDebugView(m_DebugView);
                    TranslateDlssFeatureArray(m_Data.dlssFeatureInfos, m_DebugView);
                }
                else
                {
                    m_Data.deviceState = DeviceState.Unknown;
                }
            }
            else if (device == null)
            {
                bool isPluginLoaded = NVUnityPlugin.IsLoaded();
                m_Data.deviceState = isPluginLoaded ?  DeviceState.DeviceCreationFailed : DeviceState.MissingPluginDLL;
                m_Data.dlssSupported = false;
            }

            UpdateDebugUITable();
        }

        private static void TranslateDlssFeatureArray(DLSSDebugFeatureInfos[] featureArray, in GraphicsDeviceDebugView debugView)
        {
            new Span<DLSSDebugFeatureInfos>(featureArray).Clear(); // Clear the local array first.
            debugView.dlssFeatureInfosSpan.CopyTo(featureArray); // Directly copy the data from the source span to the destination array.
        }

        #endregion

        #region Debug User Interface

        private const int MaxDebugRows = 4;
        private DebugUI.Container m_DebugWidget = null;
        private DebugUI.Table.Row[] m_DlssViewStateTableRows = null;
        private DebugUI.Container m_DlssViewStateTableHeader = null;
        private DebugUI.Table m_DlssViewStateTable = null;
        private DebugUI.Widget InternalCreateWidget()
        {
            if (m_DebugWidget != null)
                return m_DebugWidget;

            m_DlssViewStateTableHeader = new DebugUI.Table.Row()
            {
                displayName = "",
                children =
                {
                    new DebugUI.Container() {
                        displayName = "Status",
                    },
                    new DebugUI.Container() {
                        displayName = "Input resolution",
                    },
                    new DebugUI.Container() {
                        displayName = "Output resolution",
                    },
                    new DebugUI.Container() {
                        displayName = "Quality",
                    },
                    new DebugUI.Container() {
                        displayName = "Render Preset",
                    }
                }
            };

            m_DlssViewStateTable = new DebugUI.Table()
            {
                displayName = "Feature Slot ID",
                isReadOnly = true
            };

            m_DlssViewStateTable.children.Add(m_DlssViewStateTableHeader);

            m_DebugWidget = new DebugUI.Foldout() {
                displayName = "NVIDIA Device settings",
                children =
                {
                    new DebugUI.Value()
                    {
                        displayName = "NVUnityPlugin Version",
                        getter = () => m_DebugView == null ? "-" : m_DebugView.deviceVersion.ToString("X2"),
                    },
                    new DebugUI.Value()
                    {
                        displayName = "DLSS Version", // Must match NVUnityPlugin preprocessor definition NV_MAKE_BIT_VERSION
                        getter = () => m_DebugView == null ? "-" : String.Format("{0}.{1}.{2}", (m_DebugView.ngxVersion >> 18) & 0x3FF, (m_DebugView.ngxVersion >> 7) & 0x7F, m_DebugView.ngxVersion & 0x7F),
                    },
                    new DebugUI.Value()
                    {
                        displayName = "Device Status",
                        getter = () => m_Data.deviceState.ToString(),
                    },
                    new DebugUI.Value()
                    {
                        displayName = "DLSS Supported",
                        getter = () => m_Data.dlssSupported ? "True" : "False",
                    },
                    new DebugUI.Value {
                        displayName = "DLSS Injection Point",
                        getter = () => HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.dynamicResolutionSettings.DLSSInjectionPoint
                    },
                    m_DlssViewStateTable
                }
            };

            m_Data.dlssFeatureInfos = new DLSSDebugFeatureInfos[MaxDebugRows];
            m_DlssViewStateTableRows = new DebugUI.Table.Row[m_Data.dlssFeatureInfos.Length];

            String resToString(uint a, uint b)
            {
                return "" + a + "x" + b;
            }

            for (int r = 0; r < m_Data.dlssFeatureInfos.Length; ++r)
            {
                int currentIndex = r;
                string GetPresetLabel(DLSSQuality quality, int index)
                {
                    DLSSPreset presetValue = DLSSPreset.Preset_Default;
                    switch (quality)
                    {
                        case DLSSQuality.DLAA: presetValue = m_Data.dlssFeatureInfos[index].initData.presetDlaaMode; break;
                        case DLSSQuality.Balanced: presetValue = m_Data.dlssFeatureInfos[index].initData.presetBalancedMode; break;
                        case DLSSQuality.MaximumQuality: presetValue = m_Data.dlssFeatureInfos[index].initData.presetQualityMode; break;
                        case DLSSQuality.UltraPerformance: presetValue = m_Data.dlssFeatureInfos[index].initData.presetUltraPerformanceMode; break;
                        case DLSSQuality.MaximumPerformance: presetValue = m_Data.dlssFeatureInfos[index].initData.presetPerformanceMode; break;
                    }
                    string presetLabel = presetValue.ToString();
                    int delimiterIndex = presetLabel.IndexOf(" - "); // trim explanation from explanation separator token
                    if (delimiterIndex != -1)
                        presetLabel = presetLabel.Substring(0, delimiterIndex);
                    return presetLabel;
                }
                
                var dlssStateRow = new DebugUI.Table.Row()
                {
                    children =
                        {
                            new DebugUI.Value()
                            {
                                getter = () => m_Data.dlssFeatureInfos[currentIndex].validFeature ? "Valid" : "-"
                            },
                            new DebugUI.Value()
                            {
                                getter = () => m_Data.dlssFeatureInfos[currentIndex].validFeature ? resToString(m_Data.dlssFeatureInfos[currentIndex].execData.subrectWidth, m_Data.dlssFeatureInfos[currentIndex].execData.subrectHeight) : "-"
                            },
                            new DebugUI.Value()
                            {
                                getter = () => m_Data.dlssFeatureInfos[currentIndex].validFeature ? resToString(m_Data.dlssFeatureInfos[currentIndex].initData.outputRTWidth, m_Data.dlssFeatureInfos[currentIndex].initData.outputRTHeight) : "-"
                            },
                            new DebugUI.Value()
                            {
                                getter = () => m_Data.dlssFeatureInfos[currentIndex].validFeature ? m_Data.dlssFeatureInfos[currentIndex].initData.quality.ToString() : "-"
                            },
                            new DebugUI.Value()
                            {
                                getter = () => m_Data.dlssFeatureInfos[currentIndex].validFeature ? GetPresetLabel(m_Data.dlssFeatureInfos[currentIndex].initData.quality, currentIndex) : "-"
                            }
                        }
                };
                dlssStateRow.isHiddenCallback = () => !m_Data.dlssFeatureInfos[currentIndex].validFeature;
                m_DlssViewStateTableRows[currentIndex] = dlssStateRow;
            }
            m_DlssViewStateTable.children.Add(m_DlssViewStateTableRows);
            m_DlssViewStateTable.isHiddenCallback = () =>
            {
                foreach (var row in m_DlssViewStateTableRows)
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
            if (m_DlssViewStateTableRows == null)
                return;

            for (int r = 0; r < m_DlssViewStateTableRows.Length; ++r)
            {
                var d = m_Data.dlssFeatureInfos[r];
                m_DlssViewStateTableRows[r].displayName = "";
                if(d.validFeature)
                {
                    // we know we dont support more than 16 features at one time on the plugin side.
                    // here's an allocation-free way to setup display name string values.
                    switch (d.featureSlot)
                    {
                        case  0: m_DlssViewStateTableRows[r].displayName = "0"; break;
                        case  1: m_DlssViewStateTableRows[r].displayName = "1"; break;
                        case  2: m_DlssViewStateTableRows[r].displayName = "2"; break;
                        case  3: m_DlssViewStateTableRows[r].displayName = "3"; break;
                        case  4: m_DlssViewStateTableRows[r].displayName = "4"; break;
                        case  5: m_DlssViewStateTableRows[r].displayName = "5"; break;
                        case  6: m_DlssViewStateTableRows[r].displayName = "6"; break;
                        case  7: m_DlssViewStateTableRows[r].displayName = "7"; break;
                        case  8: m_DlssViewStateTableRows[r].displayName = "8"; break;
                        case  9: m_DlssViewStateTableRows[r].displayName = "9"; break;
                        case 10: m_DlssViewStateTableRows[r].displayName = "10"; break;
                        case 11: m_DlssViewStateTableRows[r].displayName = "11"; break;
                        case 12: m_DlssViewStateTableRows[r].displayName = "12"; break;
                        case 13: m_DlssViewStateTableRows[r].displayName = "13"; break;
                        case 14: m_DlssViewStateTableRows[r].displayName = "14"; break;
                        case 15: m_DlssViewStateTableRows[r].displayName = "15"; break;
                    }
                }
            }
        }

#endregion
    }
}
#endif
