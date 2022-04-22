using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class CopyPasteTests
    {
        class CopyPasteGraphModel : GraphModel
        {
            public override Type DefaultStencilType => typeof(CopyPasteStencil);
        }
        class CopyPasteGraphAsset : GraphAsset
        {
            protected override Type GraphModelType => typeof(CopyPasteGraphModel);
        }

        class CopyPasteStencil : Stencil
        {
            public override bool CanPasteNode(INodeModel originalModel, IGraphModel graph)
            {
                return true;
            }

            public override  bool CanPasteVariable(IVariableDeclarationModel originalModel, IGraphModel graph)
            {
                return true;
            }

            public override IBlackboardGraphModel CreateBlackboardGraphModel(IGraphModel graphModel)
            {
                return new BlackboardGraphModel { GraphModel = graphModel };
            }

            /// <inheritdoc />
            public override IInspectorModel CreateInspectorModel(IModel inspectedModel)
            {
                return new InspectorModel(inspectedModel);
            }
        }

        class CopyPasteGraphViewWindow : GraphViewEditorWindow
        {
            public const string toolName = "Copy Paste GTF Tests";

            [InitializeOnLoadMethod]
            static void RegisterTool()
            {
                ShortcutHelper.RegisterDefaultShortcuts<CopyPasteGraphViewWindow>(toolName);
            }
            protected override bool CanHandleAssetType(IGraphAsset asset)
            {
                return asset is CopyPasteGraphAsset;
            }
        }

        IStickyNoteModel m_StickyNoteModel;
        IPlacematModel m_PlacematModel;
        CopyPasteNodeModel m_NodeModel;
        GraphViewEditorWindow m_SecondWindow;

        class OtherStencil : Stencil
        {
            public override bool CanPasteNode(INodeModel originalNode, IGraphModel graphModel)
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

            /// <inheritdoc />
            public override IInspectorModel CreateInspectorModel(IModel inspectedModel)
            {
                return new InspectorModel(inspectedModel);
            }
        }

        class CopyPasteOtherWindow : GraphViewEditorWindow
        {
            public const string toolName = "Other GTF Tests";

            [InitializeOnLoadMethod]
            static void RegisterTool()
            {
                ShortcutHelper.RegisterDefaultShortcuts<CopyPasteOtherWindow>(toolName);
            }

            public CopyPasteOtherWindow()
            {
                this.SetDisableInputEvents(true);
#if !UNITY_2022_2_OR_NEWER
                WithSidePanel = false;
#endif
            }

            protected override GraphView CreateGraphView()
            {
                return new TestGraphView(this, GraphTool);
            }

            protected override bool CanHandleAssetType(IGraphAsset asset)
            {
                return true;
            }
        }

        [Serializable]
        class CopyPasteNodeModel : NodeModel
        {
            [SerializeField]
            int inputCount;

            [SerializeField]
            int outputCount;

            public int InputCount { get => inputCount; set => inputCount = value; }
            public int OutputCount { get => outputCount; set => outputCount = value; }

            protected override void OnDefineNode()
            {
                for (var i = 0; i < InputCount; i++)
                    this.AddDataInputPort("In " + i, TypeHandle.Unknown);

                for (var i = 0; i < OutputCount; i++)
                    this.AddDataOutputPort("Out " + i, TypeHandle.Unknown);
            }
        }

        CopyPasteGraphViewWindow Window { get; set; }
        IGraphModel SecondGraphModel => m_SecondWindow.GraphView.GraphModel;
        BlackboardView SecondWindowBlackboardView { get; set; }

        IGraphModel GraphModel => Window.GraphView.GraphModel;

        GraphView GraphView => Window.GraphView;

        BlackboardView BlackboardView { get; set; }

        [SetUp]
        public void SetUp()
        {
            Window = EditorWindow.GetWindow<CopyPasteGraphViewWindow>();
            Window.position = new Rect(100, 100, 1600, 800);
            Window.CloseAllOverlays();

            BlackboardView = new BlackboardView(Window, Window.GraphView);
            Window.rootVisualElement.Add(BlackboardView);

            var graphAsset = GraphAssetCreationHelpers<CopyPasteGraphAsset>.CreateInMemoryGraphAsset(typeof(CopyPasteStencil), "Test");
            Window.GraphTool.Dispatch(new LoadGraphCommand(graphAsset.GraphModel));

            Vector3 frameTranslation = Vector3.zero;
            Vector3 frameScaling = Vector3.one;
            GraphView.Dispatch(new ReframeGraphViewCommand(frameTranslation, frameScaling));
        }

        IEnumerator Start()
        {
            m_StickyNoteModel = GraphModel.CreateStickyNote(new Rect(100, 100, 300, 300));
            m_StickyNoteModel.Title = "Hello";
            m_StickyNoteModel.Contents = "My name is Harry Potter";

            m_PlacematModel = GraphModel.CreatePlacemat(new Rect(500, 100, 600, 600));
            m_PlacematModel.Title = "Placemat";

            m_NodeModel = GraphModel.CreateNode<CopyPasteNodeModel>("My Node", Vector2.one, default, m=>{m.InputCount = 1; m.OutputCount = 2;});

            Window.Focus();

            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            // See case: https://fogbugz.unity3d.com/f/cases/998343/
            // Clearing the capture needs to happen before closing the window
            MouseCaptureController.ReleaseMouse();

            if (Window != null)
            {
                GraphModel?.QuickCleanup();
                Window.Close();
            }

            if (m_SecondWindow != null)
            {
                m_SecondWindow.GraphView?.GraphModel?.QuickCleanup();
                m_SecondWindow.Close();
            }
        }


        IEnumerator MakeSecondWindow()
        {
            m_SecondWindow = EditorWindow.CreateWindow<CopyPasteGraphViewWindow>();
            m_SecondWindow.titleContent = new GUIContent("Second Window");
            m_SecondWindow.position = new Rect(50, 100, 400, 400);

            SecondWindowBlackboardView = new BlackboardView(m_SecondWindow, m_SecondWindow.GraphView);
            m_SecondWindow.rootVisualElement.Add(SecondWindowBlackboardView);

            var graphAsset = GraphAssetCreationHelpers<CopyPasteGraphAsset>.CreateInMemoryGraphAsset(typeof(CopyPasteStencil), "Other Test");
            m_SecondWindow.GraphTool.Dispatch(new LoadGraphCommand(graphAsset.GraphModel));

            yield return null;

            m_SecondWindow.Focus();
        }
        IEnumerator MakeOtherSecondWindow()
        {
            m_SecondWindow = EditorWindow.CreateWindow<CopyPasteOtherWindow>();
            m_SecondWindow.titleContent = new GUIContent("Second Window");
            m_SecondWindow.position = new Rect(50, 100, 400, 400);

            var graphAsset = GraphAssetCreationHelpers<CopyPasteGraphAsset>.CreateInMemoryGraphAsset(typeof(OtherStencil), "Other Test");
            m_SecondWindow.GraphTool.Dispatch(new LoadGraphCommand(graphAsset.GraphModel));

            yield return null;

            m_SecondWindow.Focus();
        }

        [UnityTest]
        public IEnumerator CopyPasteStickyNote()
        {
            yield return Start();

            GraphView.Dispatch(new ClearSelectionCommand());

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, m_StickyNoteModel));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Copy));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Copy));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            Assert.AreEqual(2, GraphModel.StickyNoteModels.Count);
            Assert.IsTrue(GraphModel.StickyNoteModels.All(t=>t.Title == "Hello"));
            Assert.IsTrue(GraphModel.StickyNoteModels.All(t=>t.Contents == "My name is Harry Potter"));

            yield return MakeSecondWindow();

            m_SecondWindow.Focus();
            m_SecondWindow.GraphView.Focus();
            m_SecondWindow.GraphView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            m_SecondWindow.GraphView.Focus();
            m_SecondWindow.GraphView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            Assert.AreEqual(1, SecondGraphModel.StickyNoteModels.Count);
            Assert.IsTrue(SecondGraphModel.StickyNoteModels.All(t=>t.Title == "Hello"));
            Assert.IsTrue(SecondGraphModel.StickyNoteModels.All(t=>t.Contents == "My name is Harry Potter"));

        }

        [UnityTest]
        public IEnumerator CopyPastePlacemat()
        {
            yield return Start();

            GraphView.Dispatch(new ClearSelectionCommand());

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, m_PlacematModel));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Copy));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Copy));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            Assert.AreEqual(2, GraphModel.PlacematModels.Count);
            Assert.IsTrue(GraphModel.PlacematModels.Any(t=>t.Title == "Placemat"));
            Assert.IsTrue(GraphModel.PlacematModels.Any(t=>t.Title == "Copy of Placemat"));

            yield return MakeSecondWindow();

            m_SecondWindow.Focus();
            m_SecondWindow.GraphView.Focus();
            m_SecondWindow.GraphView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            m_SecondWindow.GraphView.Focus();
            m_SecondWindow.GraphView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            Assert.AreEqual(1, SecondGraphModel.PlacematModels.Count);
            Assert.IsTrue(SecondGraphModel.PlacematModels.All(t=>t.Title == "Copy of Placemat"));
        }

        [UnityTest]
        public IEnumerator CopyPasteNode()
        {
            yield return Start();

            GraphView.Dispatch(new ClearSelectionCommand());

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, m_NodeModel));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Copy));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Copy));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            Assert.AreEqual(2, GraphModel.NodeModels.Count);

            yield return MakeSecondWindow();

            m_SecondWindow.GraphView.Focus();
            m_SecondWindow.GraphView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            m_SecondWindow.GraphView.Focus();
            m_SecondWindow.GraphView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            Assert.AreEqual(1, SecondGraphModel.NodeModels.Count);
        }

        [UnityTest]
        public IEnumerator CopyPasteVariableNode()
        {
            yield return Start();

            var variableDecl = GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "toto",ModifierFlags.None,true);

            var variableNode = GraphModel.CreateVariableNode(variableDecl,Vector2.zero);

            Assert.AreEqual(2,GraphModel.NodeModels.Count);

            GraphView.Dispatch(new ClearSelectionCommand());

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace,variableNode));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Copy));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Copy));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            Assert.AreEqual(3,GraphModel.NodeModels.Count);
            Assert.AreEqual(1,GraphModel.VariableDeclarations.Count);

            yield return MakeSecondWindow();

            m_SecondWindow.GraphView.Focus();
            m_SecondWindow.GraphView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            m_SecondWindow.GraphView.Focus();
            m_SecondWindow.GraphView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            // Pasting a variable node in a new window should result in the new window having one more variable and the new variable node pointing to this variable.
            Assert.AreEqual(1,SecondGraphModel.NodeModels.Count);
            Assert.AreEqual(1,SecondGraphModel.VariableDeclarations.Count);
            Assert.AreEqual(SecondGraphModel.VariableDeclarations.First(),((IVariableNodeModel)SecondGraphModel.NodeModels.First()).VariableDeclarationModel);
        }

        [UnityTest]
        public IEnumerator CopyPasteVariableNodeTwice()
        {
            yield return Start();

            var variableDecl = GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "toto",ModifierFlags.None,true);

            var variableNode = GraphModel.CreateVariableNode(variableDecl,Vector2.zero);

            Assert.AreEqual(2,GraphModel.NodeModels.Count);

            GraphView.Dispatch(new ClearSelectionCommand());

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace,variableNode));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Copy));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Copy));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            Assert.AreEqual(4,GraphModel.NodeModels.Count);
            Assert.AreEqual(1,GraphModel.VariableDeclarations.Count);

            yield return MakeSecondWindow();

            m_SecondWindow.GraphView.Focus();
            m_SecondWindow.GraphView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            m_SecondWindow.GraphView.Focus();
            m_SecondWindow.GraphView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            m_SecondWindow.GraphView.Focus();
            m_SecondWindow.GraphView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            m_SecondWindow.GraphView.Focus();
            m_SecondWindow.GraphView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            // Pasting a variable node in a new window twice should result in the new window having one more variable and two new variables nodes pointing to this variable.
            Assert.AreEqual(2,SecondGraphModel.NodeModels.Count);
            Assert.AreEqual(1,SecondGraphModel.VariableDeclarations.Count);
        }

        [UnityTest]
        public IEnumerator CopyPasteVariableDeclaration()
        {
            yield return Start();

            var section = GraphModel.GetSectionModel(GraphModel.Stencil.SectionNames.First());
            var group = GraphModel.CreateGroup("titi");
            section.InsertItem(group);
            var variableDecl = GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "toto",ModifierFlags.None,true,group);
            Assert.AreEqual(1,GraphModel.VariableDeclarations.Count);
            Assert.IsTrue(GraphModel.VariableDeclarations.All(t => t.ParentGroup == group));

            using (var updater = GraphView.GraphViewModel.GraphModelState.UpdateScope)
            {
                updater.ForceCompleteUpdate();
            }

            BlackboardView.Dispatch(new ClearSelectionCommand());

            BlackboardView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace,variableDecl));
            yield return null;

            BlackboardView.Focus();
            BlackboardView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Copy));
            yield return null;

            BlackboardView.Focus();
            BlackboardView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Copy));
            yield return null;

            BlackboardView.Focus();
            BlackboardView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            BlackboardView.Focus();
            BlackboardView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            Assert.AreEqual(2,GraphModel.VariableDeclarations.Count);
            // A duplicated variable withing the same graph should be in the same group.
            Assert.IsTrue(GraphModel.VariableDeclarations.All(t => t.ParentGroup == group));

            yield return MakeSecondWindow();

            SecondWindowBlackboardView.Focus();
            SecondWindowBlackboardView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            SecondWindowBlackboardView.Focus();
            SecondWindowBlackboardView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            Assert.AreEqual(1,SecondGraphModel.VariableDeclarations.Count);
            Assert.AreNotEqual(SecondGraphModel.VariableDeclarations.First(),GraphModel.VariableDeclarations.First());
        }

        [UnityTest]
        public IEnumerator DuplicateTwoVariableDeclarations()
        {
            yield return Start();

            var section = GraphModel.GetSectionModel(GraphModel.Stencil.SectionNames.First());
            var group = GraphModel.CreateGroup("titi");
            section.InsertItem(group);
            var variableDecl = GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "toto",ModifierFlags.None,true,group);
            var variableDecl2 = GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "tutu",ModifierFlags.None,true,group);
            Assert.AreEqual(2,GraphModel.VariableDeclarations.Count);
            Assert.IsTrue(GraphModel.VariableDeclarations.All(t => t.ParentGroup == group));
            Assert.AreEqual(group.Items[0], variableDecl);
            Assert.AreEqual(group.Items[1], variableDecl2);

            using (var updater = GraphView.GraphViewModel.GraphModelState.UpdateScope)
            {
                updater.ForceCompleteUpdate();
            }

            BlackboardView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace,variableDecl,variableDecl2));
            yield return null;

            BlackboardView.Focus();
            BlackboardView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Duplicate));
            yield return null;

            BlackboardView.Focus();
            BlackboardView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Duplicate));
            yield return null;

            Assert.AreEqual(4,GraphModel.VariableDeclarations.Count);
            // A duplicated variable withing the same graph should be in the same group.
            Assert.IsTrue(GraphModel.VariableDeclarations.All(t => t.ParentGroup == group));

            // When duplicated the new variables should be placed just after the original
            Assert.AreEqual(group.Items[0], variableDecl);
            Assert.AreEqual(group.Items[1], variableDecl2);
        }

        [UnityTest]
        public IEnumerator CopyPasteTwoVariableDeclarations()
        {
            yield return Start();

            var section = GraphModel.GetSectionModel(GraphModel.Stencil.SectionNames.First());
            var group = GraphModel.CreateGroup("titi");
            section.InsertItem(group);
            var variableDecl = GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "toto",ModifierFlags.None,true,group);
            var variableDecl2 = GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "tutu",ModifierFlags.None,true,group);
            Assert.AreEqual(2,GraphModel.VariableDeclarations.Count);
            Assert.IsTrue(GraphModel.VariableDeclarations.All(t => t.ParentGroup == group));
            Assert.AreEqual(group.Items[0], variableDecl);
            Assert.AreEqual(group.Items[1], variableDecl2);

            using (var updater = GraphView.GraphViewModel.GraphModelState.UpdateScope)
            {
                updater.ForceCompleteUpdate();
            }

            BlackboardView.Dispatch(new ClearSelectionCommand());

            BlackboardView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace,variableDecl,variableDecl2));
            yield return null;

            BlackboardView.Focus();
            BlackboardView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Copy));
            yield return null;

            BlackboardView.Focus();
            BlackboardView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Copy));
            yield return null;

            BlackboardView.Focus();
            BlackboardView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            BlackboardView.Focus();
            BlackboardView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            Assert.AreEqual(4,GraphModel.VariableDeclarations.Count);
            // A duplicated variable withing the same graph should be in the same group.
            Assert.IsTrue(GraphModel.VariableDeclarations.All(t => t.ParentGroup == group));

            // When copy/pasted the new variables should be placed just after the last selected
            Assert.AreEqual(group.Items[0], variableDecl);
            Assert.AreEqual(group.Items[1], variableDecl2);
        }

        [UnityTest]
        public IEnumerator CopyPasteGroupSimple()
        {
            yield return Start();

            var section = GraphModel.GetSectionModel(GraphModel.Stencil.SectionNames.First());
            var group = GraphModel.CreateGroup("titi");
            section.InsertItem(group);
            GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "toto",ModifierFlags.None,true,group);
            Assert.AreEqual(1,GraphModel.VariableDeclarations.Count);
            Assert.IsTrue(GraphModel.VariableDeclarations.All(t => t.ParentGroup == group));
            Assert.AreEqual(1,group.Items.Count);

            using (var updater = GraphView.GraphViewModel.GraphModelState.UpdateScope)
            {
                updater.ForceCompleteUpdate();
            }

            BlackboardView.Dispatch(new ClearSelectionCommand());

            BlackboardView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace,group));
            yield return null;

            BlackboardView.Focus();
            BlackboardView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Copy));
            yield return null;

            BlackboardView.Focus();
            BlackboardView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Copy));
            yield return null;

            BlackboardView.Dispatch(new ClearSelectionCommand());
            yield return null;

            BlackboardView.Focus();
            BlackboardView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            BlackboardView.Focus();
            BlackboardView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            Assert.AreEqual(2,GraphModel.VariableDeclarations.Count);

            Assert.AreEqual(2,section.Items.Count);

            Assert.AreEqual(1,group.Items.Count);

            var copiedGroup = (IGroupModel)section.Items[1];

            Assert.IsTrue(copiedGroup.Items.Contains(GraphModel.VariableDeclarations[1]));

            yield return MakeSecondWindow();

            SecondWindowBlackboardView.Focus();
            SecondWindowBlackboardView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            SecondWindowBlackboardView.Focus();
            SecondWindowBlackboardView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            Assert.AreEqual(1,SecondGraphModel.VariableDeclarations.Count);
            var secondSection = SecondGraphModel.GetSectionModel(SecondGraphModel.Stencil.SectionNames.First());

            Assert.AreNotEqual(SecondGraphModel.VariableDeclarations.First(),GraphModel.VariableDeclarations.First());
            Assert.AreNotEqual(SecondGraphModel.VariableDeclarations.First(),GraphModel.VariableDeclarations[1]);

            Assert.AreEqual(1,secondSection.Items.Count);
        }

        [UnityTest]
        public IEnumerator CopyPasteTwoGroups()
        {
            yield return Start();

            var section = GraphModel.GetSectionModel(GraphModel.Stencil.SectionNames.First());
            var group = GraphModel.CreateGroup("titi");
            var group2 = GraphModel.CreateGroup("titi");
            section.InsertItem(group);
            section.InsertItem(group2);

            using (var updater = GraphView.GraphViewModel.GraphModelState.UpdateScope)
            {
                updater.ForceCompleteUpdate();
            }

            BlackboardView.Dispatch(new ClearSelectionCommand());

            BlackboardView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace,group,group2));
            yield return null;

            BlackboardView.Focus();
            BlackboardView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Copy));
            yield return null;

            BlackboardView.Focus();
            BlackboardView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Copy));
            yield return null;

            BlackboardView.Dispatch(new ClearSelectionCommand());
            yield return null;

            BlackboardView.Focus();
            BlackboardView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            BlackboardView.Focus();
            BlackboardView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            Assert.AreEqual(4,section.Items.Count);

            Assert.AreEqual(group,section.Items[0]);
            Assert.AreEqual(group2,section.Items[1]);
        }

        [UnityTest]
        public IEnumerator DuplicateTwoGroups()
        {
            yield return Start();

            var section = GraphModel.GetSectionModel(GraphModel.Stencil.SectionNames.First());
            var group = GraphModel.CreateGroup("titi");
            var group2 = GraphModel.CreateGroup("titi");
            section.InsertItem(group);
            section.InsertItem(group2);

            using (var updater = GraphView.GraphViewModel.GraphModelState.UpdateScope)
            {
                updater.ForceCompleteUpdate();
            }

            BlackboardView.Dispatch(new ClearSelectionCommand());

            BlackboardView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace,group,group2));
            yield return null;

            BlackboardView.Focus();
            BlackboardView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Duplicate));
            yield return null;

            BlackboardView.Focus();
            BlackboardView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Duplicate));
            yield return null;

            Assert.AreEqual(4,section.Items.Count);

            Assert.AreEqual(group,section.Items[0]);
            Assert.AreEqual(group2,section.Items[2]);
        }

        [UnityTest]
        public IEnumerator CopyPasteNodeOtherGraph()
        {
            yield return Start();

            GraphView.Dispatch(new ClearSelectionCommand());

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, m_NodeModel));
            yield return null;

            Window.Focus();

            GraphView.Focus();
            GraphView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Copy));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Copy));
            yield return null;

            yield return MakeOtherSecondWindow();

            m_SecondWindow.Focus();

            m_SecondWindow.GraphView.Focus();
            m_SecondWindow.GraphView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            m_SecondWindow.GraphView.Focus();
            m_SecondWindow.GraphView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            Assert.AreEqual(0, SecondGraphModel.NodeModels.Count);
        }

        [UnityTest]
        public IEnumerator CopyPastePlacematCopiesNodesAndEdges()
        {
            yield return Start();

            var firstNode = GraphModel.CreateNode<CopyPasteNodeModel>("My Node1", new Vector2(510, 110), default, m=>{m.InputCount = 1; m.OutputCount = 2;});
            var secondNode = GraphModel.CreateNode<CopyPasteNodeModel>("My Node2", new Vector2(750, 110), default, m=>{m.InputCount = 1; m.OutputCount = 0;});

            Assert.IsTrue(firstNode.Container == GraphModel);
            Assert.IsTrue(secondNode.Container == GraphModel);
            Assert.IsTrue(firstNode.IsSelectable());
            Assert.IsTrue(secondNode.IsSelectable());

            GraphModel.CreateEdge(secondNode.InputsByDisplayOrder.First(), firstNode.OutputsByDisplayOrder.First());
            GraphModel.CreateEdge(firstNode.InputsByDisplayOrder.First(), m_NodeModel.OutputsByDisplayOrder.First());

            Assert.AreEqual(3, GraphModel.NodeModels.Count);
            Assert.AreEqual(2, GraphModel.EdgeModels.Count);

            //force ui build based on changes in GraphModel
            using (var updater = GraphView.GraphViewModel.GraphModelState.UpdateScope)
            {
                updater.ForceCompleteUpdate();
            }

            yield return null;

            GraphView.Dispatch(new ClearSelectionCommand());

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, m_PlacematModel, m_NodeModel));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Copy));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Copy));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            // The three nodes must be duplicated.
            Assert.AreEqual(6, GraphModel.NodeModels.Count);

            // One of the edge must have been duplicated.
            Assert.AreEqual(3, GraphModel.EdgeModels.Count);
        }


        [UnityTest]
        public IEnumerator CopyPasteBlock()
        {
            yield return Start();

            var context = GraphModel.CreateNode<ContextNodeModel>("toto");
            var context2 = GraphModel.CreateNode<ContextNodeModel>("titi");

            var block = context.CreateAndInsertBlock(typeof(BlockNodeModel));

            using (var graphUpdater = GraphView.GraphViewModel.GraphModelState.UpdateScope)
            {
                graphUpdater.ForceCompleteUpdate();
            }

            yield return null;

            GraphView.Dispatch(new ClearSelectionCommand());

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, block));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Copy));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Copy));
            yield return null;

            GraphView.Dispatch(new ClearSelectionCommand());
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));

            yield return null;

            //pasting on the graph should do nothing

            Assert.IsFalse(GraphModel.NodeModels.Any(t=> t is IBlockNodeModel));

            GraphView.Dispatch(new ClearSelectionCommand());

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, context2));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));

            yield return null;

            Assert.AreEqual(1,context2.GraphElementModels.Count());
            Assert.AreEqual(context.GraphElementModels.First().GetType(),context2.GraphElementModels.First().GetType());
        }

        [UnityTest]
        public IEnumerator TestThatPastingBlockWhenBlockIsSelectedWorks()
        {
            yield return Start();

            var context = GraphModel.CreateNode<ContextNodeModel>("toto");
            var block = context.CreateAndInsertBlock(typeof(BlockNodeModel));

            using (var graphUpdater = GraphView.GraphViewModel.GraphModelState.UpdateScope)
            {
                graphUpdater.ForceCompleteUpdate();
            }

            yield return null;

            GraphView.Dispatch(new ClearSelectionCommand());

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, block));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Copy));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Copy));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));

            yield return null;

            Assert.AreEqual(2,context.GraphElementModels.Count());
            Assert.AreEqual(context.GraphElementModels.First().GetType(),context.GraphElementModels.ElementAt(1).GetType());
        }

        [UnityTest]
        public IEnumerator CopyPasteSubgraphNode()
        {
            yield return Start();

            var subgraphAsset = GraphAssetCreationHelpers<ClassGraphAsset>.CreateInMemoryGraphAsset(typeof(ClassStencil), "Subgraph") as GraphAsset;
            Assert.IsNotNull(subgraphAsset);

            var subgraphNodeModel = GraphModel.CreateSubgraphNode(subgraphAsset.GraphModel, Vector2.zero);
            Assert.IsTrue(subgraphNodeModel.Container == GraphModel);
            Assert.IsTrue(subgraphNodeModel.IsSelectable());

            Window.Focus();

            yield return null;

            GraphView.Dispatch(new ClearSelectionCommand());

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, subgraphNodeModel));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Copy));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Copy));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            var subgraphNodesInGraph = GraphModel.NodeModels.OfType<ISubgraphNodeModel>().ToList();

            Assert.AreEqual(2,subgraphNodesInGraph.Count);

            foreach (var subgraphNode in subgraphNodesInGraph)
                Assert.AreEqual(subgraphAsset.GraphModel, subgraphNode.SubgraphModel);

            yield return MakeSecondWindow();

            m_SecondWindow.GraphView.Focus();
            m_SecondWindow.GraphView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            m_SecondWindow.GraphView.Focus();
            m_SecondWindow.GraphView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            var subgraphNodesInSecondGraph = SecondGraphModel.NodeModels.OfType<ISubgraphNodeModel>().ToList();

            Assert.AreEqual(1,subgraphNodesInSecondGraph.Count);
            Assert.AreEqual(subgraphAsset.GraphModel, subgraphNodesInGraph.First().SubgraphModel);

            m_SecondWindow.Close();

            yield return MakeOtherSecondWindow();

            m_SecondWindow.GraphView.Focus();
            m_SecondWindow.GraphView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            m_SecondWindow.GraphView.Focus();
            m_SecondWindow.GraphView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            Assert.AreEqual(0,SecondGraphModel.NodeModels.OfType<ISubgraphNodeModel>().ToList().Count);
        }

        [UnityTest]
        public IEnumerator CopyPasteVariableOtherGraph()
        {
            yield return Start();

            var variableModel = GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "variable", ModifierFlags.ReadWrite, true);

            BlackboardView.Dispatch(new ClearSelectionCommand());

            BlackboardView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, variableModel));
            yield return null;

            Window.Focus();

            BlackboardView.Focus();
            BlackboardView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Copy));
            yield return null;

            BlackboardView.Focus();
            BlackboardView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Copy));
            yield return null;

            yield return MakeSecondWindow();

            m_SecondWindow.Focus();

            SecondWindowBlackboardView.Focus();
            SecondWindowBlackboardView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            SecondWindowBlackboardView.Focus();
            SecondWindowBlackboardView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            Assert.AreEqual(1, SecondGraphModel.VariableDeclarations.Count);
        }

        [UnityTest]
        public IEnumerator CopyPasteVariableOtherIncompatibleGraph()
        {
            yield return Start();

            var variableModel = GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "variable", ModifierFlags.ReadWrite, true);

            BlackboardView.Dispatch(new ClearSelectionCommand());

            BlackboardView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, variableModel));
            yield return null;

            Window.Focus();

            BlackboardView.Focus();
            BlackboardView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Copy));
            yield return null;

            BlackboardView.Focus();
            BlackboardView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Copy));
            yield return null;

            yield return MakeOtherSecondWindow();

            m_SecondWindow.Focus();

            SecondWindowBlackboardView.Focus();
            SecondWindowBlackboardView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            SecondWindowBlackboardView.Focus();
            SecondWindowBlackboardView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Paste));
            yield return null;

            Assert.AreEqual(0, SecondGraphModel.VariableDeclarations.Count);
        }

        [UnityTest]
        public IEnumerator SerializedDataForCopiedNodeIsOnlyNode()
        {
            yield return Start();

            GraphModel.DeleteNodes(GraphModel.NodeModels, true);
            var node = GraphModel.CreateNode<CopyPasteNodeModel>("My Node", Vector2.one, new SerializableGUID("42"),
                m =>
            {
                m.InputCount = 1;
                m.OutputCount = 2;
            });

            GraphView.Dispatch(new ClearSelectionCommand());

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, node));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ValidateCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Copy));
            yield return null;

            GraphView.Focus();
            GraphView.SendEvent(ExecuteCommandEvent.GetPooled(GraphViewStaticBridge.EventCommandNames.Copy));
            yield return null;

            var clipboard = GraphView.ViewSelection.Clipboard;

            // To update this value, run the test and paste the clipboard content here.
            var expected = @"application/vnd.unity.graphview.elements {
    ""nodes"": [
        {
            ""rid"": 1000
        }
    ],
    ""edges"": [],
    ""variableGroupPaths"": [],
    ""variableDeclarations"": [],
    ""implicitVariableDeclarations"": [],
    ""topLeftNodePosition"": {
        ""x"": 1.0,
        ""y"": 1.0
    },
    ""stickyNotes"": [],
    ""placemats"": [],
    ""references"": {
        ""version"": 2,
        ""RefIds"": [
            {
                ""rid"": 1000,
                ""type"": {
                    ""class"": ""CopyPasteTests/CopyPasteNodeModel"",
                    ""ns"": ""UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements"",
                    ""asm"": ""Unity.GraphTools.Foundation.Editor.Tests""
                },
                ""data"": {
                    ""m_Guid"": {
                        ""m_Value0"": 66,
                        ""m_Value1"": 0
                    },
                    ""m_Color"": {
                        ""r"": 0.0,
                        ""g"": 0.0,
                        ""b"": 0.0,
                        ""a"": 0.0
                    },
                    ""m_HasUserColor"": false,
                    ""m_Version"": 2,
                    ""m_Position"": {
                        ""x"": 1.0,
                        ""y"": 1.0
                    },
                    ""m_Title"": ""My Node"",
                    ""m_Tooltip"": """",
                    ""m_InputConstantsById"": {
                        ""m_KeyList"": [],
                        ""m_ValueList"": []
                    },
                    ""m_State"": 0,
                    ""m_Collapsed"": false,
                    ""inputCount"": 1,
                    ""outputCount"": 2
                }
            }
        ]
    }
}";

            Assert.AreEqual(expected, clipboard);
        }
    }
}
