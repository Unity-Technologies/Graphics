using System;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine.Networking.PlayerConnection;

namespace UnityEditor.Rendering
{
    public partial class RenderGraphViewer
    {
        class PlayerConnection : IDisposable
        {
            public IConnectionState connectionState { get; private set; }

            readonly UnityEngine.Events.UnityAction<int> m_OnPlayerConnected;
            readonly UnityEngine.Events.UnityAction<int> m_OnPlayerDisconnected;

            public PlayerConnection(EditorWindow rgvWindow, UnityEngine.Events.UnityAction<int> onPlayerConnected, UnityEngine.Events.UnityAction<int> onPlayerDisconnected)
            {
                connectionState = PlayerConnectionGUIUtility.GetConnectionState(rgvWindow);
                m_OnPlayerConnected = onPlayerConnected;
                m_OnPlayerDisconnected = onPlayerDisconnected;
            }

            public void Connect()
            {
                EditorConnection.instance.Initialize();
                EditorConnection.instance.RegisterConnection(m_OnPlayerConnected);
                EditorConnection.instance.RegisterDisconnection(m_OnPlayerDisconnected);
            }

            public void Dispose()
            {
                if (connectionState != null)
                {
                    EditorConnection.instance.UnregisterConnection(m_OnPlayerConnected);
                    EditorConnection.instance.UnregisterDisconnection(m_OnPlayerDisconnected);

                    connectionState.Dispose();
                    connectionState = null;
                }
            }

            public void OnConnectionDropdownIMGUI()
            {
                PlayerConnectionGUILayout.ConnectionTargetSelectionDropdown(connectionState, EditorStyles.toolbarDropDown, 250);
            }
        }
    }
}
