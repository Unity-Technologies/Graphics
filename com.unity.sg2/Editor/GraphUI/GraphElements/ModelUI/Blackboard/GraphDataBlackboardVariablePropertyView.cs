using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class GraphDataBlackboardVariablePropertyView : BlackboardVariablePropertyView
    {
        protected override void BuildRows()
        {
            if (Model is not GraphDataVariableDeclarationModel graphDataModel) return;
            if (graphDataModel.IsExposable)
            {
                AddExposedToggle();
            }

            AddInitializationField();
            AddTooltipField();
        }
    }
}
