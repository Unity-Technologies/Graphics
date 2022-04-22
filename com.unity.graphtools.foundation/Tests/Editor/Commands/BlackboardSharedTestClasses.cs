using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Commands
{
    public class BlackboardSharedTestClasses
    {

        protected class Stencil : UnityEditor.GraphToolsFoundation.Overdrive.Stencil
        {
            public override bool CanPasteNode(INodeModel originalModel, IGraphModel graph)
            {
                return false;
            }

            public override  bool CanPasteVariable(IVariableDeclarationModel originalModel, IGraphModel graph)
            {
                return false;
            }

            public override IBlackboardGraphModel CreateBlackboardGraphModel(IGraphModel graphModel)
            {
                return new BlackboardGraphModel { GraphModel = graphModel };
            }

            public override IInspectorModel CreateInspectorModel(IModel inspectedModel)
            {
                return new InspectorModel(inspectedModel);
            }

            public static readonly string[] sections = { "input","output","variables"};

            public override IEnumerable<string> SectionNames => sections;

            public override bool CanConvertVariable(IVariableDeclarationModel variable, string sectionName)
            {
                if (variable is BlackboardOutputVariableDeclarationModel output)
                    return output.someToggle;
                return true;
            }

            public override string GetVariableSection(IVariableDeclarationModel variable)
            {
                if( variable is BlackboardInputVariableDeclarationModel)
                        return sections[(int)VariableType.Input];
                if( variable is BlackboardOutputVariableDeclarationModel)
                        return sections[(int)VariableType.Output];
                if( variable is BlackboardVariableDeclarationModel)
                        return sections[(int)VariableType.Variable];

                return null;
            }

            public override IVariableDeclarationModel ConvertVariable(IVariableDeclarationModel variable,
                string sectionName)
            {
                switch (Array.IndexOf(sections, sectionName))
                {
                    case (int)VariableType.Input:
                        return GraphModel.CreateGraphVariableDeclaration(typeof(BlackboardInputVariableDeclarationModel),TypeHandle.Float, variable.GetVariableName(),
                            variable.Modifiers, variable.IsExposed);
                    case (int)VariableType.Output:
                        return GraphModel.CreateGraphVariableDeclaration(typeof(BlackboardOutputVariableDeclarationModel),TypeHandle.Float, variable.GetVariableName(),
                            variable.Modifiers, variable.IsExposed);
                    case (int)VariableType.Variable:
                        return GraphModel.CreateGraphVariableDeclaration(typeof(BlackboardVariableDeclarationModel),TypeHandle.Float, variable.GetVariableName(),
                            variable.Modifiers, variable.IsExposed);
                    default:
                        return null;
                }
            }
        }

        public enum VariableType
        {
            Input,
            Output,
            Variable
        }

        protected abstract class BlackboardDeclarationModel : VariableDeclarationModel
        {
        }

        protected class BlackboardInputVariableDeclarationModel : BlackboardDeclarationModel
        {
        }

        protected class BlackboardOutputVariableDeclarationModel : BlackboardDeclarationModel
        {
            bool m_Toggle;

            public bool someToggle
            {
                get => m_Toggle;
                set => m_Toggle = value;
            }
        }

        protected class BlackboardVariableDeclarationModel : BlackboardDeclarationModel
        {
        }

        protected class GraphViewEditorWindow : UnityEditor.GraphToolsFoundation.Overdrive.GraphViewEditorWindow
        {
            protected override bool CanHandleAssetType(IGraphAsset asset)
            {
                return true;
            }
        }

        protected IGraphAsset m_GraphAsset;
        protected GraphViewEditorWindow m_Window;
        protected BlackboardView m_BlackboardView;

        [SetUp]
        public virtual void Setup()
        {
            m_Window = EditorWindow.GetWindow<GraphViewEditorWindow>();
            m_Window.CloseAllOverlays();

            m_GraphAsset = GraphAssetCreationHelpers<TestGraphAsset>.CreateInMemoryGraphAsset(typeof(Stencil), "Test");
            m_Window.GraphView.Dispatch(new LoadGraphCommand(m_GraphAsset.GraphModel));

            m_BlackboardView = new BlackboardView(m_Window, m_Window.GraphView);
            m_Window.rootVisualElement.Add(m_BlackboardView);
        }

        [TearDown]
        public virtual void TearDown()
        {
            m_Window.Close();
        }
    }
}
