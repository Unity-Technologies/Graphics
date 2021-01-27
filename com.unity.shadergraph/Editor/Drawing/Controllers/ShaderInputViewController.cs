using UnityEngine;
using System;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.UIElements;
using GraphDataStore = UnityEditor.ShaderGraph.DataStore<UnityEditor.ShaderGraph.GraphData>;

namespace UnityEditor.ShaderGraph.Drawing
{
    class ShaderInputViewController : SGViewController<ShaderInput, ShaderInputViewModel>
    {
        internal ShaderInputViewController(ShaderInput shaderInput, ShaderInputViewModel inViewModel, GraphDataStore graphDataStore)
            : base(shaderInput, inViewModel, graphDataStore)
        {
            InitializeViewModel();

            m_BlackboardPropertyView = new BlackboardPropertyView(ViewModel);
            m_BlackboardRowView = new SGBlackboardRow(m_BlackboardPropertyView, null);
        }

        void InitializeViewModel()
        {
            ViewModel.IsInputExposed = (DataStore.State.isSubGraph || (Model.isExposable && Model.generatePropertyBlock));
            ViewModel.InputName = Model.displayName;
            switch (Model)
            {
                case AbstractShaderProperty shaderProperty:
                    ViewModel.InputTypeName = shaderProperty.GetPropertyTypeString();
                    break;
                case ShaderKeyword shaderKeyword:
                    ViewModel.InputTypeName = shaderKeyword.keywordType  + " Keyword";
                    ViewModel.InputTypeName = shaderKeyword.isBuiltIn ? "Built-in " + ViewModel.InputTypeName : ViewModel.InputTypeName;
                    break;
            }

            ViewModel.RequestModelChangeAction = this.RequestModelChange;
        }

        VisualElement m_BlackboardRowView;
        BlackboardPropertyView m_BlackboardPropertyView;

        protected override void RequestModelChange(IGraphDataAction changeAction)
        {
            DataStore.Dispatch(changeAction);
        }

        // Called by GraphDataStore.Subscribe after the model has been changed
        protected override void ModelChanged(GraphData graphData, IGraphDataAction changeAction) { }

        public override void ApplyChanges() { }
    }
}
