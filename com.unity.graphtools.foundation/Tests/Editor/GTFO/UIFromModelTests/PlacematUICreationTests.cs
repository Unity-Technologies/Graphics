using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    public class PlacematUICreationTests : GtfTestFixture
    {
        [Test]
        public void PlacematHasExpectedParts()
        {
            var placematModel = GraphTool.ToolState.GraphModel.CreatePlacemat(Rect.zero);
            var placemat = new Placemat();
            placemat.SetupBuildAndUpdate(placematModel, GraphView);

            Assert.IsNotNull(placemat.SafeQ<VisualElement>(Placemat.titleContainerPartName));
            Assert.IsNotNull(placemat.SafeQ<VisualElement>(Placemat.collapseButtonPartName));
            Assert.IsNotNull(placemat.SafeQ<VisualElement>(Placemat.resizerPartName));
        }
    }
}
