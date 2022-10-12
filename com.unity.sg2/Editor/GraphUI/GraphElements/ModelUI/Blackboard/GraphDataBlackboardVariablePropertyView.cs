using Unity.GraphToolsFoundation.Editor;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class GraphDataBlackboardVariablePropertyView : BlackboardVariablePropertyView
    {
        protected override void BuildRows()
        {
            if (Model is not GraphDataVariableDeclarationModel graphDataModel) return;
            if (graphDataModel.IsExposable)
            {
                AddExposedToggle();
            }

            if (graphDataModel.HasEditableInitialization)
            {
                AddInitializationField();
            }

            AddTooltipField();
        }
    }
}
