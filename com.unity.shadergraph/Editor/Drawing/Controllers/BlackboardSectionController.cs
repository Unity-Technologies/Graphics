using System;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using GraphDataStore = UnityEditor.ShaderGraph.DataStore<UnityEditor.ShaderGraph.GraphData>;

namespace UnityEditor.ShaderGraph.Drawing
{
    class MoveShaderInputAction : IGraphDataAction
    {
        void MoveShaderInput(GraphData graphData)
        {
            Assert.IsNotNull(graphData, "GraphData is null while carrying out MoveShaderInputAction");
            Assert.IsNotNull(ShaderInputReference, "ShaderInputReference is null while carrying out MoveShaderInputAction");
            graphData.owner.RegisterCompleteObjectUndo("Move Graph Input");
            switch (ShaderInputReference)
            {
                case AbstractShaderProperty property:
                    graphData.MoveProperty(property, NewIndexValue);
                    break;
                case ShaderKeyword keyword:
                    graphData.MoveKeyword(keyword, NewIndexValue);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public Action<GraphData> ModifyGraphDataAction =>  MoveShaderInput;

        // Reference to the shader input being modified
        internal ShaderInput ShaderInputReference { get; set; }

        internal int NewIndexValue { get; set; }
    }

    class BlackboardSectionController : SGViewController<GraphData, BlackboardSectionViewModel>
    {
        internal SGBlackboardSection BlackboardSectionView => m_BlackboardSectionView;

        SGBlackboardSection m_BlackboardSectionView;

        internal BlackboardSectionController(GraphData graphData, BlackboardSectionViewModel sectionViewModel, GraphDataStore dataStore)
            : base(graphData, sectionViewModel, dataStore)
        {
            m_BlackboardSectionView = new SGBlackboardSection(sectionViewModel);
        }

        protected override void RequestModelChange(IGraphDataAction changeAction)
        {
            DataStore.Dispatch(changeAction);
        }

        // Called by GraphDataStore.Subscribe after the model has been changed
        protected override void ModelChanged(GraphData graphData, IGraphDataAction changeAction)
        {

        }
    }
}
