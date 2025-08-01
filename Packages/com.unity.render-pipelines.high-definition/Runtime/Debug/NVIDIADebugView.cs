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

        private class Container<T> where T : struct
        {
            public T data = new T();
        }

        private class Data
        {
            public DeviceState deviceState = DeviceState.Unknown;
            public bool dlssSupported = false;
            public Container<DLSSDebugFeatureInfos>[] dlssFeatureInfos = null;
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
                ClearFeatureStateContainer(m_Data.dlssFeatureInfos);
            }

            UpdateDebugUITable();
        }

        private static void ClearFeatureStateContainer(Container<DLSSDebugFeatureInfos>[] containerArray)
        {
            for (int i = 0; i < containerArray.Length; ++i)
            {
                containerArray[i].data = new DLSSDebugFeatureInfos();
            }
        }

        private static void TranslateDlssFeatureArray(Container<DLSSDebugFeatureInfos>[] containerArray, in GraphicsDeviceDebugView debugView)
        {
            ClearFeatureStateContainer(containerArray);
            if (!debugView.dlssFeatureInfos.Any())
                return;

            //copy data over
            int i = 0;
            foreach (var featureInfo in debugView.dlssFeatureInfos)
            {
                if (i == containerArray.Length)
                    break;
                containerArray[i++].data = featureInfo;
            }
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
                displayName = "DLSS Slot ID",
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

            m_Data.dlssFeatureInfos = new Container<DLSSDebugFeatureInfos>[MaxDebugRows];
            m_DlssViewStateTableRows = new DebugUI.Table.Row[m_Data.dlssFeatureInfos.Length];

            String resToString(uint a, uint b)
            {
                return "" + a + "x" + b;
            }

            for (int r = 0; r < m_Data.dlssFeatureInfos.Length; ++r)
            {
                var c = new Container<DLSSDebugFeatureInfos>()
                {
                    data = new DLSSDebugFeatureInfos()
                };
                m_Data.dlssFeatureInfos[r] = c;

                string GetPresetLabel(DLSSQuality quality)
                {
                    DLSSPreset presetValue = DLSSPreset.Preset_Default;
                    switch (quality)
                    {
                        case DLSSQuality.DLAA: presetValue = c.data.initData.presetDlaaMode; break;
                        case DLSSQuality.Balanced: presetValue = c.data.initData.presetBalancedMode; break;
                        case DLSSQuality.MaximumQuality: presetValue = c.data.initData.presetQualityMode; break;
                        case DLSSQuality.UltraPerformance: presetValue = c.data.initData.presetUltraPerformanceMode; break;
                        case DLSSQuality.MaximumPerformance: presetValue = c.data.initData.presetPerformanceMode; break;
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
                                getter = () => c.data.validFeature ? "Valid" : "-"
                            },
                            new DebugUI.Value()
                            {
                                getter = () => c.data.validFeature ? resToString(c.data.execData.subrectWidth, c.data.execData.subrectHeight) : "-"
                            },
                            new DebugUI.Value()
                            {
                                getter = () => c.data.validFeature ? resToString(c.data.initData.outputRTWidth, c.data.initData.outputRTHeight) : "-"
                            },
                            new DebugUI.Value()
                            {
                                getter = () => c.data.validFeature ? c.data.initData.quality.ToString() : "-"
                            },
                            new DebugUI.Value()
                            {
                                getter = () => c.data.validFeature ? GetPresetLabel(c.data.initData.quality) : "-"
                            }
                        }
                };
                dlssStateRow.isHiddenCallback = () => !c.data.validFeature;
                m_DlssViewStateTableRows[r] = dlssStateRow;
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
            for (int r = 0; r < m_DlssViewStateTableRows.Length; ++r)
            {
                var d = m_Data.dlssFeatureInfos[r].data;
                m_DlssViewStateTableRows[r].displayName = d.validFeature ? Convert.ToString(d.featureSlot) : "";
            }
        }

#endregion
    }
}
#endif
