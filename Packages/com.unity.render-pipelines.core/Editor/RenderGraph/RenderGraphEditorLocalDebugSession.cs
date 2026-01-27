using UnityEditor.Rendering.Analytics;

namespace UnityEngine.Rendering.RenderGraphModule
{
    internal sealed class RenderGraphEditorLocalDebugSession : RenderGraphDebugSession
    {
        public override bool isActive => true;

        public RenderGraphEditorLocalDebugSession() : base()
        {
            connectionName = "Editor";

            RegisterAllLocallyKnownGraphsAndExecutions();

            var analyticsPayload = new DebugMessageHandler.AnalyticsPayload();
            RenderGraphViewerSessionCreatedAnalytic.Send(RenderGraphViewerSessionCreatedAnalytic.SessionType.Local, analyticsPayload);
        }
    }
}
