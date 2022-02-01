using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class GraphViewDelegatesTests : GraphViewTester
    {
        class TestGraphElement : GraphElement
        {
            public TestGraphElement(GraphView graphView)
            {
                View = graphView;
                style.backgroundColor = Color.red;
                MinimapColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            }
        }

        [UnityTest]
        public IEnumerator ChangingZoomLevelExecutesViewTransformedDelegate()
        {
            float minZoomScale = 0.1f;
            float maxZoomScale = 3;

            graphView.SetupZoom(minZoomScale, maxZoomScale, 1.0f);
            yield return null;

            Vector3 sOrig = graphView.ContentViewContainer.transform.scale;
            helpers.ScrollWheelEvent(10.0f, graphView.worldBound.center);
            yield return null;

            Vector3 s = graphView.ContentViewContainer.transform.scale;
            Assert.AreNotEqual(sOrig, s);
        }

        [UnityTest]
        public IEnumerator ChangingGraphViewTransformRoundsToPixelGrid()
        {
            var pos = new Vector3(10.3f, 10.6f, 10.0f);
            graphView.Dispatch(new ReframeGraphViewCommand(pos, new Vector3(10, 10)));
            yield return null;

            Vector3 p = graphView.ContentViewContainer.transform.position;
            Assert.AreEqual(new Vector3(GraphViewStaticBridge.RoundToPixelGrid(pos.x), GraphViewStaticBridge.RoundToPixelGrid(pos.y), 10.0f), p);
        }
    }
}
