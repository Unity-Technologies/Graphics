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

                EditorApplication.quitting += OnEditorQuitting;
            }

            public void Dispose()
            {
                if (m_ConnectionState != null)
                {
                    EditorConnection.instance.UnregisterConnection(m_OnPlayerConnected);
                    EditorConnection.instance.UnregisterDisconnection(m_OnPlayerDisconnected);

                    // NOTE: There is a bug where editor crashes if we call DisconnectAll during shutdown flow. In this case
                    // it's fine to skip the disconnect as the player will get notified of it anyway.
                    if (!m_EditorQuitting)
                        EditorConnection.instance.DisconnectAll();

                    m_ConnectionState.Dispose();
                    m_ConnectionState = null;

                    EditorApplication.quitting -= OnEditorQuitting;
                }
            }

            public void OnConnectionDropdownIMGUI()
            {
                PlayerConnectionGUILayout.ConnectionTargetSelectionDropdown(m_ConnectionState, EditorStyles.toolbarDropDown, 250);
            }

            void OnEditorQuitting()
            {
                m_EditorQuitting = true;
            }
        }
    }
}
