using System.Collections.Generic;
using System.Diagnostics;

namespace UnityEngine.Rendering.RenderGraphModule
{
    public partial class RenderGraph
    {
        internal static class RenderGraphExceptionMessages
        {
            internal static bool enableCaller = true;

            internal const string k_RenderGraphExecutionError = "Render Graph Execution error";

            static readonly Dictionary<RenderGraphState, string> m_RenderGraphStateMessages = new()
            {
                { RenderGraphState.RecordingPass, "This API cannot be called when Render Graph records a pass, please call it within SetRenderFunc() or outside of AddUnsafePass()/AddComputePass()/AddRasterRenderPass()." },
                { RenderGraphState.RecordingGraph, "This API cannot be called during the Render Graph high-level recording step, please call it within AddUnsafePass()/AddComputePass()/AddRasterRenderPass() or outside of RecordRenderGraph()." },
                { RenderGraphState.RecordingPass | RenderGraphState.Executing, "This API cannot be called when Render Graph records a pass or executes it, please call it outside of AddUnsafePass()/AddComputePass()/AddRasterRenderPass()." },
                { RenderGraphState.Executing, "This API cannot be called during the Render Graph execution, please call it outside of SetRenderFunc()." },
                { RenderGraphState.Active, "This API cannot be called when Render Graph is active, please call it outside of RecordRenderGraph()." }
            };

            const string k_ErrorDefaultMessage = "Invalid render graph state, impossible to log the exception.";

            internal static string GetExceptionMessage(RenderGraphState state)
            {
                string caller = GetHigherCaller();
                if (!m_RenderGraphStateMessages.TryGetValue(state, out var messageException))
                {
                    return enableCaller ? $"[{caller}] {k_ErrorDefaultMessage}" : k_ErrorDefaultMessage;
                }

                return enableCaller ? $"[{caller}] {messageException}" : messageException;
            }

            static string GetHigherCaller()
            {
                // k_CurrentStackCaller is used here to skip three levels in the call stack:
                // Level 0: GetHigherCaller() itself.
                // Level 1: GetExceptionMessage() or any other wrapper method that calls GetHigherCaller().
                // Level 2: The function that directly calls GetMessage() (e.g CheckNotUsedWhenExecute).
                // Level 3: The actual function we are interested in, our public API.
                const int k_CurrentStackCaller = 3;

                var stackTrace = new StackTrace(k_CurrentStackCaller, false);

                if (stackTrace.FrameCount > 0)
                {
                    var frame = stackTrace.GetFrame(0);
                    return frame?.GetMethod()?.Name ?? "UnknownCaller";
                }

                return "UnknownCaller";
            }
        }
    }
}
