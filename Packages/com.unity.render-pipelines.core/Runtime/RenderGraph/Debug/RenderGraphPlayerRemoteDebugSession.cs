#if !UNITY_EDITOR && DEVELOPMENT_BUILD
using UnityEngine.Networking.PlayerConnection;
using static UnityEngine.Rendering.RenderGraphModule.RenderGraph;

namespace UnityEngine.Rendering.RenderGraphModule
{
    internal sealed class RenderGraphPlayerRemoteDebugSession : RenderGraphDebugSession
    {
        bool m_IsActive;

        public override bool isActive => m_IsActive;

        DebugMessageHandler m_MessageHandler = ScriptableObject.CreateInstance<DebugMessageHandler>();

        public RenderGraphPlayerRemoteDebugSession() : base()
        {
            m_MessageHandler.Register(OnMessageFromEditor);
            PlayerConnection.instance.RegisterConnection(OnEditorConnected);
            PlayerConnection.instance.RegisterDisconnection(OnEditorDisconnected);

            // If the editor is already connected when we get here, we request activation.
            // We cannot activate the session immediately because the connection might exist because of the profiler
            // or unit test runner, but Render Graph Viewer is not open.
            // On the other hand, in the event of disconnection, we can deactivate the session directly.
            if (PlayerConnection.instance.isConnected)
                RequestActivate();
        }

        public override void Dispose()
        {
            base.Dispose();
            PlayerConnection.instance.UnregisterConnection(OnEditorConnected);
            PlayerConnection.instance.UnregisterDisconnection(OnEditorDisconnected);
            m_MessageHandler.UnregisterAll();
            CoreUtils.Destroy(m_MessageHandler);
        }

        void OnEditorConnected(int playerId) => RequestActivate();

        void OnEditorDisconnected(int playerId) => DeactivateSession();

        void RequestActivate()
        {
            m_MessageHandler.Send(DebugMessageHandler.MessageType.Activate);
        }

        void ActivateSession()
        {
            InvalidateData(); // Invalidate so that data gets re-sent if the session gets reactivated
            if (!m_IsActive)
            {
                m_IsActive = true;
                RegisterAllLocallyKnownGraphsAndExecutions();
                onDebugDataUpdated += SendDebugDataToEditor;

                SendAnalyticsDataToEditor();
            }
        }

        void DeactivateSession()
        {
            if (m_IsActive)
            {
                m_IsActive = false;
                onDebugDataUpdated -= SendDebugDataToEditor;
            }
        }

        void OnMessageFromEditor(DebugMessageHandler.MessageType messageType, DebugMessageHandler.IPayload _)
        {
            if (messageType == DebugMessageHandler.MessageType.Activate)
                ActivateSession();
        }

        void SendDebugDataToEditor(string graph, EntityId executionId)
        {
            var debugData = GetDebugData(graph, executionId);

            DebugMessageHandler.DebugDataPayload payload = new ()
            {
                graphName = graph,
                executionId = executionId,
                debugData = debugData
            };
            m_MessageHandler.Send(DebugMessageHandler.MessageType.DebugData, payload);
        }

        void SendAnalyticsDataToEditor()
        {
            var payload = new DebugMessageHandler.AnalyticsPayload();
            m_MessageHandler.Send(DebugMessageHandler.MessageType.AnalyticsData, payload);
        }
    }
}
#endif
