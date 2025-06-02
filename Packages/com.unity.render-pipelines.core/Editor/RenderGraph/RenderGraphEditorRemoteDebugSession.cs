using UnityEditor.Rendering.Analytics;
using static UnityEngine.Rendering.RenderGraphModule.RenderGraph;

namespace UnityEngine.Rendering.RenderGraphModule
{
    internal sealed class RenderGraphEditorRemoteDebugSession : RenderGraphDebugSession
    {
        public override bool isActive => false;

        DebugMessageHandler m_DebugMessageHandler = ScriptableObject.CreateInstance<DebugMessageHandler>();

        public RenderGraphEditorRemoteDebugSession() : base()
        {
            m_DebugMessageHandler.Register(OnMessageFromPlayer);

            // In the case of auto-connect profiler option, the player doesn't receive the OnEditorConnected callback.
            // Therefore send an explicit activation message to ensure the player becomes aware of the editor.
            m_DebugMessageHandler.Send(DebugMessageHandler.MessageType.Activate);
        }

        public override void Dispose()
        {
            base.Dispose();

            m_DebugMessageHandler.UnregisterAll();
            CoreUtils.Destroy(m_DebugMessageHandler);
        }

        void OnMessageFromPlayer(DebugMessageHandler.MessageType messageType, DebugMessageHandler.IPayload payload)
        {
            if (messageType == DebugMessageHandler.MessageType.Activate)
            {
                // In the event that the player starts after the editor, it will request activation from the editor.
                m_DebugMessageHandler.Send(DebugMessageHandler.MessageType.Activate);
            }
            else if (messageType == DebugMessageHandler.MessageType.DebugData)
            {
                var debugDataPayload = payload as DebugMessageHandler.DebugDataPayload;
                if (!debugDataPayload.isCompatible)
                {
                    string errorStr = "<Incompatible version>";
                    RegisterAndUpdateDebugData(errorStr, EntityId.None, errorStr, null);
                }
                else
                {
                    RegisterAndUpdateDebugData(
                        debugDataPayload.graphName,
                        debugDataPayload.executionId,
                        debugDataPayload.debugData.executionName,
                        debugDataPayload.debugData);
                }
            }
            else if (messageType == DebugMessageHandler.MessageType.AnalyticsData)
            {
                if (payload is DebugMessageHandler.AnalyticsPayload { isCompatible: true } analyticsPayload)
                {
                    RenderGraphViewerSessionCreatedAnalytic.Send(RenderGraphViewerSessionCreatedAnalytic.SessionType.Remote, analyticsPayload);
                }
            }
        }

        void RegisterAndUpdateDebugData(string graphName, EntityId executionId, string executionName, DebugData debugData)
        {
            RegisterGraph(graphName);
            RegisterExecution(graphName, executionId, debugData.executionName);
            SetDebugData(graphName, executionId, debugData);
        }
    }
}
