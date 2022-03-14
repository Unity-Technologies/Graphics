using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Models
{
    public static class GraphChecks
    {
        static string NiceGuid(SerializableGUID guid)
        {
            var(a, b) = guid.ToParts();
            return $"GUID {{m_Value0: {a} m_Value1: {b}}}";
        }

        static string UnexpectedValueMessage(string fieldName, SerializableGUID guid)
        {
            return UnexpectedValueMessage(fieldName, NiceGuid(guid));
        }

        static string UnexpectedValueMessage(string fieldName, string id)
        {
            return $"{fieldName} is not as expected for element {id}";
        }

        public static void AssertIsGraphAsExpected(IGraphModel expected, IGraphModel actual)
        {
            AssertArePlacematsAsExpected(expected.PlacematModels, actual.PlacematModels);
            AssertAreStickyNotesAsExpected(expected.StickyNoteModels, actual.StickyNoteModels);

            Assert.AreEqual(expected.NodeModels.Count, actual.NodeModels.Count);
            AssertAreNodesAsExpected(expected.NodeModels.OfType<CompatibilityTestNodeModel>().ToList(),
                actual.NodeModels.OfType<CompatibilityTestNodeModel>().ToList());

            AssertAreConstantNodesAsExpected(expected.NodeModels.OfType<IConstantNodeModel>().ToList(),
                actual.NodeModels.OfType<IConstantNodeModel>().ToList());

            AssertAreVariableDeclarationsAsExpected(expected.VariableDeclarations, actual.VariableDeclarations);

            AssertAreVariableNodesAsExpected(expected.NodeModels.OfType<IVariableNodeModel>().ToList(),
                actual.NodeModels.OfType<IVariableNodeModel>().ToList());

            AssertAreEdgesAsExpected(expected.EdgeModels, actual.EdgeModels);

            AssertArePortalDeclarationsAsExpected(expected.PortalDeclarations, actual.PortalDeclarations);
            AssertArePortalsAsExpected(expected.NodeModels.OfType<IEdgePortalModel>().ToList(),
                actual.NodeModels.OfType<IEdgePortalModel>().ToList());
        }

        public static void AssertIsGraphElementAsExpected(IGraphElementModel expected, IGraphElementModel actual, string elementId)
        {
            Assert.AreSame(expected.GetType(), actual.GetType(),
                UnexpectedValueMessage("Type", elementId));

            Assert.AreEqual(expected.Capabilities, actual.Capabilities,
                UnexpectedValueMessage(nameof(expected.Capabilities), elementId));
            Assert.AreEqual(expected.Color, actual.Color,
                UnexpectedValueMessage(nameof(expected.Color), elementId));
            Assert.AreEqual(expected.HasUserColor, actual.HasUserColor,
                UnexpectedValueMessage(nameof(expected.HasUserColor), elementId));

            if (expected is IHasTitle expectedTitle)
            {
                var actualTitle = actual as IHasTitle;
                Assert.IsNotNull(actualTitle, elementId);
                Assert.AreEqual(expectedTitle.Title, actualTitle.Title,
                    UnexpectedValueMessage(nameof(expectedTitle.Title), elementId));
            }

            if (expected is IHasProgress expectedProgress)
            {
                var actualProgress = actual as IHasProgress;
                Assert.IsNotNull(actualProgress, elementId);
                Assert.AreEqual(expectedProgress.HasProgress, actualProgress.HasProgress,
                    UnexpectedValueMessage(nameof(expectedProgress.HasProgress), elementId));
            }

            if (expected is ICollapsible expectedCollapsible)
            {
                var actualCollapsible = actual as ICollapsible;
                Assert.IsNotNull(actualCollapsible, elementId);
                Assert.AreEqual(expectedCollapsible.Collapsed, actualCollapsible.Collapsed,
                    UnexpectedValueMessage(nameof(expectedCollapsible.Collapsed), elementId));
            }

            if (expected is IResizable expectedResizable)
            {
                var actualResizable = actual as IResizable;
                Assert.IsNotNull(actualResizable, elementId);
                Assert.AreEqual(expectedResizable.PositionAndSize, actualResizable.PositionAndSize,
                    UnexpectedValueMessage(nameof(expectedResizable.PositionAndSize), elementId));
            }

            if (expected is IMovable expectedMovable)
            {
                var actualMovable = actual as IMovable;
                Assert.IsNotNull(actualMovable, elementId);
                Assert.AreEqual(expectedMovable.Position, actualMovable.Position,
                    UnexpectedValueMessage(nameof(expectedMovable.Position), elementId));
            }

            if (expected is IHasDeclarationModel expectedDeclarationModel)
            {
                var actualDeclarationModel = actual as IHasDeclarationModel;
                Assert.IsNotNull(actualDeclarationModel, elementId);
                Assert.AreEqual(expectedDeclarationModel.DeclarationModel.Guid, actualDeclarationModel.DeclarationModel.Guid,
                    UnexpectedValueMessage(nameof(expectedDeclarationModel.DeclarationModel), elementId));
            }
        }

        public static void AssertArePlacematsAsExpected(IReadOnlyList<IPlacematModel> expected, IReadOnlyList<IPlacematModel> actual)
        {
            Assert.AreEqual(expected.Count, actual.Count);

            foreach (var e in expected)
            {
                var placemat = actual.FirstOrDefault(p => p.Guid == e.Guid);
                Assert.IsNotNull(placemat, NiceGuid(e.Guid));
                AssertIsGraphElementAsExpected(e, placemat, NiceGuid(e.Guid));

                Assert.AreEqual(e.HiddenElements, placemat.HiddenElements,
                    UnexpectedValueMessage(nameof(e.HiddenElements), e.Guid));
            }
        }

        public static void AssertAreStickyNotesAsExpected(IReadOnlyList<IStickyNoteModel> expected, IReadOnlyList<IStickyNoteModel> actual)
        {
            Assert.AreEqual(expected.Count, actual.Count);

            foreach (var e in expected)
            {
                var stickyNote = actual.FirstOrDefault(p => p.Guid == e.Guid);
                Assert.IsNotNull(stickyNote, NiceGuid(e.Guid));
                AssertIsGraphElementAsExpected(e, stickyNote, NiceGuid(e.Guid));

                Assert.AreEqual(e.Contents, stickyNote.Contents, UnexpectedValueMessage(nameof(e.Contents), e.Guid));
                Assert.AreEqual(e.Theme, stickyNote.Theme, UnexpectedValueMessage(nameof(e.Theme), e.Guid));
                Assert.AreEqual(e.TextSize, stickyNote.TextSize, UnexpectedValueMessage(nameof(e.TextSize), e.Guid));
            }
        }

        public static void AssertAreNodesAsExpected(IReadOnlyList<INodeModel> expected, IReadOnlyList<INodeModel> actual)
        {
            Assert.AreEqual(expected.Count, actual.Count);

            foreach (var e in expected)
            {
                var node = actual.FirstOrDefault(p => p.Guid == e.Guid);
                Assert.IsNotNull(node, NiceGuid(e.Guid));
                AssertIsGraphElementAsExpected(e, node, NiceGuid(e.Guid));

                Assert.AreEqual(e.State, node.State);

                if (e is IPortNodeModel expectedPortNodeModel)
                {
                    var actualPortNodeModel = node as IPortNodeModel;
                    Assert.IsNotNull(actualPortNodeModel, NiceGuid(e.Guid));
                    AssertArePortsAsExpected(expectedPortNodeModel.Ports.ToList(), actualPortNodeModel.Ports.ToList());
                }
            }
        }

        public static void AssertAreConstantNodesAsExpected(IReadOnlyList<IConstantNodeModel> expected,
            IReadOnlyList<IConstantNodeModel> actual)
        {
            AssertAreNodesAsExpected(expected, actual);

            foreach (var e in expected)
            {
                var node = actual.FirstOrDefault(p => p.Guid == e.Guid);
                Assert.IsNotNull(node, NiceGuid(e.Guid));

                Assert.AreEqual(e.Type, node.Type, UnexpectedValueMessage(nameof(e.Type), e.Guid));
                Assert.AreEqual(e.IsLocked, node.IsLocked, UnexpectedValueMessage(nameof(e.IsLocked), e.Guid));

                AssertAreConstantAsExpected(e.Value, node.Value, NiceGuid(e.Guid));
            }
        }

        public static void AssertAreVariableDeclarationsAsExpected(IReadOnlyList<IVariableDeclarationModel> expected,
            IReadOnlyList<IVariableDeclarationModel> actual)
        {
            foreach (var e in expected)
            {
                var vdm = actual.FirstOrDefault(p => p.Guid == e.Guid);
                Assert.IsNotNull(vdm, NiceGuid(e.Guid));

                Assert.AreEqual(e.DataType, vdm.DataType, UnexpectedValueMessage(nameof(e.DataType), e.Guid));
                Assert.AreEqual(e.Modifiers, vdm.Modifiers, UnexpectedValueMessage(nameof(e.Modifiers), e.Guid));
                Assert.AreEqual(e.IsExposed, vdm.IsExposed, UnexpectedValueMessage(nameof(e.IsExposed), e.Guid));

                AssertAreConstantAsExpected(e.InitializationModel, vdm.InitializationModel, NiceGuid(e.Guid));
            }
        }

        public static void AssertAreVariableNodesAsExpected(IReadOnlyList<IVariableNodeModel> expected,
            IReadOnlyList<IVariableNodeModel> actual)
        {
            AssertAreNodesAsExpected(expected, actual);

            foreach (var e in expected)
            {
                var node = actual.FirstOrDefault(p => p.Guid == e.Guid);
                Assert.IsNotNull(node, NiceGuid(e.Guid));

                Assert.AreEqual(e.VariableDeclarationModel.Guid, node.VariableDeclarationModel.Guid,
                    UnexpectedValueMessage(nameof(e.VariableDeclarationModel), e.Guid));
            }
        }

        public static void AssertArePortsAsExpected(IReadOnlyList<IPortModel> expected, IReadOnlyList<IPortModel> actual)
        {
            Assert.AreEqual(expected.Count, actual.Count);

            foreach (var e in expected)
            {
                var port = actual.FirstOrDefault(p => p.UniqueName == e.UniqueName);
                Assert.IsNotNull(port, e.UniqueName);
                AssertIsGraphElementAsExpected(e, port, e.UniqueName);

                Assert.AreEqual(e.NodeModel.Guid, port.NodeModel.Guid,
                    UnexpectedValueMessage(nameof(e.NodeModel), e.UniqueName));
                Assert.AreEqual(e.PortType, port.PortType,
                    UnexpectedValueMessage(nameof(e.PortType), e.UniqueName));
                Assert.AreEqual(e.Orientation, port.Orientation,
                    UnexpectedValueMessage(nameof(e.Orientation), e.UniqueName));
                Assert.AreEqual(e.Capacity, port.Capacity,
                    UnexpectedValueMessage(nameof(e.Capacity), e.UniqueName));
                Assert.AreEqual(e.PortDataType, port.PortDataType,
                    UnexpectedValueMessage(nameof(e.PortDataType), e.UniqueName));
                Assert.AreEqual(e.Options, port.Options,
                    UnexpectedValueMessage(nameof(e.Options), e.UniqueName));
                Assert.AreEqual(e.DataTypeHandle, port.DataTypeHandle,
                    UnexpectedValueMessage(nameof(e.DataTypeHandle), e.UniqueName));
                Assert.AreEqual(e.UniqueName, port.UniqueName,
                    UnexpectedValueMessage(nameof(e.UniqueName), e.UniqueName));

                AssertAreConstantAsExpected(e.EmbeddedValue, port.EmbeddedValue, e.UniqueName);
            }
        }

        public static void AssertAreConstantAsExpected(IConstant expected, IConstant actual, string ownerId)
        {
            if (expected == null && actual == null)
                return;

            Assert.IsNotNull(expected, ownerId);
            Assert.IsNotNull(actual, ownerId);

            Assert.AreEqual(expected.Type, actual.Type, UnexpectedValueMessage(nameof(expected.Type), ownerId));

            if (expected.Type == typeof(string) && expected.ObjectValue == null)
            {
                // Null string are deserialized as "".
                Assert.IsTrue(string.IsNullOrEmpty(actual.ObjectValue as string),
                    UnexpectedValueMessage(nameof(expected.ObjectValue), ownerId));
            }
            else
            {
                Assert.AreEqual(expected.ObjectValue, actual.ObjectValue,
                    UnexpectedValueMessage(nameof(expected.ObjectValue), ownerId));
            }
        }

        public static void AssertAreEdgesAsExpected(IReadOnlyList<IEdgeModel> expected, IReadOnlyList<IEdgeModel> actual)
        {
            Assert.AreEqual(expected.Count, actual.Count);

            foreach (var e in expected)
            {
                var edge = actual.FirstOrDefault(a => a.Guid == e.Guid);
                Assert.IsNotNull(edge, NiceGuid(e.Guid));
                AssertIsGraphElementAsExpected(e, edge, NiceGuid(e.Guid));

                Assert.AreEqual(e.FromPortId, edge.FromPortId, UnexpectedValueMessage(nameof(e.FromPortId), e.Guid));
                Assert.AreEqual(e.ToPortId, edge.ToPortId, UnexpectedValueMessage(nameof(e.ToPortId), e.Guid));
            }
        }

        public static void AssertArePortalDeclarationsAsExpected(IReadOnlyList<IDeclarationModel> expected,
            IReadOnlyList<IDeclarationModel> actual)
        {
            foreach (var e in expected)
            {
                var declarationModel = actual.FirstOrDefault(p => p.Guid == e.Guid);
                Assert.IsNotNull(declarationModel, NiceGuid(e.Guid));
            }
        }

        public static void AssertArePortalsAsExpected(IReadOnlyList<IEdgePortalModel> expected,
            IReadOnlyList<IEdgePortalModel> actual)
        {
            AssertAreNodesAsExpected(expected, actual);

            foreach (var e in expected)
            {
                var node = actual.FirstOrDefault(p => p.Guid == e.Guid);
                Assert.IsNotNull(node, NiceGuid(e.Guid));

                Assert.AreEqual(e.EvaluationOrder, node.EvaluationOrder,
                    UnexpectedValueMessage(nameof(e.EvaluationOrder), e.Guid));
            }
        }
    }
}
