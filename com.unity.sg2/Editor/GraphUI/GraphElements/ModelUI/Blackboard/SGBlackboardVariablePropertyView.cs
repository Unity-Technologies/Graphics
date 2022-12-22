using Unity.GraphToolsFoundation.Editor;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class SGBlackboardVariablePropertyView : BlackboardVariablePropertyView
    {
        protected override void BuildRows()
        {
            if (Model is not SGVariableDeclarationModel graphDataModel) return;
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
