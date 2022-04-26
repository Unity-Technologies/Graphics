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

        protected virtual Type GetWindowType()
        {
            return WithSidePanel ? typeof(UITestWindowWithSidePanel) : typeof(UITestWindow);
        }

        protected abstract bool CreateGraphOnStartup { get; }
        protected virtual Type CreatedGraphType => typeof(ClassStencil);

        protected virtual bool WithSidePanel => false;

        protected virtual bool WithOverlays => false;

        protected virtual bool EnablePanning => true;

        // store PanSpeed to restore it after tests
        static float s_OriginalPanSpeed;

        [SetUp]
        public virtual void SetUp()
        {
            var windowType = GetWindowType();
            Window = EditorWindow.GetWindowWithRect(windowType, k_WindowRect) as UITestWindow;
            Assert.IsNotNull(Window);

            if (!WithOverlays)
            {
                Window.CloseAllOverlays();
            }

            GraphView = Window.GraphView;
            Helpers = new TestEventHelpers(Window);

            if (CreateGraphOnStartup)
            {
                var graphAsset = GraphAssetCreationHelpers<ClassGraphAsset>.CreateInMemoryGraphAsset(CreatedGraphType, "Test");
                GraphView.Dispatch(new LoadGraphCommand(graphAsset.GraphModel));
                // Complete the graph loading.
                GraphTool.Update();
            }

            Vector3 frameTranslation = Vector3.zero;
            Vector3 frameScaling = Vector3.one;
            GraphView.Dispatch(new ReframeGraphViewCommand(frameTranslation, frameScaling));

            s_OriginalPanSpeed = GraphView.basePanSpeed;
            if (!EnablePanning)
            {
                GraphView.basePanSpeed = 0f;
            }
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
            GraphView.basePanSpeed = s_OriginalPanSpeed;
        }

        protected IList<GraphElement> GetGraphElements()
        {
            return ((IGraphElementContainer) GraphModel).GraphElementModels.GetAllViewsInList(GraphView, null, new List<ModelView>()).OfType<GraphElement>().ToList();
        }

        protected GraphElement GetGraphElement(int index)
        {
            return GetGraphElements()[index];
        }

        protected void MarkGraphModelStateDirty()
        {
            using (var updater = GraphView.GraphViewModel.GraphModelState.UpdateScope)
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
                    BaseFixtureHelpers.AssumePreviousTest(checkReqs);

                    currentFrame = 0;
                    while (doUndoableStuff(currentFrame++) == TestPhase.WaitForNextFrame)
                        yield return null;

                    yield return WaitFrames();

                    checkPostReqs();
                    break;

                case TestingMode.UndoRedo:
                    Undo.ClearAll();

                    Undo.IncrementCurrentGroup();
                    BaseFixtureHelpers.AssumePreviousTest(checkReqs);

                    currentFrame = 0;
                    while (doUndoableStuff(currentFrame++) == TestPhase.WaitForNextFrame)
                        yield return null;

                    Undo.IncrementCurrentGroup();

                    yield return WaitFrames();

                    BaseFixtureHelpers.AssumePreviousTest(checkPostReqs);

                    yield return WaitFrames();

                    Undo.PerformUndo();

                    yield return WaitFrames();

                    BaseFixtureHelpers.AssertPreviousTest(checkReqs);

                    Undo.PerformRedo();

                    yield return WaitFrames();

                    BaseFixtureHelpers.AssertPreviousTest(checkPostReqs);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
