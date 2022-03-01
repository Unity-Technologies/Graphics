using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI
{
    [PublicAPI]
    [Category("UI")]
    public abstract class BaseUIFixture
    {
        protected const string k_GraphPath = "Assets/test.asset";
        const int k_PanAreaWidth = 100;
        static readonly Rect k_WindowRect = new Rect(Vector2.zero, new Vector2(k_PanAreaWidth * 8, k_PanAreaWidth * 6));
        protected UITestWindow Window { get; set; }
        protected GraphView GraphView { get; private set; }
        protected TestEventHelpers Helpers { get; set; }
        protected BaseGraphTool GraphTool => Window.GraphTool;
        protected GraphModel GraphModel => (GraphModel)GraphTool?.ToolState.GraphModel;

        protected abstract bool CreateGraphOnStartup { get; }
        protected virtual Type CreatedGraphType => typeof(ClassStencil);

        [SetUp]
        public virtual void SetUp()
        {
            Window = EditorWindow.GetWindowWithRect<UITestWindow>(k_WindowRect);
            GraphView = Window.GraphView;
            Helpers = new TestEventHelpers(Window);

            if (CreateGraphOnStartup)
            {
                var graphAsset = GraphAssetCreationHelpers<ClassGraphAssetModel>.CreateInMemoryGraphAsset(CreatedGraphType, "Test");
                GraphView.Dispatch(new LoadGraphAssetCommand(graphAsset));
                // Complete the graph loading.
                GraphTool.Update();
            }

            Vector3 frameTranslation = Vector3.zero;
            Vector3 frameScaling = Vector3.one;
            GraphView.Dispatch(new ReframeGraphViewCommand(frameTranslation, frameScaling));
        }

        [TearDown]
        public virtual void TearDown()
        {
            // See case: https://fogbugz.unity3d.com/f/cases/998343/
            // Clearing the capture needs to happen before closing the window
            MouseCaptureController.ReleaseMouse();
            if (Window != null)
            {
                GraphModel?.QuickCleanup();
                Window.Close();
            }
        }

        protected IList<GraphElement> GetGraphElements()
        {
            return ((IGraphElementContainer) GraphModel).GraphElementModels.GetAllUIsInList(GraphView, null, new List<ModelUI>()).OfType<GraphElement>().ToList();
        }

        protected GraphElement GetGraphElement(int index)
        {
            return GetGraphElements()[index];
        }

        protected void MarkGraphViewStateDirty()
        {
            using (var updater = GraphView.GraphViewState.UpdateScope)
            {
                updater.ForceCompleteUpdate();
            }
        }

        IReadOnlyList<INodeModel> GetNodeModels()
        {
            return GraphModel.NodeModels;
        }

        protected INodeModel GetNodeModel(int index)
        {
            return GetNodeModels()[index];
        }

        public enum TestPhase
        {
            WaitForNextFrame,
            Done,
        }
        protected IEnumerator TestPrereqCommandPostreq(TestingMode mode, Action checkReqs, Func<int, TestPhase> doUndoableStuff, Action checkPostReqs, int framesToWait = 1)
        {
            yield return null;

            IEnumerator WaitFrames()
            {
                for (int i = 0; i < framesToWait; ++i)
                    yield return null;
            }

            int currentFrame;
            switch (mode)
            {
                case TestingMode.Command:
                    BaseFixture.AssumePreviousTest(checkReqs);

                    currentFrame = 0;
                    while (doUndoableStuff(currentFrame++) == TestPhase.WaitForNextFrame)
                        yield return null;

                    yield return WaitFrames();

                    checkPostReqs();
                    break;

                case TestingMode.UndoRedo:
                    Undo.ClearAll();

                    Undo.IncrementCurrentGroup();
                    BaseFixture.AssumePreviousTest(checkReqs);

                    currentFrame = 0;
                    while (doUndoableStuff(currentFrame++) == TestPhase.WaitForNextFrame)
                        yield return null;

                    Undo.IncrementCurrentGroup();

                    yield return WaitFrames();

                    BaseFixture.AssumePreviousTest(checkPostReqs);

                    yield return WaitFrames();

                    Undo.PerformUndo();

                    yield return WaitFrames();

                    BaseFixture.AssertPreviousTest(checkReqs);

                    Undo.PerformRedo();

                    yield return WaitFrames();

                    BaseFixture.AssertPreviousTest(checkPostReqs);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
