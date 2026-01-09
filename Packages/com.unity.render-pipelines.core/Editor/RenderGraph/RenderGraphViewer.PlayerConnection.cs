using System;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine.Networking.PlayerConnection;

namespace UnityEditor.Rendering
{
    public partial class RenderGraphViewer
    {
        class PlayerConnection : IDisposable
        {
            IConnectionState m_ConnectionState;

            bool m_EditorQuitting;

            readonly UnityEngine.Events.UnityAction<int> m_OnPlayerConnected;
            readonly UnityEngine.Events.UnityAction<int> m_OnPlayerDisconnected;

            public PlayerConnection(IConnectionState connectionState, UnityEngine.Events.UnityAction<int> onPlayerConnected, UnityEngine.Events.UnityAction<int> onPlayerDisconnected)
            {
                m_ConnectionState = connectionState;
                m_OnPlayerConnected = onPlayerConnected;
                m_OnPlayerDisconnected = onPlayerDisconnected;

                EditorConnection.instance.Initialize();
                EditorConnection.instance.RegisterConnection(m_OnPlayerConnected);
                EditorConnection.instance.RegisterDisconnection(m_OnPlayerDisconnected);
            }

            public void Dispose()
            {
                if (m_ConnectionState != null)
                {
                    EditorConnection.instance.UnregisterConnection(m_OnPlayerConnected);
                    EditorConnection.instance.UnregisterDisconnection(m_OnPlayerDisconnected);

                    m_ConnectionState.Dispose();
                    m_ConnectionState = null;
                }
            }

            public void OnConnectionDropdownIMGUI()
            {
                PlayerConnectionGUILayout.ConnectionTargetSelectionDropdown(m_ConnectionState, EditorStyles.toolbarDropDown, 250);
            }
        }
    }
}
