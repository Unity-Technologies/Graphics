using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Plugins.Debugging
{
    class DebuggingErrorBadgeModel : ErrorBadgeModel
    {
        public DebuggingErrorBadgeModel(TracingStep step)
            : base(step.NodeModel)
        {
            m_ErrorMessage = step.ErrorMessage;
        }
    }
}
