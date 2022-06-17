using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;

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

        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            if (Model is not GraphDataVariableDeclarationModel graphDataModel) return;
            var stencil = (ShaderGraphStencil)graphDataModel.GraphModel.Stencil;

            if (!stencil.IsExposable(graphDataModel.DataType))
            {
                m_ExposedToggle.SetEnabled(false);
                m_ExposedToggle.SetValueWithoutNotify(false);
            }

            Debug.Log(graphDataModel.InitializationModel);
        }
    }
}
