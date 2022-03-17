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
        [UnityTest]
        public IEnumerator ChangingZoomLevelExecutesViewTransformedDelegate()
        {
            float minZoomScale = 0.1f;
            float maxZoomScale = 3;

            GraphView.SetupZoom(minZoomScale, maxZoomScale, 1.0f);
            yield return null;

            Vector3 sOrig = GraphView.ContentViewContainer.transform.scale;
            Helpers.ScrollWheelEvent(10.0f, GraphView.worldBound.center);
            yield return null;

            Vector3 s = GraphView.ContentViewContainer.transform.scale;
            Assert.AreNotEqual(sOrig, s);
        }

        [UnityTest]
        public IEnumerator ChangingGraphViewTransformRoundsToPixelGrid()
        {
            var pos = new Vector3(10.3f, 10.6f, 10.0f);
            GraphView.Dispatch(new ReframeGraphViewCommand(pos, new Vector3(10, 10)));
            yield return null;

            Vector3 p = GraphView.ContentViewContainer.transform.position;
            Assert.AreEqual(new Vector3(GraphViewStaticBridge.RoundToPixelGrid(pos.x), GraphViewStaticBridge.RoundToPixelGrid(pos.y), 10.0f), p);
        }
    }
}
