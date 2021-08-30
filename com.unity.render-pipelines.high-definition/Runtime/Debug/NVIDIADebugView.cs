using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;

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
                    m_Data.dlssFeatureInfos = TranslateDlssFeatureArray(m_Data.dlssFeatureInfos, m_DebugView);
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
                m_Data.dlssFeatureInfos = null;
            }

            UpdateDebugUITable();
        }

        private static Container<DLSSDebugFeatureInfos>[] TranslateDlssFeatureArray(Container<DLSSDebugFeatureInfos>[] oldArray, in GraphicsDeviceDebugView debugView)
        {
            if (!debugView.dlssFeatureInfos.Any())
                return null;

            Container<DLSSDebugFeatureInfos>[] targetArray = oldArray;
            int dlssFeatureInfosCount = debugView.dlssFeatureInfos.Count();
            if (targetArray == null || targetArray.Length != dlssFeatureInfosCount)
            {
                targetArray = new Container<DLSSDebugFeatureInfos>[dlssFeatureInfosCount];
            }

            //copy data over
            int i = 0;
            foreach (var featureInfo in debugView.dlssFeatureInfos)
            {
                if (targetArray[i] == null)
                    targetArray[i] = new Container<DLSSDebugFeatureInfos>();
                targetArray[i++].data = featureInfo;
            }


            return targetArray;
        }

#endregion

#region Debug User Interface

        private DebugUI.Container m_DebugWidget = null;
        private DebugUI.Table.Row[] m_DlssViewStateTableRows = null;
        private DebugUI.Container m_DlssViewStateTableHeader = null;
        private DebugUI.Table m_DlssViewStateTable = null;
        private DebugUI.Widget InternalCreateWidget()
        {
            if (m_DebugWidget != null)
                return m_DebugWidget;

            m_DlssViewStateTableHeader = new DebugUI.Container()
            {
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
                    }
                }
            };

            m_DlssViewStateTable = new DebugUI.Table()
            {
                displayName = "DLSS Slot ID",
                isReadOnly = true
            };

            m_DlssViewStateTable.children.Add(m_DlssViewStateTableHeader);

            m_DebugWidget = new DebugUI.Container() {
                displayName = "NVIDIA device debug view",
                children =
                {
                    new DebugUI.Value()
                    {
                        displayName = "NVUnityPlugin Version",
                        getter = () => m_DebugView == null ? "-" : m_DebugView.deviceVersion.ToString("X2"),
                    },
                    new DebugUI.Value()
                    {
                        displayName = "NGX API Version",
                        getter = () => m_DebugView == null ? "-" : m_DebugView.ngxVersion.ToString("X2"),
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
                    m_DlssViewStateTable
                }
            };

            return m_DebugWidget;
        }

        private void UpdateDebugUITable()
        {
            if (m_Data.dlssFeatureInfos == null)
            {
                if (m_DlssViewStateTableRows != null)
                    foreach (var w in m_DlssViewStateTableRows)
                        m_DlssViewStateTable.children.Remove(w);
                m_DlssViewStateTableRows = null;
                return;
            }

            String resToString(uint a, uint b)
            {
                return "" + a + "x" + b;
            }

            if (m_DlssViewStateTableRows == null || m_DlssViewStateTableRows.Length != m_Data.dlssFeatureInfos.Length)
            {
                if (m_DlssViewStateTableRows != null)
                    foreach (var w in m_DlssViewStateTableRows)
                        m_DlssViewStateTable.children.Remove(w);

                m_DlssViewStateTableRows = new DebugUI.Table.Row[m_Data.dlssFeatureInfos.Length];
                for (int r = 0; r < m_Data.dlssFeatureInfos.Length; ++r)
                {
                    var c = m_Data.dlssFeatureInfos[r];
                    var dlssStateRow = new DebugUI.Table.Row()
                    {
                        children =
                        {
                            new DebugUI.Value()
                            {
                                getter = () => c.data.validFeature ? "Valid" : "Invalid"
                            },
                            new DebugUI.Value()
                            {
                                getter = () => resToString(c.data.execData.subrectWidth, c.data.execData.subrectHeight)
                            },
                            new DebugUI.Value()
                            {
                                getter = () => resToString(c.data.initData.outputRTWidth, c.data.initData.outputRTHeight)
                            },
                            new DebugUI.Value()
                            {
                                getter = () => c.data.initData.quality.ToString()
                            }
                        }
                    };
                    m_DlssViewStateTableRows[r] = dlssStateRow;
                }
                m_DlssViewStateTable.children.Add(m_DlssViewStateTableRows);
            }

            for (int r = 0; r < m_DlssViewStateTableRows.Length; ++r)
            {
                m_DlssViewStateTableRows[r].displayName = Convert.ToString(m_Data.dlssFeatureInfos[r].data.featureSlot);
            }
        }

#endregion
    }
}
#endif
