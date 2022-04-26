using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.Profiling;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    public enum TestingMode { Command, UndoRedo }

    static class BaseFixtureHelpers
    {
        public static void AssumePreviousTest(Action delegateToRun)
        {
            try
            {
                delegateToRun();
            }
            catch (AssertionException e)
            {
                var inconclusiveException = new InconclusiveException(e.Message + "Delegate stack trace:\n" + e.StackTrace, e);
                throw inconclusiveException;
            }
        }

        public static void AssertPreviousTest(Action delegateToRun)
        {
            try
            {
                delegateToRun();
            }
            catch (InconclusiveException e)
            {
                var inconclusiveException = new AssertionException(e.Message + "Delegate stack trace:\n" + e.StackTrace, e);
                throw inconclusiveException;
            }
        }
    }

    [PublicAPI]
    public abstract class BaseFixture<TGraphTool> where TGraphTool : NoUITestGraphTool, new()
    {
        protected const string k_GraphPath = "Assets/test.asset";

        protected TGraphTool GraphTool { get; set; }

        protected IGraphModel GraphModel => GraphTool.ToolState.GraphModel;
        protected Preferences Preferences => GraphTool.Preferences;
        protected Stencil Stencil => (Stencil)GraphModel.Stencil;

        protected abstract bool CreateGraphOnStartup { get; }
        protected virtual Type CreatedGraphType => typeof(ClassStencil);

        protected void TestPrereqCommandPostreq<T>(TestingMode mode, Action checkReqs, Func<T> provideCommand, Action checkPostReqs)
            where T : UndoableCommand
        {
            T command;
            switch (mode)
            {
                case TestingMode.Command:
                    checkReqs();
                    command = provideCommand();
                    GraphTool.Dispatch(command);

                    checkPostReqs();
                    break;
                case TestingMode.UndoRedo:
                    var asset = GraphTool.ToolState.CurrentGraph.GetGraphAsset();

                    MockSaveGraphAsset(asset);

                    Undo.IncrementCurrentGroup();

                    BaseFixtureHelpers.AssumePreviousTest(() =>
                    {
                        checkReqs();
                        command = provideCommand();
                        GraphTool.Dispatch(command);
                        checkPostReqs();
                    });

                    Undo.IncrementCurrentGroup();

                    Undo.PerformUndo();

                    CheckUndo(checkReqs, provideCommand, asset);

                    MockSaveGraphAsset(asset);

                    Undo.PerformRedo();
                    CheckRedo(checkPostReqs, asset);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        static void CheckRedo(Action checkPostReqs, IGraphAsset asset)
        {
            BaseFixtureHelpers.AssertPreviousTest(checkPostReqs);

            if (asset != null)
                Assert.IsTrue(asset.Dirty);
        }

        static void MockSaveGraphAsset(IGraphAsset asset)
        {
            if (asset == null)
                return;

            asset.Dirty = false;
            Assert.IsFalse(asset.Dirty);
        }

        void CheckUndo<T>(Action checkReqs, Func<T> provideCommand, IGraphAsset asset) where T : UndoableCommand
        {
            BaseFixtureHelpers.AssertPreviousTest(checkReqs);
            BaseFixtureHelpers.AssertPreviousTest(() => provideCommand());

            if (asset != null)
                Assert.IsTrue(asset.Dirty);
        }

        protected void TestPrereqCommandPostreq<T>(TestingMode mode, Func<T> checkReqsAndProvideCommand, Action checkPostReqs) where T : UndoableCommand
        {
            TestPrereqCommandPostreq(mode, () => {}, checkReqsAndProvideCommand, checkPostReqs);
        }

        [SetUp]
        public virtual void SetUp()
        {
            GraphTool = CsoTool.Create<TGraphTool>(new Hash128());
            Preferences.SetBoolNoEditorUpdate(BoolPref.ErrorOnRecursiveDispatch, false);

            GraphTool.Dispatcher.CheckIntegrity =
                () => GraphModel == null || GraphModel.CheckIntegrity(Verbosity.Errors);

            if (CreateGraphOnStartup)
            {
                var graphAsset = GraphAssetCreationHelpers<ClassGraphAsset>.CreateInMemoryGraphAsset(CreatedGraphType, "Test");
                GraphTool.Dispatch(new LoadGraphCommand(graphAsset.GraphModel));
                GraphTool.Update();
                AssumeIntegrity();
            }
        }

        [TearDown]
        public virtual void TearDown()
        {
            UnloadGraph();
            GraphTool.Dispose();
            GraphTool = null;
            Profiler.enabled = false;

            AssetDatabase.DeleteAsset(k_GraphPath);
        }

        void UnloadGraph()
        {
            GraphTool.Dispatch(new UnloadGraphCommand());
        }

        protected void AssertIntegrity()
        {
            if (GraphModel != null)
                Assert.IsTrue(GraphModel.CheckIntegrity(Verbosity.Errors));
        }

        protected void AssumeIntegrity()
        {
            if (GraphModel != null)
                Assume.That(GraphModel.CheckIntegrity(Verbosity.Errors));
        }

        protected IEnumerable<INodeModel> GetAllNodes()
        {
            return GraphModel.NodeModels;
        }

        protected INodeModel GetNode(int index)
        {
            return GetAllNodes().ElementAt(index);
        }

        protected IConstant GetConstantNode(int index)
        {
            return (GetAllNodes().ElementAt(index) as ConstantNodeModel)?.Value;
        }

        protected int GetNodeCount()
        {
            return GraphModel.NodeModels.Count;
        }

        protected IEnumerable<IEdgeModel> GetAllEdges()
        {
            return GraphModel.EdgeModels;
        }

        protected IEdgeModel GetEdge(int index)
        {
            return GetAllEdges().ElementAt(index);
        }

        protected int GetEdgeCount()
        {
            return GetAllEdges().Count();
        }

        protected IEnumerable<IVariableDeclarationModel> GetAllVariableDeclarations()
        {
            return GraphModel.VariableDeclarations;
        }

        protected IVariableDeclarationModel GetVariableDeclaration(int index)
        {
            return GetAllVariableDeclarations().ElementAt(index);
        }

        protected IGroupItemModel GetSectionItem(int index)
        {
            return GraphModel.GetSectionModel(GraphModel.Stencil.SectionNames.First()).Items.ElementAt(index);
        }

        protected int GetVariableDeclarationCount()
        {
            return GetAllVariableDeclarations().Count();
        }

        protected IEnumerable<IStickyNoteModel> GetAllStickyNotes()
        {
            return GraphModel.StickyNoteModels;
        }

        protected IStickyNoteModel GetStickyNote(int index)
        {
            return GetAllStickyNotes().ElementAt(index);
        }

        protected int GetStickyNoteCount()
        {
            return GetAllStickyNotes().Count();
        }

        protected IEnumerable<IPlacematModel> GetAllPlacemats()
        {
            return GraphModel.PlacematModels;
        }

        protected IPlacematModel GetPlacemat(int index)
        {
            return GetAllPlacemats().ElementAt(index);
        }

        protected IVariableDeclarationModel GetGraphVariableDeclaration(string fieldName)
        {
            return GraphModel.VariableDeclarations.Single(f => f.DisplayTitle == fieldName);
        }

        protected void AddUsage(IVariableDeclarationModel fieldModel)
        {
            int prevCount = GetFloatingVariableModels(GraphModel).Count();
            GraphTool.Dispatch(CreateNodeCommand.OnGraph(fieldModel, Vector2.one));
            Assume.That(GetFloatingVariableModels(GraphModel).Count(), Is.EqualTo(prevCount + 1));
        }

        protected IVariableNodeModel GetGraphVariableUsage(string fieldName)
        {
            return GetFloatingVariableModels(GraphModel).First(f => f.Title == fieldName);
        }

        protected IVariableDeclarationModel CreateGraphVariableDeclaration(string fieldName, Type type)
        {
            int prevCount = GraphModel.VariableDeclarations.Count();

            GraphTool.Dispatch(new CreateGraphVariableDeclarationCommand(fieldName, false, type.GenerateTypeHandle()));

            Assert.AreEqual(prevCount + 1, GraphModel.VariableDeclarations.Count());
            IVariableDeclarationModel decl = GetGraphVariableDeclaration(fieldName);
            Assume.That(decl, Is.Not.Null);
            Assume.That(decl.DisplayTitle, Is.EqualTo(fieldName));
            return decl;
        }

        protected void EnableUndoRedoModificationsLogging()
        {
            GraphTool.Dispatcher.RegisterCommandPreDispatchCallback(a => Debug.Log("Command " + a.GetType().Name));
            // TODO : Undo.postprocessModifications += PostprocessModifications;
        }

        public IEnumerable<IVariableNodeModel> GetFloatingVariableModels(IGraphModel graphModel)
        {
            return graphModel.NodeModels.OfType<IVariableNodeModel>().Where(v => !v.OutputPort.IsConnected());
        }

        public void RefreshReference<T>(ref T model) where T : class, INodeModel
        {
            model = GraphModel.TryGetModelFromGuid(model.Guid, out T newOne) ? newOne : model;
        }

        public void RefreshReference(ref IEdgeModel model)
        {
            var orig = model;

            model = orig == null ? null : GraphModel.EdgeModels.FirstOrDefault(e =>
            {
                return e?.ToNodeGuid == orig.ToNodeGuid && e.FromNodeGuid == orig.FromNodeGuid && e.ToPortId == orig.ToPortId && e.FromPortId == orig.FromPortId;
            });
        }

        public void RefreshReference(ref IStickyNoteModel model)
        {
            var orig = model;

            model = orig == null ? null : GraphModel.StickyNoteModels.FirstOrDefault(m => m.Guid == orig.Guid);
        }

        protected void DebugLogAllEdges()
        {
            foreach (var edgeModel in GraphModel.EdgeModels)
            {
                Debug.Log(((IHasTitle)edgeModel.ToPort.NodeModel).Title + "<->" + ((IHasTitle)edgeModel.FromPort.NodeModel).Title);
            }
        }
    }
}
