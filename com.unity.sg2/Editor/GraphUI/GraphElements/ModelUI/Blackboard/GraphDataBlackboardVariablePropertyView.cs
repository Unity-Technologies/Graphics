using UnityEditor.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class GraphDataBlackboardVariablePropertyView : BlackboardVariablePropertyView
    {
        protected override void BuildRows()
        {
            AddExposedToggle();
            AddInitializationField();
            AddTooltipField();
        }
    }
}
