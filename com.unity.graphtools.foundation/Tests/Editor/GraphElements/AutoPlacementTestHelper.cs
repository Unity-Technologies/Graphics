using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class AutoPlacementTestHelper : GraphViewTester
    {
        protected IInputOutputPortsNodeModel FirstNodeModel { get; set; }
        protected IInputOutputPortsNodeModel SecondNodeModel { get; set; }
        protected IInputOutputPortsNodeModel ThirdNodeModel { get; set; }
        protected IInputOutputPortsNodeModel FourthNodeModel { get; set; }
        protected IPlacematModel PlacematModel { get; private set; }
        protected IStickyNoteModel StickyNoteModel { get; private set; }

        protected Node m_FirstNode;
        protected Node m_SecondNode;
        protected Node m_ThirdNode;
        protected Node m_FourthNode;
        protected Placemat m_Placemat;
        protected StickyNote m_StickyNote;

        protected static readonly Vector2 k_SelectionOffset = new Vector2(50, 50);

        protected IEnumerator SetupElements(bool smallerSize, Vector2 firstNodePos, Vector2 secondNodePos, Vector2 placematPos, Vector2 stickyNotePos)
        {
            var actions = CreateElements(firstNodePos, secondNodePos, placematPos, stickyNotePos, smallerSize);
            while (actions.MoveNext())
            {
                yield return null;
            }

            SelectElements();
            yield return null;
        }

        protected IEnumerator CreateConnectedNodes(Vector2 firstNodePos, Vector2 secondNodePos, Vector2 thirdNodePos, Vector2 fourthNodePos, bool isVerticalPort)
        {
            var orientation = isVerticalPort ? PortOrientation.Vertical : PortOrientation.Horizontal;

            FirstNodeModel = CreateNode("Node1", firstNodePos, 0, 0, 0, 1, orientation);
            SecondNodeModel = CreateNode("Node2", secondNodePos, 0, 0, 0, 1, orientation);
            ThirdNodeModel = CreateNode("Node3", thirdNodePos, 0, 0, 1, 1, orientation);
            FourthNodeModel = CreateNode("Node4", fourthNodePos, 0, 0, 1, 0, orientation);

            MarkGraphViewStateDirty();
            yield return null;

            IPortModel outputPortFirstNode = FirstNodeModel.OutputsByDisplayOrder[0];
            IPortModel outputPortSecondNode = SecondNodeModel.OutputsByDisplayOrder[0];
            Assert.IsNotNull(outputPortFirstNode);
            Assert.IsNotNull(outputPortSecondNode);

            IPortModel intputPortThirdNode = ThirdNodeModel.InputsByDisplayOrder[0];
            IPortModel outputPortThirdNode = ThirdNodeModel.OutputsByDisplayOrder[0];
            Assert.IsNotNull(intputPortThirdNode);
            Assert.IsNotNull(outputPortThirdNode);

            IPortModel inputPortFourthNode = FourthNodeModel.InputsByDisplayOrder[0];
            Assert.IsNotNull(inputPortFourthNode);

            // Connect the ports together
            var actions = ConnectPorts(outputPortFirstNode, intputPortThirdNode);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = ConnectPorts(outputPortSecondNode, intputPortThirdNode);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = ConnectPorts(outputPortThirdNode, inputPortFourthNode);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // Get the UI nodes
            m_FirstNode = FirstNodeModel.GetView<Node>(GraphView);
            m_SecondNode = SecondNodeModel.GetView<Node>(GraphView);
            m_ThirdNode = ThirdNodeModel.GetView<Node>(GraphView);
            m_FourthNode = FourthNodeModel.GetView<Node>(GraphView);
            Assert.IsNotNull(m_FirstNode);
            Assert.IsNotNull(m_SecondNode);
            Assert.IsNotNull(m_ThirdNode);
            Assert.IsNotNull(m_FourthNode);
        }

        IEnumerator CreateElements(Vector2 firstNodePos, Vector2 secondNodePos, Vector2 placematPos, Vector2 stickyNotePos, bool smallerSize)
        {
            FirstNodeModel = CreateNode("Node1", firstNodePos);
            SecondNodeModel = CreateNode("Node2", secondNodePos);
            PlacematModel = CreatePlacemat(new Rect(placematPos, new Vector2(200, smallerSize ? 100 : 200)), "Placemat");
            StickyNoteModel = CreateSticky("Sticky", "", new Rect(stickyNotePos, smallerSize ? new Vector2(100, 100) : new Vector2(200, 200)));

            MarkGraphViewStateDirty();
            yield return null;

            // Get the UI elements
            m_FirstNode = FirstNodeModel.GetView<Node>(GraphView);
            m_SecondNode = SecondNodeModel.GetView<Node>(GraphView);
            m_Placemat = PlacematModel.GetView<Placemat>(GraphView);
            m_StickyNote = StickyNoteModel.GetView<StickyNote>(GraphView);
            Assert.IsNotNull(m_FirstNode);
            Assert.IsNotNull(m_SecondNode);
            Assert.IsNotNull(m_Placemat);
            Assert.IsNotNull(m_StickyNote);
        }

        protected void SelectConnectedNodes()
        {
            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, FirstNodeModel, SecondNodeModel, ThirdNodeModel, FourthNodeModel));
        }

        void SelectElements()
        {
            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, FirstNodeModel, SecondNodeModel, PlacematModel, StickyNoteModel));
        }

        protected IEnumerator SelectElement(Vector2 selectedElementPos)
        {
            Helpers.MouseDownEvent(selectedElementPos, MouseButton.LeftMouse, TestEventHelpers.multiSelectModifier);
            yield return null;
            Helpers.MouseUpEvent(selectedElementPos, MouseButton.LeftMouse, TestEventHelpers.multiSelectModifier);
            yield return null;
        }
    }
}
