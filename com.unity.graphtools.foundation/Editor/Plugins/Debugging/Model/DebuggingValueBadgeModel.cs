using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Plugins.Debugging
{
    class DebuggingValueBadgeModel : ValueBadgeModel
    {
        public DebuggingValueBadgeModel(TracingStep step)
            : base(step.PortModel, step.ValueString)
        {
        }
    }
}
