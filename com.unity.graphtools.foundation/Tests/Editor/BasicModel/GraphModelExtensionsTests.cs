using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.BasicModelTests
{
    [TestFixture]
    public class GraphModelExtensionsTests
    {
        IGraphAssetModel m_GraphAsset;
        IGraphModel m_GraphModel;
        IPlacematModel m_Model0;
        IPlacematModel m_Model1;
        IPlacematModel m_Model2;
        IPlacematModel m_Model3;
        IPlacematModel m_Model4;
        IPlacematModel m_Model5;
        IPlacematModel m_Model6;
        IPlacematModel m_Model7;
        IPlacematModel m_Model8;
        IPlacematModel m_Model9;

        [SetUp]
        public void SetUp()
        {
            m_GraphAsset = GraphAssetCreationHelpers<TestGraphAssetModel>.CreateInMemoryGraphAsset(typeof(ClassStencil), "Test");
            m_GraphAsset.CreateGraph("Graph");
            m_GraphModel = m_GraphAsset.GraphModel;

            var rect = new Rect(0, 0, 100, 100);
            m_Model0 = m_GraphModel.CreatePlacemat(rect); m_Model0.Title = "0";
            m_Model1 = m_GraphModel.CreatePlacemat(rect); m_Model1.Title = "1";
            m_Model2 = m_GraphModel.CreatePlacemat(rect); m_Model2.Title = "2";
            m_Model3 = m_GraphModel.CreatePlacemat(rect); m_Model3.Title = "3";
            m_Model4 = m_GraphModel.CreatePlacemat(rect); m_Model4.Title = "4";
            m_Model5 = m_GraphModel.CreatePlacemat(rect); m_Model5.Title = "5";
            m_Model6 = m_GraphModel.CreatePlacemat(rect); m_Model6.Title = "6";
            m_Model7 = m_GraphModel.CreatePlacemat(rect); m_Model7.Title = "7";
            m_Model8 = m_GraphModel.CreatePlacemat(rect); m_Model8.Title = "8";
            m_Model9 = m_GraphModel.CreatePlacemat(rect); m_Model9.Title = "9";
        }

        // Move FORWARD tests
        [Test]
        public void MoveTopElementForwardDoesNothing()
        {
            m_GraphModel.MoveForward(new[] {m_Model9});
            Assert.AreEqual(m_GraphModel.PlacematModels,
                new[] { m_Model0, m_Model1, m_Model2, m_Model3, m_Model4, m_Model5, m_Model6, m_Model7, m_Model8, m_Model9 }, "Unexpected placemat order after moving top element forward.");
        }

        [Test]
        public void MoveTopElementTopDoesNothing()
        {
            m_GraphModel.MoveForward(new[] {m_Model9}, true);
            Assert.AreEqual(m_GraphModel.PlacematModels,
                new[] { m_Model0, m_Model1, m_Model2, m_Model3, m_Model4, m_Model5, m_Model6, m_Model7, m_Model8, m_Model9 }, "Unexpected placemat order after moving top element top.");
        }

        [Test]
        public void MoveSingleElementForwardWorks()
        {
            m_GraphModel.MoveForward(new[] {m_Model4});
            Assert.AreEqual(m_GraphModel.PlacematModels,
                new[] { m_Model0, m_Model1, m_Model2, m_Model3, m_Model5, m_Model4, m_Model6, m_Model7, m_Model8, m_Model9 }, "Unexpected placemat order after moving forward middle element.");
            //                                                         \---------^  Models have switched

            m_GraphModel.MoveForward(new[] {m_Model8});
            Assert.AreEqual(m_GraphModel.PlacematModels,
                new[] { m_Model0, m_Model1, m_Model2, m_Model3, m_Model5, m_Model4, m_Model6, m_Model7, m_Model9, m_Model8 }, "Unexpected placemat order after moving forward second-to-last element.");
            //                                                                                                 \---------^  Models have switched

            m_GraphModel.MoveForward(new[] {m_Model0});
            Assert.AreEqual(m_GraphModel.PlacematModels,
                new[] { m_Model1, m_Model0, m_Model2, m_Model3, m_Model5, m_Model4, m_Model6, m_Model7, m_Model9, m_Model8 }, "Unexpected placemat order after moving forward first element.");
            //                 \---------^  Models have switched
        }

        [Test]
        public void MoveSingleElementTopWorks()
        {
            m_GraphModel.MoveForward(new[] {m_Model4}, true);
            Assert.AreEqual(m_GraphModel.PlacematModels,
                new[] { m_Model0, m_Model1, m_Model2, m_Model3, m_Model5, m_Model6, m_Model7, m_Model8, m_Model9, m_Model4 }, "Unexpected placemat order after moving middle element to top.");
            //                                                         \-------------->>>--------------------------------^  Model went to top

            m_GraphModel.MoveForward(new[] {m_Model9}, true);
            Assert.AreEqual(m_GraphModel.PlacematModels,
                new[] { m_Model0, m_Model1, m_Model2, m_Model3, m_Model5, m_Model6, m_Model7, m_Model8, m_Model4, m_Model9 }, "Unexpected placemat order after moving second-to-last element to top.");
            //                                                                                                 \---------^  Model went to top

            m_GraphModel.MoveForward(new[] {m_Model0}, true);
            Assert.AreEqual(m_GraphModel.PlacematModels,
                new[] { m_Model1, m_Model2, m_Model3, m_Model5, m_Model6, m_Model7, m_Model8, m_Model4, m_Model9, m_Model0 }, "Unexpected placemat order after moving first element to top.");
            //                 \-----------------------------------------------------------------------------------------^  Model went to top
        }

        [Test]
        public void MoveMultipleContiguousTopElementsForwardDoesNothing()
        {
            var groupToMove = new[] { m_Model7, m_Model8, m_Model9 };
            m_GraphModel.MoveForward(groupToMove);
            Assert.AreEqual(m_GraphModel.PlacematModels,
                new[] { m_Model0, m_Model1, m_Model2, m_Model3, m_Model4, m_Model5, m_Model6, m_Model7, m_Model8, m_Model9 }, "Unexpected placemat order after moving forward multiple contiguous top elements.");
        }

        [Test]
        public void MoveMultipleContiguousElementsForwardWorks()
        {
            var groupToMove = new[] { m_Model5, m_Model4, m_Model6 }; // Order of the items passed in does not influence where their order in the final list.
            m_GraphModel.MoveForward(groupToMove);
            Assert.AreEqual(m_GraphModel.PlacematModels,
                new[] { m_Model0, m_Model1, m_Model2, m_Model3, m_Model7, m_Model4, m_Model5, m_Model6, m_Model8, m_Model9 }, "Unexpected placemat order after moving forward multiple contiguous middle elements.");
            //                                                                  ^^        ^^        ^^   Models moved up
        }

        [Test]
        public void MoveMultipleContiguousElementsTopWorks()
        {
            var groupToMove = new[] { m_Model4, m_Model5, m_Model6 };
            m_GraphModel.MoveForward(groupToMove, true);
            Assert.AreEqual(m_GraphModel.PlacematModels,
                new[] { m_Model0, m_Model1, m_Model2, m_Model3, m_Model7, m_Model8, m_Model9, m_Model4, m_Model5, m_Model6 }, "Unexpected placemat order after moving top multiple contiguous middle elements.");
            //                                                                                      ^^        ^^        ^^   Models moved top
        }

        [Test]
        public void MoveMultipleNonContiguousElementsForwardWorks()
        {
            var groupToMove = new[] { m_Model4, m_Model6, m_Model8 };
            m_GraphModel.MoveForward(groupToMove);
            Assert.AreEqual(m_GraphModel.PlacematModels,
                new[] { m_Model0, m_Model1, m_Model2, m_Model3, m_Model5, m_Model4, m_Model7, m_Model6, m_Model9, m_Model8 }, "Unexpected placemat order after moving forward multiple non contiguous elements.");
            //                                                                  ^^                  ^^                  ^^   Models moved up

            // Moving a second time will "compact" at the top
            m_GraphModel.MoveForward(groupToMove);
            Assert.AreEqual(m_GraphModel.PlacematModels,
                new[] { m_Model0, m_Model1, m_Model2, m_Model3, m_Model5, m_Model7, m_Model4, m_Model9, m_Model6, m_Model8 }, "Unexpected placemat order after moving forward multiple non contiguous elements a second time.");
            //                                                                            ^^                  ^^             Models moved up

            // Moving a third time will "compact" even more at the top
            m_GraphModel.MoveForward(groupToMove);
            Assert.AreEqual(m_GraphModel.PlacematModels,
                new[] { m_Model0, m_Model1, m_Model2, m_Model3, m_Model5, m_Model7, m_Model9, m_Model4, m_Model6, m_Model8 }, "Unexpected placemat order after moving forward multiple non contiguous elements a third time.");
            //                                                                                      ^^                       Model moved up
        }

        [Test]
        public void MoveMultipleNonContiguousElementsTopWorks()
        {
            var groupToMove = new[] { m_Model4, m_Model6, m_Model8 };
            m_GraphModel.MoveForward(groupToMove, true);
            Assert.AreEqual(m_GraphModel.PlacematModels,
                new[] { m_Model0, m_Model1, m_Model2, m_Model3, m_Model5, m_Model7, m_Model9, m_Model4, m_Model6, m_Model8 }, "Unexpected placemat order after moving top multiple non contiguous elements.");
            //                                                                                      ^^        ^^        ^^   Models moved top
        }

        // Move BACKWARD tests
        [Test]
        public void MoveBottomElementBackwardDoesNothing()
        {
            m_GraphModel.MoveBackward(new[] {m_Model0});
            Assert.AreEqual(m_GraphModel.PlacematModels,
                new[] { m_Model0, m_Model1, m_Model2, m_Model3, m_Model4, m_Model5, m_Model6, m_Model7, m_Model8, m_Model9 }, "Unexpected placemat order after moving bottom element backward.");
        }

        [Test]
        public void MoveBottomElementBottomDoesNothing()
        {
            m_GraphModel.MoveBackward(new[] {m_Model0}, true);
            Assert.AreEqual(m_GraphModel.PlacematModels,
                new[] { m_Model0, m_Model1, m_Model2, m_Model3, m_Model4, m_Model5, m_Model6, m_Model7, m_Model8, m_Model9 }, "Unexpected placemat order after moving bottom element bottom.");
        }

        [Test]
        public void MoveSingleElementBackwardWorks()
        {
            m_GraphModel.MoveBackward(new[] {m_Model4});
            Assert.AreEqual(m_GraphModel.PlacematModels,
                new[] { m_Model0, m_Model1, m_Model2, m_Model4, m_Model3, m_Model5, m_Model6, m_Model7, m_Model8, m_Model9 }, "Unexpected placemat order after moving backward middle element.");
            //                                               ^---------/  Models have switched

            m_GraphModel.MoveBackward(new[] {m_Model1});
            Assert.AreEqual(m_GraphModel.PlacematModels,
                new[] { m_Model1, m_Model0, m_Model2, m_Model4, m_Model3, m_Model5, m_Model6, m_Model7, m_Model8, m_Model9 }, "Unexpected placemat order after moving backward second element.");
            //                 ^---------/  Models have switched

            m_GraphModel.MoveBackward(new[] {m_Model9});
            Assert.AreEqual(m_GraphModel.PlacematModels,
                new[] { m_Model1, m_Model0, m_Model2, m_Model4, m_Model3, m_Model5, m_Model6, m_Model7, m_Model9, m_Model8 }, "Unexpected placemat order after moving backward last element.");
            //                                                                                                 ^---------/  Models have switched
        }

        [Test]
        public void MoveSingleElementBottomWorks()
        {
            m_GraphModel.MoveBackward(new[] {m_Model4}, true);
            Assert.AreEqual(m_GraphModel.PlacematModels,
                new[] { m_Model4, m_Model0, m_Model1, m_Model2, m_Model3, m_Model5, m_Model6, m_Model7, m_Model8, m_Model9 }, "Unexpected placemat order after moving middle element to bottom.");
            //                 ^--------------<<<--------------------------------/  Model went to bottom

            m_GraphModel.MoveBackward(new[] {m_Model0}, true);
            Assert.AreEqual(m_GraphModel.PlacematModels,
                new[] { m_Model0, m_Model4, m_Model1, m_Model2, m_Model3, m_Model5, m_Model6, m_Model7, m_Model8, m_Model9 }, "Unexpected placemat order after moving second element to bottom.");
            //                 \---------^  Model went to bottom

            m_GraphModel.MoveBackward(new[] {m_Model9}, true);
            Assert.AreEqual(m_GraphModel.PlacematModels,
                new[] { m_Model9, m_Model0, m_Model4, m_Model1, m_Model2, m_Model3, m_Model5, m_Model6, m_Model7, m_Model8 }, "Unexpected placemat order after moving last element to bottom.");
            //                 ^-----------------------------------------------------------------------------------------/  Model went to bottom
        }

        [Test]
        public void MoveMultipleContiguousBottomElementsBackwardDoesNothing()
        {
            var groupToMove = new[] { m_Model0, m_Model1, m_Model2 };
            m_GraphModel.MoveBackward(groupToMove);
            Assert.AreEqual(m_GraphModel.PlacematModels,
                new[] { m_Model0, m_Model1, m_Model2, m_Model3, m_Model4, m_Model5, m_Model6, m_Model7, m_Model8, m_Model9 }, "Unexpected placemat order after moving backward multiple contiguous bottom elements.");
        }

        [Test]
        public void MoveMultipleContiguousElementsBackwardWorks()
        {
            var groupToMove = new[] { m_Model5, m_Model4, m_Model6 };  // Order of the items passed in does not influence where their order in the final list.
            m_GraphModel.MoveBackward(groupToMove);
            Assert.AreEqual(m_GraphModel.PlacematModels,
                new[] { m_Model0, m_Model1, m_Model2, m_Model4, m_Model5, m_Model6, m_Model3, m_Model7, m_Model8, m_Model9 }, "Unexpected placemat order after moving backward multiple contiguous middle elements.");
            //                                              ^^        ^^        ^^   Models moved down
        }

        [Test]
        public void MoveMultipleContiguousElementsBottomWorks()
        {
            var groupToMove = new[] { m_Model4, m_Model5, m_Model6 };
            m_GraphModel.MoveBackward(groupToMove, true);
            Assert.AreEqual(m_GraphModel.PlacematModels,
                new[] { m_Model4, m_Model5, m_Model6, m_Model0, m_Model1, m_Model2, m_Model3, m_Model7, m_Model8, m_Model9 }, "Unexpected placemat order after moving bottom multiple contiguous middle elements.");
            //                ^^        ^^        ^^   Models moved bottom
        }

        [Test]
        public void MoveMultipleNonContiguousElementsBackwardWorks()
        {
            var groupToMove = new[] { m_Model1, m_Model3, m_Model5 };
            m_GraphModel.MoveBackward(groupToMove);
            Assert.AreEqual(m_GraphModel.PlacematModels,
                new[] { m_Model1, m_Model0, m_Model3, m_Model2, m_Model5, m_Model4, m_Model6, m_Model7, m_Model8, m_Model9 }, "Unexpected placemat order after moving backward multiple non contiguous elements.");
            //                ^^                  ^^                  ^^   Models moved down

            // Moving a second time will "compact" at the bottom
            m_GraphModel.MoveBackward(groupToMove);
            Assert.AreEqual(m_GraphModel.PlacematModels,
                new[] { m_Model1, m_Model3, m_Model0, m_Model5, m_Model2, m_Model4, m_Model6, m_Model7, m_Model8, m_Model9 }, "Unexpected placemat order after moving backward multiple non contiguous elements a second time.");
            //                          ^^                  ^^             Models moved down

            // Moving a third time will "compact" even more at the bottom
            m_GraphModel.MoveBackward(groupToMove);
            Assert.AreEqual(m_GraphModel.PlacematModels,
                new[] { m_Model1, m_Model3, m_Model5, m_Model0, m_Model2, m_Model4, m_Model6, m_Model7, m_Model8, m_Model9 }, "Unexpected placemat order after moving backward multiple non contiguous elements a third time.");
            //                                    ^^                       Model moved down
        }

        [Test]
        public void MoveMultipleNonContiguousElementsBottomWorks()
        {
            var groupToMove = new[] { m_Model4, m_Model6, m_Model8 };
            m_GraphModel.MoveBackward(groupToMove, true);
            Assert.AreEqual(m_GraphModel.PlacematModels,
                new[] { m_Model4, m_Model6, m_Model8, m_Model0, m_Model1, m_Model2, m_Model3, m_Model5, m_Model7, m_Model9 }, "Unexpected placemat order after moving bottom multiple non contiguous elements.");
            //                ^^        ^^        ^^   Models moved bottom
        }
    }
}
